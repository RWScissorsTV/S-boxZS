using System;

public sealed class ZombieSurvivalFirstPersonPunchViewModel : Component
{
	[Property] public GameObject VisualRoot { get; set; }
	[Property] public SkinnedModelRenderer Renderer { get; set; }

	[Property] public float PunchDuration { get; set; } = 0.18f;
	[Property] public float ForwardDistance { get; set; } = 8f;
	[Property] public float VerticalOffset { get; set; } = -1.5f;
	[Property] public float SideOffset { get; set; } = 2.5f;
	[Property] public Angles PunchAngles { get; set; } = new( -12f, 0f, 10f );

	private TimeSince _timeSincePunch = 999f;
	private float _punchSide = 1f;

	protected override void OnUpdate()
	{
		if ( !VisualRoot.IsValid() )
			return;

		var duration = MathF.Max( PunchDuration, 0.001f );
		var progress = Math.Clamp( _timeSincePunch / duration, 0f, 1f );
		var blend = MathF.Sin( progress * MathF.PI );

		var position = new Vector3(
			SideOffset * _punchSide * blend * 0.35f,
			ForwardDistance * blend,
			VerticalOffset * blend
		);

		var rotation = new Angles(
			PunchAngles.pitch * blend,
			PunchAngles.yaw * _punchSide * blend,
			PunchAngles.roll * _punchSide * blend
		);

		VisualRoot.LocalPosition = position;
		VisualRoot.LocalRotation = rotation.ToRotation();
	}

	public void TriggerPunch()
	{
		_punchSide *= -1f;
		_timeSincePunch = 0f;
		Renderer?.Set( "b_attack", true );
	}
}
