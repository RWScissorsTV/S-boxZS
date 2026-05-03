using System;

public static class ZombieSurvivalMenuRules
{
	public static bool IsActive => ZombieSurvivalGame.Enabled;

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

		return type?.Name == "PropsPage";
	}

	public static bool ShowsUtilityTabs => !IsActive;

	public static bool ShowsFullPropCatalog => !IsActive;
}
