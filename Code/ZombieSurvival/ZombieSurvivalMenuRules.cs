using System;
using Sandbox;

public static class ZombieSurvivalMenuRules
{
	public static bool IsActive => ZombieSurvivalGame.Enabled;

	public static int GetSpawnMenuTabSetVersion()
	{
		return HashCode.Combine( IsActive, (int)GetLocalRoleForMenus() );
	}

	public static bool AllowsSpawnMenuMode( Type type )
	{
		if ( !IsActive )
			return true;

		return type?.Name == "SpawnMenu";
	}

	public static bool AllowsSpawnMenuTab( Type type )
	{
		if ( !IsActive )
			return true;

		if ( type is null )
			return false;

		return type.Name switch
		{
			"PropsPage" => true,
			"ZombieSurvivalShopPage" => GetLocalRoleForMenus() != ZombieSurvivalRole.Zombie,
			"ZombieSurvivalZombieShopPage" => GetLocalRoleForMenus() == ZombieSurvivalRole.Zombie,
			_ => false
		};
	}

	public static bool ShowsUtilityTabs => !IsActive;

	public static bool ShowsFullPropCatalog => !IsActive;

	private static ZombieSurvivalRole GetLocalRoleForMenus()
	{
		var data = PlayerData.For( Connection.Local );

		if ( !data.IsValid() )
			return ZombieSurvivalRole.Human;

		return data.ZombieSurvivalRole == ZombieSurvivalRole.Unassigned
			? ZombieSurvivalRole.Human
			: data.ZombieSurvivalRole;
	}
}
