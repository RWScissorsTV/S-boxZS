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
	/// Optional prefab/resource path used later by the buy system.
	/// This is intentionally data-only right now so the catalog does not fight the sandbox base inventory system.
	/// </summary>
	public string ResourcePath { get; init; } = "";

	/// <summary>
	/// Optional weapon/component class name used later if the inventory gives weapons by type instead of prefab.
	/// </summary>
	public string ClassName { get; init; } = "";

	public string Category { get; init; } = "Weapons";

	public bool IsEnabled { get; init; } = true;
}

public static class ZombieSurvivalShopCatalog
{
	private static readonly IReadOnlyList<ZombieSurvivalShopItem> Items = new List<ZombieSurvivalShopItem>
	{
		new()
		{
			Id = "crowbar",
			DisplayName = "Crowbar",
			Description = "Basic melee weapon. Cheap and reliable.",
			Cost = 0,
			Type = ZombieSurvivalShopItemType.Weapon,
			Category = "Melee",
			ClassName = "CrowbarWeapon",
			ResourcePath = "weapons/Crowbar"
		},

		new()
		{
			Id = "glock",
			DisplayName = "Glock",
			Description = "Cheap pistol for early survival.",
			Cost = 25,
			Type = ZombieSurvivalShopItemType.Weapon,
			Category = "Pistols",
			ClassName = "GlockWeapon",
			ResourcePath = "weapons/Glock"
		},

		new()
		{
			Id = "colt1911",
			DisplayName = "Colt 1911",
			Description = "Stronger pistol with better stopping power.",
			Cost = 40,
			Type = ZombieSurvivalShopItemType.Weapon,
			Category = "Pistols",
			ClassName = "Colt1911Weapon",
			ResourcePath = "weapons/Colt1911"
		},

		new()
		{
			Id = "mp5",
			DisplayName = "MP5",
			Description = "Fast-firing SMG for close to mid range.",
			Cost = 90,
			Type = ZombieSurvivalShopItemType.Weapon,
			Category = "SMGs",
			ClassName = "Mp5Weapon",
			ResourcePath = "weapons/Mp5"
		},

		new()
		{
			Id = "shotgun",
			DisplayName = "Shotgun",
			Description = "Heavy close-range damage.",
			Cost = 110,
			Type = ZombieSurvivalShopItemType.Weapon,
			Category = "Shotguns",
			ClassName = "ShotgunWeapon",
			ResourcePath = "weapons/Shotgun"
		},

		new()
		{
			Id = "m4a1",
			DisplayName = "M4A1",
			Description = "Reliable rifle for sustained defense.",
			Cost = 150,
			Type = ZombieSurvivalShopItemType.Weapon,
			Category = "Rifles",
			ClassName = "M4a1Weapon",
			ResourcePath = "weapons/M4a1"
		},

		new()
		{
			Id = "sniper",
			DisplayName = "Sniper",
			Description = "Long-range precision weapon.",
			Cost = 175,
			Type = ZombieSurvivalShopItemType.Weapon,
			Category = "Rifles",
			ClassName = "SniperWeapon",
			ResourcePath = "weapons/Sniper"
		},

		new()
		{
			Id = "rpg",
			DisplayName = "RPG",
			Description = "Expensive explosive weapon. Use carefully.",
			Cost = 250,
			Type = ZombieSurvivalShopItemType.Weapon,
			Category = "Heavy",
			ClassName = "RpgWeapon",
			ResourcePath = "weapons/Rpg"
		},

		new()
		{
			Id = "grenade",
			DisplayName = "Grenade",
			Description = "Throwable explosive.",
			Cost = 60,
			Type = ZombieSurvivalShopItemType.Weapon,
			Category = "Explosives",
			ClassName = "HandGrenadeWeapon",
			ResourcePath = "weapons/Grenade"
		},

		new()
		{
			Id = "pistol_ammo",
			DisplayName = "Pistol Ammo",
			Description = "Ammo pack for pistols.",
			Cost = 15,
			Type = ZombieSurvivalShopItemType.Ammo,
			Category = "Ammo",
			ResourcePath = "ammo/pistol"
		},

		new()
		{
			Id = "smg_ammo",
			DisplayName = "SMG Ammo",
			Description = "Ammo pack for SMGs.",
			Cost = 25,
			Type = ZombieSurvivalShopItemType.Ammo,
			Category = "Ammo",
			ResourcePath = "ammo/smg"
		},

		new()
		{
			Id = "rifle_ammo",
			DisplayName = "Rifle Ammo",
			Description = "Ammo pack for rifles.",
			Cost = 35,
			Type = ZombieSurvivalShopItemType.Ammo,
			Category = "Ammo",
			ResourcePath = "ammo/rifle"
		},

		new()
		{
			Id = "shotgun_ammo",
			DisplayName = "Shotgun Ammo",
			Description = "Ammo pack for shotguns.",
			Cost = 30,
			Type = ZombieSurvivalShopItemType.Ammo,
			Category = "Ammo",
			ResourcePath = "ammo/shotgun"
		},

		new()
		{
			Id = "medkit",
			DisplayName = "Medkit",
			Description = "Restores health. Placeholder until medical item behavior is wired.",
			Cost = 50,
			Type = ZombieSurvivalShopItemType.Medical,
			Category = "Medical",
			ResourcePath = "items/medkit"
		},

		new()
		{
			Id = "repair_tool",
			DisplayName = "Repair Tool",
			Description = "Repairs barricades. Placeholder until repair tool behavior is wired.",
			Cost = 75,
			Type = ZombieSurvivalShopItemType.Utility,
			Category = "Tools",
			ResourcePath = "tools/repair"
		}
	};

	public static IEnumerable<ZombieSurvivalShopItem> All => Items.Where( x => x.IsEnabled );

	public static IEnumerable<ZombieSurvivalShopItem> Weapons => All.Where( x => x.Type == ZombieSurvivalShopItemType.Weapon );
	public static IEnumerable<ZombieSurvivalShopItem> Ammo => All.Where( x => x.Type == ZombieSurvivalShopItemType.Ammo );
	public static IEnumerable<ZombieSurvivalShopItem> Utility => All.Where( x => x.Type == ZombieSurvivalShopItemType.Utility );
	public static IEnumerable<ZombieSurvivalShopItem> Medical => All.Where( x => x.Type == ZombieSurvivalShopItemType.Medical );

	public static IEnumerable<string> Categories => All
		.Select( x => x.Category )
		.Where( x => !string.IsNullOrWhiteSpace( x ) )
		.Distinct()
		.OrderBy( x => x );

	public static IEnumerable<ZombieSurvivalShopItem> ByCategory( string category )
	{
		if ( string.IsNullOrWhiteSpace( category ) )
			return Enumerable.Empty<ZombieSurvivalShopItem>();

		return All.Where( x => string.Equals( x.Category, category, StringComparison.OrdinalIgnoreCase ) );
	}

	public static ZombieSurvivalShopItem? Find( string id )
	{
		if ( string.IsNullOrWhiteSpace( id ) )
			return null;

		return All.FirstOrDefault( x => string.Equals( x.Id, id, StringComparison.OrdinalIgnoreCase ) );
	}

	public static bool Exists( string id )
	{
		return Find( id ) is not null;
	}
}
