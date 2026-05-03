using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox;

public sealed record ZombieSurvivalFormDefinition
{
	public ZombieSurvivalForm Form { get; init; }
	public string DisplayName { get; init; } = "";
	public string Description { get; init; } = "";
	public int SortOrder { get; init; }

	/// <summary>
	/// Main model path used by ZombieSurvivalZombieFormPresenter.
	/// This should point to a .vmdl that exists in Assets.
	/// </summary>
	public string ModelPath { get; init; } = "";
	public string PreviewPath { get; init; } = "";

	/// <summary>
	/// Optional explicit animgraph path. Leave empty if the model already has its animgraph assigned.
	/// </summary>
	public string AnimGraphPath { get; init; } = "";

	/// <summary>
	/// Visual-only local transform applied to the mounted zombie form object.
	/// Tune these from the catalog instead of hardcoding offsets in the presenter.
	/// </summary>
	public Vector3 LocalPosition { get; init; } = Vector3.Zero;
	public Angles LocalAngles { get; init; } = Angles.Zero;
	public float ModelScale { get; init; } = 1f;

	public float Health { get; init; } = 100f;
	public float WalkSpeed { get; init; } = 260f;
	public float RunSpeed { get; init; } = 390f;

	public float MeleeDamage { get; init; } = 25f;
	public float MeleeRange { get; init; } = 90f;
	public float MeleeRadius { get; init; } = 14f;
	public float MeleeCooldown { get; init; } = 1.0f;

	/// <summary>
	/// Animator tuning. These do not force a specific animgraph, but the animator can use them.
	/// </summary>
	public float MoveSpeedForFullBlend { get; init; } = 250f;
	public float MinimumPlaybackRate { get; init; } = 0.75f;
	public float MaximumPlaybackRate { get; init; } = 1.35f;

	public string[] AttackSoundPaths { get; init; } = Array.Empty<string>();
	public string[] AmbientSoundPaths { get; init; } = Array.Empty<string>();
}

public static class ZombieSurvivalFormCatalog
{
	private static readonly ZombieSurvivalFormDefinition Walker = new()
	{
		Form = ZombieSurvivalForm.Walker,
		DisplayName = "Zombie",
		Description = "Balanced undead bruiser with solid health and reach.",
		SortOrder = 10,

		// Keep these paths matching your current imported assets.
		// If the player still becomes invisible, this is the first thing to verify in the Asset Browser.
		ModelPath = "Model/Zombie/zombie.vmdl",
		PreviewPath = "Model/Zombie/zombie.vmdl",

		// Leave empty unless you know the exact animgraph path.
		// The presenter/renderer can use the model's assigned animgraph by default.
		AnimGraphPath = "",

		// The original project used a much smaller walker presentation scale.
		ModelScale = 0.5f,

		// Tune these if the zombie appears floating/sunk/rotated after it becomes visible.
		LocalPosition = Vector3.Zero,
		LocalAngles = Angles.Zero,

		Health = 125f,
		WalkSpeed = 260f,
		RunSpeed = 390f,

		MeleeDamage = 25f,
		MeleeRange = 90f,
		MeleeRadius = 14f,
		MeleeCooldown = 1.0f,

		MoveSpeedForFullBlend = 250f,
		MinimumPlaybackRate = 0.75f,
		MaximumPlaybackRate = 1.35f,

		AttackSoundPaths = new[]
		{
			"sounds/zombieattack1.sound",
			"sounds/zombieattack2.sound"
		},

		AmbientSoundPaths = new[]
		{
			"sounds/zombiegroan1.sound",
			"sounds/zombiegroan2.sound",
			"sounds/zombiegroan3.sound",
			"sounds/zombiegroan4.sound"
		}
	};

	private static readonly ZombieSurvivalFormDefinition Headcrab = new()
	{
		Form = ZombieSurvivalForm.Headcrab,
		DisplayName = "Headcrab",
		Description = "Fast, smaller attacker with lower health and quicker strikes.",
		SortOrder = 20,

		ModelPath = "Model/hc2.vmdl",
		PreviewPath = "Model/hc2.vmdl",
		AnimGraphPath = "",

		// The original headcrab prefab root was scaled to 0.1.
		ModelScale = 0.1f,

		// Headcrab may need to be lowered/raised after visibility is confirmed.
		LocalPosition = Vector3.Zero,
		LocalAngles = Angles.Zero,

		Health = 65f,
		WalkSpeed = 290f,
		RunSpeed = 430f,

		MeleeDamage = 18f,
		MeleeRange = 68f,
		MeleeRadius = 18f,
		MeleeCooldown = 0.8f,

		MoveSpeedForFullBlend = 250f,
		MinimumPlaybackRate = 0.80f,
		MaximumPlaybackRate = 1.45f,

		AttackSoundPaths = new[]
		{
			"sounds/headcrab_attack1.sound"
		},

		AmbientSoundPaths = new[]
		{
			"sounds/headcrab_cute1.sound",
			"sounds/headcrab_cute2.sound",
			"sounds/headcrab_cute3.sound"
		}
	};

	public static ZombieSurvivalFormDefinition Get( ZombieSurvivalForm form )
	{
		return form switch
		{
			ZombieSurvivalForm.Headcrab => Headcrab,
			ZombieSurvivalForm.Walker => Walker,
			_ => Walker
		};
	}

	public static IEnumerable<ZombieSurvivalFormDefinition> All =>
		new[] { Walker, Headcrab }.OrderBy( x => x.SortOrder );

	public static ZombieSurvivalForm FromConVarValue( int value )
	{
		return Math.Clamp( value, 1, 2 ) switch
		{
			2 => ZombieSurvivalForm.Headcrab,
			_ => ZombieSurvivalForm.Walker
		};
	}

	public static bool IsValidForm( ZombieSurvivalForm form )
	{
		return form is ZombieSurvivalForm.Walker or ZombieSurvivalForm.Headcrab;
	}
}
