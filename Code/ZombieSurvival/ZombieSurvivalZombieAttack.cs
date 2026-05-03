using System;
using System.Collections.Generic;
using Sandbox.CameraNoise;

public sealed class ZombieSurvivalZombieAttack : Component
{
	private const string HumanFirstPersonArmsModelPath = "models/first_person/v_first_person_arms_human.vmdl";
	private const string FallbackFirstPersonArmsModelPath = "Model/First Person/first_person_arms_preview.vmdl";

	[ConVar( "zs.zombie_ambient_enabled", ConVarFlags.Replicated | ConVarFlags.Server | ConVarFlags.GameSetting )]
	public static bool AmbientEnabled { get; set; } = false;

	[Property] public float Damage { get; set; } = 25f;
	[Property] public float Range { get; set; } = 90f;
	[Property] public float Radius { get; set; } = 14f;
	[Property] public float Cooldown { get; set; } = 1f;
	[Property] public float Force { get; set; } = 200f;
	[Property] public SoundEvent[] AttackSounds { get; set; } = Array.Empty<SoundEvent>();
	[Property] public SoundEvent[] HitSounds { get; set; } = Array.Empty<SoundEvent>();
	[Property] public SoundEvent[] AmbientSounds { get; set; } = Array.Empty<SoundEvent>();
	[Property] public float AmbientMinDelay { get; set; } = 8f;
	[Property] public float AmbientMaxDelay { get; set; } = 18f;

	private TimeUntil _timeUntilNextAttack;
	private TimeUntil _timeUntilAmbientSound;
	private GameObject _firstPersonViewModel;
	private ViewModel _firstPersonViewModelController;
	private ZombieSurvivalFirstPersonPunchViewModel _firstPersonPunchViewModel;

	protected override void OnStart()
	{
		ResetAmbientDelay();
	}

	protected override void OnUpdate()
	{
		UpdateFirstPersonViewModel();

		if ( IsProxy )
			return;

		if ( !IsControlledZombie() )
			return;

		if ( Input.Down( "attack1" ) )
			RequestAttack();

		if ( AmbientEnabled && _timeUntilAmbientSound <= 0f )
		{
			RpcAmbientEffects();
			ResetAmbientDelay();
		}
	}

	protected override void OnDisabled()
	{
		DestroyFirstPersonViewModel();
	}

	protected override void OnDestroy()
	{
		DestroyFirstPersonViewModel();
	}

	public void ConfigureForForm( ZombieSurvivalForm form )
	{
		var definition = ZombieSurvivalFormCatalog.Get( form );
		Damage = definition.MeleeDamage;
		Range = definition.MeleeRange;
		Radius = definition.MeleeRadius;
		Cooldown = definition.MeleeCooldown;
		AttackSounds = LoadSounds( definition.AttackSoundPaths );
		AmbientSounds = LoadSounds( definition.AmbientSoundPaths );
	}

	private bool IsControlledZombie()
	{
		var player = GetComponent<Player>();
		if ( !player.IsValid() || !player.PlayerData.IsValid() )
			return false;

		if ( player.PlayerData.ZombieSurvivalRole != ZombieSurvivalRole.Zombie )
			return false;

		if ( ZombieSurvivalGame.Current?.Phase != ZombieSurvivalPhase.Survival )
			return false;

		return true;
	}

	private void RequestAttack()
	{
		if ( Networking.IsHost )
		{
			AttackHost();
			return;
		}

		RpcRequestAttack();
	}

	[Rpc.Host]
	private void RpcRequestAttack()
	{
		AttackHost();
	}

	private void AttackHost()
	{
		if ( !Networking.IsHost )
			return;

		if ( _timeUntilNextAttack > 0f )
			return;

		var player = GetComponent<Player>();
		if ( !player.IsValid() || !player.PlayerData.IsValid() )
			return;

		if ( player.PlayerData.ZombieSurvivalRole != ZombieSurvivalRole.Zombie )
			return;

		if ( ZombieSurvivalGame.Current?.Phase != ZombieSurvivalPhase.Survival )
			return;

		_timeUntilNextAttack = MathF.Max( Cooldown, 0.05f );

		var ray = player.EyeTransform.ForwardRay;
		var trace = TraceAttackTarget( ray, useHitboxes: true );
		if ( !trace.Hit )
			trace = TraceAttackTarget( ray, useHitboxes: false );

		RpcAttackEffects( trace.Hit, trace.EndPosition );

		if ( !trace.Hit || !trace.GameObject.IsValid() )
			return;

		var damageable = ResolveDamageable( trace.GameObject );
		if ( damageable is not null )
		{
			var damageInfo = new DamageInfo( Damage, GameObject, GameObject )
			{
				Position = trace.HitPosition,
				Origin = ray.Position
			};
			damageInfo.Tags.Add( "zombie" );
			damageable.OnDamage( damageInfo );
		}

		ApplyKnockback( trace.GameObject, player, ray );
	}

	private SceneTraceResult TraceAttackTarget( Ray ray, bool useHitboxes )
	{
		var trace = Scene.Trace.Ray( ray, Range )
			.IgnoreGameObjectHierarchy( GameObject )
			.WithoutTags( "playercontroller" )
			.Radius( Radius );

		if ( useHitboxes )
			trace = trace.UseHitboxes();

		return trace.Run();
	}

	private static Component.IDamageable ResolveDamageable( GameObject target )
	{
		if ( !target.IsValid() )
			return null;

		var barricade = target.GetComponentInParent<ZombieSurvivalBarricade>( true );
		if ( barricade.IsValid() )
			return barricade;

		return target.GetComponentInParent<Component.IDamageable>( true );
	}

	private void ApplyKnockback( GameObject target, Player attacker, Ray ray )
	{
		var rb = ResolveKnockbackBody( target, out var targetPlayer );
		if ( !rb.IsValid() )
			return;

		var direction = GetKnockbackDirection( target, attacker, targetPlayer, ray );
		var velocityDelta = direction * Force;

		if ( targetPlayer.IsValid() )
		{
			targetPlayer.ApplyZombieSurvivalKnockbackHost( velocityDelta, 0.15f );
			return;
		}

		rb.ApplyImpulse( velocityDelta * rb.Mass );
	}

	private static Rigidbody ResolveKnockbackBody( GameObject target, out Player targetPlayer )
	{
		targetPlayer = null;

		if ( !target.IsValid() )
			return null;

		targetPlayer = target.GetComponentInParent<Player>( true );
		if ( targetPlayer.IsValid() && targetPlayer.Controller.IsValid() && targetPlayer.Controller.Body.IsValid() )
			return targetPlayer.Controller.Body;

		var rb = target.Components.Get<Rigidbody>( FindMode.EverythingInSelfAndParent );
		if ( rb.IsValid() )
			return rb;

		return target.GetComponentInChildren<Rigidbody>( true );
	}

	private static Vector3 GetKnockbackDirection( GameObject target, Player attacker, Player targetPlayer, Ray ray )
	{
		var direction = Vector3.Zero;

		if ( attacker.IsValid() && targetPlayer.IsValid() && targetPlayer != attacker )
			direction = (targetPlayer.WorldPosition - attacker.WorldPosition).WithZ( 0f );

		if ( direction.Length < 0.001f && attacker.IsValid() && target.IsValid() )
			direction = (target.WorldPosition - attacker.WorldPosition).WithZ( 0f );

		if ( direction.Length < 0.001f )
			direction = ray.Forward.WithZ( 0f );

		if ( direction.Length < 0.001f )
			direction = ray.Forward;

		return (direction.Normal + Vector3.Up * 0.15f).Normal;
	}

	[Rpc.Broadcast]
	private void RpcAttackEffects( bool hit, Vector3 hitPosition )
	{
		var player = GetComponent<Player>();
		if ( player.IsValid() )
		{
			player.Controller?.Renderer?.Set( "b_attack", true );
			player.GetComponent<ZombieSurvivalZombieFormPresenter>()?.TriggerAttack();
		}

		if ( player.IsValid() && player.IsLocalPlayer )
		{
			TriggerFirstPersonAttackViewModel();
			_ = new Punch( new Vector3( Random.Shared.Float( -8f, -12f ), Random.Shared.Float( -6f, 6f ), 0f ), 0.9f, 2.2f, 0.35f );
			_ = new Shake( hit ? 0.22f : 0.12f, hit ? 1.0f : 0.55f );
		}

		PlayRandomSound( AttackSounds, WorldPosition );

		if ( hit )
			PlayRandomSound( HitSounds, hitPosition );
	}

	[Rpc.Broadcast]
	private void RpcAmbientEffects()
	{
		PlayRandomSound( AmbientSounds, WorldPosition );
	}

	private static void PlayRandomSound( SoundEvent[] sounds, Vector3 position )
	{
		if ( sounds is null || sounds.Length == 0 )
			return;

		var sound = Game.Random.FromArray( sounds );
		if ( sound.IsValid() )
			Sound.Play( sound, position );
	}

	private void ResetAmbientDelay()
	{
		_timeUntilAmbientSound = Game.Random.Float( AmbientMinDelay, MathF.Max( AmbientMaxDelay, AmbientMinDelay ) );
	}

	private static SoundEvent[] LoadSounds( string[] paths )
	{
		if ( paths is null || paths.Length == 0 )
			return Array.Empty<SoundEvent>();

		var sounds = new List<SoundEvent>();
		foreach ( var path in paths )
		{
			var sound = ResourceLibrary.Get<SoundEvent>( path );
			if ( sound.IsValid() )
				sounds.Add( sound );
		}

		return sounds.ToArray();
	}

	private void UpdateFirstPersonViewModel()
	{
		var player = GetComponent<Player>();

		if ( ShouldShowFirstPersonViewModel( player ) )
		{
			EnsureFirstPersonViewModel();
			return;
		}

		DestroyFirstPersonViewModel();
	}

	private static bool ShouldShowFirstPersonViewModel( Player player )
	{
		return player.IsValid()
			&& player.IsLocalPlayer
			&& player.PlayerData.IsValid()
			&& player.PlayerData.ZombieSurvivalRole == ZombieSurvivalRole.Zombie
			&& player.Controller.IsValid()
			&& !player.Controller.ThirdPerson;
	}

	private void EnsureFirstPersonViewModel()
	{
		if ( _firstPersonViewModel.IsValid() )
			return;

		var model = LoadFirstPersonPunchModel();
		if ( model is null || model.IsError )
			return;

		_firstPersonViewModel = new GameObject( true, "ZS Punch Viewmodel" );
		_firstPersonViewModel.Flags |= GameObjectFlags.NotSaved | GameObjectFlags.NotNetworked | GameObjectFlags.Absolute;
		_firstPersonViewModel.SetParent( GameObject, false );
		_firstPersonViewModel.Enabled = true;
		_firstPersonViewModel.Tags.Add( "firstperson", "viewmodel" );

		_firstPersonViewModelController = _firstPersonViewModel.AddComponent<ViewModel>();

		var visualRoot = new GameObject( true, "Punch Arms" );
		visualRoot.SetParent( _firstPersonViewModel, false );
		visualRoot.Enabled = true;

		var renderer = visualRoot.AddComponent<SkinnedModelRenderer>();
		renderer.Model = model;
		renderer.Enabled = true;
		renderer.UseAnimGraph = true;
		renderer.CreateBoneObjects = true;

		_firstPersonViewModelController.Renderer = renderer;
		_firstPersonPunchViewModel = _firstPersonViewModel.AddComponent<ZombieSurvivalFirstPersonPunchViewModel>();
		_firstPersonPunchViewModel.VisualRoot = visualRoot;
		_firstPersonPunchViewModel.Renderer = renderer;

		_firstPersonViewModelController.Deploy();
	}

	private void TriggerFirstPersonAttackViewModel()
	{
		if ( !_firstPersonViewModelController.IsValid() )
			return;

		_firstPersonViewModelController.OnAttack();
		_firstPersonPunchViewModel?.TriggerPunch();
	}

	private void DestroyFirstPersonViewModel()
	{
		if ( _firstPersonViewModel.IsValid() )
			_firstPersonViewModel.Destroy();

		_firstPersonViewModel = null;
		_firstPersonViewModelController = null;
		_firstPersonPunchViewModel = null;
	}

	private static Model LoadFirstPersonPunchModel()
	{
		var model = Model.Load( HumanFirstPersonArmsModelPath );
		if ( model is not null && !model.IsError )
			return model;

		model = Model.Load( FallbackFirstPersonArmsModelPath );
		if ( model is not null && !model.IsError )
			return model;

		Log.Warning( $"Zombie Survival: failed to load first-person punch model '{HumanFirstPersonArmsModelPath}' or fallback '{FallbackFirstPersonArmsModelPath}'." );
		return null;
	}
}
