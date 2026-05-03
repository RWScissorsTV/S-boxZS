using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sandbox;
using Sandbox.UI;

public sealed class ZombieSurvivalGame : GameObjectSystem, Global.IPlayerEvents, Global.ISpawnEvents, IToolActionEvents
{
	public static ZombieSurvivalGame Current { get; private set; }

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

	[ConVar( "zs.starting_coins", ConVarFlags.Replicated | ConVarFlags.Server | ConVarFlags.GameSetting )]
	public static int StartingCoins { get; set; } = 100;

	[ConVar( "zs.allow_buy_during_waiting", ConVarFlags.Replicated | ConVarFlags.Server | ConVarFlags.GameSetting )]
	public static bool AllowBuyDuringWaiting { get; set; } = true;

	[ConVar( "zs.allow_buy_during_survival", ConVarFlags.Replicated | ConVarFlags.Server | ConVarFlags.GameSetting )]
	public static bool AllowBuyDuringSurvival { get; set; } = true;

	[ConVar( "zs.allow_toolgun_actions", ConVarFlags.Replicated | ConVarFlags.Server | ConVarFlags.GameSetting )]
	public static bool AllowToolgunActions { get; set; } = false;

	public ZombieSurvivalPhase Phase { get; private set; } = ZombieSurvivalPhase.WaitingForPlayers;
	public float RemainingTime { get; private set; }
	public bool HumansWonLastRound { get; private set; }
	public int HumanCount { get; private set; }
	public int ZombieCount { get; private set; }

	private TimeUntil _phaseTimer;
	private TimeUntil _stateBroadcastCooldown;

	private bool _hasDefaultControllerShape;

	private float _defaultBodyHeight = 72f;
	private float _defaultBodyRadius = 16f;
	private float _defaultDuckedHeight = 36f;
	private float _defaultEyeDistanceFromTop = 8f;
	private Vector3 _defaultCameraOffset = Vector3.Zero;
	private float _defaultReachLength = 85f;

	/// <summary>
	/// Prevents coins from being reset every tick while waiting.
	/// Also prevents waiting-phase purchases from becoming free when the build phase starts.
	/// </summary>
	private readonly HashSet<Connection> _economyInitializedConnections = new();

	public ZombieSurvivalGame( Scene scene ) : base( scene )
	{
		Current = this;
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

	private IEnumerable<PlayerData> ConnectedPlayers =>
		PlayerData.All.Where( x => x.IsValid() && x.Connection is not null );

	private void EnsureConnectedPlayerState()
	{
		foreach ( var data in ConnectedPlayers )
		{
			if ( data.ZombieSurvivalRequestedForm == ZombieSurvivalForm.None )
				data.ZombieSurvivalRequestedForm = GetDefaultZombieForm();

			if ( Phase != ZombieSurvivalPhase.WaitingForPlayers )
				continue;

			if ( data.ZombieSurvivalRole == ZombieSurvivalRole.Unassigned )
			{
				data.ZombieSurvivalRole = ZombieSurvivalRole.Human;
				data.ZombieSurvivalForm = ZombieSurvivalForm.None;
				data.ZombieSurvivalAlive = true;
				data.ZombieSurvivalBuildPoints = 0;
			}

			InitializeEconomyIfNeeded( data );
		}
	}

	private void InitializeEconomyIfNeeded( PlayerData data )
	{
		if ( !Networking.IsHost )
			return;

		if ( !data.IsValid() || data.Connection is null )
			return;

		if ( _economyInitializedConnections.Contains( data.Connection ) )
			return;

		data.ZombieSurvivalResetEconomyHost( StartingCoins );
		_economyInitializedConnections.Add( data.Connection );
	}

	private void BeginWaitingForPlayers()
	{
		CleanupRoundBarricades();
		var players = ConnectedPlayers.ToList();

		Phase = ZombieSurvivalPhase.WaitingForPlayers;
		RemainingTime = 0f;
		HumansWonLastRound = false;

		_economyInitializedConnections.Clear();

		foreach ( var data in players )
		{
			data.ZombieSurvivalRole = ZombieSurvivalRole.Human;
			data.ZombieSurvivalForm = ZombieSurvivalForm.None;
			data.ZombieSurvivalAlive = true;
			data.ZombieSurvivalBuildPoints = 0;

			if ( data.ZombieSurvivalRequestedForm == ZombieSurvivalForm.None )
				data.ZombieSurvivalRequestedForm = GetDefaultZombieForm();

			InitializeEconomyIfNeeded( data );
		}

		_ = RespawnPlayersForRoundStateAsync( players );

		PostSystemText( "Waiting for players." );
		BroadcastState( force: true );
	}

	private void BeginBuildPhase()
	{
		CleanupRoundBarricades();
		var players = ConnectedPlayers.ToList();

		var resetEconomyForNewRound = Phase == ZombieSurvivalPhase.RoundEnd;

		Phase = ZombieSurvivalPhase.Build;
		_phaseTimer = MathF.Max( BuildSeconds, 1f );
		RemainingTime = BuildSeconds;
		HumansWonLastRound = false;

		if ( resetEconomyForNewRound )
			_economyInitializedConnections.Clear();

		foreach ( var data in players )
		{
			data.ZombieSurvivalRole = ZombieSurvivalRole.Human;
			data.ZombieSurvivalForm = ZombieSurvivalForm.None;
			data.ZombieSurvivalAlive = true;

			if ( data.ZombieSurvivalRequestedForm == ZombieSurvivalForm.None )
				data.ZombieSurvivalRequestedForm = GetDefaultZombieForm();

			InitializeEconomyIfNeeded( data );
			GrantBuildPoints( data );
		}

		_ = RespawnPlayersForRoundStateAsync( players );

		PostSystemText( "Build phase started.\nPrepare your barricades." );
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

			if ( data.ZombieSurvivalRequestedForm == ZombieSurvivalForm.None )
				data.ZombieSurvivalRequestedForm = GetDefaultZombieForm();
		}

		var zombieCount = GetInitialZombieCount( players.Count );

		foreach ( var zombie in players.OrderBy( _ => Guid.NewGuid() ).Take( zombieCount ) )
		{
			zombie.ZombieSurvivalRole = ZombieSurvivalRole.Zombie;
			zombie.ZombieSurvivalForm = GetPreferredZombieForm( zombie );
		}

		ApplyRolesToSpawnedPlayers();

		PostSystemText( $"Survival phase started.\n{zombieCount} player(s) infected." );
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

	private int CountZombies()
	{
		return ConnectedPlayers.Count( x => x.ZombieSurvivalRole == ZombieSurvivalRole.Zombie );
	}

	public bool CanUseHumanShop( PlayerData data )
	{
		if ( !Enabled || !data.IsValid() )
			return false;

		if ( data.ZombieSurvivalRole != ZombieSurvivalRole.Human )
			return false;

		return Phase switch
		{
			ZombieSurvivalPhase.WaitingForPlayers => AllowBuyDuringWaiting,
			ZombieSurvivalPhase.Build => true,
			ZombieSurvivalPhase.Survival => AllowBuyDuringSurvival,
			_ => false
		};
	}

	public bool CanUseZombieShop( PlayerData data )
	{
		if ( !Enabled || !data.IsValid() )
			return false;

		if ( data.ZombieSurvivalRole != ZombieSurvivalRole.Zombie )
			return false;

		return Phase == ZombieSurvivalPhase.Survival;
	}

	private void ApplyRolesToSpawnedPlayers()
	{
		foreach ( var player in Scene.GetAll<Player>().ToArray() )
		{
			ApplyRoleToPlayer( player );
		}
	}

	private async Task RespawnPlayersForRoundStateAsync( IReadOnlyList<PlayerData> players )
	{
		if ( !Networking.IsHost || players is null || players.Count == 0 )
			return;

		foreach ( var data in players )
		{
			DestroyPlayerActorsFor( data );
		}

		await Task.Yield();

		foreach ( var data in players )
		{
			if ( data.IsValid() )
				GameManager.Current?.SpawnPlayer( data );
		}

		await Task.Yield();
		ApplyRolesToSpawnedPlayers();
	}

	private void DestroyPlayerActorsFor( PlayerData data )
	{
		if ( !data.IsValid() )
			return;

		foreach ( var player in (Scene?.GetAll<Player>() ?? Enumerable.Empty<Player>()).Where( x => x is not null && x.IsValid() && x.PlayerData == data ).ToArray() )
		{
			player.GameObject.Destroy();
		}

		foreach ( var observer in (Scene?.GetAllComponents<PlayerObserver>() ?? Enumerable.Empty<PlayerObserver>()).Where( x => x is not null && x.IsValid() && x.Network.Owner?.Id == data.PlayerId ).ToArray() )
		{
			observer.GameObject.Destroy();
		}
	}

	private void ApplyRoleToPlayer( Player player, bool preserveHealthFraction = false )
	{
		if ( !player.IsValid() || !player.PlayerData.IsValid() )
			return;

		var data = player.PlayerData;
		var isZombie = data.ZombieSurvivalRole == ZombieSurvivalRole.Zombie;
		var previousMaxHealth = Math.Max( player.MaxHealth, 1f );
		var previousHealthFraction = Math.Clamp( player.Health / previousMaxHealth, 0.01f, 1f );

		if ( isZombie && data.ZombieSurvivalForm == ZombieSurvivalForm.None )
			data.ZombieSurvivalForm = GetPreferredZombieForm( data );
		else if ( !isZombie )
			data.ZombieSurvivalForm = ZombieSurvivalForm.None;

		var zombieForm = ZombieSurvivalFormCatalog.Get( data.ZombieSurvivalForm );

		player.GameObject.Tags.Remove( "human" );
		player.GameObject.Tags.Remove( "zombie" );
		player.GameObject.Tags.Add( isZombie ? "zombie" : "human" );

		if ( isZombie )
		{
			player.MaxHealth = data.ZombieSurvivalForm == ZombieSurvivalForm.Headcrab ? zombieForm.Health : ZombieHealth;
		}
		else
		{
			player.MaxHealth = HumanHealth;
		}

		player.Health = preserveHealthFraction
			? Math.Clamp( player.MaxHealth * previousHealthFraction, 1f, player.MaxHealth )
			: player.MaxHealth;

		player.Armour = 0f;
		data.ZombieSurvivalAlive = true;

		if ( player.Controller.IsValid() )
		{
			if ( isZombie )
			{
				player.Controller.WalkSpeed = data.ZombieSurvivalForm == ZombieSurvivalForm.Headcrab ? zombieForm.WalkSpeed : ZombieWalkSpeed;
				player.Controller.RunSpeed = data.ZombieSurvivalForm == ZombieSurvivalForm.Headcrab ? zombieForm.RunSpeed : ZombieRunSpeed;
				player.Controller.ThirdPerson = true;
			}
			else
			{
				player.Controller.WalkSpeed = HumanWalkSpeed;
				player.Controller.RunSpeed = HumanRunSpeed;
			}

			ApplyControllerShapeForRole( player, zombieForm );
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
		var loadout = player.GetComponent<PlayerLoadout>();

		if ( isZombie )
		{
			StripZombieInventory( player );

			if ( inventory.IsValid() )
				inventory.Enabled = false;

			if ( loadout.IsValid() )
				loadout.Enabled = false;

			_ = StripZombieInventoryNextFrame( player );
		}
		else
		{
			if ( inventory.IsValid() )
				inventory.Enabled = true;

			if ( loadout.IsValid() )
				loadout.Enabled = true;
		}

		player.GameObject.Network?.Refresh();
	}

	private void CaptureDefaultControllerShapeIfNeeded( PlayerController controller )
	{
		if ( _hasDefaultControllerShape || !controller.IsValid() )
			return;

		_defaultBodyHeight = controller.BodyHeight;
		_defaultBodyRadius = controller.BodyRadius;
		_defaultDuckedHeight = controller.DuckedHeight;
		_defaultEyeDistanceFromTop = controller.EyeDistanceFromTop;
		_defaultCameraOffset = controller.CameraOffset;
		_defaultReachLength = controller.ReachLength;

		_hasDefaultControllerShape = true;
	}

	private void ApplyControllerShapeForRole( Player player, ZombieSurvivalFormDefinition zombieForm )
	{
		if ( !player.IsValid() || !player.PlayerData.IsValid() || !player.Controller.IsValid() )
			return;

		var controller = player.Controller;
		CaptureDefaultControllerShapeIfNeeded( controller );

		var isHeadcrab =
			player.PlayerData.ZombieSurvivalRole == ZombieSurvivalRole.Zombie
			&& player.PlayerData.ZombieSurvivalForm == ZombieSurvivalForm.Headcrab;

		if ( isHeadcrab )
		{
			controller.BodyHeight = zombieForm.BodyHeight;
			controller.BodyRadius = zombieForm.BodyRadius;
			controller.DuckedHeight = zombieForm.DuckedHeight;
			controller.EyeDistanceFromTop = zombieForm.EyeDistanceFromTop;
			controller.CameraOffset = zombieForm.CameraOffset;
			controller.ReachLength = zombieForm.ReachLength;
			return;
		}

		controller.BodyHeight = _defaultBodyHeight;
		controller.BodyRadius = _defaultBodyRadius;
		controller.DuckedHeight = _defaultDuckedHeight;
		controller.EyeDistanceFromTop = _defaultEyeDistanceFromTop;
		controller.CameraOffset = _defaultCameraOffset;
		controller.ReachLength = _defaultReachLength;
	}

	private async Task StripZombieInventoryNextFrame( Player player )
	{
		for ( var i = 0; i < 4; i++ )
		{
			await Task.Yield();

			if ( !player.IsValid() || !player.PlayerData.IsValid() || player.PlayerData.ZombieSurvivalRole != ZombieSurvivalRole.Zombie )
				return;

			StripZombieInventory( player );
		}
	}

	private void StripZombieInventory( Player player )
	{
		if ( !player.IsValid() || !player.PlayerData.IsValid() || player.PlayerData.ZombieSurvivalRole != ZombieSurvivalRole.Zombie )
			return;

		var inventory = player.GetComponent<PlayerInventory>();

		if ( !inventory.IsValid() )
			return;

		inventory.SwitchWeapon( null, true );

		foreach ( var weapon in inventory.Weapons.ToArray() )
		{
			if ( !weapon.IsValid() )
				continue;

			if ( weapon.ViewModel.IsValid() )
				weapon.ViewModel.Destroy();

			if ( weapon.WorldModel.IsValid() )
				weapon.WorldModel.Destroy();

			weapon.GameObject.Enabled = false;
			weapon.DestroyGameObject();
		}

		player.Controller?.Renderer?.Set( "holdtype", (int)Sandbox.Citizen.CitizenAnimationHelper.HoldTypes.None );
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

		var joiningThisRound =
			player.IsValid()
			&& player.PlayerData.IsValid()
			&& player.PlayerData.ZombieSurvivalRole == ZombieSurvivalRole.Unassigned;

		if ( joiningThisRound )
		{
			player.PlayerData.ZombieSurvivalRole = ZombieSurvivalRole.Human;
			player.PlayerData.ZombieSurvivalForm = ZombieSurvivalForm.None;
			player.PlayerData.ZombieSurvivalAlive = true;

			if ( player.PlayerData.ZombieSurvivalRequestedForm == ZombieSurvivalForm.None )
				player.PlayerData.ZombieSurvivalRequestedForm = GetDefaultZombieForm();
		}

		if ( player.IsValid() && player.PlayerData.IsValid() )
		{
			if ( Phase == ZombieSurvivalPhase.WaitingForPlayers || Phase == ZombieSurvivalPhase.Build )
				InitializeEconomyIfNeeded( player.PlayerData );

			if ( joiningThisRound && Phase == ZombieSurvivalPhase.Build )
				GrantBuildPoints( player.PlayerData );
		}

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
			data.ZombieSurvivalForm = GetPreferredZombieForm( data );

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
			SendBuildNotice( e.Player, $"Need {cost} build points.\n{e.Player.ZombieSurvivalBuildPoints} left." );
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

	[Rpc.Host]
	public static void RequestSelectZombieForm( ZombieSurvivalForm form )
	{
		if ( Current is not ZombieSurvivalGame game )
			return;

		game.SelectZombieFormHost( Rpc.Caller, form );
	}

	private void SelectZombieFormHost( Connection caller, ZombieSurvivalForm form )
	{
		if ( !Enabled || !Networking.IsHost || caller is null )
			return;

		var data = PlayerData.For( caller );

		if ( !data.IsValid() )
			return;

		if ( !CanUseZombieShop( data ) )
		{
			SendZombieShopNotice( caller, "Only active zombies can use the zombie shop." );
			return;
		}

		if ( !ZombieSurvivalFormCatalog.IsValidForm( form ) )
		{
			SendZombieShopNotice( caller, "Unknown zombie form." );
			return;
		}

		data.ZombieSurvivalSetRequestedFormHost( form );
		data.ZombieSurvivalForm = form;

		var player = FindPlayerForData( data );

		if ( !player.IsValid() )
		{
			SendZombieShopNotice( caller, "Could not update your zombie form right now." );
			return;
		}

		ApplyRoleToPlayer( player, preserveHealthFraction: true );
		SendZombieShopNotice( caller, $"Selected {ZombieSurvivalFormCatalog.Get( form ).DisplayName}.", Color.Green );
	}

	[Rpc.Host]
	public static void RequestBuyItem( string itemId )
	{
		if ( Current is not ZombieSurvivalGame game )
			return;

		game.BuyItemHost( Rpc.Caller, itemId );
	}

	private void BuyItemHost( Connection caller, string itemId )
	{
		if ( !Enabled )
			return;

		if ( !Networking.IsHost )
			return;

		if ( caller is null )
			return;

		var data = PlayerData.For( caller );

		if ( !data.IsValid() )
			return;

		if ( data.ZombieSurvivalRole != ZombieSurvivalRole.Human )
		{
			SendShopNotice( caller, "Zombies cannot buy from the human shop." );
			return;
		}

		if ( !CanUseHumanShop( data ) )
		{
			SendShopNotice( caller, "You cannot buy right now." );
			return;
		}

		var player = FindPlayerForData( data );

		if ( !player.IsValid() )
		{
			SendShopNotice( caller, "Finish respawning before buying." );
			return;
		}

		InitializeEconomyIfNeeded( data );

		var item = ZombieSurvivalShopCatalog.Find( itemId );

		if ( item is null )
		{
			SendShopNotice( caller, "Unknown shop item." );
			return;
		}

		if ( item.Type != ZombieSurvivalShopItemType.Weapon )
		{
			SendShopNotice( caller, $"{item.DisplayName} is listed, but that item type is not wired yet." );
			return;
		}

		if ( !data.ZombieSurvivalSpendCoinsHost( item.Cost ) )
		{
			SendShopNotice( caller, $"Need {item.Cost} coins." );
			return;
		}

		if ( !GiveShopItemHost( data, item ) )
		{
			data.ZombieSurvivalAddCoinsHost( item.Cost );
			SendShopNotice( caller, $"Could not give {item.DisplayName}.\nCoins refunded." );
			return;
		}

		SendShopNotice( caller, $"Bought {item.DisplayName} for {item.Cost} coins.", Color.Green );
	}

	private bool GiveShopItemHost( PlayerData data, ZombieSurvivalShopItem item )
	{
		if ( !Networking.IsHost )
			return false;

		if ( !data.IsValid() )
			return false;

		var player = FindPlayerForData( data );

		if ( !player.IsValid() )
			return false;

		if ( player.PlayerData.ZombieSurvivalRole != ZombieSurvivalRole.Human )
			return false;

		var inventory = player.GetComponent<PlayerInventory>();

		if ( !inventory.IsValid() )
			return false;

		inventory.Enabled = true;

		var prefabPath = ResolveShopPrefabPath( item );

		if ( string.IsNullOrWhiteSpace( prefabPath ) )
			return false;

		return inventory.Pickup( prefabPath, true );
	}

	private Player FindPlayerForData( PlayerData data )
	{
		if ( !data.IsValid() )
			return null;

		return (Scene?.GetAll<Player>() ?? Enumerable.Empty<Player>())
			.FirstOrDefault( x => x is not null && x.IsValid() && x.PlayerData.IsValid() && x.PlayerData == data );
	}

	private static string ResolveShopPrefabPath( ZombieSurvivalShopItem item )
	{
		if ( item is null )
			return "";

		foreach ( var candidate in GetShopPrefabPathCandidates( item ) )
		{
			var prefab = GameObject.GetPrefab( candidate );

			if ( prefab is not null )
				return candidate;
		}

		return "";
	}

	private static IEnumerable<string> GetShopPrefabPathCandidates( ZombieSurvivalShopItem item )
	{
		if ( item is null )
			yield break;

		if ( !string.IsNullOrWhiteSpace( item.ResourcePath ) )
		{
			var path = item.ResourcePath.Replace( "\\", "/" ).Trim();

			yield return path;

			if ( !path.EndsWith( ".prefab", StringComparison.OrdinalIgnoreCase ) )
				yield return $"{path}.prefab";

			var lastSlash = path.LastIndexOf( '/' );

			if ( lastSlash >= 0 )
			{
				var folder = path[..lastSlash];
				var name = path[(lastSlash + 1)..];

				if ( !string.IsNullOrWhiteSpace( folder ) && !string.IsNullOrWhiteSpace( name ) )
					yield return $"{folder}/{name.ToLowerInvariant()}.prefab";
			}
		}

		var id = item.Id?.Trim();

		if ( string.IsNullOrWhiteSpace( id ) )
			yield break;

		var normalizedId = id.ToLowerInvariant();

		yield return $"weapons/{id}/{normalizedId}.prefab";
		yield return $"weapons/{ToPascalPathSegment( id )}/{normalizedId}.prefab";
	}

	private static string ToPascalPathSegment( string value )
	{
		if ( string.IsNullOrWhiteSpace( value ) )
			return "";

		var pieces = value
			.Split( new[] { '_', '-', ' ' }, StringSplitOptions.RemoveEmptyEntries )
			.Select( x => x.Length <= 1 ? x.ToUpperInvariant() : char.ToUpperInvariant( x[0] ) + x[1..] );

		return string.Join( "", pieces );
	}

	private static void SendBuildNotice( PlayerData player, string text )
	{
		var target = player?.Connection;

		if ( target is null )
			return;

		Notices.SendNotice( target, "block", Color.Red, text, 3 );
	}

	private static void SendShopNotice( Connection target, string text )
	{
		SendShopNotice( target, text, Color.Red );
	}

	private static void SendShopNotice( Connection target, string text, Color color )
	{
		if ( target is null )
			return;

		Notices.SendNotice( target, "shopping_cart", color, text, 3 );
	}

	private static void SendZombieShopNotice( Connection target, string text )
	{
		SendZombieShopNotice( target, text, Color.Red );
	}

	private static void SendZombieShopNotice( Connection target, string text, Color color )
	{
		if ( target is null )
			return;

		Notices.SendNotice( target, "skull", color, text, 3 );
	}

	public static int GetPropSpawnCost()
	{
		return Math.Max( PropSpawnCost, 0 );
	}

	private static ZombieSurvivalForm GetDefaultZombieForm()
	{
		return ZombieSurvivalFormCatalog.FromConVarValue( DefaultZombieForm );
	}

	private static ZombieSurvivalForm GetPreferredZombieForm( PlayerData data )
	{
		if ( !data.IsValid() )
			return GetDefaultZombieForm();

		if ( data.ZombieSurvivalRequestedForm == ZombieSurvivalForm.None )
			return GetDefaultZombieForm();

		return data.ZombieSurvivalRequestedForm;
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
