using Sandbox;

namespace Sandbox;

[Title( "Human Shop" ), Order( 5 ), Icon( "" )]
public sealed class ZombieSurvivalShopPage : BaseSpawnMenu
{
	protected override void Rebuild()
	{
		AddHeader( "Zombie Survival" );

		AddOption(
			"shopping_cart",
			"All Items",
			() => new ZombieSurvivalShopPanel()
		);

		AddHeader( "Shop Categories" );

		foreach ( var category in ZombieSurvivalShopCatalog.Categories )
		{
			var capturedCategory = category;

			AddOption(
				GetIconForCategory( capturedCategory ),
				capturedCategory,
				() => new ZombieSurvivalShopPanel
				{
					Category = capturedCategory
				}
			);
		}
	}

	private static string GetIconForCategory( string category )
	{
		return category switch
		{
			"Melee" => "sports_mma",
			"Pistols" => "gps_fixed",
			"SMGs" => "speed",
			"Shotguns" => "flare",
			"Rifles" => "my_location",
			"Explosives" => "local_fire_department",
			"Heavy" => "rocket_launch",
			"Ammo" => "inventory_2",
			"Tools" => "construction",
			"Medical" => "medical_services",
			_ => "shopping_cart"
		};
	}
}
