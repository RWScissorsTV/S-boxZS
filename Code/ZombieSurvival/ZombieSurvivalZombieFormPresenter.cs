public sealed class ZombieSurvivalZombieFormPresenter : Component
{
	private const string VisualTag = "zs_form_visual";

	private GameObject _visual;
	private ZombieSurvivalForm _activeForm;
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
		if ( !player.IsValid() || !player.PlayerData.IsValid() || player.PlayerData.ZombieSurvivalRole != ZombieSurvivalRole.Zombie )
		{
			DestroyVisual();
			SetBaseRenderersVisible( true );
			return;
		}

		var form = player.PlayerData.ZombieSurvivalForm == ZombieSurvivalForm.None
			? ZombieSurvivalForm.Walker
			: player.PlayerData.ZombieSurvivalForm;

		SetBaseRenderersVisible( false );

		if ( _visual.IsValid() && _activeForm == form )
			return;

		DestroyVisual();
		CreateVisual( player, form );
	}

	private void CreateVisual( Player player, ZombieSurvivalForm form )
	{
		var definition = ZombieSurvivalFormCatalog.Get( form );
		var model = Model.Load( definition.ModelPath );
		if ( model is null || model.IsError )
		{
			Log.Warning( $"Zombie Survival: failed to load zombie form model '{definition.ModelPath}'." );
			SetBaseRenderersVisible( true );
			return;
		}

		_visual = new GameObject( false, $"ZS {definition.DisplayName} Visual" );
		_visual.Tags.Add( VisualTag );
		_visual.SetParent( GameObject, false );
		_visual.LocalTransform = new Transform( Vector3.Zero, Rotation.Identity, definition.ModelScale );

		_visualRenderer = _visual.AddComponent<SkinnedModelRenderer>();
		_visualRenderer.Model = model;
		_visualRenderer.UseAnimGraph = true;
		_visualRenderer.CreateBoneObjects = false;

		_animator = _visual.AddComponent<ZombieSurvivalFormAnimator>();
		_animator.Renderer = _visualRenderer;
		_animator.PlayerController = player.Controller;
		_animator.WalkSpeedForFullBlend = form == ZombieSurvivalForm.Headcrab ? 120f : 180f;
		_animator.WalkSpeedForNormalPlayback = form == ZombieSurvivalForm.Headcrab ? 120f : 180f;

		_activeForm = form;
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
			if ( renderer.GameObject.Tags.Has( VisualTag ) )
				continue;

			renderer.Enabled = visible;
		}

		_baseRenderersHidden = !visible;
	}
}
