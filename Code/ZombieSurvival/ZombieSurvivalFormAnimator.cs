using System;

public sealed class ZombieSurvivalFormAnimator : Component, PlayerController.IEvents
{
	[Property] public SkinnedModelRenderer Renderer { get; set; }
	[Property] public PlayerController PlayerController { get; set; }

	[Property] public bool TriggerJumpFromPlayerEvents { get; set; } = true;
	[Property] public bool TriggerJumpFromAirborneFallback { get; set; } = true;
	[Property] public bool SetAirborneParameter { get; set; } = true;
	[Property] public bool UseWishVelocityForDirection { get; set; } = true;

	[Property] public float WalkSpeedForFullBlend { get; set; } = 120f;
	[Property] public float WalkSpeedForNormalPlayback { get; set; } = 120f;
	[Property] public float IdleDeadzone { get; set; } = 2f;
	[Property] public float MinWalkPlaybackRate { get; set; } = 0.75f;
	[Property] public float MaxWalkPlaybackRate { get; set; } = 1.35f;
	[Property] public float BlendSmoothing { get; set; } = 8f;
	[Property] public float PlaybackRateSmoothing { get; set; } = 10f;
	[Property] public float JumpCooldown { get; set; } = 0.25f;
	[Property] public float AttackCooldown { get; set; } = 0.5f;
	[Property] public float StrafeThreshold { get; set; } = 0.25f;
	[Property] public float BackwardThreshold { get; set; } = 0.10f;

	private Vector3 _lastWorldPosition;
	private bool _wasAirborne;
	private float _smoothedMoveSpeed;
	private float _smoothedMoveForward;
	private float _smoothedPlaybackRate = 1f;
	private TimeSince _timeSinceJumpTriggered;
	private TimeSince _timeSinceAttackTriggered;
	private bool _jumpPulseArmed;
	private bool _attackPulseArmed;

	protected override void OnStart()
	{
		Renderer ??= GetComponent<SkinnedModelRenderer>();
		PlayerController ??= GetComponentInParent<Player>( true )?.Controller;

		if ( !Renderer.IsValid() )
			return;

		Renderer.UseAnimGraph = true;

		_lastWorldPosition = WorldPosition;
		_wasAirborne = PlayerController.IsValid() && PlayerController.IsAirborne;
		_timeSinceJumpTriggered = 999f;
		_timeSinceAttackTriggered = 999f;

		Renderer.Set( "move_speed", 0f );
		Renderer.Set( "move_forward", 0f );
		Renderer.Set( "move_backward_bool", false );
		Renderer.Set( "strafe_left", false );
		Renderer.Set( "strafe_right", false );
		Renderer.Set( "jump1", false );
		Renderer.Set( "attack1", false );

		if ( SetAirborneParameter )
			Renderer.Set( "airborne", false );
	}

	protected override void OnUpdate()
	{
		if ( !Renderer.IsValid() )
			return;

		UpdateJumpFallback();

		Renderer.Set( "jump1", _jumpPulseArmed );
		Renderer.Set( "attack1", _attackPulseArmed );

		_jumpPulseArmed = false;
		_attackPulseArmed = false;

		var directionVelocity = GetHorizontalDirectionVelocity();
		var playbackVelocity = GetHorizontalPlaybackVelocity();

		var directionSpeed = directionVelocity.Length;
		var playbackSpeed = playbackVelocity.Length;

		if ( directionSpeed < IdleDeadzone )
			directionSpeed = 0f;

		var targetMoveSpeed = Math.Clamp( directionSpeed / MathF.Max( WalkSpeedForFullBlend, 0.0001f ), 0f, 1f );
		var forward = GetForwardBasis();
		var targetMoveForward = Math.Clamp( directionVelocity.Dot( forward ) / MathF.Max( WalkSpeedForFullBlend, 0.0001f ), -1f, 1f );

		_smoothedMoveSpeed = MathX.Lerp( _smoothedMoveSpeed, targetMoveSpeed, Time.Delta * BlendSmoothing );
		_smoothedMoveForward = MathX.Lerp( _smoothedMoveForward, targetMoveForward, Time.Delta * BlendSmoothing );

		Renderer.Set( "move_speed", _smoothedMoveSpeed );
		Renderer.Set( "move_forward", _smoothedMoveForward );

		var strafeLeft = false;
		var strafeRight = false;

		if ( directionSpeed > IdleDeadzone )
		{
			strafeLeft = directionVelocity.Dot( -GetRightBasis() ) / MathF.Max( WalkSpeedForFullBlend, 0.0001f ) > StrafeThreshold;
			strafeRight = directionVelocity.Dot( GetRightBasis() ) / MathF.Max( WalkSpeedForFullBlend, 0.0001f ) > StrafeThreshold;
		}

		Renderer.Set( "move_backward_bool", _smoothedMoveSpeed > 0.05f && _smoothedMoveForward < -BackwardThreshold );
		Renderer.Set( "strafe_left", strafeLeft );
		Renderer.Set( "strafe_right", strafeRight );

		if ( SetAirborneParameter )
			Renderer.Set( "airborne", PlayerController.IsValid() && PlayerController.IsAirborne );

		var targetPlaybackRate = 1f;
		if ( playbackSpeed > IdleDeadzone )
			targetPlaybackRate = Math.Clamp( playbackSpeed / MathF.Max( WalkSpeedForNormalPlayback, 0.0001f ), MinWalkPlaybackRate, MaxWalkPlaybackRate );

		_smoothedPlaybackRate = MathX.Lerp( _smoothedPlaybackRate, targetPlaybackRate, Time.Delta * PlaybackRateSmoothing );
		Renderer.PlaybackRate = _smoothedPlaybackRate;
	}

	public bool TriggerJump1()
	{
		if ( _timeSinceJumpTriggered < JumpCooldown )
			return false;

		_jumpPulseArmed = true;
		_timeSinceJumpTriggered = 0f;
		return true;
	}

	public bool TriggerAttack1()
	{
		if ( _timeSinceAttackTriggered < AttackCooldown )
			return false;

		_attackPulseArmed = true;
		_timeSinceAttackTriggered = 0f;
		return true;
	}

	public void OnJumped()
	{
		if ( TriggerJumpFromPlayerEvents )
			TriggerJump1();
	}

	private void UpdateJumpFallback()
	{
		if ( !PlayerController.IsValid() )
			return;

		var isAirborne = PlayerController.IsAirborne;
		if ( TriggerJumpFromAirborneFallback && isAirborne && !_wasAirborne )
			TriggerJump1();

		_wasAirborne = isAirborne;
	}

	private Vector3 GetHorizontalDirectionVelocity()
	{
		if ( PlayerController.IsValid() )
		{
			var v = UseWishVelocityForDirection ? PlayerController.WishVelocity : PlayerController.Velocity;
			return v.WithZ( 0f );
		}

		return GetFallbackHorizontalVelocity();
	}

	private Vector3 GetHorizontalPlaybackVelocity()
	{
		if ( PlayerController.IsValid() )
			return PlayerController.Velocity.WithZ( 0f );

		return GetFallbackHorizontalVelocity();
	}

	private Vector3 GetForwardBasis()
	{
		var rotation = PlayerController.IsValid() ? PlayerController.EyeTransform.Rotation : WorldRotation;
		return FlattenDirection( rotation.Forward, Vector3.Forward );
	}

	private Vector3 GetRightBasis()
	{
		var rotation = PlayerController.IsValid() ? PlayerController.EyeTransform.Rotation : WorldRotation;
		return FlattenDirection( rotation.Right, Vector3.Right );
	}

	private static Vector3 FlattenDirection( Vector3 value, Vector3 fallback )
	{
		value = value.WithZ( 0f );
		return value.Length > 0.0001f ? value.Normal : fallback;
	}

	private Vector3 GetFallbackHorizontalVelocity()
	{
		var delta = WorldPosition - _lastWorldPosition;
		_lastWorldPosition = WorldPosition;

		return delta.WithZ( 0f ) / MathF.Max( Time.Delta, 0.0001f );
	}
}
