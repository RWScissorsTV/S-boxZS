using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox;

public sealed class ZombieSurvivalPropCarrySystem : GameObjectSystem
{
	private const float MinCarryDistance = 64f;
	private const float MaxCarryDistance = 220f;
	private const float CarryMoveStep = 20f;

	private static ZombieSurvivalPropCarrySystem Current { get; set; }

	private readonly Dictionary<Connection, GameObject> _hostCarriedProps = new();

	private ZombieSurvivalBarricade _localBarricade;
	private GameObject _localProp;
	private float _localDistance;
	private Rotation _localRotationOffset = Rotation.Identity;
	private bool _ignoreUseUntilReleased;
	private RealTimeSince _timeSinceUseHandled;
	private float _lastCarryYaw;
	private float _lastCarryPitch;

	public ZombieSurvivalPropCarrySystem( Scene scene ) : base( scene )
	{
		Current = this;
		Listen( Stage.StartUpdate, 60, Tick, "ZombieSurvivalPropCarrySystem" );
	}

	public static bool IsLocalCarrying( GameObject prop )
	{
		return Current?._localProp == prop;
	}

	public static void ToggleCarryLocal( ZombieSurvivalBarricade barricade, GameObject source )
	{
		Current?.ToggleLocal( barricade, source );
	}

	public static void ToggleCarryHost( Connection caller, GameObject prop )
	{
		Current?.ToggleHost( caller, prop );
	}

	private void Tick()
	{
		CleanupHostCarries();
		TryStartLocalCarryFromUse();
		UpdateLocalCarry();
	}

	private void ToggleLocal( ZombieSurvivalBarricade barricade, GameObject source )
	{
		if ( _timeSinceUseHandled < 0.05f )
			return;

		if ( !barricade.IsValid() || !source.IsValid() )
			return;

		var player = GetSourcePlayer( source );
		if ( !player.IsValid() )
			player = Player.FindLocalPlayer();

		if ( !CanHumanCarry( player ) || !CanCarryBarricade( barricade ) )
			return;

		if ( _localProp.IsValid() )
		{
			DropLocal();
			return;
		}

		var prop = barricade.GameObject;
		_localBarricade = barricade;
		_localProp = prop;
		_localDistance = Math.Clamp( player.EyeTransform.Position.Distance( prop.WorldPosition ), MinCarryDistance, MaxCarryDistance );
		_localRotationOffset = Rotation.FromYaw( player.Controller.EyeAngles.yaw ).Inverse * prop.WorldRotation;
		_lastCarryYaw = player.Controller.EyeAngles.yaw;
		_lastCarryPitch = player.Controller.EyeAngles.pitch;
		_ignoreUseUntilReleased = true;
		_timeSinceUseHandled = 0;
	}

	private void TryStartLocalCarryFromUse()
	{
		if ( _ignoreUseUntilReleased && !Input.Down( "use" ) )
			_ignoreUseUntilReleased = false;

		if ( _localProp.IsValid() || _ignoreUseUntilReleased || _timeSinceUseHandled < 0.05f )
			return;

		if ( !Input.Pressed( "use" ) )
			return;

		var player = Player.FindLocalPlayer();
		if ( !CanHumanCarry( player ) )
			return;

		var trace = Scene.Trace.Ray( player.EyeTransform.ForwardRay, MaxCarryDistance )
			.IgnoreGameObjectHierarchy( player.GameObject )
			.WithoutTags( "player" )
			.Run();

		if ( !trace.GameObject.IsValid() )
			return;

		var barricade = trace.GameObject.GetComponentInParent<ZombieSurvivalBarricade>( true )
			?? trace.GameObject.Root.GetComponent<ZombieSurvivalBarricade>();

		if ( !CanCarryBarricade( barricade ) )
			return;

		ToggleLocal( barricade, player.GameObject );
		RequestToggleCarry( barricade.GameObject );
		Input.Clear( "use" );
	}

	private void UpdateLocalCarry()
	{
		if ( !_localProp.IsValid() || !_localBarricade.IsValid() )
			return;

		var player = Player.FindLocalPlayer();
		if ( !CanHumanCarry( player ) || !CanCarryBarricade( _localBarricade ) )
		{
			DropLocal();
			return;
		}

		if ( _ignoreUseUntilReleased && !Input.Down( "use" ) )
			_ignoreUseUntilReleased = false;

		if ( !_ignoreUseUntilReleased && Input.Pressed( "use" ) )
		{
			DropLocal();
			Input.Clear( "use" );
			_timeSinceUseHandled = 0;
			return;
		}

		if ( !Input.MouseWheel.IsNearZeroLength )
		{
			_localDistance += Input.MouseWheel.y * CarryMoveStep;
			_localDistance = Math.Clamp( _localDistance, MinCarryDistance, MaxCarryDistance );
			Input.MouseWheel = default;
		}

		if ( Input.Down( "walk" ) )
		{
			var yawDelta = player.Controller.EyeAngles.yaw - _lastCarryYaw;
			var pitchDelta = player.Controller.EyeAngles.pitch - _lastCarryPitch;
			_localRotationOffset = Rotation.From( new Angles( -pitchDelta, -yawDelta, 0f ) ) * _localRotationOffset;
			Input.Clear( "walk" );
		}

		_lastCarryYaw = player.Controller.EyeAngles.yaw;
		_lastCarryPitch = player.Controller.EyeAngles.pitch;

		var eye = player.EyeTransform;
		var rotation = Rotation.FromYaw( player.Controller.EyeAngles.yaw ) * _localRotationOffset;
		var transform = new Transform( eye.Position + eye.Rotation.Forward * _localDistance, rotation, _localProp.WorldScale );

		MoveCarry( _localProp, transform );
	}

	private void DropLocal()
	{
		if ( !_localProp.IsValid() )
			return;

		DropCarry( _localProp );
		_localBarricade = null;
		_localProp = null;
		_ignoreUseUntilReleased = true;
	}

	private void ToggleHost( Connection caller, GameObject prop )
	{
		if ( !Networking.IsHost )
			return;

		var player = Player.FindForConnection( caller );
		if ( !CanHumanCarry( player ) || !prop.IsValid() )
			return;

		var owner = player.Network.Owner;
		if ( owner is null )
			return;

		if ( _hostCarriedProps.TryGetValue( owner, out var current ) && current.IsValid() )
		{
			DropHost( owner, current );

			if ( current == prop )
				return;
		}

		var barricade = prop.GetComponent<ZombieSurvivalBarricade>();
		if ( !CanCarryBarricade( barricade ) )
			return;

		var body = prop.GetComponent<Rigidbody>();
		if ( body.IsValid() )
		{
			body.MotionEnabled = false;
			body.Velocity = Vector3.Zero;
			body.AngularVelocity = Vector3.Zero;
		}

		_hostCarriedProps[owner] = prop;
	}

	[Rpc.Host]
	private static void MoveCarry( GameObject prop, Transform transform )
	{
		Current?.MoveHost( Rpc.Caller, prop, transform );
	}

	[Rpc.Host]
	private static void RequestToggleCarry( GameObject prop )
	{
		Current?.ToggleHost( Rpc.Caller, prop );
	}

	private void MoveHost( Connection caller, GameObject prop, Transform transform )
	{
		if ( !Networking.IsHost || caller is null || !prop.IsValid() )
			return;

		if ( !_hostCarriedProps.TryGetValue( caller, out var carried ) || carried != prop )
			return;

		var player = Player.FindForConnection( caller );
		if ( !CanHumanCarry( player ) )
		{
			DropHost( caller, prop );
			return;
		}

		prop.WorldTransform = transform;

		var body = prop.GetComponent<Rigidbody>();
		if ( body.IsValid() )
		{
			body.MotionEnabled = false;
			body.Velocity = Vector3.Zero;
			body.AngularVelocity = Vector3.Zero;
		}
	}

	[Rpc.Host]
	private static void DropCarry( GameObject prop )
	{
		Current?.DropHost( Rpc.Caller, prop );
	}

	private void DropHost( Connection caller, GameObject prop )
	{
		if ( !Networking.IsHost || caller is null )
			return;

		if ( !_hostCarriedProps.TryGetValue( caller, out var carried ) || carried != prop )
			return;

		_hostCarriedProps.Remove( caller );

		var body = prop.GetComponent<Rigidbody>();
		if ( body.IsValid() && !IsNailed( prop ) )
		{
			body.MotionEnabled = true;
			body.Velocity = Vector3.Zero;
			body.AngularVelocity = Vector3.Zero;
		}
	}

	private void CleanupHostCarries()
	{
		if ( !Networking.IsHost || _hostCarriedProps.Count == 0 )
			return;

		foreach ( var (connection, prop) in _hostCarriedProps.ToArray() )
		{
			var player = Player.FindForConnection( connection );
			if ( prop.IsValid() && CanHumanCarry( player ) )
				continue;

			if ( prop.IsValid() )
			{
				var body = prop.GetComponent<Rigidbody>();
				if ( body.IsValid() && !IsNailed( prop ) )
					body.MotionEnabled = true;
			}

			_hostCarriedProps.Remove( connection );
		}
	}

	private static bool CanHumanCarry( Player player )
	{
		if ( !player.IsValid() || !player.PlayerData.IsValid() )
			return false;

		var game = ZombieSurvivalGame.Current;
		if ( game is null || !ZombieSurvivalGame.Enabled )
			return false;

		if ( game.Phase != ZombieSurvivalPhase.Build && game.Phase != ZombieSurvivalPhase.Survival )
			return false;

		return player.PlayerData.ZombieSurvivalAlive
			&& player.PlayerData.ZombieSurvivalRole == ZombieSurvivalRole.Human;
	}

	private static bool CanCarryBarricade( ZombieSurvivalBarricade barricade )
	{
		return barricade.IsValid()
			&& !barricade.IsBroken
			&& barricade.NailCount <= 0;
	}

	private static bool IsNailed( GameObject prop )
	{
		var barricade = prop.GetComponent<ZombieSurvivalBarricade>();
		return barricade.IsValid() && barricade.NailCount > 0;
	}

	private static Player GetSourcePlayer( GameObject source )
	{
		if ( !source.IsValid() )
			return null;

		return source.GetComponentInParent<Player>( true )
			?? source.Root.GetComponent<Player>();
	}
}
