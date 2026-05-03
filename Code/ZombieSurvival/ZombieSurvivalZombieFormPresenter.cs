using System;
using System.Collections.Generic;
using Sandbox;

public sealed class ZombieSurvivalZombieFormPresenter : Component
{
	private const string VisualTag = "zs_form_visual";

	private GameObject _visual;
	private ZombieSurvivalForm _activeForm = ZombieSurvivalForm.None;
	private SkinnedModelRenderer _visualRenderer;
	private ZombieSurvivalFormAnimator _animator;
	private readonly Dictionary<SkinnedModelRenderer, Color> _baseRendererTints = new();

	private bool _baseRenderersHidden;

	protected override void OnUpdate()
	{
		UpdateForm();
	}

	protected override void OnDisabled()
	{
		DestroyVisual();
		SetBaseRenderersVisible( true );
	}

	protected override void OnDestroy()
	{
		DestroyVisual();
		SetBaseRenderersVisible( true );
	}

	public void ApplyNow()
	{
		UpdateForm();
	}

	public void TriggerAttack()
	{
		if ( _animator.IsValid() )
			_animator.TriggerAttack1();
	}

	private void UpdateForm()
	{
		var player = GetComponent<Player>();

		if ( !player.IsValid() || !player.PlayerData.IsValid() )
		{
			DestroyVisual();
			SetBaseRenderersVisible( true );
			return;
		}

		if ( player.PlayerData.ZombieSurvivalRole != ZombieSurvivalRole.Zombie )
		{
			DestroyVisual();
			SetBaseRenderersVisible( true );
			return;
		}

		var form = player.PlayerData.ZombieSurvivalForm == ZombieSurvivalForm.None
			? ZombieSurvivalForm.Walker
			: player.PlayerData.ZombieSurvivalForm;

		// If the visual already exists and is valid, make sure the base body is hidden.
		if ( _visual.IsValid() && _visualRenderer.IsValid() && _activeForm == form )
		{
			SyncVisualTransform( player, form );
			UpdateViewVisibility( player );
			SetBaseRenderersVisible( false );
			return;
		}

		// If the form changed or the old visual is invalid, rebuild it.
		DestroyVisual();

		if ( CreateVisual( player, form ) )
		{
			SyncVisualTransform( player, form );
			UpdateViewVisibility( player );

			// Only hide the base Citizen body after the replacement visual is actually valid.
			SetBaseRenderersVisible( false );
			return;
		}

		// If anything failed, keep the normal player body visible.
		SetBaseRenderersVisible( true );
	}

	private bool CreateVisual( Player player, ZombieSurvivalForm form )
	{
		var definition = ZombieSurvivalFormCatalog.Get( form );

		if ( string.IsNullOrWhiteSpace( definition.ModelPath ) )
		{
			Log.Warning( $"Zombie Survival: zombie form '{definition.DisplayName}' has no model path." );
			return false;
		}

		var model = Model.Load( definition.ModelPath );

		if ( model is null || model.IsError )
		{
			Log.Warning( $"Zombie Survival: failed to load zombie form model '{definition.ModelPath}' for '{definition.DisplayName}'." );
			return false;
		}

		var parentObject = GetVisualParent( player );

		_visual = new GameObject( true, $"ZS {definition.DisplayName} Visual" );
		_visual.Tags.Add( VisualTag );
		_visual.Flags |= GameObjectFlags.NotSaved | GameObjectFlags.NotNetworked;
		_visual.SetParent( parentObject, false );
		_visual.Enabled = true;

		_visualRenderer = _visual.AddComponent<SkinnedModelRenderer>();
		_visualRenderer.Model = model;
		_visualRenderer.Enabled = true;
		_visualRenderer.UseAnimGraph = true;
		_visualRenderer.CreateBoneObjects = true;

		_animator = _visual.AddComponent<ZombieSurvivalFormAnimator>();
		_animator.Renderer = _visualRenderer;
		_animator.PlayerController = player.Controller;
		_animator.WalkSpeedForFullBlend = definition.MoveSpeedForFullBlend;
		_animator.WalkSpeedForNormalPlayback = definition.MoveSpeedForFullBlend;

		_activeForm = form;

		Log.Info( $"Zombie Survival: applied zombie form '{definition.DisplayName}' using model '{definition.ModelPath}' at scale {definition.ModelScale}." );

		return true;
	}

	private void SyncVisualTransform( Player player, ZombieSurvivalForm form )
	{
		if ( !_visual.IsValid() )
			return;

		var definition = ZombieSurvivalFormCatalog.Get( form );
		var parentObject = GetVisualParent( player );

		if ( parentObject.IsValid() && _visual.Parent != parentObject )
			_visual.SetParent( parentObject, false );

		_visual.LocalTransform = new Transform(
			definition.LocalPosition,
			definition.LocalAngles.ToRotation(),
			GetCompensatedScale( parentObject, definition.ModelScale )
		);
	}

	private void UpdateViewVisibility( Player player )
	{
		if ( !_visualRenderer.IsValid() )
			return;

		var hideForFirstPerson = player.IsLocalPlayer
			&& player.Controller.IsValid()
			&& !player.Controller.ThirdPerson;

		_visualRenderer.Enabled = !hideForFirstPerson;
	}

	private static GameObject GetVisualParent( Player player )
	{
		if ( player?.Controller?.Renderer?.GameObject.IsValid() ?? false )
			return player.Controller.Renderer.GameObject;

		if ( player?.Body.IsValid() ?? false )
			return player.Body;

		return player?.GameObject;
	}

	private static Vector3 GetCompensatedScale( GameObject parentObject, float desiredWorldScale )
	{
		var parentScale = parentObject.IsValid() ? parentObject.WorldScale : Vector3.One;

		return new Vector3(
			desiredWorldScale / SafeScaleAxis( parentScale.x ),
			desiredWorldScale / SafeScaleAxis( parentScale.y ),
			desiredWorldScale / SafeScaleAxis( parentScale.z )
		);
	}

	private static float SafeScaleAxis( float value )
	{
		if ( MathF.Abs( value ) < 0.0001f )
			return 1f;

		return value;
	}

	private void DestroyVisual()
	{
		if ( _visual.IsValid() )
			_visual.Destroy();

		_visual = null;
		_visualRenderer = null;
		_animator = null;
		_activeForm = ZombieSurvivalForm.None;
	}

	private void SetBaseRenderersVisible( bool visible )
	{
		if ( _baseRenderersHidden == !visible )
			return;

		foreach ( var renderer in GameObject.GetComponentsInChildren<SkinnedModelRenderer>( true ) )
		{
			if ( !renderer.IsValid() )
				continue;

			if ( renderer.GameObject.Tags.Has( VisualTag ) )
				continue;

			if ( visible )
			{
				if ( _baseRendererTints.TryGetValue( renderer, out var tint ) )
				{
					renderer.Tint = tint;
				}
				else
				{
					renderer.Tint = Color.White;
				}
			}
			else
			{
				if ( !_baseRendererTints.ContainsKey( renderer ) )
					_baseRendererTints[renderer] = renderer.Tint;

				var tint = renderer.Tint;
				tint.a = 0f;
				renderer.Tint = tint;
			}
		}

		_baseRenderersHidden = !visible;
	}
}
