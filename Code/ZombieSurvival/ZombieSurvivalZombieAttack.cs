using System;
using System.Collections.Generic;

public sealed class ZombieSurvivalZombieAttack : Component
{
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

	protected override void OnStart()
	{
		ResetAmbientDelay();
	}

	protected override void OnUpdate()
	{
		if ( IsProxy )
			return;

		if ( !IsControlledZombie() )
			return;

		if ( Input.Pressed( "attack1" ) )
			RequestAttack();

		if ( _timeUntilAmbientSound <= 0f )
		{
			RpcAmbientEffects();
			ResetAmbientDelay();
		}
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
		var trace = Scene.Trace.Ray( ray, Range )
			.IgnoreGameObjectHierarchy( GameObject )
			.WithoutTags( "playercontroller" )
			.Radius( Radius )
			.UseHitboxes()
			.Run();

		RpcAttackEffects( trace.Hit, trace.EndPosition );

		if ( !trace.Hit || !trace.GameObject.IsValid() )
			return;

		var damageable = trace.GameObject.GetComponentInParent<Component.IDamageable>( true );
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

	[Rpc.Broadcast]
	private void RpcAttackEffects( bool hit, Vector3 hitPosition )
	{
		var player = GetComponent<Player>();
		if ( player.IsValid() )
		{
			player.Controller?.Renderer?.Set( "b_attack", true );
			player.GetComponent<ZombieSurvivalZombieFormPresenter>()?.TriggerAttack();
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
}
