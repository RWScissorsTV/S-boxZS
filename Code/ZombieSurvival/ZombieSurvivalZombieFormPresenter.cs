using Sandbox;

public sealed class ZombieSurvivalZombieFormPresenter : Component
{
	private const string VisualTag = "zs_form_visual";

	private GameObject _visual;
	private ZombieSurvivalForm _activeForm = ZombieSurvivalForm.None;
	private SkinnedModelRenderer _visualRenderer;
	private ZombieSurvivalFormAnimator _animator;

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
			SetBaseRenderersVisible( false );
			return;
		}

		// If the form changed or the old visual is invalid, rebuild it.
		DestroyVisual();

		if ( CreateVisual( player, form ) )
		{
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

		_visual = new GameObject( false, $"ZS {definition.DisplayName} Visual" );
		_visual.Tags.Add( VisualTag );
		_visual.SetParent( GameObject, false );

		_visual.LocalTransform = new Transform(
			definition.LocalPosition,
			definition.LocalAngles.ToRotation(),
			definition.ModelScale
		);

		_visualRenderer = _visual.AddComponent<SkinnedModelRenderer>();
		_visualRenderer.Model = model;
		_visualRenderer.Enabled = true;
		_visualRenderer.UseAnimGraph = true;
		_visualRenderer.CreateBoneObjects = false;

		_animator = _visual.AddComponent<ZombieSurvivalFormAnimator>();
		_animator.Renderer = _visualRenderer;
		_animator.PlayerController = player.Controller;
		_animator.WalkSpeedForFullBlend = definition.MoveSpeedForFullBlend;
		_animator.WalkSpeedForNormalPlayback = definition.MoveSpeedForFullBlend;

		_activeForm = form;

		Log.Info( $"Zombie Survival: applied zombie form '{definition.DisplayName}' using model '{definition.ModelPath}' at scale {definition.ModelScale}." );

		return true;
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

			renderer.Enabled = visible;
		}

		_baseRenderersHidden = !visible;
	}
}
