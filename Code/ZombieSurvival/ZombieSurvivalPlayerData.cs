public sealed partial class PlayerData
{
	[Sync( SyncFlags.FromHost )] public ZombieSurvivalRole ZombieSurvivalRole { get; set; } = ZombieSurvivalRole.Unassigned;
	[Sync( SyncFlags.FromHost )] public ZombieSurvivalForm ZombieSurvivalForm { get; set; } = ZombieSurvivalForm.None;
	[Sync( SyncFlags.FromHost )] public bool ZombieSurvivalAlive { get; set; } = true;
	[Sync( SyncFlags.FromHost )] public int ZombieSurvivalBuildPoints { get; set; }

	public bool IsZombieSurvivalHuman => ZombieSurvivalRole == ZombieSurvivalRole.Human;
	public bool IsZombieSurvivalZombie => ZombieSurvivalRole == ZombieSurvivalRole.Zombie;
}
