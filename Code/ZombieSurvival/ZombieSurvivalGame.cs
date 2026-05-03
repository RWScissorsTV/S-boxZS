using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sandbox.UI;

public sealed class ZombieSurvivalGame : GameObjectSystem<ZombieSurvivalGame>, Global.IPlayerEvents, Global.ISpawnEvents, IToolActionEvents
{
	[ConVar( "zs.enabled", ConVarFlags.Replicated | ConVarFlags.Server | ConVarFlags.GameSetting )]
	public static bool Enabled { get; set; } = true;

	[ConVar( "zs.min_players", ConVarFlags.Replicated | ConVarFlags.Server | ConVarFlags.GameSetting )]
	public static int MinimumPlayersToStart { get; set; } = 2;

	[ConVar( "zs.build_seconds", ConVarFlags.Replicated | ConVarFlags.Server | ConVarFlags.GameSetting )]
	public static float BuildSeconds { get; set; } = 120f;

	[ConVar( "zs.survival_seconds", ConVarFlags.Replicated | ConVarFlags.Server | ConVarFlags.GameSetting )]
	public static float SurvivalSeconds { get; set; } = 180f;

	[ConVar( "zs.round_end_seconds", ConVarFlags.Replicated | ConVarFlags.Server | ConVarFlags.GameSetting )]
	public static float RoundEndSeconds { get; set; } = 20f;

	[ConVar( "zs.initial_zombie_fraction", ConVarFlags.Replicated | ConVarFlags.Server | ConVarFlags.GameSetting )]
	public static float InitialZombieFraction { get; set; } = 0.3f;

	[ConVar( "zs.minimum_initial_zombies", ConVarFlags.Replicated | ConVarFlags.Server | ConVarFlags.GameSetting )]
	public static int MinimumInitialZombies { get; set; } = 1;

	[ConVar( "zs.human_health", ConVarFlags.Replicated | ConVarFlags.Server | ConVarFlags.GameSetting )]
	public static float HumanHealth { get; set; } = 100f;

	[ConVar( "zs.zombie_health", ConVarFlags.Replicated | ConVarFlags.Server | ConVarFlags.GameSetting )]
	public static float ZombieHealth { get; set; } = 125f;

	[ConVar( "zs.human_walk_speed", ConVarFlags.Replicated | ConVarFlags.Server | ConVarFlags.GameSetting )]
	public static float HumanWalkSpeed { get; set; } = 230f;

	[ConVar( "zs.human_run_speed", ConVarFlags.Replicated | ConVarFlags.Server | ConVarFlags.GameSetting )]
	public static float HumanRunSpeed { get; set; } = 350f;

	[ConVar( "zs.zombie_walk_speed", ConVarFlags.Replicated | ConVarFlags.Server | ConVarFlags.GameSetting )]
	public static float ZombieWalkSpeed { get; set; } = 260f;

	[ConVar( "zs.zombie_run_speed", ConVarFlags.Replicated | ConVarFlags.Server | ConVarFlags.GameSetting )]
	public static float ZombieRunSpeed { get; set; } = 390f;

	[ConVar( "zs.default_zombie_form", ConVarFlags.Replicated | ConVarFlags.Server | ConVarFlags.GameSetting, Help = "1 = walker, 2 = headcrab." )]
	public static int DefaultZombieForm { get; set; } = 1;

	[ConVar( "zs.barricade_health", ConVarFlags.Replicated | ConVarFlags.Server | ConVarFlags.GameSetting )]
	public static float BarricadeHealth { get; set; } = 150f;

	[ConVar( "zs.barricade_max_health", ConVarFlags.Replicated | ConVarFlags.Server | ConVarFlags.GameSetting )]
	public static float BarricadeMaxHealth { get; set; } = 225f;

	[ConVar( "zs.barricade_repair_amount", ConVarFlags.Replicated | ConVarFlags.Server | ConVarFlags.GameSetting )]
	public static float BarricadeRepairAmount { get; set; } = 25f;

	[ConVar( "zs.barricade_fortify_amount", ConVarFlags.Replicated | ConVarFlags.Server | ConVarFlags.GameSetting )]
	public static float BarricadeFortifyAmount { get; set; } = 15f;

	[ConVar( "zs.build_points_start", ConVarFlags.Replicated | ConVarFlags.Server | ConVarFlags.GameSetting )]
	public static int StartingBuildPoints { get; set; } = 100;

	[ConVar( "zs.prop_spawn_cost", ConVarFlags.Replicated | ConVarFlags.Server | ConVarFlags.GameSetting )]
	public static int PropSpawnCost { get; set; } = 10;

	[ConVar( "zs.allow_toolgun_actions", ConVarFlags.Replicated | ConVarFlags.Server | ConVarFlags.GameSetting )]
	public static bool AllowToolgunActions { get; set; } = false;

	public ZombieSurvivalPhase Phase { get; private set; } = ZombieSurvivalPhase.WaitingForPlayers;
	public float RemainingTime { get; private set; }
	public bool HumansWonLastRound { get; private set; }
	public int HumanCount { get; private set; }
	public int ZombieCount { get; private set; }

	private TimeUntil _phaseTimer;
	private TimeUntil _stateBroadcastCooldown;

	public ZombieSurvivalGame( Scene scene ) : base( scene )
	{
		Listen( Stage.StartUpdate, 20, Tick, "ZombieSurvivalGame" );
	}

	private void Tick()
	{
		if ( !Enabled )
			return;

		if ( Networking.IsActive && !Networking.IsHost )
			return;

		EnsureConnectedPlayerState();

		switch ( Phase )
		{
			case ZombieSurvivalPhase.WaitingForPlayers:
				RemainingTime = 0f;
				if ( ConnectedPlayers.Count() >= MinimumPlayersToStart )
					BeginBuildPhase();
				break;

			case ZombieSurvivalPhase.Build:
				RemainingTime = MathF.Max( _phaseTimer, 0f );
				if ( ConnectedPlayers.Count() < MinimumPlayersToStart )
				{
					BeginWaitingForPlayers();
					break;
				}

				if ( _phaseTimer <= 0f )
					BeginSurvivalPhase();
				break;

			case ZombieSurvivalPhase.Survival:
				RemainingTime = MathF.Max( _phaseTimer, 0f );
				if ( ConnectedPlayers.Count() < MinimumPlayersToStart )
				{
					BeginWaitingForPlayers();
					break;
				}

				if ( CountHumans() <= 0 )
				{
					EndRound( humansWon: false );
					break;
				}

				if ( _phaseTimer <= 0f )
					EndRound( humansWon: true );
				break;

			case ZombieSurvivalPhase.RoundEnd:
				RemainingTime = MathF.Max( _phaseTimer, 0f );
				if ( _phaseTimer <= 0f )
				{
					if ( ConnectedPlayers.Count() >= MinimumPlayersToStart )
						BeginBuildPhase();
					else
						BeginWaitingForPlayers();
				}
				break;
		}

		BroadcastState();
	}

	private IEnumerable<PlayerData> ConnectedPlayers => PlayerData.All.Where( x => x.IsValid() && x.Connection is not null );

	private void EnsureConnectedPlayerState()
	{
		foreach ( var data in ConnectedPlayers )
		{
			if ( Phase == ZombieSurvivalPhase.WaitingForPlayers && data.ZombieSurvivalRole == ZombieSurvivalRole.Unassigned )
			{
				data.ZombieSurvivalRole = ZombieSurvivalRole.Human;
				data.ZombieSurvivalAlive = true;
			}
		}
	}

	private void BeginWaitingForPlayers()
	{
		CleanupRoundBarricades();

		Phase = ZombieSurvivalPhase.WaitingForPlayers;
		RemainingTime = 0f;
		HumansWonLastRound = false;

		foreach ( var data in ConnectedPlayers )
		{
			data.ZombieSurvivalRole = ZombieSurvivalRole.Human;
			data.ZombieSurvivalForm = ZombieSurvivalForm.None;
			data.ZombieSurvivalAlive = true;
			data.ZombieSurvivalBuildPoints = 0;
		}

		ApplyRolesToSpawnedPlayers();
		PostSystemText( "Waiting for players." );
		BroadcastState( force: true );
	}

	private void BeginBuildPhase()
	{
		CleanupRoundBarricades();

		Phase = ZombieSurvivalPhase.Build;
		_phaseTimer = MathF.Max( BuildSeconds, 1f );
		RemainingTime = BuildSeconds;
		HumansWonLastRound = false;

		foreach ( var data in ConnectedPlayers )
		{
			data.ZombieSurvivalRole = ZombieSurvivalRole.Human;
			data.ZombieSurvivalForm = ZombieSurvivalForm.None;
			data.ZombieSurvivalAlive = true;
			GrantBuildPoints( data );
		}

		ApplyRolesToSpawnedPlayers();
		PostSystemText( "Build phase started. Prepare your barricades." );
		BroadcastState( force: true );
	}

	private void BeginSurvivalPhase()
	{
		Phase = ZombieSurvivalPhase.Survival;
		_phaseTimer = MathF.Max( SurvivalSeconds, 1f );
		RemainingTime = SurvivalSeconds;

		var players = ConnectedPlayers.ToList();
		foreach ( var data in players )
		{
			data.ZombieSurvivalRole = ZombieSurvivalRole.Human;
			data.ZombieSurvivalForm = ZombieSurvivalForm.None;
			data.ZombieSurvivalAlive = true;
			data.ZombieSurvivalBuildPoints = 0;
		}

		var zombieCount = GetInitialZombieCount( players.Count );
		foreach ( var zombie in players.OrderBy( _ => Guid.NewGuid() ).Take( zombieCount ) )
		{
			zombie.ZombieSurvivalRole = ZombieSurvivalRole.Zombie;
			zombie.ZombieSurvivalForm = GetDefaultZombieForm();
		}

		ApplyRolesToSpawnedPlayers();
		PostSystemText( $"Survival phase started. {zombieCount} player(s) infected." );
		BroadcastState( force: true );
	}

	private void EndRound( bool humansWon )
	{
		Phase = ZombieSurvivalPhase.RoundEnd;
		_phaseTimer = MathF.Max( RoundEndSeconds, 1f );
		RemainingTime = RoundEndSeconds;
		HumansWonLastRound = humansWon;

		PostSystemText( humansWon ? "Humans survived the round." : "Zombies overran the humans." );
		BroadcastState( force: true );
	}

	private int GetInitialZombieCount( int playerCount )
	{
		if ( playerCount <= 0 )
			return 0;

		var zombieCount = (int)Math.Ceiling( playerCount * Math.Clamp( InitialZombieFraction, 0f, 1f ) );
		zombieCount = Math.Max( zombieCount, Math.Max( MinimumInitialZombies, 1 ) );
		return Math.Clamp( zombieCount, 1, playerCount );
	}

	private int CountHumans()
	{
		return ConnectedPlayers.Count( x => x.ZombieSurvivalRole == ZombieSurvivalRole.Human && x.ZombieSurvivalAlive );
	}

	private void ApplyRolesToSpawnedPlayers()
	{
		foreach ( var player in Scene.GetAll<Player>().ToArray() )
		{
			ApplyRoleToPlayer( player );
		}
	}

	private void ApplyRoleToPlayer( Player player )
	{
		if ( !player.IsValid() || !player.PlayerData.IsValid() )
			return;

		var data = player.PlayerData;
		var isZombie = data.ZombieSurvivalRole == ZombieSurvivalRole.Zombie;
		if ( isZombie && data.ZombieSurvivalForm == ZombieSurvivalForm.None )
			data.ZombieSurvivalForm = GetDefaultZombieForm();
		else if ( !isZombie )
			data.ZombieSurvivalForm = ZombieSurvivalForm.None;

		var zombieForm = ZombieSurvivalFormCatalog.Get( data.ZombieSurvivalForm );

		player.GameObject.Tags.Remove( "human" );
		player.GameObject.Tags.Remove( "zombie" );
		player.GameObject.Tags.Add( isZombie ? "zombie" : "human" );

		player.MaxHealth = isZombie
			? data.ZombieSurvivalForm == ZombieSurvivalForm.Headcrab ? zombieForm.Health : ZombieHealth
			: HumanHealth;
		player.Health = player.MaxHealth;
		player.Armour = 0f;
		data.ZombieSurvivalAlive = true;

		if ( player.Controller.IsValid() )
		{
			player.Controller.WalkSpeed = isZombie
				? data.ZombieSurvivalForm == ZombieSurvivalForm.Headcrab ? zombieForm.WalkSpeed : ZombieWalkSpeed
				: HumanWalkSpeed;
			player.Controller.RunSpeed = isZombie
				? data.ZombieSurvivalForm == ZombieSurvivalForm.Headcrab ? zombieForm.RunSpeed : ZombieRunSpeed
				: HumanRunSpeed;

			if ( isZombie )
				player.Controller.ThirdPerson = true;
		}

		var attack = player.GameObject.GetOrAddComponent<ZombieSurvivalZombieAttack>();
		attack.Enabled = isZombie;
		if ( isZombie )
			attack.ConfigureForForm( data.ZombieSurvivalForm );

		var formPresenter = player.GameObject.GetOrAddComponent<ZombieSurvivalZombieFormPresenter>();
		formPresenter.Enabled = isZombie;
		if ( isZombie )
			formPresenter.ApplyNow();

		var inventory = player.GetComponent<PlayerInventory>();
		if ( inventory.IsValid() )
			inventory.Enabled = !isZombie;

		var loadout = player.GetComponent<PlayerLoadout>();
		if ( loadout.IsValid() )
			loadout.Enabled = !isZombie;

		if ( isZombie )
			_ = StripZombieInventoryNextFrame( player );

		player.GameObject.Network?.Refresh();
	}

	private async Task StripZombieInventoryNextFrame( Player player )
	{
		await Task.Yield();

		if ( !player.IsValid() || !player.PlayerData.IsValid() || player.PlayerData.ZombieSurvivalRole != ZombieSurvivalRole.Zombie )
			return;

		var inventory = player.GetComponent<PlayerInventory>();
		if ( !inventory.IsValid() )
			return;

		foreach ( var weapon in inventory.Weapons.ToArray() )
		{
			if ( weapon.IsValid() )
				inventory.Remove( weapon );
		}
	}

	private void BroadcastState( bool force = false )
	{
		if ( !force && _stateBroadcastCooldown > 0f )
			return;

		HumanCount = CountHumans();
		ZombieCount = CountZombies();

		RpcSetState( Phase, RemainingTime, HumansWonLastRound, HumanCount, ZombieCount );
		_stateBroadcastCooldown = 0.25f;
	}

	private int CountZombies()
	{
		return ConnectedPlayers.Count( x => x.ZombieSurvivalRole == ZombieSurvivalRole.Zombie );
	}

	[Rpc.Broadcast( NetFlags.HostOnly )]
	private static void RpcSetState( ZombieSurvivalPhase phase, float remainingTime, bool humansWonLastRound, int humans, int zombies )
	{
		if ( Current is null )
			return;

		Current.Phase = phase;
		Current.RemainingTime = remainingTime;
		Current.HumansWonLastRound = humansWonLastRound;
		Current.HumanCount = humans;
		Current.ZombieCount = zombies;
	}

	private void PostSystemText( string message )
	{
		Scene.Get<Chat>()?.AddSystemText( message, "info" );
	}

	private static void GrantBuildPoints( PlayerData data )
	{
		if ( !data.IsValid() )
			return;

		data.ZombieSurvivalBuildPoints = Math.Max( StartingBuildPoints, 0 );
	}

	private void CleanupRoundBarricades()
	{
		if ( !Networking.IsHost )
			return;

		foreach ( var barricade in Scene.GetAllComponents<ZombieSurvivalBarricade>().ToArray() )
		{
			if ( barricade.IsValid() && barricade.GameObject.IsValid() )
				barricade.GameObject.Destroy();
		}
	}

	void Global.IPlayerEvents.OnPlayerSpawned( Player player )
	{
		if ( !Enabled )
			return;

		if ( Networking.IsActive && !Networking.IsHost )
			return;

		var joiningThisRound = player.IsValid()
			&& player.PlayerData.IsValid()
			&& player.PlayerData.ZombieSurvivalRole == ZombieSurvivalRole.Unassigned;

		if ( joiningThisRound )
			player.PlayerData.ZombieSurvivalRole = ZombieSurvivalRole.Human;

		if ( joiningThisRound && Phase == ZombieSurvivalPhase.Build )
			GrantBuildPoints( player.PlayerData );

		ApplyRoleToPlayer( player );
	}

	void Global.IPlayerEvents.OnPlayerDied( Player player, PlayerDiedParams args )
	{
		if ( !Enabled )
			return;

		if ( Networking.IsActive && !Networking.IsHost )
			return;

		if ( !player.IsValid() || !player.PlayerData.IsValid() )
			return;

		var data = player.PlayerData;
		data.ZombieSurvivalAlive = false;

		if ( Phase == ZombieSurvivalPhase.Survival && data.ZombieSurvivalRole == ZombieSurvivalRole.Human )
		{
			data.ZombieSurvivalRole = ZombieSurvivalRole.Zombie;
			data.ZombieSurvivalForm = GetDefaultZombieForm();
			PostSystemText( $"{data.DisplayName} has joined the zombies." );
		}
	}

	void Global.IPlayerEvents.OnPlayerDamaging( PlayerDamageEvent e )
	{
		if ( !Enabled )
			return;

		if ( Networking.IsActive && !Networking.IsHost )
			return;

		var attacker = e.DamageInfo.Attacker?.GetComponentInParent<Player>( true );
		if ( !attacker.IsValid() || !attacker.PlayerData.IsValid() || !e.Player.PlayerData.IsValid() )
			return;

		if ( Phase != ZombieSurvivalPhase.Survival )
		{
			e.Cancelled = true;
			return;
		}

		if ( attacker.PlayerData.ZombieSurvivalRole == e.Player.PlayerData.ZombieSurvivalRole )
			e.Cancelled = true;
	}

	void Global.IPlayerEvents.OnPlayerPickup( PlayerPickupEvent e )
	{
		if ( !Enabled )
			return;

		if ( e.Player.IsValid() && e.Player.PlayerData.IsValid() && e.Player.PlayerData.ZombieSurvivalRole == ZombieSurvivalRole.Zombie )
			e.Cancelled = true;
	}

	void Global.IPlayerEvents.OnPlayerSwitchWeapon( PlayerSwitchWeaponEvent e )
	{
		if ( !Enabled )
			return;

		if ( e.Player.IsValid() && e.Player.PlayerData.IsValid() && e.Player.PlayerData.ZombieSurvivalRole == ZombieSurvivalRole.Zombie )
			e.Cancelled = e.To.IsValid();
	}

	void Global.ISpawnEvents.OnSpawn( Global.ISpawnEvents.SpawnData e )
	{
		if ( !Enabled )
			return;

		if ( Networking.IsActive && !Networking.IsHost )
			return;

		if ( e.Player is null )
			return;

		if ( e.Spawner is not PropSpawner )
		{
			e.Cancelled = true;
			SendBuildNotice( e.Player, "Only props can be placed for barricades." );
			return;
		}

		if ( Phase != ZombieSurvivalPhase.Build )
		{
			e.Cancelled = true;
			SendBuildNotice( e.Player, "Props can only be placed during build phase." );
			return;
		}

		if ( e.Player.ZombieSurvivalRole != ZombieSurvivalRole.Human )
		{
			e.Cancelled = true;
			SendBuildNotice( e.Player, "Only humans can place barricades." );
			return;
		}

		var cost = GetPropSpawnCost();
		if ( cost > 0 && e.Player.ZombieSurvivalBuildPoints < cost )
		{
			e.Cancelled = true;
			SendBuildNotice( e.Player, $"Need {cost} build points. {e.Player.ZombieSurvivalBuildPoints} left." );
			return;
		}

		e.Player.ZombieSurvivalBuildPoints = Math.Max( e.Player.ZombieSurvivalBuildPoints - cost, 0 );
	}

	void Global.ISpawnEvents.OnPostSpawn( Global.ISpawnEvents.PostSpawnData e )
	{
		if ( !Enabled )
			return;

		if ( Networking.IsActive && !Networking.IsHost )
			return;

		if ( e.Spawner is not PropSpawner || e.Objects is not { Count: > 0 } )
			return;

		foreach ( var go in e.Objects )
		{
			if ( !go.IsValid() )
				continue;

			var barricade = go.GetOrAddComponent<ZombieSurvivalBarricade>();
			barricade.StartingHealth = BarricadeHealth;
			barricade.MaxFortifiedHealth = BarricadeMaxHealth;
			barricade.RepairAmount = BarricadeRepairAmount;
			barricade.FortifyAmount = BarricadeFortifyAmount;
			barricade.InitializeHost();

			go.Network?.Refresh();
		}
	}

	private static void SendBuildNotice( PlayerData player, string text )
	{
		var target = player?.Connection;
		if ( target is null )
			return;

		Notices.SendNotice( target, "block", Color.Red, text, 3 );
	}

	public static int GetPropSpawnCost()
	{
		return Math.Max( PropSpawnCost, 0 );
	}

	private static ZombieSurvivalForm GetDefaultZombieForm()
	{
		return ZombieSurvivalFormCatalog.FromConVarValue( DefaultZombieForm );
	}

	void IToolActionEvents.OnToolAction( IToolActionEvents.ActionData e )
	{
		if ( !Enabled || AllowToolgunActions )
			return;

		if ( Networking.IsActive && !Networking.IsHost )
			return;

		if ( e.Player is null )
			return;

		e.Cancelled = true;
		SendBuildNotice( e.Player, "Toolgun actions are disabled in Zombie Survival." );
	}

	void IToolActionEvents.OnPostToolAction( IToolActionEvents.PostActionData e )
	{
	}
}
