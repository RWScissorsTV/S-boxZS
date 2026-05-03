using System;
using Sandbox;

public sealed partial class PlayerData
{
	[Sync( SyncFlags.FromHost )]
	public ZombieSurvivalRole ZombieSurvivalRole { get; set; } = ZombieSurvivalRole.Unassigned;

	[Sync( SyncFlags.FromHost )]
	public ZombieSurvivalForm ZombieSurvivalForm { get; set; } = ZombieSurvivalForm.None;

	/// <summary>
	/// The zombie form the player wants to use next time they are assigned/spawned as a zombie.
	/// This lets the menu selection wait until the proper respawn/death moment instead of instantly swapping models.
	/// </summary>
	[Sync( SyncFlags.FromHost )]
	public ZombieSurvivalForm ZombieSurvivalRequestedForm { get; set; } = ZombieSurvivalForm.Walker;

	[Sync( SyncFlags.FromHost )]
	public bool ZombieSurvivalAlive { get; set; } = true;

	[Sync( SyncFlags.FromHost )]
	public int ZombieSurvivalBuildPoints { get; set; }

	/// <summary>
	/// Human buy-menu currency.
	/// Keep this separate from sandbox/base stats so ZS economy does not interfere with the base game.
	/// </summary>
	[Sync( SyncFlags.FromHost )]
	public int ZombieSurvivalCoins { get; set; }

	public bool IsZombieSurvivalHuman => ZombieSurvivalRole == ZombieSurvivalRole.Human;
	public bool IsZombieSurvivalZombie => ZombieSurvivalRole == ZombieSurvivalRole.Zombie;

	public void ZombieSurvivalResetEconomyHost( int startingCoins )
	{
		if ( !Networking.IsHost )
			return;

		ZombieSurvivalCoins = Math.Max( startingCoins, 0 );
	}

	public void ZombieSurvivalAddCoinsHost( int amount )
	{
		if ( !Networking.IsHost )
			return;

		if ( amount == 0 )
			return;

		ZombieSurvivalCoins = Math.Max( ZombieSurvivalCoins + amount, 0 );
	}

	public bool ZombieSurvivalSpendCoinsHost( int amount )
	{
		if ( !Networking.IsHost )
			return false;

		if ( amount <= 0 )
			return true;

		if ( ZombieSurvivalCoins < amount )
			return false;

		ZombieSurvivalCoins -= amount;
		return true;
	}

	public void ZombieSurvivalSetRequestedFormHost( ZombieSurvivalForm form )
	{
		if ( !Networking.IsHost )
			return;

		if ( form == ZombieSurvivalForm.None )
			form = ZombieSurvivalForm.Walker;

		ZombieSurvivalRequestedForm = form;
	}
}
