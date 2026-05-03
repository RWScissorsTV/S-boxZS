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
	[Property] public float Force { get; set; } = 450f;
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
	private bool _hasLocalDefaultControllerShape;
	private float _localDefaultBodyHeight = 72f;
	private float _localDefaultBodyRadius = 16f;
	private float _localDefaultDuckedHeight = 36f;
	private float _localDefaultEyeDistanceFromTop = 8f;
	private Vector3 _localDefaultCameraOffset = new( 96f, 0f, -4f );
	private float _localDefaultReachLength = 130f;

	protected override void OnStart()
	{
		ResetAmbientDelay();
	}

	protected override void OnUpdate()
	{
		ApplyLocalHeadcrabControllerShape();
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
		RestoreLocalControllerShape();
		DestroyFirstPersonViewModel();
	}

	protected override void OnDestroy()
	{
		RestoreLocalControllerShape();
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

	private void ApplyLocalHeadcrabControllerShape()
	{
		var player = GetComponent<Player>();
		if ( !player.IsValid() || !player.IsLocalPlayer || !player.PlayerData.IsValid() || !player.Controller.IsValid() )
			return;

		var controller = player.Controller;
		if ( !_hasLocalDefaultControllerShape )
		{
			_localDefaultBodyHeight = controller.BodyHeight;
			_localDefaultBodyRadius = controller.BodyRadius;
			_localDefaultDuckedHeight = controller.DuckedHeight;
			_localDefaultEyeDistanceFromTop = controller.EyeDistanceFromTop;
			_localDefaultCameraOffset = controller.CameraOffset;
			_localDefaultReachLength = controller.ReachLength;
			_hasLocalDefaultControllerShape = true;
		}

		var isHeadcrab =
			player.PlayerData.ZombieSurvivalRole == ZombieSurvivalRole.Zombie
			&& player.PlayerData.ZombieSurvivalForm == ZombieSurvivalForm.Headcrab;

		if ( isHeadcrab )
		{
			var definition = ZombieSurvivalFormCatalog.Get( ZombieSurvivalForm.Headcrab );
			controller.BodyHeight = definition.BodyHeight;
			controller.BodyRadius = definition.BodyRadius;
			controller.DuckedHeight = definition.DuckedHeight;
			controller.EyeDistanceFromTop = definition.EyeDistanceFromTop;
			controller.CameraOffset = definition.CameraOffset;
			controller.ReachLength = definition.ReachLength;
			return;
		}

		RestoreLocalControllerShape();
	}

	private void RestoreLocalControllerShape()
	{
		var player = GetComponent<Player>();
		if ( !player.IsValid() || !player.IsLocalPlayer || !player.Controller.IsValid() || !_hasLocalDefaultControllerShape )
			return;

		var controller = player.Controller;
		controller.BodyHeight = _localDefaultBodyHeight;
		controller.BodyRadius = _localDefaultBodyRadius;
		controller.DuckedHeight = _localDefaultDuckedHeight;
		controller.EyeDistanceFromTop = _localDefaultEyeDistanceFromTop;
		controller.CameraOffset = _localDefaultCameraOffset;
		controller.ReachLength = _localDefaultReachLength;
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

		if ( trace.GameObject.GetComponentInChildren<Rigidbody>() is { } rb && rb.IsValid() )
		{
			rb.ApplyImpulse( ray.Forward * Force * rb.Mass );
		}
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
