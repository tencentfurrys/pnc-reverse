using System.Collections.Generic;
using UnityEngine;

namespace PixelCrushers.GridController;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class InventoryItem : ScriptableObject
{
	[Header("Basic Info")]
	public string itemName = "Item";

	[TextArea(3, 5)]
	public string itemDescription = "A basic item";

	public Sprite itemIcon;

	public ItemType itemType = ItemType.Consumable;

	public ItemRarity rarity;

	[Header("Shop Settings")]
	[Tooltip("Price in currency. Set to 0 if item is not for sale.")]
	public int price;

	[Header("Unlock Settings")]
	[Tooltip("If true, this item starts locked and must be unlocked before appearing in chest pools.")]
	public bool startsLocked;

	[Tooltip("Text shown in the unlock screen describing how to unlock this item.")]
	[TextArea(2, 3)]
	public string unlockConditionText = "";

	[Header("Spawn Limit")]
	[Tooltip("Max times this item can appear per run across all pools. 0 = unlimited")]
	public int maxTimesPerRun;

	[Header("Equipment Data")]
	public WeaponData weaponData;

	public ArmorData armorData;

	public TrinketData trinketData;

	[Header("Consumable Data")]
	public int healAmount;

	public float duration;

	public EffectType effectType;

	[Header("Buff/Debuff Configuration")]
	[Tooltip("The type of status effect this consumable applies")]
	public StatusEffectType statusEffectType = StatusEffectType.AttackBoost;

	[Tooltip("Is this effect negative (debuff) or positive (buff)?")]
	public bool isDebuff;

	[Tooltip("Can this effect stack with itself?")]
	public bool canStack;

	[Tooltip("If true, reapplying this buff refreshes the timer instead of stacking")]
	public bool refreshDurationOnReapply;

	[Tooltip("Maximum number of stacks if stackable")]
	public int maxStacks = 3;

	[Header("Stat Modifiers")]
	[Tooltip("Multiplies attack damage (1.0 = no change, 1.5 = 50% increase)")]
	public float attackMultiplier = 1f;

	[Tooltip("Adds flat bonus attack damage")]
	public int bonusAttackDamage;

	[Tooltip("Multiplies defense (1.0 = no change, 1.5 = 50% increase)")]
	public float defenseMultiplier = 1f;

	[Tooltip("Adds flat bonus defense")]
	public int bonusDefense;

	[Tooltip("Multiplies movement speed (1.0 = no change, 1.3 = 30% faster)")]
	public float movementSpeedMultiplier = 1f;

	[Tooltip("Multiplies attack speed (1.0 = no change, 2.0 = twice as fast)")]
	public float attackSpeedMultiplier = 1f;

	[Tooltip("Adds to critical hit chance (0.1 = 10% increased crit chance)")]
	public float criticalChanceBonus;

	[Tooltip("Multiplies critical damage (1.0 = no change, 1.5 = 50% more crit damage)")]
	public float criticalDamageMultiplier = 1f;

	[Header("Over-Time Effects (ACCUMULATED METHOD ONLY)")]
	[Tooltip("Health regenerated per second (uses accumulated method)")]
	public float healthRegenPerSecond;

	[Tooltip("Health drained per second (for poison/bleed effects)")]
	public float healthDrainPerSecond;

	[Tooltip("Heat reduced per second")]
	public float heatReductionPerSecond;

	[Tooltip("Heat generated per second")]
	public float heatGenerationPerSecond;

	[Header("Instant Effects")]
	[Tooltip("Instant health gain/loss on consumption (converted to fast accumulated healing)")]
	public int instantHealthChange;

	[Tooltip("Instant heat reduction on consumption")]
	public float instantHeatReduction;

	[Tooltip("Instant shield/barrier amount")]
	public int shieldAmount;

	[Header("Max Stat Bonuses")]
	[Tooltip("Increases maximum health capacity")]
	public int maxHealthBonus;

	[Tooltip("Increases maximum heat capacity")]
	public float maxHeatCapacityBonus;

	[Tooltip("Is this a permanent upgrade? If true, max stat bonuses persist forever. If false, they're temporary buffs.")]
	public bool isPermanentUpgrade;

	[Header("Special Properties")]
	[Tooltip("Does this effect prevent movement?")]
	public bool preventsMovement;

	[Tooltip("Does this effect prevent actions/attacks?")]
	public bool preventsActions;

	[Tooltip("Status effect types this grants immunity to")]
	public List<StatusEffectType> grantsImmunityTo = new List<StatusEffectType>();

	[Tooltip("Does this remove all debuffs when consumed?")]
	public bool removesAllDebuffs;

	[Tooltip("Does this remove specific debuff types?")]
	public List<StatusEffectType> removesDebuffTypes = new List<StatusEffectType>();

	[Header("Buff Screen Effect")]
	[Tooltip("Material applied to the full-screen pass while this consumable's buff is active")]
	public Material buffScreenMaterial;

	[Header("Visual/Audio")]
	public GameObject worldModel;

	public Material itemMaterial;

	public GameObject consumeEffectPrefab;

	[Header("Item Sounds")]
	[Tooltip("Sound played when this item is consumed")]
	public AudioClip consumeSound;

	[Tooltip("Sound played when this item is equipped")]
	public AudioClip equipSound;

	[Tooltip("Sound played when this item is picked up from or dropped into the world")]
	public AudioClip pickupDropSound;

	[Tooltip("Sound played when this item is moved within the inventory grid")]
	public AudioClip inventoryMoveSound;

	public bool IsWeapon => weaponData != null;

	public bool IsArmor => armorData != null;

	public bool IsTrinket => trinketData != null;

	public bool IsConsumable => itemType == ItemType.Consumable;

	public bool HasBuffEffect
	{
		get
		{
			if (IsConsumable)
			{
				if (effectType != EffectType.Buff)
				{
					return effectType == EffectType.Temporary;
				}
				return true;
			}
			return false;
		}
	}

	public bool HasDebuffEffect
	{
		get
		{
			if (IsConsumable)
			{
				return effectType == EffectType.Debuff;
			}
			return false;
		}
	}

	private void OnValidate()
	{
		if (!string.IsNullOrEmpty(itemName) && name != itemName)
		{
			name = itemName;
		}
		if (!IsConsumable)
		{
			return;
		}
		switch (effectType)
		{
		case EffectType.Heal:
			if (healAmount > 0 && instantHealthChange == 0)
			{
				instantHealthChange = healAmount;
			}
			break;
		case EffectType.Debuff:
			isDebuff = true;
			if (!removesAllDebuffs && removesDebuffTypes.Count == 0)
			{
				removesAllDebuffs = true;
			}
			break;
		}
	}

	public string GetDisplayName()
	{
		return itemName;
	}

	public Color GetRarityColor()
	{
		return rarity switch
		{
			ItemRarity.Common => Color.white, 
			ItemRarity.Uncommon => Color.green, 
			ItemRarity.Rare => Color.blue, 
			ItemRarity.Epic => Color.magenta, 
			ItemRarity.Legendary => Color.yellow, 
			_ => Color.white, 
		};
	}

	public string GetTooltipDescription()
	{
		string text = "";
		if (IsWeapon && weaponData != null)
		{
			text += $"Damage: {weaponData.damage}";
			if (weaponData.IsProjectileWeapon && weaponData.UsesHeat)
			{
				text += $"\nHeat per Shot: {weaponData.GetHeatCost():F1}";
			}
			if (weaponData.SpecialAttackDamage > 0)
			{
				text += $"\nSpecial Damage: {weaponData.SpecialAttackDamage}";
				if (weaponData.SpecialAttackHeatCost > 0f)
				{
					text += $"\nSpecial Heat Cost: {weaponData.SpecialAttackHeatCost:F1}";
				}
				else if (weaponData.UsesHeat)
				{
					text += $"\nSpecial Heat Cost: {weaponData.GetHeatCost() * 1.5f:F1}";
				}
			}
		}
		else if (IsArmor && armorData != null)
		{
			text += $"Defense: {armorData.defense}";
			if (armorData.magicResistance != 0)
			{
				text += $"\nMagic Resistance: {armorData.magicResistance:+0;-0;0}";
			}
			text += $"\nType: {armorData.armorType}";
			string statModifierDescription = armorData.GetStatModifierDescription();
			if (!string.IsNullOrEmpty(statModifierDescription))
			{
				text = text + "\n" + statModifierDescription;
			}
		}
		else if (IsConsumable)
		{
			if (instantHealthChange != 0)
			{
				text = ((instantHealthChange <= 0) ? (text + $"Damages: {-instantHealthChange} HP (instant)") : (text + $"Heals: {instantHealthChange} HP (instant)"));
			}
			if (healthRegenPerSecond > 0f)
			{
				if (text.Length > 0)
				{
					text += "\n";
				}
				text += $"Regenerates: {healthRegenPerSecond} HP/sec";
				if (duration > 0f)
				{
					text += $" for {duration}s (Total: {healthRegenPerSecond * duration} HP)";
				}
			}
			else if (healthDrainPerSecond > 0f)
			{
				if (text.Length > 0)
				{
					text += "\n";
				}
				text += $"Drains: {healthDrainPerSecond} HP/sec";
			}
			if (attackMultiplier != 1f)
			{
				if (text.Length > 0)
				{
					text += "\n";
				}
				text += $"Attack: x{attackMultiplier:F1}";
			}
			if (bonusAttackDamage != 0)
			{
				if (text.Length > 0)
				{
					text += "\n";
				}
				text += $"Attack Damage: +{bonusAttackDamage}";
			}
			if (defenseMultiplier != 1f)
			{
				if (text.Length > 0)
				{
					text += "\n";
				}
				text += $"Defense: x{defenseMultiplier:F1}";
			}
			if (bonusDefense != 0)
			{
				if (text.Length > 0)
				{
					text += "\n";
				}
				text += $"Defense: +{bonusDefense}";
			}
			if (movementSpeedMultiplier != 1f)
			{
				if (text.Length > 0)
				{
					text += "\n";
				}
				float num = (movementSpeedMultiplier - 1f) * 100f;
				text += $"Movement Speed: {num:+0;-0}%";
			}
			if (attackSpeedMultiplier != 1f)
			{
				if (text.Length > 0)
				{
					text += "\n";
				}
				float num2 = (attackSpeedMultiplier - 1f) * 100f;
				text += $"Attack Speed: {num2:+0;-0}%";
			}
			if (criticalChanceBonus != 0f)
			{
				if (text.Length > 0)
				{
					text += "\n";
				}
				text += $"Critical Chance: {criticalChanceBonus:+0.0%;-0.0%}";
			}
			if (instantHeatReduction > 0f)
			{
				if (text.Length > 0)
				{
					text += "\n";
				}
				text += $"Cools: {instantHeatReduction} heat";
			}
			if (heatReductionPerSecond > 0f)
			{
				if (text.Length > 0)
				{
					text += "\n";
				}
				text += $"Heat Cooling: {heatReductionPerSecond}/sec";
			}
			if (shieldAmount > 0)
			{
				if (text.Length > 0)
				{
					text += "\n";
				}
				text += $"Shield: {shieldAmount} HP";
			}
			if (maxHealthBonus != 0)
			{
				if (text.Length > 0)
				{
					text += "\n";
				}
				text += $"Max Health: +{maxHealthBonus}";
			}
			if (maxHeatCapacityBonus != 0f)
			{
				if (text.Length > 0)
				{
					text += "\n";
				}
				text += $"Max Heat Capacity: +{maxHeatCapacityBonus}";
			}
			if (isPermanentUpgrade)
			{
				if (text.Length > 0)
				{
					text += "\n";
				}
				text += "(Permanent)";
			}
			if (removesAllDebuffs)
			{
				if (text.Length > 0)
				{
					text += "\n";
				}
				text += "Removes all debuffs";
			}
			else if (removesDebuffTypes.Count > 0)
			{
				if (text.Length > 0)
				{
					text += "\n";
				}
				text += "Removes: ";
				for (int i = 0; i < removesDebuffTypes.Count; i++)
				{
					text += removesDebuffTypes[i];
					if (i < removesDebuffTypes.Count - 1)
					{
						text += ", ";
					}
				}
			}
			if (grantsImmunityTo.Count > 0)
			{
				if (text.Length > 0)
				{
					text += "\n";
				}
				text += "Grants immunity to: ";
				for (int j = 0; j < grantsImmunityTo.Count; j++)
				{
					text += grantsImmunityTo[j];
					if (j < grantsImmunityTo.Count - 1)
					{
						text += ", ";
					}
				}
			}
			if (duration > 0f)
			{
				if (text.Length > 0)
				{
					text += "\n";
				}
				text += $"Duration: {duration}s";
			}
			if (effectType != EffectType.None)
			{
				if (text.Length > 0)
				{
					text += "\n";
				}
				text += $"Effect: {effectType}";
			}
		}
		return text;
	}

	public List<TooltipStatEntry> GetTooltipStatEntries()
	{
		List<TooltipStatEntry> list = new List<TooltipStatEntry>();
		if (IsWeapon && weaponData != null)
		{
			string val = ((weaponData.IsProjectileWeapon && weaponData.ProjectileCount > 1) ? $"{weaponData.damage} X{weaponData.ProjectileCount}" : weaponData.damage.ToString());
			list.Add(new TooltipStatEntry(TooltipStatType.Damage, val));
			if (weaponData.IsProjectileWeapon && weaponData.UsesHeat)
			{
				list.Add(new TooltipStatEntry(TooltipStatType.Heat, weaponData.GetHeatCost().ToString("F1")));
			}
			if (weaponData.SpecialAttackDamage > 0)
			{
				string val2 = ((weaponData.SpecialProjectileCount > 1) ? $"{weaponData.SpecialAttackDamage} X{weaponData.SpecialProjectileCount}" : weaponData.SpecialAttackDamage.ToString());
				list.Add(new TooltipStatEntry(TooltipStatType.SpecialDamage, val2));
				float num;
				if (weaponData.SpecialAttackHeatCost > 0f)
				{
					num = weaponData.SpecialAttackHeatCost;
				}
				else
				{
					num = (weaponData.UsesHeat ? (weaponData.GetHeatCost() * 1.5f) : 0f);
				}
				if (num > 0f)
				{
					list.Add(new TooltipStatEntry(TooltipStatType.SpecialHeatCost, num.ToString("F1")));
				}
			}
			if (weaponData.criticalMultiplier > 1f)
			{
				list.Add(new TooltipStatEntry(TooltipStatType.CritMultiplier, $"X{weaponData.criticalMultiplier:G}"));
			}
			if (weaponData.criticalChance > 0f)
			{
				list.Add(new TooltipStatEntry(TooltipStatType.CritChance, $"{weaponData.criticalChance * 100f:0}%"));
			}
		}
		else if (IsArmor && armorData != null)
		{
			list.Add(new TooltipStatEntry(TooltipStatType.Defense, armorData.defense.ToString()));
			if (armorData.healthBonus != 0)
			{
				list.Add(new TooltipStatEntry(TooltipStatType.MaxHealth, $"{armorData.healthBonus:+0;-0;0}"));
			}
			if (armorData.heatCapacityBonus != 0f)
			{
				list.Add(new TooltipStatEntry(TooltipStatType.MaxHeatCapacity, $"{armorData.heatCapacityBonus:+0;-0;0}"));
			}
			if (armorData.walkSpeedBonus != 0f)
			{
				list.Add(new TooltipStatEntry(TooltipStatType.MovementSpeed, $"{armorData.walkSpeedBonus:+0.0;-0.0;0}"));
			}
			if (armorData.heatCooldownBonus != 0f)
			{
				list.Add(new TooltipStatEntry(TooltipStatType.HeatCooldown, $"{armorData.heatCooldownBonus:+0.0;-0.0;0}"));
			}
			if (armorData.dashForceBonus != 0f)
			{
				list.Add(new TooltipStatEntry(TooltipStatType.DashForce, $"{armorData.dashForceBonus:+0.0;-0.0;0}"));
			}
			if (armorData.dashDurationBonus != 0f)
			{
				list.Add(new TooltipStatEntry(TooltipStatType.DashDuration, $"{armorData.dashDurationBonus:+0.00;-0.00;0}s"));
			}
			if (armorData.bonusDashCharges != 0)
			{
				list.Add(new TooltipStatEntry(TooltipStatType.DashCharges, $"{armorData.bonusDashCharges:+0;-0;0}"));
			}
			if (armorData.lifeStealOnKill)
			{
				list.Add(new TooltipStatEntry(TooltipStatType.LifeSteal, "10%"));
			}
			if (armorData.thornsDamage > 0)
			{
				list.Add(new TooltipStatEntry(TooltipStatType.Thorns, armorData.thornsDamage.ToString()));
			}
		}
		else if (IsConsumable)
		{
			if (instantHealthChange > 0)
			{
				list.Add(new TooltipStatEntry(TooltipStatType.Health, $"{instantHealthChange:+0;-0;0}"));
			}
			else if (instantHealthChange < 0)
			{
				list.Add(new TooltipStatEntry(TooltipStatType.Health, instantHealthChange.ToString()));
			}
			if (healthRegenPerSecond > 0f)
			{
				list.Add(new TooltipStatEntry(TooltipStatType.HealthRegen, $"+{healthRegenPerSecond}/s"));
			}
			if (shieldAmount > 0)
			{
				list.Add(new TooltipStatEntry(TooltipStatType.Shield, shieldAmount.ToString()));
			}
			if (maxHealthBonus != 0)
			{
				list.Add(new TooltipStatEntry(TooltipStatType.MaxHealth, $"{maxHealthBonus:+0;-0;0}"));
			}
			if (maxHeatCapacityBonus != 0f)
			{
				list.Add(new TooltipStatEntry(TooltipStatType.MaxHeatCapacity, $"{maxHeatCapacityBonus:+0;-0;0}"));
			}
			if (criticalDamageMultiplier != 1f)
			{
				list.Add(new TooltipStatEntry(TooltipStatType.CritMultiplier, $"X{criticalDamageMultiplier:G}"));
			}
			if (criticalChanceBonus != 0f)
			{
				list.Add(new TooltipStatEntry(TooltipStatType.CritChance, $"{criticalChanceBonus * 100f:+0;-0;0}%"));
			}
			if (attackSpeedMultiplier != 1f)
			{
				float num2 = (attackSpeedMultiplier - 1f) * 100f;
				list.Add(new TooltipStatEntry(TooltipStatType.AttackSpeed, $"{num2:+0;-0}%"));
			}
			if (movementSpeedMultiplier != 1f)
			{
				float num3 = (movementSpeedMultiplier - 1f) * 100f;
				list.Add(new TooltipStatEntry(TooltipStatType.MovementSpeed, $"{num3:+0;-0}%"));
			}
			if (bonusDefense != 0)
			{
				list.Add(new TooltipStatEntry(TooltipStatType.Defense, $"{bonusDefense:+0;-0;0}"));
			}
			if (instantHeatReduction > 0f)
			{
				list.Add(new TooltipStatEntry(TooltipStatType.Heat, $"-{instantHeatReduction}"));
			}
		}
		return list;
	}

	public string GetTooltipExtraText()
	{
		string text = "";
		if (IsArmor && armorData != null)
		{
			if (armorData.healthRegenBonus != 0f)
			{
				Append(ref text, $"Health Regen: +{armorData.healthRegenBonus:F1}/s");
			}
			if (armorData.hasSetBonus)
			{
				Append(ref text, "Set: " + armorData.setBonusName);
			}
		}
		else if (IsConsumable)
		{
			if (healthDrainPerSecond > 0f)
			{
				Append(ref text, $"Drains: {healthDrainPerSecond} HP/s");
			}
			if (heatReductionPerSecond > 0f)
			{
				Append(ref text, $"Heat Cooling: {heatReductionPerSecond}/s");
			}
			if (attackMultiplier != 1f)
			{
				Append(ref text, $"Attack: x{attackMultiplier:F1}");
			}
			if (bonusAttackDamage != 0)
			{
				Append(ref text, $"Attack Damage: +{bonusAttackDamage}");
			}
			if (defenseMultiplier != 1f)
			{
				Append(ref text, $"Defense: x{defenseMultiplier:F1}");
			}
			if (isPermanentUpgrade)
			{
				Append(ref text, "(Permanent)");
			}
			if (removesAllDebuffs)
			{
				Append(ref text, "Removes all debuffs");
			}
			if (duration > 0f)
			{
				Append(ref text, $"Duration: {duration}s");
			}
			if (effectType != EffectType.None)
			{
				Append(ref text, $"Effect: {effectType}");
			}
		}
		return text;
	}

	private static void Append(ref string text, string line)
	{
		if (text.Length > 0)
		{
			text += "\n";
		}
		text += line;
	}

	public bool CanEquipInSlot(EquipmentSlotType slotType)
	{
		switch (slotType)
		{
		case EquipmentSlotType.UpperArmor:
			if (IsArmor)
			{
				return armorData.armorType == ArmorType.UpperBody;
			}
			return false;
		case EquipmentSlotType.LowerArmor:
			if (IsArmor)
			{
				return armorData.armorType == ArmorType.LowerBody;
			}
			return false;
		case EquipmentSlotType.LeftWeapon:
		case EquipmentSlotType.RightWeapon:
			if (!IsWeapon)
			{
				return false;
			}
			return true;
		case EquipmentSlotType.Trinket1:
		case EquipmentSlotType.Trinket2:
			return IsTrinket;
		default:
			return false;
		}
	}

	public StatusEffect CreateStatusEffect()
	{
		if (!IsConsumable)
		{
			return null;
		}
		return new StatusEffect
		{
			effectName = itemName + " Effect",
			effectType = statusEffectType,
			duration = duration,
			isPermanent = (duration <= 0f),
			isDebuff = isDebuff,
			stackCount = 1,
			sourceItem = this,
			refreshDurationOnReapply = refreshDurationOnReapply,
			screenEffectMaterial = buffScreenMaterial,
			modifiers = new StatModifiers
			{
				attackMultiplier = attackMultiplier,
				defenseMultiplier = defenseMultiplier,
				movementSpeedMultiplier = movementSpeedMultiplier,
				attackSpeedMultiplier = attackSpeedMultiplier,
				criticalDamageMultiplier = criticalDamageMultiplier,
				bonusAttackDamage = bonusAttackDamage,
				bonusDefense = bonusDefense,
				criticalChanceBonus = criticalChanceBonus,
				maxHealthBonus = maxHealthBonus,
				heatCapacityBonus = maxHeatCapacityBonus,
				healthRegenPerSecond = healthRegenPerSecond,
				healthDrainPerSecond = healthDrainPerSecond,
				heatReductionPerSecond = heatReductionPerSecond,
				heatGenerationPerSecond = heatGenerationPerSecond,
				instantHeatReduction = instantHeatReduction
			},
			grantsImmunityTo = new List<StatusEffectType>(grantsImmunityTo),
			preventsActions = preventsActions,
			preventsMovement = preventsMovement
		};
	}

	public bool HasStatusEffects()
	{
		if (!IsConsumable)
		{
			return false;
		}
		if (attackMultiplier == 1f && bonusAttackDamage == 0 && defenseMultiplier == 1f && bonusDefense == 0 && movementSpeedMultiplier == 1f && attackSpeedMultiplier == 1f && criticalChanceBonus == 0f && criticalDamageMultiplier == 1f && healthRegenPerSecond == 0f && healthDrainPerSecond == 0f && heatReductionPerSecond == 0f && heatGenerationPerSecond == 0f && shieldAmount == 0 && maxHealthBonus == 0 && maxHeatCapacityBonus == 0f && grantsImmunityTo.Count <= 0 && !removesAllDebuffs && removesDebuffTypes.Count <= 0 && !preventsMovement)
		{
			return preventsActions;
		}
		return true;
	}
}
