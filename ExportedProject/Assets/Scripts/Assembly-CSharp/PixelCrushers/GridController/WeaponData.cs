using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace PixelCrushers.GridController
{
	[CreateAssetMenu(fileName = "New Weapon", menuName = "Combat/Weapon Data")]
	public class WeaponData : ScriptableObject
	{
		[Header("Basic Info")]
		public string weaponName = "Weapon";

		public WeaponType weaponType = WeaponType.Sword;

		public Sprite weaponIcon;

		[Header("2D Animation")]
		public RuntimeAnimatorController weaponAnimatorController;

		[Header("Combat Stats")]
		public int damage = 10;

		[Tooltip("Attacks per second - determines cooldown between attacks")]
		public float attackSpeed = 1f;

		public float range = 1f;

		public float criticalChance = 0.1f;

		public float criticalMultiplier = 2f;

		[Header("Attack Properties")]
		public AttackType attackType;

		public bool canBlock;

		public float blockReduction = 0.5f;

		public bool twoHanded;

		[Tooltip("If true, melee attacks hit all enemies in range. If false, only hits the first target.")]
		[SerializeField]
		public bool isMultiHit = true;

		[Tooltip("If true, special melee attacks hit all enemies in range. If false, only hits the first target.")]
		[SerializeField]
		public bool specialAttackIsMultiHit = true;

		[Tooltip("Which hand slot this weapon occupies")]
		public HandSlot handSlot = HandSlot.RightHand;

		[Header("Heat System")]
		[SerializeField]
		public bool usesHeat = true;

		[SerializeField]
		public float heatPerShot = 5f;

		[SerializeField]
		public float heatMultiplier = 1f;

		[SerializeField]
		public bool overheatsQuickly;

		[Header("Projectile Settings")]
		[SerializeField]
		public bool isProjectileWeapon;

		[SerializeField]
		public ProjectileData projectileData;

		[SerializeField]
		public int projectileCount = 1;

		[SerializeField]
		public float projectileSpread;

		[Header("Firepoint Settings (Assign in DuelWieldingSystem Inspector)")]
		[Tooltip("Per-weapon firepoint overrides - not currently used. Assign firepoints in DuelWieldingSystem component")]
		[SerializeField]
		public Transform leftHandFirePoint;

		[SerializeField]
		public Transform rightHandFirePoint;

		[Header("Special Attack Projectiles")]
		[SerializeField]
		public bool specialAttackUsesProjectiles;

		[SerializeField]
		public ProjectileData specialProjectileData;

		[SerializeField]
		public int specialProjectileCount = 1;

		[SerializeField]
		public float specialProjectileSpread;

		[Header("Attack Stages (rotating multi-stage attacks)")]
		[Tooltip("If true, each attack advances through the attackStages list (one stage per attack), instead of always firing the regular projectile.")]
		[SerializeField]
		public bool usesAttackStages;

		[SerializeField]
		public List<WeaponAttackStage> attackStages = new List<WeaponAttackStage>();

		[Header("Special Attack Settings")]
		[Tooltip("Custom damage for special attack. 0 = use base weapon damage")]
		public int specialAttackDamage;

		[Tooltip("Heat cost specifically for special attacks. 0 = use normal heat calculation")]
		public float specialAttackHeatCost;

		[Tooltip("When true, only special attacks generate/check heat. Normal attacks are heat-free")]
		public bool heatOnSpecialOnly;

		[Header("Audio")]
		public AudioClip swingSound;

		public AudioClip hitSound;

		public AudioClip blockSound;

		[SerializeField]
		public AudioClip overheatedSound;

		[Header("Two-Handed Audio")]
		public AudioClip twoHandedSwingSound;

		public AudioClip twoHandedHitSound;

		[Header("Special Attack Audio")]
		public AudioClip specialAttackSound;

		public AudioClip specialAttackHitSound;

		[Header("2D Effects")]
		public GameObject hitEffect2D;

		public Sprite[] attackEffectSprites;

		[SerializeField]
		public GameObject muzzleEffect;

		[Header("Validation")]
		[SerializeField]
		public bool showValidationWarnings = true;

		public bool IsProjectileWeapon => isProjectileWeapon;

		public ProjectileData ProjectileData => projectileData;

		public int ProjectileCount => projectileCount;

		public float ProjectileSpread => projectileSpread;

		public GameObject MuzzleEffect => muzzleEffect;

		public bool SpecialAttackUsesProjectiles => specialAttackUsesProjectiles;

		public ProjectileData SpecialProjectileData => specialProjectileData;

		public int SpecialProjectileCount => specialProjectileCount;

		public float SpecialProjectileSpread => specialProjectileSpread;

		public bool UsesAttackStages => usesAttackStages;

		public List<WeaponAttackStage> AttackStages => attackStages;

		public int AttackStageCount
		{
			get
			{
				if (attackStages == null)
				{
					return 0;
				}
				return attackStages.Count;
			}
		}

		public int SpecialAttackDamage => specialAttackDamage;

		public float SpecialAttackHeatCost => specialAttackHeatCost;

		public bool HeatOnSpecialOnly => heatOnSpecialOnly;

		public bool IsMultiHit => isMultiHit;

		public bool SpecialAttackIsMultiHit => specialAttackIsMultiHit;

		public bool UsesHeat => usesHeat;

		public float HeatPerShot => heatPerShot;

		public float HeatMultiplier => heatMultiplier;

		public bool OverheatsQuickly => overheatsQuickly;

		public AudioClip OverheatedSound => overheatedSound;

		public WeaponAttackStage GetStage(int index)
		{
			if (attackStages == null || attackStages.Count == 0)
			{
				return null;
			}
			int index2 = (index % attackStages.Count + attackStages.Count) % attackStages.Count;
			return attackStages[index2];
		}

		private void OnValidate()
		{
			if (showValidationWarnings && usesHeat)
			{
				if (heatPerShot < 0f)
				{
					heatPerShot = 0f;
				}
				if (heatMultiplier <= 0f)
				{
					heatMultiplier = 1f;
				}
			}
		}

		public float GetAttackCooldown()
		{
			if (!(attackSpeed > 0f))
			{
				return 1f;
			}
			return 1f / attackSpeed;
		}

		public float GetEffectiveRange()
		{
			if (isProjectileWeapon && projectileData != null)
			{
				return Mathf.Min(range, projectileData.maxRange);
			}
			return range;
		}

		public float GetHeatCost()
		{
			if (!usesHeat)
			{
				return 0f;
			}
			float num = heatPerShot * heatMultiplier;
			if (isProjectileWeapon && projectileCount > 1)
			{
				num *= (float)projectileCount;
			}
			return num;
		}

		public float GetHeatCostWithRapidFire(float rapidFireMultiplier = 1f)
		{
			if (!usesHeat)
			{
				return 0f;
			}
			float num = GetHeatCost();
			if (overheatsQuickly && rapidFireMultiplier > 1f)
			{
				num *= rapidFireMultiplier;
			}
			return num;
		}

		public bool IsHighHeatWeapon()
		{
			if (!usesHeat)
			{
				return false;
			}
			if (!(GetHeatCost() >= 10f))
			{
				return overheatsQuickly;
			}
			return true;
		}

		public string GetHeatDescription()
		{
			if (!usesHeat)
			{
				return "No Heat";
			}
			float heatCost = GetHeatCost();
			if (heatCost <= 2f)
			{
				return "Low Heat";
			}
			if (heatCost <= 5f)
			{
				return "Moderate Heat";
			}
			if (heatCost <= 10f)
			{
				return "High Heat";
			}
			return "Very High Heat";
		}

		public WeaponData CreateVariant(string variantName, Action<WeaponData> modifications = null)
		{
			WeaponData weaponData = UnityEngine.Object.Instantiate(this);
			weaponData.weaponName = variantName;
			modifications?.Invoke(weaponData);
			return weaponData;
		}

		public WeaponData CreateHighHeatVariant(string variantName, float heatIncrease = 2f, float damageIncrease = 1.5f)
		{
			return CreateVariant(variantName, (WeaponData variant) =>
			{
				variant.heatPerShot = heatPerShot * heatIncrease;
				variant.damage = Mathf.RoundToInt((float)damage * damageIncrease);
				variant.overheatsQuickly = true;
			});
		}

		public WeaponData CreateLowHeatVariant(string variantName, float heatReduction = 0.5f, float damageReduction = 0.8f)
		{
			return CreateVariant(variantName, (WeaponData variant) =>
			{
				variant.heatPerShot = heatPerShot * heatReduction;
				variant.damage = Mathf.RoundToInt((float)damage * damageReduction);
				variant.overheatsQuickly = false;
			});
		}

		public string GetWeaponStats()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine($"Damage: {damage}");
			stringBuilder.AppendLine($"Attack Speed: {attackSpeed:F1}/sec");
			stringBuilder.AppendLine($"Range: {range:F1}m");
			if (criticalChance > 0f)
			{
				stringBuilder.AppendLine($"Critical: {criticalChance:P0} ({criticalMultiplier:F1}x)");
			}
			if (usesHeat)
			{
				stringBuilder.AppendLine($"Heat: {GetHeatDescription()} ({GetHeatCost():F1})");
			}
			else
			{
				stringBuilder.AppendLine("Heat: None");
			}
			if (isProjectileWeapon)
			{
				stringBuilder.AppendLine("Type: Projectile");
				if (projectileCount > 1)
				{
					stringBuilder.AppendLine($"Projectiles: {projectileCount}");
				}
			}
			if (canBlock)
			{
				stringBuilder.AppendLine($"Block: {blockReduction:P0} reduction");
			}
			if (twoHanded)
			{
				stringBuilder.AppendLine("Two-Handed");
			}
			return stringBuilder.ToString().TrimEnd();
		}

		public string GetHeatWarning()
		{
			if (!usesHeat)
			{
				return "";
			}
			if (IsHighHeatWeapon())
			{
				if (overheatsQuickly)
				{
					return "⚠\ufe0f Rapid Heat Build-up";
				}
				return "⚠\ufe0f High Heat Generation";
			}
			return "";
		}
	}
}
