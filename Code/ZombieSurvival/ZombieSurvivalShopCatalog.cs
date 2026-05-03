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
	/// Optional visual preview path for Q-menu display.
	/// For now this can match ResourcePath. Later we can point this at model/icon assets.
	/// </summary>
	public string PreviewPath { get; init; } = "";

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
			ResourcePath = "weapons/Crowbar/crowbar.prefab",
			PreviewPath = "weapons/Crowbar/crowbar.prefab"
		},

		new()
		{
			Id = "hammer",
			DisplayName = "Hammer",
			Description = "Nail and repair barricades. Light damage against zombies.",
			Cost = 0,
			Type = ZombieSurvivalShopItemType.Weapon,
			Category = "Melee",
			SortOrder = 15,
			ClassName = "HammerWeapon",
			ResourcePath = "weapons/Hammer/hammer.prefab",
			PreviewPath = "weapons/Hammer/hammer.prefab"
		},

		new()
		{
			Id = "glock",
			DisplayName = "Glock",
			Description = "Cheap pistol for early survival.",
			Cost = 0,
			Type = ZombieSurvivalShopItemType.Weapon,
			Category = "Pistols",
			SortOrder = 20,
			ClassName = "GlockWeapon",
			ResourcePath = "weapons/Glock/glock.prefab",
			PreviewPath = "weapons/Glock/glock.prefab"
		},

		new()
		{
			Id = "colt1911",
			DisplayName = "Colt 1911",
			Description = "Stronger pistol with better stopping power.",
			Cost = 0,
			Type = ZombieSurvivalShopItemType.Weapon,
			Category = "Pistols",
			SortOrder = 30,
			ClassName = "Colt1911Weapon",
			ResourcePath = "weapons/Colt1911/colt1911.prefab",
			PreviewPath = "weapons/Colt1911/colt1911.prefab"
		},

		new()
		{
			Id = "mp5",
			DisplayName = "MP5",
			Description = "Fast-firing SMG for close to mid range.",
			Cost = 0,
			Type = ZombieSurvivalShopItemType.Weapon,
			Category = "SMGs",
			SortOrder = 40,
			ClassName = "Mp5Weapon",
			ResourcePath = "weapons/Mp5/mp5.prefab",
			PreviewPath = "weapons/Mp5/mp5.prefab"
		},

		new()
		{
			Id = "shotgun",
			DisplayName = "Shotgun",
			Description = "Heavy close-range damage.",
			Cost = 0,
			Type = ZombieSurvivalShopItemType.Weapon,
			Category = "Shotguns",
			SortOrder = 50,
			ClassName = "ShotgunWeapon",
			ResourcePath = "weapons/Shotgun/shotgun.prefab",
			PreviewPath = "weapons/Shotgun/shotgun.prefab"
		},

		new()
		{
			Id = "m4a1",
			DisplayName = "M4A1",
			Description = "Reliable rifle for sustained defense.",
			Cost = 0,
			Type = ZombieSurvivalShopItemType.Weapon,
			Category = "Rifles",
			SortOrder = 60,
			ClassName = "M4a1Weapon",
			ResourcePath = "weapons/M4a1/m4a1.prefab",
			PreviewPath = "weapons/M4a1/m4a1.prefab"
		},

		new()
		{
			Id = "sniper",
			DisplayName = "Sniper",
			Description = "Long-range precision weapon.",
			Cost = 0,
			Type = ZombieSurvivalShopItemType.Weapon,
			Category = "Rifles",
			SortOrder = 70,
			ClassName = "SniperWeapon",
			ResourcePath = "weapons/Sniper/sniper.prefab",
			PreviewPath = "weapons/Sniper/sniper.prefab"
		},

		new()
		{
			Id = "grenade",
			DisplayName = "Grenade",
			Description = "Throwable explosive.",
			Cost = 0,
			Type = ZombieSurvivalShopItemType.Weapon,
			Category = "Explosives",
			SortOrder = 80,
			ClassName = "HandGrenadeWeapon",
			ResourcePath = "weapons/HandGrenade/handgrenade.prefab",
			PreviewPath = "weapons/HandGrenade/handgrenade.prefab"
		},

		new()
		{
			Id = "rpg",
			DisplayName = "RPG",
			Description = "Heavy explosive weapon.",
			Cost = 0,
			Type = ZombieSurvivalShopItemType.Weapon,
			Category = "Heavy",
			SortOrder = 90,
			ClassName = "RpgWeapon",
			ResourcePath = "weapons/Rpg/rpg.prefab",
			PreviewPath = "weapons/Rpg/rpg.prefab"
		},
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
			_ => 1000
		};
	}
}
