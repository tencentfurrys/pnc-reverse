using System.Text;
using UnityEngine;

namespace PixelCrushers.GridController;

[CreateAssetMenu(fileName = "New Armor", menuName = "Combat/Armor Data")]
public class ArmorData : ScriptableObject
{
	[Header("Basic Info")]
	public string armorName = "Armor";

	public ArmorType armorType;

	public Sprite armorIcon;

	[Header("Defense Stats")]
	public int defense = 5;

	public int magicResistance;

	[Header("Special Properties")]
	public bool hasSetBonus;

	public string setBonusName = "";

	public int setBonusDefense;

	[Header("Thorns")]
	[Tooltip("Flat damage reflected back to melee attackers when hit")]
	public int thornsDamage;

	[Header("Life Steal")]
	[Tooltip("If true, heals player for 10% of killed enemy's max HP on kill (max 5 HP)")]
	public bool lifeStealOnKill;

	[Header("Pitfall Immunity")]
	public bool negatesPitfallDamage;

	[Header("Stat Modifiers")]
	[Tooltip("Added to max health. Positive = more HP, negative = less HP")]
	public int healthBonus;

	[Tooltip("Added to max heat capacity. Positive = can build more heat before overheating")]
	public float heatCapacityBonus;

	[Tooltip("Added to base walk speed. Positive = faster, negative = slower")]
	public float walkSpeedBonus;

	[Tooltip("Added to base crouch speed. Positive = faster, negative = slower")]
	public float crouchSpeedBonus;

	[Tooltip("HP healed per second while equipped. Positive = healing")]
	public float healthRegenBonus;

	[Tooltip("Heat cooled per second while equipped. Positive = faster cooldown, negative = slower cooldown (minimum rate of 1)")]
	public float heatCooldownBonus;

	[Tooltip("Added to base dash force. Positive = stronger dash, negative = weaker")]
	public float dashForceBonus;

	[Tooltip("Added to base dash duration in seconds. Positive = longer dash")]
	public float dashDurationBonus;

	[Tooltip("Extra dash charges granted. Player always has 1 base dash. Use positive values only")]
	public int bonusDashCharges;

	public void ApplyStatModifiers(PlayerStats playerStats)
	{
		if (!(playerStats == null))
		{
			if (healthBonus != 0)
			{
				int newMaxHealth = playerStats.MaxHealth + healthBonus;
				playerStats.SetMaxHealth(newMaxHealth, heal: false);
			}
			if (heatCapacityBonus != 0f)
			{
				float maxHeat = playerStats.MaxHeat + heatCapacityBonus;
				playerStats.SetMaxHeat(maxHeat);
			}
			if (walkSpeedBonus != 0f)
			{
				float baseWalkSpeed = playerStats.GetBaseWalkSpeed() + walkSpeedBonus;
				playerStats.SetBaseWalkSpeed(baseWalkSpeed);
			}
			if (crouchSpeedBonus != 0f)
			{
				float baseCrouchSpeed = playerStats.GetBaseCrouchSpeed() + crouchSpeedBonus;
				playerStats.SetBaseCrouchSpeed(baseCrouchSpeed);
			}
			if (dashForceBonus != 0f)
			{
				float baseDashForce = playerStats.GetBaseDashForce() + dashForceBonus;
				playerStats.SetBaseDashForce(baseDashForce);
			}
			if (dashDurationBonus != 0f)
			{
				float dashDuration = playerStats.DashDuration + dashDurationBonus;
				playerStats.SetDashDuration(dashDuration);
			}
			if (bonusDashCharges != 0)
			{
				playerStats.AddBonusDashCharges(bonusDashCharges);
			}
			if (healthRegenBonus != 0f)
			{
				playerStats.SetHealthRegenRate(playerStats.GetHealthRegenRate() + healthRegenBonus);
			}
			if (heatCooldownBonus != 0f)
			{
				playerStats.SetHeatCooldownRate(playerStats.GetHeatCooldownRate() + heatCooldownBonus);
			}
		}
	}

	public void RemoveStatModifiers(PlayerStats playerStats)
	{
		if (!(playerStats == null))
		{
			if (healthBonus != 0)
			{
				int newMaxHealth = playerStats.MaxHealth - healthBonus;
				playerStats.SetMaxHealth(newMaxHealth, heal: false);
			}
			if (heatCapacityBonus != 0f)
			{
				float maxHeat = playerStats.MaxHeat - heatCapacityBonus;
				playerStats.SetMaxHeat(maxHeat);
			}
			if (walkSpeedBonus != 0f)
			{
				float baseWalkSpeed = playerStats.GetBaseWalkSpeed() - walkSpeedBonus;
				playerStats.SetBaseWalkSpeed(baseWalkSpeed);
			}
			if (crouchSpeedBonus != 0f)
			{
				float baseCrouchSpeed = playerStats.GetBaseCrouchSpeed() - crouchSpeedBonus;
				playerStats.SetBaseCrouchSpeed(baseCrouchSpeed);
			}
			if (dashForceBonus != 0f)
			{
				float baseDashForce = playerStats.GetBaseDashForce() - dashForceBonus;
				playerStats.SetBaseDashForce(baseDashForce);
			}
			if (dashDurationBonus != 0f)
			{
				float dashDuration = playerStats.DashDuration - dashDurationBonus;
				playerStats.SetDashDuration(dashDuration);
			}
			if (bonusDashCharges != 0)
			{
				playerStats.AddBonusDashCharges(-bonusDashCharges);
			}
			if (healthRegenBonus != 0f)
			{
				playerStats.SetHealthRegenRate(playerStats.GetHealthRegenRate() - healthRegenBonus);
			}
			if (heatCooldownBonus != 0f)
			{
				playerStats.SetHeatCooldownRate(playerStats.GetHeatCooldownRate() - heatCooldownBonus);
			}
		}
	}

	public string GetStatModifierDescription()
	{
		StringBuilder stringBuilder = new StringBuilder();
		if (healthBonus != 0)
		{
			stringBuilder.AppendLine(string.Format("Health: {0}{1}", (healthBonus > 0) ? "+" : "", healthBonus));
		}
		if (heatCapacityBonus != 0f)
		{
			stringBuilder.AppendLine(string.Format("Heat Capacity: {0}{1:F0}", (heatCapacityBonus > 0f) ? "+" : "", heatCapacityBonus));
		}
		if (walkSpeedBonus != 0f)
		{
			stringBuilder.AppendLine(string.Format("Walk Speed: {0}{1:F1}", (walkSpeedBonus > 0f) ? "+" : "", walkSpeedBonus));
		}
		if (crouchSpeedBonus != 0f)
		{
			stringBuilder.AppendLine(string.Format("Crouch Speed: {0}{1:F1}", (crouchSpeedBonus > 0f) ? "+" : "", crouchSpeedBonus));
		}
		if (dashForceBonus != 0f)
		{
			stringBuilder.AppendLine(string.Format("Dash Force: {0}{1:F1}", (dashForceBonus > 0f) ? "+" : "", dashForceBonus));
		}
		if (dashDurationBonus != 0f)
		{
			stringBuilder.AppendLine(string.Format("Dash Duration: {0}{1:F2}s", (dashDurationBonus > 0f) ? "+" : "", dashDurationBonus));
		}
		if (healthRegenBonus != 0f)
		{
			stringBuilder.AppendLine(string.Format("Health Regen: {0}{1:F1}/s", (healthRegenBonus > 0f) ? "+" : "", healthRegenBonus));
		}
		if (heatCooldownBonus != 0f)
		{
			stringBuilder.AppendLine(string.Format("Heat Cooldown: {0}{1:F1}/s", (heatCooldownBonus > 0f) ? "+" : "", heatCooldownBonus));
		}
		if (thornsDamage > 0)
		{
			stringBuilder.AppendLine($"Thorns: {thornsDamage} damage");
		}
		if (bonusDashCharges > 0)
		{
			stringBuilder.AppendLine($"Dash Charges: +{bonusDashCharges}");
		}
		return stringBuilder.ToString().TrimEnd();
	}
}
