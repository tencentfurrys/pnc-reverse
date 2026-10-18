using System;
using UnityEngine;

namespace PixelCrushers.GridController;

[Serializable]
public class WeaponAttackStage
{
	public WeaponAttackStageType stageType;

	public ProjectileData projectileData;

	public int projectileCount = 1;

	public float projectileSpread;

	[Tooltip("If true, this stage's projectile always critically hits (at the weapon's crit multiplier). On a heat-drain stage this means it always cools the crit-boosted amount.")]
	public bool alwaysCrit;

	public WeaponAttackStageType StageType => stageType;

	public ProjectileData ProjectileData => projectileData;

	public int ProjectileCount => projectileCount;

	public float ProjectileSpread => projectileSpread;

	public bool AlwaysCrit => alwaysCrit;
}
