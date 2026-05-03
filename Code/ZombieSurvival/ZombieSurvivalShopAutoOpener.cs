using System.Linq;
using Sandbox;

/// <summary>
/// Auto-opens the human Worth Menu (ZombieSurvivalShopPage tab) once per life when
/// the local player is a Human in Zombie Survival. Poll-based instead of event-based:
/// joining clients receive their local Player, PlayerData, ZombieSurvivalGame system,
/// and SpawnMenuHost panel at slightly different times, and Global.IPlayerEvents
/// often fires before all four are ready. A frame-by-frame check trips the open as
/// soon as every prerequisite is satisfied.
/// </summary>
public sealed class ZombieSurvivalShopAutoOpener : GameObjectSystem
{
	private Player _trackedPlayer;
	private bool _openedThisLife;

	public ZombieSurvivalShopAutoOpener( Scene scene ) : base( scene )
	{
		Listen( Stage.StartUpdate, 50, Tick, "ZombieSurvivalShopAutoOpener" );
	}

	private void Tick()
	{
		if ( ZombieSurvivalGame.Current is null || !ZombieSurvivalGame.Enabled )
			return;

		if ( Game.ActiveScene is not { } activeScene )
			return;

		var local = activeScene.GetAll<Player>()
			.FirstOrDefault( p => p is not null && p.IsValid() && p.IsLocalPlayer );

		if ( !local.IsValid() || !local.PlayerData.IsValid() )
		{
			_trackedPlayer = null;
			_openedThisLife = false;
			return;
		}

		// Each new local Player instance counts as a new "life": respawn,
		// late join, or scene reload all create a fresh GameObject.
		if ( _trackedPlayer != local )
		{
			_trackedPlayer = local;
			_openedThisLife = false;
		}

		if ( _openedThisLife )
			return;

		if ( local.PlayerData.ZombieSurvivalRole == ZombieSurvivalRole.Zombie )
			return;

		// Wait until the SpawnMenu mode panel is registered. On joining clients
		// this can take many frames after the local Player first appears.
		if ( SpawnMenuHost.FindActiveSpawnMenu() is null )
			return;

		SpawnMenuHost.OpenSticky();
		SpawnMenu.GotoTab( nameof(ZombieSurvivalShopPage) );
		_openedThisLife = true;
	}
}
