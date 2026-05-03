using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox;

public enum ZombieSurvivalShopItemType
{
	Weapon,
	Ammo,
	Utility,
	Medical
}

public sealed class ZombieSurvivalShopItem
{
	public string Id { get; init; } = "";
	public string DisplayName { get; init; } = "";
	public string Description { get; init; } = "";
	public int Cost { get; init; }

	public ZombieSurvivalShopItemType Type { get; init; } = ZombieSurvivalShopItemType.Weapon;

	/// <summary>
	/// For weapons, this should point to the actual prefab that PlayerInventory.Pickup() can spawn.
	/// Example: weapons/Glock/glock.prefab
	/// </summary>
	public string ResourcePath { get; init; } = "";

	/// <summary>
	/// Optional class name reference for future logic/debugging.
	/// The current buy system should prefer ResourcePath.
	/// </summary>
	public string ClassName { get; init; } = "";

	public string Category { get; init; } = "Weapons";

	/// <summary>
	/// Keep unfinished systems hidden until their buy behavior is actually wired.
	/// </summary>
	public bool IsEnabled { get; init; } = true;

	/// <summary>
	/// Used for display sorting inside each category.
	/// </summary>
	public int SortOrder { get; init; }
}

public static class ZombieSurvivalShopCatalog
{
	private static readonly IReadOnlyList<ZombieSurvivalShopItem> Items = new List<ZombieSurvivalShopItem>
	{
		new()
		{
			Id = "crowbar",
			DisplayName = "Crowbar",
			Description = "Free backup melee weapon.",
			Cost = 0,
			Type = ZombieSurvivalShopItemType.Weapon,
			Category = "Melee",
			SortOrder = 10,
			ClassName = "CrowbarWeapon",
			ResourcePath = "weapons/Crowbar/crowbar.prefab"
		},

		new()
		{
			Id = "glock",
			DisplayName = "Glock",
			Description = "Cheap pistol for early survival.",
			Cost = 25,
			Type = ZombieSurvivalShopItemType.Weapon,
			Category = "Pistols",
			SortOrder = 20,
			ClassName = "GlockWeapon",
			ResourcePath = "weapons/Glock/glock.prefab"
		},

		new()
		{
			Id = "colt1911",
			DisplayName = "Colt 1911",
			Description = "Stronger pistol with better stopping power.",
			Cost = 45,
			Type = ZombieSurvivalShopItemType.Weapon,
			Category = "Pistols",
			SortOrder = 30,
			ClassName = "Colt1911Weapon",
			ResourcePath = "weapons/Colt1911/colt1911.prefab"
		},

		new()
		{
			Id = "mp5",
			DisplayName = "MP5",
			Description = "Fast-firing SMG for close to mid range.",
			Cost = 90,
			Type = ZombieSurvivalShopItemType.Weapon,
			Category = "SMGs",
			SortOrder = 40,
			ClassName = "Mp5Weapon",
			ResourcePath = "weapons/Mp5/mp5.prefab"
		},

		new()
		{
			Id = "shotgun",
			DisplayName = "Shotgun",
			Description = "Heavy close-range damage.",
			Cost = 110,
			Type = ZombieSurvivalShopItemType.Weapon,
			Category = "Shotguns",
			SortOrder = 50,
			ClassName = "ShotgunWeapon",
			ResourcePath = "weapons/Shotgun/shotgun.prefab"
		},

		new()
		{
			Id = "m4a1",
			DisplayName = "M4A1",
			Description = "Reliable rifle for sustained defense.",
			Cost = 150,
			Type = ZombieSurvivalShopItemType.Weapon,
			Category = "Rifles",
			SortOrder = 60,
			ClassName = "M4a1Weapon",
			ResourcePath = "weapons/M4a1/m4a1.prefab"
		},

		new()
		{
			Id = "sniper",
			DisplayName = "Sniper",
			Description = "Long-range precision weapon.",
			Cost = 175,
			Type = ZombieSurvivalShopItemType.Weapon,
			Category = "Rifles",
			SortOrder = 70,
			ClassName = "SniperWeapon",
			ResourcePath = "weapons/Sniper/sniper.prefab"
		},

		new()
		{
			Id = "grenade",
			DisplayName = "Grenade",
			Description = "Throwable explosive.",
			Cost = 60,
			Type = ZombieSurvivalShopItemType.Weapon,
			Category = "Explosives",
			SortOrder = 80,
			ClassName = "HandGrenadeWeapon",
			ResourcePath = "weapons/HandGrenade/handgrenade.prefab"
		},

		new()
		{
			Id = "rpg",
			DisplayName = "RPG",
			Description = "Expensive explosive weapon. Use carefully.",
			Cost = 250,
			Type = ZombieSurvivalShopItemType.Weapon,
			Category = "Heavy",
			SortOrder = 90,
			ClassName = "RpgWeapon",
			ResourcePath = "weapons/Rpg/rpg.prefab"
		},

		// These are intentionally disabled until we wire ammo purchasing in ZombieSurvivalGame.
		new()
		{
			Id = "pistol_ammo",
			DisplayName = "Pistol Ammo",
			Description = "Ammo pack for pistols.",
			Cost = 15,
			Type = ZombieSurvivalShopItemType.Ammo,
			Category = "Ammo",
			SortOrder = 100,
			ResourcePath = "ammo/pistol",
			IsEnabled = false
		},

		new()
		{
			Id = "smg_ammo",
			DisplayName = "SMG Ammo",
			Description = "Ammo pack for SMGs.",
			Cost = 25,
			Type = ZombieSurvivalShopItemType.Ammo,
			Category = "Ammo",
			SortOrder = 110,
			ResourcePath = "ammo/smg",
			IsEnabled = false
		},

		new()
		{
			Id = "rifle_ammo",
			DisplayName = "Rifle Ammo",
			Description = "Ammo pack for rifles.",
			Cost = 35,
			Type = ZombieSurvivalShopItemType.Ammo,
			Category = "Ammo",
			SortOrder = 120,
			ResourcePath = "ammo/rifle",
			IsEnabled = false
		},

		new()
		{
			Id = "shotgun_ammo",
			DisplayName = "Shotgun Ammo",
			Description = "Ammo pack for shotguns.",
			Cost = 30,
			Type = ZombieSurvivalShopItemType.Ammo,
			Category = "Ammo",
			SortOrder = 130,
			ResourcePath = "ammo/shotgun",
			IsEnabled = false
		},

		// Disabled until medical item behavior exists.
		new()
		{
			Id = "medkit",
			DisplayName = "Medkit",
			Description = "Restores health.",
			Cost = 50,
			Type = ZombieSurvivalShopItemType.Medical,
			Category = "Medical",
			SortOrder = 140,
			ResourcePath = "items/medkit",
			IsEnabled = false
		},

		// Disabled until repair/fortify tool behavior exists.
		new()
		{
			Id = "repair_tool",
			DisplayName = "Repair Tool",
			Description = "Repairs and fortifies barricades.",
			Cost = 75,
			Type = ZombieSurvivalShopItemType.Utility,
			Category = "Tools",
			SortOrder = 150,
			ResourcePath = "tools/repair",
			IsEnabled = false
		}
	};

	public static IEnumerable<ZombieSurvivalShopItem> All =>
		Items
			.Where( x => x.IsEnabled )
			.OrderBy( x => GetCategorySortOrder( x.Category ) )
			.ThenBy( x => x.SortOrder )
			.ThenBy( x => x.DisplayName );

	public static IEnumerable<ZombieSurvivalShopItem> Weapons =>
		All.Where( x => x.Type == ZombieSurvivalShopItemType.Weapon );

	public static IEnumerable<ZombieSurvivalShopItem> Ammo =>
		All.Where( x => x.Type == ZombieSurvivalShopItemType.Ammo );

	public static IEnumerable<ZombieSurvivalShopItem> Utility =>
		All.Where( x => x.Type == ZombieSurvivalShopItemType.Utility );

	public static IEnumerable<ZombieSurvivalShopItem> Medical =>
		All.Where( x => x.Type == ZombieSurvivalShopItemType.Medical );

	public static IEnumerable<string> Categories =>
		All
			.Select( x => x.Category )
			.Where( x => !string.IsNullOrWhiteSpace( x ) )
			.Distinct()
			.OrderBy( GetCategorySortOrder )
			.ThenBy( x => x );

	public static IEnumerable<ZombieSurvivalShopItem> ByCategory( string category )
	{
		if ( string.IsNullOrWhiteSpace( category ) )
			return Enumerable.Empty<ZombieSurvivalShopItem>();

		return All.Where( x => string.Equals( x.Category, category, StringComparison.OrdinalIgnoreCase ) );
	}

	public static ZombieSurvivalShopItem Find( string id )
	{
		if ( string.IsNullOrWhiteSpace( id ) )
			return null;

		return All.FirstOrDefault( x => string.Equals( x.Id, id, StringComparison.OrdinalIgnoreCase ) );
	}

	public static bool Exists( string id )
	{
		return Find( id ) is not null;
	}

	private static int GetCategorySortOrder( string category )
	{
		return category switch
		{
			"Melee" => 10,
			"Pistols" => 20,
			"SMGs" => 30,
			"Shotguns" => 40,
			"Rifles" => 50,
			"Explosives" => 60,
			"Heavy" => 70,
			"Ammo" => 80,
			"Tools" => 90,
			"Medical" => 100,
			_ => 1000
		};
	}
}
