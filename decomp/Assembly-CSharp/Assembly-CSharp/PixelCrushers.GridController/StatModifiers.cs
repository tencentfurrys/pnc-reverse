using System;

namespace PixelCrushers.GridController;

[Serializable]
public class StatModifiers
{
	public float attackMultiplier = 1f;

	public float defenseMultiplier = 1f;

	public float movementSpeedMultiplier = 1f;

	public float attackSpeedMultiplier = 1f;

	public float criticalDamageMultiplier = 1f;

	public int bonusAttackDamage;

	public int bonusDefense;

	public float criticalChanceBonus;

	public int maxHealthBonus;

	public float heatCapacityBonus;

	public float healthRegenPerSecond;

	public float healthDrainPerSecond;

	public float heatReductionPerSecond;

	public float heatGenerationPerSecond;

	public float instantHeatReduction;
}
