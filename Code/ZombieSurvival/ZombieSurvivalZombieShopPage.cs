using Sandbox;

namespace Sandbox;

[Title( "Zombie Shop" ), Order( 6 ), Icon( "" )]
public sealed class ZombieSurvivalZombieShopPage : BaseSpawnMenu
{
	protected override void Rebuild()
	{
		AddHeader( "Zombie Survival" );

		AddOption(
			"skull",
			"All Forms",
			() => new ZombieSurvivalZombieShopPanel()
		);
	}
}
