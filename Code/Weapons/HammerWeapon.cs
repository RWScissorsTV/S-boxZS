using System;
using Sandbox.Rendering;

public sealed class HammerWeapon : MeleeWeapon
{
	[Property] public float ZombieDamage { get; set; } = 8f;
	[Property] public float RepairRange { get; set; } = 96f;
	[Property] public float NailSearchDistance { get; set; } = 96f;
	[Property] public float NailDelay { get; set; } = 0.45f;

	private TimeUntil _nextPrimary;
	private TimeUntil _nextNail;

	public override void OnControl( Player player )
	{
		if ( Input.Pressed( "attack2" ) )
		{
			TryNail( player );
			return;
		}

		if ( Input.Down( "attack1" ) )
			TryPrimary( player );
	}

	private void TryPrimary( Player player )
	{
		if ( _nextPrimary > 0f )
			return;

		var repairRange = MathF.Max( ZombieSurvivalGame.BarricadeRepairRange, RepairRange );
		var trace = TraceHammer( repairRange );
		_nextPrimary = trace.Hit ? SwingDelay : MissSwingDelay;

		SwingEffects( trace.EndPosition, trace.Hit, trace.Normal, trace.GameObject, trace.Surface );

		var barricade = ResolveBarricade( trace.GameObject );
		if ( barricade.IsValid() )
		{
			if ( barricade.NailCount > 0 )
				barricade.RepairWithHammer( Owner.GameObject, repairRange );

			return;
		}

		var targetPlayer = trace.GameObject.GetComponentInParent<Player>( true );
		if ( !targetPlayer.IsValid() || !targetPlayer.PlayerData.IsValid() )
			return;

		if ( targetPlayer.PlayerData.ZombieSurvivalRole != ZombieSurvivalRole.Zombie )
			return;

		TraceAttack( TraceAttackInfo.From( trace, ZombieDamage, localise: false ) );
	}

	private void TryNail( Player player )
	{
		if ( _nextNail > 0f )
			return;

		_nextNail = NailDelay;

		var trace = TraceHammer( Range );
		SwingEffects( trace.EndPosition, trace.Hit, trace.Normal, trace.GameObject, trace.Surface );

		var barricade = ResolveBarricade( trace.GameObject );
		if ( !barricade.IsValid() )
			return;

		if ( !TryFindNailTarget( barricade, trace, out var target, out var targetPoint ) )
			return;

		barricade.AddHammerNail( Owner.GameObject, target, trace.HitPosition, targetPoint );
	}

	private bool TryFindNailTarget( ZombieSurvivalBarricade barricade, SceneTraceResult hitTrace, out GameObject target, out Vector3 targetPoint )
	{
		target = null;
		targetPoint = default;

		if ( TryUseNailTargetTrace( barricade, TraceNailTarget( barricade, hitTrace.HitPosition + AimRay.Forward * 4f, AimRay.Forward ), out target, out targetPoint ) )
			return true;

		if ( TryUseNailTargetTrace( barricade, TraceNailTarget( barricade, barricade.WorldPosition + Vector3.Up * 8f, Vector3.Down ), out target, out targetPoint ) )
			return true;

		if ( TryUseNailTargetTrace( barricade, TraceNailTarget( barricade, hitTrace.HitPosition + hitTrace.Normal * 4f, -hitTrace.Normal ), out target, out targetPoint ) )
			return true;

		return false;
	}

	private SceneTraceResult TraceNailTarget( ZombieSurvivalBarricade barricade, Vector3 start, Vector3 direction )
	{
		return Scene.Trace.Ray( start, start + direction.Normal * NailSearchDistance )
			.IgnoreGameObjectHierarchy( barricade.GameObject.Root )
			.IgnoreGameObjectHierarchy( AimIgnoreRoot )
			.WithoutTags( "player" )
			.Run();
	}

	private static bool TryUseNailTargetTrace( ZombieSurvivalBarricade barricade, SceneTraceResult trace, out GameObject target, out Vector3 targetPoint )
	{
		target = null;
		targetPoint = default;

		if ( !trace.Hit || !trace.GameObject.IsValid() )
			return false;

		var targetBarricade = ResolveBarricade( trace.GameObject );
		if ( targetBarricade.IsValid() && targetBarricade == barricade )
			return false;

		target = trace.GameObject;
		targetPoint = trace.HitPosition;
		return true;
	}

	private SceneTraceResult TraceHammer( float range )
	{
		return Scene.Trace.Ray( AimRay, range )
			.IgnoreGameObjectHierarchy( AimIgnoreRoot )
			.WithoutTags( "playercontroller" )
			.Radius( SwingRadius )
			.UseHitboxes()
			.Run();
	}

	private static ZombieSurvivalBarricade ResolveBarricade( GameObject target )
	{
		if ( !target.IsValid() )
			return null;

		return target.GetComponentInParent<ZombieSurvivalBarricade>( true )
			?? target.Root.GetComponent<ZombieSurvivalBarricade>();
	}

	public override void DrawHud( HudPainter painter, Vector2 crosshair )
	{
		DrawCrosshair( painter, crosshair );
	}
}
