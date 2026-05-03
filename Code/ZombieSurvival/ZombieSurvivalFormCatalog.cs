using System;

public sealed record ZombieSurvivalFormDefinition(
	ZombieSurvivalForm Form,
	string DisplayName,
	string ModelPath,
	float ModelScale,
	float Health,
	float WalkSpeed,
	float RunSpeed,
	float MeleeDamage,
	float MeleeRange,
	float MeleeRadius,
	float MeleeCooldown,
	string[] AttackSoundPaths,
	string[] AmbientSoundPaths );

public static class ZombieSurvivalFormCatalog
{
	private static readonly ZombieSurvivalFormDefinition Walker = new(
		ZombieSurvivalForm.Walker,
		"Zombie",
		"Model/Zombie/zombie.vmdl",
		0.3f,
		125f,
		260f,
		390f,
		25f,
		90f,
		14f,
		1.0f,
		[
			"sounds/zombieattack1.sound",
			"sounds/zombieattack2.sound"
		],
		[
			"sounds/zombiegroan1.sound",
			"sounds/zombiegroan2.sound",
			"sounds/zombiegroan3.sound",
			"sounds/zombiegroan4.sound"
		] );

	private static readonly ZombieSurvivalFormDefinition Headcrab = new(
		ZombieSurvivalForm.Headcrab,
		"Headcrab",
		"Model/hc2.vmdl",
		0.1f,
		65f,
		290f,
		430f,
		18f,
		68f,
		18f,
		0.8f,
		[
			"sounds/headcrab_attack1.sound"
		],
		[
			"sounds/headcrab_cute1.sound",
			"sounds/headcrab_cute2.sound",
			"sounds/headcrab_cute3.sound"
		] );

	public static ZombieSurvivalFormDefinition Get( ZombieSurvivalForm form )
	{
		return form switch
		{
			ZombieSurvivalForm.Headcrab => Headcrab,
			_ => Walker
		};
	}

	public static ZombieSurvivalForm FromConVarValue( int value )
	{
		return Math.Clamp( value, 1, 2 ) switch
		{
			2 => ZombieSurvivalForm.Headcrab,
			_ => ZombieSurvivalForm.Walker
		};
	}
}
