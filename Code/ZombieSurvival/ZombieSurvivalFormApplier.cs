using Sandbox;

/// <summary>
/// Single source of truth for the controller shape (body height/radius, ducked height,
/// eye distance, camera offset, reach length). Runs on every peer for every player and
/// derives the shape from networked PlayerData, so host and clients always agree without
/// any RPCs or snapshotting.
/// </summary>
public sealed class ZombieSurvivalFormApplier : Component
{
	private static readonly ZombieSurvivalFormDefinition Human = new()
	{
		Form = ZombieSurvivalForm.None,
		BodyHeight = 72f,
		BodyRadius = 16f,
		DuckedHeight = 36f,
		EyeDistanceFromTop = 8f,
		CameraOffset = new Vector3( 96f, 0f, -4f ),
		ReachLength = 130f
	};

	protected override void OnUpdate()
	{
		var player = GetComponent<Player>();

		if ( !player.IsValid() || !player.PlayerData.IsValid() || !player.Controller.IsValid() )
			return;

		var definition = Resolve( player.PlayerData );
		var controller = player.Controller;

		controller.BodyHeight = definition.BodyHeight;
		controller.BodyRadius = definition.BodyRadius;
		controller.DuckedHeight = definition.DuckedHeight;
		controller.EyeDistanceFromTop = definition.EyeDistanceFromTop;
		controller.CameraOffset = definition.CameraOffset;
		controller.ReachLength = definition.ReachLength;
	}

	private static ZombieSurvivalFormDefinition Resolve( PlayerData data )
	{
		if ( data.ZombieSurvivalRole != ZombieSurvivalRole.Zombie )
			return Human;

		var form = data.ZombieSurvivalForm == ZombieSurvivalForm.None
			? ZombieSurvivalForm.Walker
			: data.ZombieSurvivalForm;

		return ZombieSurvivalFormCatalog.Get( form );
	}
}
