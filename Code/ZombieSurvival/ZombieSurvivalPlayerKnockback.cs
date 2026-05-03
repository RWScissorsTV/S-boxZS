using Sandbox;

public sealed partial class Player
{
	public void ApplyZombieSurvivalKnockbackHost( Vector3 velocityDelta, float ungroundDuration )
	{
		if ( !Networking.IsHost )
			return;

		ApplyZombieSurvivalKnockback( velocityDelta, ungroundDuration );

		if ( Network.Owner is not null && Network.Owner != Connection.Local )
			ApplyZombieSurvivalKnockbackOwner( velocityDelta, ungroundDuration );
	}

	[Rpc.Owner( NetFlags.HostOnly )]
	private void ApplyZombieSurvivalKnockbackOwner( Vector3 velocityDelta, float ungroundDuration )
	{
		ApplyZombieSurvivalKnockback( velocityDelta, ungroundDuration );
	}

	private void ApplyZombieSurvivalKnockback( Vector3 velocityDelta, float ungroundDuration )
	{
		if ( !Controller.IsValid() || !Controller.Body.IsValid() )
			return;

		if ( ungroundDuration > 0f )
			Controller.PreventGrounding( ungroundDuration );

		Controller.Body.Velocity += velocityDelta;
	}
}
