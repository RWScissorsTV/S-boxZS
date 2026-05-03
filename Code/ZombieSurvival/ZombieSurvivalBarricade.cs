using System;

public sealed class ZombieSurvivalBarricade : Component, Component.IDamageable, Component.IPressable
{
	public const string RoundPropTag = "zs_round_prop";
	public const string BarricadeTag = "zs_barricade";

	[Property] public float StartingHealth { get; set; } = 150f;
	[Property] public float MaxFortifiedHealth { get; set; } = 225f;
	[Property] public float RepairAmount { get; set; } = 25f;
	[Property] public float FortifyAmount { get; set; } = 15f;
	[Property] public float RepairCooldown { get; set; } = 0.35f;

	[Property, Sync( SyncFlags.FromHost )] public float Health { get; private set; }
	[Property, Sync( SyncFlags.FromHost )] public float MaxHealth { get; private set; }
	[Property, Sync( SyncFlags.FromHost )] public bool IsBroken { get; private set; }

	[Property] public SoundEvent ImpactSound { get; set; }
	[Property] public SoundEvent BreakSound { get; set; }
	[Property] public SoundEvent RepairSound { get; set; }
	[Property] public SoundEvent FortifySound { get; set; }

	private TimeUntil _repairCooldown;

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
		MaxFortifiedHealth = MathF.Max( MaxFortifiedHealth, MaxHealth );
		Health = MaxHealth;
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
		if ( !CanHumanInteract( e.Source.GameObject ) )
			return new IPressable.Tooltip( "Can't repair", "block", "Humans only" );

		var healthText = $"{MathF.Ceiling( Health )}/{MathF.Ceiling( MaxHealth )}";

		if ( Health < MaxHealth )
			return new IPressable.Tooltip( "Repair", "construction", healthText );

		if ( MaxHealth < MaxFortifiedHealth )
			return new IPressable.Tooltip( "Fortify", "construction", healthText );

		return new IPressable.Tooltip( "Fortified", "check", healthText );
	}

	bool IPressable.CanPress( IPressable.Event e )
	{
		if ( IsBroken )
			return false;

		if ( !CanHumanInteract( e.Source.GameObject ) )
			return false;

		return Health < MaxHealth || MaxHealth < MaxFortifiedHealth;
	}

	bool IPressable.Press( IPressable.Event e )
	{
		RepairOrFortify( e.Source.GameObject );
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

		GameObject.Tags.Remove( BarricadeTag );
		GameObject.Tags.Remove( "barricade" );

		GameObject.Destroy();
	}

	[Rpc.Host]
	private void RepairOrFortify( GameObject presserObject )
	{
		if ( !Networking.IsHost )
			return;

		if ( _repairCooldown > 0f )
			return;

		if ( IsBroken || !CanHumanInteract( presserObject ) )
			return;

		var fortified = false;
		if ( Health < MaxHealth )
		{
			Health = MathF.Min( Health + MathF.Max( RepairAmount, 0f ), MaxHealth );
		}
		else if ( MaxHealth < MaxFortifiedHealth )
		{
			var amount = MathF.Max( FortifyAmount, 0f );
			MaxHealth = MathF.Min( MaxHealth + amount, MaxFortifiedHealth );
			Health = MathF.Min( Health + amount, MaxHealth );
			fortified = true;
		}
		else
		{
			return;
		}

		_repairCooldown = MathF.Max( RepairCooldown, 0.05f );
		PlayRepairEffects( fortified );
	}

	private bool CanHumanInteract( GameObject source )
	{
		if ( !source.IsValid() )
			return false;

		var game = ZombieSurvivalGame.Current;
		if ( game is null )
			return false;

		if ( game.Phase != ZombieSurvivalPhase.Build && game.Phase != ZombieSurvivalPhase.Survival )
			return false;

		var player = source.Root.GetComponent<Player>();
		if ( !player.IsValid() || !player.PlayerData.IsValid() )
			return false;

		return player.PlayerData.ZombieSurvivalAlive
			&& player.PlayerData.ZombieSurvivalRole == ZombieSurvivalRole.Human;
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
