using System;
using System.Collections.Generic;

public sealed class ZombieSurvivalBarricade : Component, Component.IDamageable, Component.IPressable
{
	public const string RoundPropTag = "zs_round_prop";
	public const string BarricadeTag = "zs_barricade";
	public const string NailTag = "zs_nail";

	[Property] public float StartingHealth { get; set; } = 150f;
	[Property] public float MaxFortifiedHealth { get; set; } = 225f;
	[Property] public float RepairAmount { get; set; } = 25f;
	[Property] public float FortifyAmount { get; set; } = 15f;
	[Property] public float RepairCooldown { get; set; } = 0.35f;
	[Property] public float NailHealth { get; set; } = 25f;
	[Property] public int MaxNails { get; set; } = 4;

	[Property, Sync( SyncFlags.FromHost )] public float Health { get; private set; }
	[Property, Sync( SyncFlags.FromHost )] public float MaxHealth { get; private set; }
	[Property, Sync( SyncFlags.FromHost )] public bool IsBroken { get; private set; }
	[Property, Sync( SyncFlags.FromHost )] public int NailCount { get; private set; }

	[Property] public SoundEvent ImpactSound { get; set; }
	[Property] public SoundEvent BreakSound { get; set; }
	[Property] public SoundEvent RepairSound { get; set; }
	[Property] public SoundEvent FortifySound { get; set; }

	private TimeUntil _repairCooldown;
	private readonly List<GameObject> _nails = new();

	protected override void OnStart()
	{
		if ( Networking.IsHost )
			InitializeHost();
	}

	public void InitializeHost()
	{
		if ( !Networking.IsHost )
			return;

		MaxHealth = MathF.Max( StartingHealth, 1f );
		MaxFortifiedHealth = MaxHealth;
		Health = MaxHealth;
		NailCount = 0;
		IsBroken = false;
		GameObject.Tags.Add( RoundPropTag );
		GameObject.Tags.Add( BarricadeTag );
		GameObject.Tags.Add( "barricade" );
	}

	void Component.IDamageable.OnDamage( in DamageInfo damage )
	{
		if ( !Networking.IsHost )
			return;

		if ( IsBroken || damage.Damage <= 0f )
			return;

		if ( !IsZombieDamage( damage ) )
			return;

		Health = MathF.Max( Health - damage.Damage, 0f );

		if ( ImpactSound.IsValid() )
			PlayImpactEffects( damage.Position );

		if ( Health <= 0f )
			BreakHost();
	}

	IPressable.Tooltip? IPressable.GetTooltip( IPressable.Event e )
	{
		var healthText = $"{MathF.Ceiling( Health )}/{MathF.Ceiling( MaxHealth )}";
		var player = GetSourcePlayer( e.Source.GameObject );

		if ( player.IsValid() && player.PlayerData.IsValid() && player.PlayerData.ZombieSurvivalRole == ZombieSurvivalRole.Zombie )
			return new IPressable.Tooltip( "Break", "construction", healthText );

		if ( !CanHumanInteract( e.Source.GameObject ) )
			return new IPressable.Tooltip( "Barricade", "construction", healthText );

		if ( NailCount > 0 )
			return new IPressable.Tooltip( "Nailed", "construction", $"Nails {NailCount}/{MaxNails} - {healthText}" );

		if ( ZombieSurvivalPropCarrySystem.IsLocalCarrying( GameObject ) )
			return new IPressable.Tooltip( "Place", "open_with", $"Nails {NailCount}/{MaxNails}" );

		return new IPressable.Tooltip( "Pick up", "open_with", $"Nails {NailCount}/{MaxNails} - {healthText}" );
	}

	bool IPressable.CanPress( IPressable.Event e )
	{
		if ( IsBroken )
			return false;

		if ( !CanHumanInteract( e.Source.GameObject ) )
			return false;

		return NailCount <= 0;
	}

	bool IPressable.Press( IPressable.Event e )
	{
		ZombieSurvivalPropCarrySystem.ToggleCarryLocal( this, e.Source.GameObject );
		ToggleCarry( e.Source.GameObject );
		return true;
	}

	private bool IsZombieDamage( in DamageInfo damage )
	{
		if ( damage.Tags.Contains( "zombie" ) )
			return true;

		var attacker = damage.Attacker?.GetComponentInParent<Player>( true );
		return attacker.IsValid()
			&& attacker.PlayerData.IsValid()
			&& attacker.PlayerData.ZombieSurvivalRole == ZombieSurvivalRole.Zombie;
	}

	private void BreakHost()
	{
		if ( IsBroken )
			return;

		IsBroken = true;
		Health = 0f;

		if ( BreakSound.IsValid() )
			PlayBreakEffects( WorldPosition );

		DestroyNails();
		GameObject.Tags.Remove( BarricadeTag );
		GameObject.Tags.Remove( "barricade" );

		GameObject.Destroy();
	}

	[Rpc.Host]
	private void ToggleCarry( GameObject presserObject )
	{
		if ( !Networking.IsHost )
			return;

		if ( IsBroken || !CanHumanInteract( presserObject, Rpc.Caller ) )
			return;

		if ( NailCount > 0 )
			return;

		ZombieSurvivalPropCarrySystem.ToggleCarryHost( Rpc.Caller, GameObject );
	}

	public bool CanHammerRepair( GameObject source, float range )
	{
		if ( IsBroken || !CanHumanInteract( source ) )
			return false;

		if ( NailCount <= 0 || Health >= MaxHealth )
			return false;

		var player = GetSourcePlayer( source );
		return player.IsValid() && GetDistanceFromPlayer( player ) <= MathF.Max( range, 0f );
	}

	[Rpc.Host]
	public void RepairWithHammer( GameObject source, float range )
	{
		if ( !Networking.IsHost )
			return;

		if ( _repairCooldown > 0f || !CanHammerRepair( source, range ) )
			return;

		Health = MathF.Min( Health + MathF.Max( RepairAmount, 0f ), MaxHealth );
		_repairCooldown = MathF.Max( RepairCooldown, 0.05f );
		PlayRepairEffects( false );
	}

	[Rpc.Host]
	public void AddHammerNail( GameObject source, GameObject target, Vector3 propPoint, Vector3 targetPoint )
	{
		if ( !Networking.IsHost )
			return;

		if ( IsBroken || !CanHumanInteract( source ) )
			return;

		if ( NailCount >= MaxNails )
		{
			SendNotice( source, "Max nails reached." );
			return;
		}

		var targetRoot = target.IsValid()
			? target.Network.RootGameObject ?? target
			: null;

		if ( targetRoot == GameObject || targetRoot == GameObject.Root )
			return;

		CreateNailConstraint( targetRoot, propPoint, targetPoint );
		FreezePropHost();

		NailCount++;
		var amount = MathF.Max( NailHealth, 0f );
		MaxHealth = MathF.Max( MaxHealth + amount, StartingHealth );
		Health = MathF.Min( Health + amount, MaxHealth );

		PlayRepairEffects( true );
	}

	private bool CanHumanInteract( GameObject source, Connection fallbackConnection = null )
	{
		var game = ZombieSurvivalGame.Current;
		if ( game is null )
			return false;

		if ( game.Phase != ZombieSurvivalPhase.Build && game.Phase != ZombieSurvivalPhase.Survival )
			return false;

		var player = GetSourcePlayer( source );
		if ( !player.IsValid() && fallbackConnection is not null )
			player = Player.FindForConnection( fallbackConnection );

		if ( !player.IsValid() && !Networking.IsHost )
			player = Player.FindLocalPlayer();

		if ( !player.IsValid() && Networking.IsHost && Connection.Local is not null )
			player = Player.FindForConnection( Connection.Local );

		if ( !player.IsValid() || !player.PlayerData.IsValid() )
			return false;

		return player.PlayerData.ZombieSurvivalAlive
			&& player.PlayerData.ZombieSurvivalRole == ZombieSurvivalRole.Human;
	}

	public bool CanHammerNail( GameObject source )
	{
		return !IsBroken
			&& CanHumanInteract( source )
			&& NailCount < MaxNails;
	}

	private void CreateNailConstraint( GameObject targetRoot, Vector3 propPoint, Vector3 targetPoint )
	{
		var propAnchor = new GameObject( false, "zs_nail" );
		propAnchor.Tags.Add( NailTag );
		propAnchor.Parent = GameObject;
		propAnchor.WorldPosition = propPoint;
		propAnchor.WorldRotation = Rotation.Identity;

		var targetAnchor = new GameObject( false, "zs_nail_anchor" );
		targetAnchor.Tags.Add( NailTag );

		if ( targetRoot.IsValid() )
			targetAnchor.Parent = targetRoot;

		targetAnchor.WorldPosition = targetPoint;
		targetAnchor.WorldRotation = Rotation.Identity;

		var cleanup = propAnchor.AddComponent<ConstraintCleanup>();
		cleanup.Attachment = targetAnchor;

		var joint = propAnchor.AddComponent<FixedJoint>();
		joint.Attachment = Joint.AttachmentMode.Auto;
		joint.Body = targetAnchor;
		joint.EnableCollision = true;
		joint.AngularFrequency = 10;
		joint.LinearFrequency = 10;

		targetAnchor.NetworkSpawn();
		propAnchor.NetworkSpawn();

		_nails.Add( propAnchor );
	}

	private void FreezePropHost()
	{
		if ( !Networking.IsHost )
			return;

		var body = GameObject.GetComponent<Rigidbody>();
		if ( !body.IsValid() )
			return;

		body.MotionEnabled = false;
		body.Velocity = Vector3.Zero;
		body.AngularVelocity = Vector3.Zero;
	}

	private void DestroyNails()
	{
		foreach ( var nail in _nails.ToArray() )
		{
			if ( nail.IsValid() )
				nail.Destroy();
		}

		_nails.Clear();
	}

	private float GetDistanceFromPlayer( Player player )
	{
		if ( !player.IsValid() )
			return float.MaxValue;

		var rb = GameObject.GetComponent<Rigidbody>();
		if ( rb.IsValid() )
			return rb.FindClosestPoint( player.EyeTransform.Position ).Distance( player.EyeTransform.Position );

		return WorldPosition.Distance( player.EyeTransform.Position );
	}

	private static void SendNotice( GameObject source, string text )
	{
		var target = GetSourcePlayer( source )?.Network.Owner;

		if ( target is null )
			return;

		Sandbox.UI.Notices.SendNotice( target, "construction", Color.Red, text, 2 );
	}

	private static Player GetSourcePlayer( GameObject source )
	{
		if ( !source.IsValid() )
			return null;

		return source.GetComponentInParent<Player>( true )
			?? source.Root.GetComponent<Player>();
	}

	[Rpc.Broadcast]
	private void PlayImpactEffects( Vector3 position )
	{
		if ( ImpactSound.IsValid() )
			Sound.Play( ImpactSound, position );
	}

	[Rpc.Broadcast]
	private void PlayBreakEffects( Vector3 position )
	{
		if ( BreakSound.IsValid() )
			Sound.Play( BreakSound, position );
	}

	[Rpc.Broadcast]
	private void PlayRepairEffects( bool fortified )
	{
		var sound = fortified ? FortifySound : RepairSound;
		if ( sound.IsValid() )
			GameObject.PlaySound( sound );
	}
}
