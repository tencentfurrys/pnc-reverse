using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;

namespace PixelCrushers.GridController
{
	public class BuffDebuffSystem : MonoBehaviour
	{
		[Header("System Settings")]
		[SerializeField]
		private bool allowStackingOfSameType;

		[SerializeField]
		private int maxStacksPerBuff = 3;

		[SerializeField]
		private bool showDebugInfo = true;

		[Header("Component References")]
		[SerializeField]
		private PlayerStats playerStats;

		[SerializeField]
		private DualWieldingSystem combatSystem;

		[SerializeField]
		private FirstPersonController movementController;

		[Header("Visual Effects")]
		[SerializeField]
		private Transform effectsContainer;

		[SerializeField]
		private GameObject buffEffectPrefab;

		[SerializeField]
		private GameObject debuffEffectPrefab;

		[Header("Audio")]
		[SerializeField]
		private AudioSource audioSource;

		[SerializeField]
		private AudioClip buffAppliedSound;

		[SerializeField]
		private AudioClip debuffAppliedSound;

		[SerializeField]
		private AudioClip buffExpiredSound;

		[SerializeField]
		private AudioClip debuffRemovedSound;

		[Header("Events")]
		public UnityEvent<StatusEffect> OnBuffApplied;

		public UnityEvent<StatusEffect> OnDebuffApplied;

		public UnityEvent<StatusEffect> OnEffectRemoved;

		public UnityEvent<StatusEffect> OnEffectExpired;

		public UnityEvent<List<StatusEffect>> OnEffectsUpdated;

		public UnityEvent OnAttacksReenabled;

		private List<StatusEffect> activeEffects = new List<StatusEffect>();

		private Dictionary<StatusEffectType, List<StatusEffect>> effectsByType = new Dictionary<StatusEffectType, List<StatusEffect>>();

		private readonly List<StatusEffect> tempEffectList = new List<StatusEffect>();

		private StatModifiers cachedModifiers = new StatModifiers();

		private bool modifiersNeedRecalculation;

		private int baseMaxHealth;

		private float baseMaxHeat;

		private bool baseStatsInitialized;

		private int permanentMaxHealthBonus;

		private float permanentMaxHeatBonus;

		private void Awake()
		{
			if (playerStats == null)
			{
				playerStats = GetComponent<PlayerStats>();
			}
			if (combatSystem == null)
			{
				combatSystem = GetComponent<DualWieldingSystem>();
			}
			if (movementController == null)
			{
				movementController = GetComponent<FirstPersonController>();
			}
			if (audioSource == null)
			{
				audioSource = GetComponent<AudioSource>();
			}
			if (audioSource == null)
			{
				audioSource = base.gameObject.AddComponent<AudioSource>();
			}
			foreach (StatusEffectType value in Enum.GetValues(typeof(StatusEffectType)))
			{
				effectsByType[value] = new List<StatusEffect>();
			}
			InitializeBaseStats();
		}

		private void InitializeBaseStats()
		{
			if (playerStats != null && !baseStatsInitialized)
			{
				baseMaxHealth = playerStats.MaxHealth;
				baseMaxHeat = playerStats.MaxHeat;
				baseStatsInitialized = true;
			}
		}

		public void ReinitializeBaseStats()
		{
			if (!(playerStats == null))
			{
				if (modifiersNeedRecalculation)
				{
					RecalculateModifiers();
				}
				CaptureBaseStats();
			}
		}

		private void CaptureBaseStats()
		{
			baseMaxHealth = playerStats.MaxHealth - permanentMaxHealthBonus - cachedModifiers.maxHealthBonus;
			baseMaxHeat = playerStats.MaxHeat - permanentMaxHeatBonus - cachedModifiers.heatCapacityBonus;
			baseStatsInitialized = true;
		}

		private void Update()
		{
			UpdateActiveEffects();
			if (modifiersNeedRecalculation)
			{
				RecalculateModifiers();
			}
		}

		public bool ApplyEffect(InventoryItem item)
		{
			if (item == null || !item.IsConsumable)
			{
				return false;
			}
			if (item.isPermanentUpgrade && ApplyPermanentUpgrade(item))
			{
				return true;
			}
			bool result = HandleSpecialConsumableEffects(item);
			if (item.HasStatusEffects())
			{
				StatusEffect statusEffect = item.CreateStatusEffect();
				if (statusEffect != null)
				{
					return ApplyEffect(statusEffect);
				}
			}
			if (item.instantHealthChange > 0)
			{
				StatusEffect statusEffect2 = CreateInstantHealAsAccumulated(item);
				if (statusEffect2 != null)
				{
					ApplyEffect(statusEffect2);
					result = true;
				}
			}
			return result;
		}

		public bool ApplyEffect(StatusEffect effect)
		{
			if (effect == null)
			{
				return false;
			}
			if (effect.refreshDurationOnReapply)
			{
				List<StatusEffect> list = effectsByType[effect.effectType];
				if (list.Count > 0)
				{
					list[0].remainingDuration = effect.duration;
					list[0].timeApplied = Time.time;
					modifiersNeedRecalculation = true;
					OnEffectsUpdated?.Invoke(activeEffects);
					return true;
				}
			}
			if (!CanApplyEffect(effect))
			{
				return false;
			}
			if (HasImmunity(effect.effectType))
			{
				return false;
			}
			effect.timeApplied = Time.time;
			effect.remainingDuration = effect.duration;
			activeEffects.Add(effect);
			effectsByType[effect.effectType].Add(effect);
			ApplyImmediateEffects(effect);
			if (effect.isDebuff)
			{
				OnDebuffApplied?.Invoke(effect);
			}
			else
			{
				OnBuffApplied?.Invoke(effect);
			}
			OnEffectsUpdated?.Invoke(activeEffects);
			PlaySound(effect.isDebuff ? debuffAppliedSound : buffAppliedSound);
			SpawnVisualEffect(effect);
			modifiersNeedRecalculation = true;
			return true;
		}

		public bool RemoveEffect(StatusEffect effect)
		{
			if (effect == null || !activeEffects.Contains(effect))
			{
				return false;
			}
			activeEffects.Remove(effect);
			effectsByType[effect.effectType].Remove(effect);
			RemoveImmediateEffects(effect);
			if (effect.visualEffectInstance != null)
			{
				UnityEngine.Object.Destroy(effect.visualEffectInstance);
			}
			OnEffectRemoved?.Invoke(effect);
			OnEffectsUpdated?.Invoke(activeEffects);
			PlaySound(effect.isDebuff ? debuffRemovedSound : buffExpiredSound);
			modifiersNeedRecalculation = true;
			return true;
		}

		public void RemoveEffectsOfType(StatusEffectType type)
		{
			tempEffectList.Clear();
			tempEffectList.AddRange(effectsByType[type]);
			foreach (StatusEffect tempEffect in tempEffectList)
			{
				RemoveEffect(tempEffect);
			}
		}

		public void RemoveAllDebuffs()
		{
			tempEffectList.Clear();
			for (int i = 0; i < activeEffects.Count; i++)
			{
				if (activeEffects[i].isDebuff)
				{
					tempEffectList.Add(activeEffects[i]);
				}
			}
			foreach (StatusEffect tempEffect in tempEffectList)
			{
				RemoveEffect(tempEffect);
			}
		}

		public void RemoveAllEffects()
		{
			tempEffectList.Clear();
			tempEffectList.AddRange(activeEffects);
			foreach (StatusEffect tempEffect in tempEffectList)
			{
				RemoveEffect(tempEffect);
			}
		}

		public bool HasEffect(StatusEffectType type)
		{
			return effectsByType[type].Count > 0;
		}

		public List<StatusEffect> GetEffectsOfType(StatusEffectType type)
		{
			return new List<StatusEffect>(effectsByType[type]);
		}

		public List<StatusEffect> GetActiveEffects()
		{
			return new List<StatusEffect>(activeEffects);
		}

		public StatModifiers GetCurrentModifiers()
		{
			if (modifiersNeedRecalculation)
			{
				RecalculateModifiers();
			}
			return cachedModifiers;
		}

		public bool HasImmunity(StatusEffectType type)
		{
			foreach (StatusEffect activeEffect in activeEffects)
			{
				if (activeEffect.grantsImmunityTo.Contains(type))
				{
					return true;
				}
			}
			return false;
		}

		public string GetActiveEffectsInfo()
		{
			if (activeEffects.Count == 0)
			{
				return "No active effects";
			}
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("Active Status Effects:");
			foreach (StatusEffect activeEffect in activeEffects)
			{
				string arg = (activeEffect.isDebuff ? "[-]" : "[+]");
				stringBuilder.AppendLine($"{arg} {activeEffect.effectName}: {activeEffect.remainingDuration:F1}s remaining");
				if (activeEffect.stackCount > 1)
				{
					stringBuilder.AppendLine($"    Stacks: {activeEffect.stackCount}");
				}
			}
			return stringBuilder.ToString();
		}

		public bool ApplyHeatCoolant(float instantCoolAmount, float coolPerSecond = 0f, float duration = 0f)
		{
			if (playerStats == null)
			{
				return false;
			}
			bool num = playerStats.CanAttackNow();
			_ = playerStats.CurrentHeat;
			if (instantCoolAmount > 0f)
			{
				playerStats.CoolHeat(instantCoolAmount);
			}
			bool flag = playerStats.CanAttackNow();
			_ = playerStats.CurrentHeat;
			if (!num & flag)
			{
				OnAttacksReenabled?.Invoke();
			}
			if (coolPerSecond > 0f && duration > 0f)
			{
				StatusEffect effect = new StatusEffect
				{
					effectName = "Heat Cooling",
					effectType = StatusEffectType.HeatCooling,
					duration = duration,
					isDebuff = false,
					modifiers = new StatModifiers
					{
						heatReductionPerSecond = coolPerSecond
					}
				};
				ApplyEffect(effect);
			}
			return true;
		}

		public bool HasHeatCoolingEffect()
		{
			foreach (StatusEffect activeEffect in activeEffects)
			{
				if (activeEffect.modifiers.heatReductionPerSecond > 0f || activeEffect.effectType == StatusEffectType.HeatCooling)
				{
					return true;
				}
			}
			return false;
		}

		public float GetTotalHeatCoolingRate()
		{
			float num = 0f;
			foreach (StatusEffect activeEffect in activeEffects)
			{
				num += activeEffect.modifiers.heatReductionPerSecond;
			}
			return num;
		}

		private StatusEffect CreateInstantHealAsAccumulated(InventoryItem item)
		{
			if (item.instantHealthChange <= 0)
			{
				return null;
			}
			return new StatusEffect
			{
				effectName = item.itemName + " Healing",
				effectType = StatusEffectType.Regeneration,
				duration = 0.1f,
				isDebuff = false,
				modifiers = new StatModifiers
				{
					healthRegenPerSecond = (float)item.instantHealthChange / 0.1f
				},
				sourceItem = item
			};
		}

		private bool ApplyPermanentUpgrade(InventoryItem item)
		{
			if (item == null)
			{
				return false;
			}
			bool flag = false;
			if (!baseStatsInitialized)
			{
				InitializeBaseStats();
			}
			if (item.maxHealthBonus != 0)
			{
				permanentMaxHealthBonus += item.maxHealthBonus;
				flag = true;
			}
			if (item.maxHeatCapacityBonus != 0f)
			{
				permanentMaxHeatBonus += item.maxHeatCapacityBonus;
				flag = true;
			}
			if (flag)
			{
				modifiersNeedRecalculation = true;
			}
			return flag;
		}

		private bool HandleSpecialConsumableEffects(InventoryItem item)
		{
			bool result = false;
			if (item.instantHeatReduction > 0f && playerStats != null)
			{
				bool num = playerStats.CanAttackNow();
				playerStats.CoolHeat(item.instantHeatReduction);
				bool flag = playerStats.CanAttackNow();
				if (!num & flag)
				{
					OnAttacksReenabled?.Invoke();
				}
				result = true;
			}
			if (item.removesAllDebuffs)
			{
				RemoveAllDebuffs();
				result = true;
			}
			else if (item.removesDebuffTypes.Count > 0)
			{
				foreach (StatusEffectType removesDebuffType in item.removesDebuffTypes)
				{
					RemoveEffectsOfType(removesDebuffType);
				}
				result = true;
			}
			return result;
		}

		private void UpdateActiveEffects()
		{
			tempEffectList.Clear();
			foreach (StatusEffect activeEffect in activeEffects)
			{
				if (!activeEffect.isPermanent)
				{
					activeEffect.remainingDuration -= Time.deltaTime;
					ApplyOverTimeEffects(activeEffect);
					if (activeEffect.remainingDuration <= 0f)
					{
						tempEffectList.Add(activeEffect);
					}
				}
			}
			foreach (StatusEffect tempEffect in tempEffectList)
			{
				RemoveEffect(tempEffect);
				OnEffectExpired?.Invoke(tempEffect);
			}
		}

		private bool CanApplyEffect(StatusEffect newEffect)
		{
			List<StatusEffect> list = effectsByType[newEffect.effectType];
			if (list.Count == 0)
			{
				return true;
			}
			if (!allowStackingOfSameType)
			{
				return false;
			}
			return list.Count < maxStacksPerBuff;
		}

		private void ApplyImmediateEffects(StatusEffect effect)
		{
			if (effect.modifiers.instantHeatReduction != 0f && playerStats != null)
			{
				bool num = playerStats.CanAttackNow();
				playerStats.CoolHeat(effect.modifiers.instantHeatReduction);
				playerStats.ClearOverheatIfBelowMax();
				bool flag = playerStats.CanAttackNow();
				if (!num & flag)
				{
					OnAttacksReenabled?.Invoke();
				}
			}
		}

		private void RemoveImmediateEffects(StatusEffect effect)
		{
			if (effect.modifiers.movementSpeedMultiplier != 1f)
			{
				modifiersNeedRecalculation = true;
			}
		}

		private void ApplyOverTimeEffects(StatusEffect effect)
		{
			float deltaTime = Time.deltaTime;
			if (effect.modifiers.healthRegenPerSecond > 0f && playerStats != null)
			{
				if (!effect.accumulatedValues.ContainsKey("health"))
				{
					effect.accumulatedValues["health"] = 0f;
				}
				float num = effect.modifiers.healthRegenPerSecond * deltaTime;
				effect.accumulatedValues["health"] += num;
				if (effect.accumulatedValues["health"] >= 1f)
				{
					int num2 = Mathf.FloorToInt(effect.accumulatedValues["health"]);
					if (!playerStats.IsAtFullHealth() && num2 > 0)
					{
						playerStats.Heal(num2);
						effect.accumulatedValues["health"] -= num2;
					}
					else if (playerStats.IsAtFullHealth())
					{
						effect.accumulatedValues["health"] = 0f;
					}
				}
			}
			if (effect.modifiers.healthDrainPerSecond > 0f && playerStats != null)
			{
				if (!effect.accumulatedValues.ContainsKey("damage"))
				{
					effect.accumulatedValues["damage"] = 0f;
				}
				float num3 = effect.modifiers.healthDrainPerSecond * deltaTime;
				effect.accumulatedValues["damage"] += num3;
				if (effect.accumulatedValues["damage"] >= 1f)
				{
					int num4 = Mathf.FloorToInt(effect.accumulatedValues["damage"]);
					if (num4 > 0)
					{
						playerStats.TakeDamage(num4);
						effect.accumulatedValues["damage"] -= num4;
					}
				}
			}
			if (effect.modifiers.heatReductionPerSecond > 0f && playerStats != null)
			{
				bool num5 = playerStats.CanAttackNow();
				_ = playerStats.CurrentHeat;
				float amount = effect.modifiers.heatReductionPerSecond * deltaTime;
				playerStats.CoolHeat(amount);
				playerStats.ClearOverheatIfBelowMax();
				bool flag = playerStats.CanAttackNow();
				if (!num5 & flag)
				{
					OnAttacksReenabled?.Invoke();
				}
			}
			if (effect.modifiers.heatGenerationPerSecond > 0f && playerStats != null)
			{
				float amount2 = effect.modifiers.heatGenerationPerSecond * deltaTime;
				playerStats.GenerateHeat(amount2);
			}
		}

		private void RecalculateModifiers()
		{
			cachedModifiers = new StatModifiers();
			foreach (StatusEffect activeEffect in activeEffects)
			{
				cachedModifiers.attackMultiplier *= activeEffect.modifiers.attackMultiplier;
				cachedModifiers.defenseMultiplier *= activeEffect.modifiers.defenseMultiplier;
				cachedModifiers.movementSpeedMultiplier *= activeEffect.modifiers.movementSpeedMultiplier;
				cachedModifiers.attackSpeedMultiplier *= activeEffect.modifiers.attackSpeedMultiplier;
				cachedModifiers.criticalChanceBonus += activeEffect.modifiers.criticalChanceBonus;
				cachedModifiers.criticalDamageMultiplier *= activeEffect.modifiers.criticalDamageMultiplier;
				cachedModifiers.bonusAttackDamage += activeEffect.modifiers.bonusAttackDamage;
				cachedModifiers.bonusDefense += activeEffect.modifiers.bonusDefense;
				cachedModifiers.healthRegenPerSecond += activeEffect.modifiers.healthRegenPerSecond;
				cachedModifiers.healthDrainPerSecond += activeEffect.modifiers.healthDrainPerSecond;
				cachedModifiers.heatReductionPerSecond += activeEffect.modifiers.heatReductionPerSecond;
				cachedModifiers.heatGenerationPerSecond += activeEffect.modifiers.heatGenerationPerSecond;
				cachedModifiers.heatCapacityBonus += activeEffect.modifiers.heatCapacityBonus;
				cachedModifiers.maxHealthBonus += activeEffect.modifiers.maxHealthBonus;
			}
			ApplyModifiersToSystems();
			modifiersNeedRecalculation = false;
		}

		private void ApplyModifiersToSystems()
		{
			if (playerStats != null)
			{
				playerStats.SetExternalMovementMultiplier(cachedModifiers.movementSpeedMultiplier);
			}
			if (playerStats != null)
			{
				if (!baseStatsInitialized)
				{
					InitializeBaseStats();
				}
				int num = baseMaxHealth + permanentMaxHealthBonus + cachedModifiers.maxHealthBonus;
				if (playerStats.MaxHealth != num)
				{
					playerStats.SetMaxHealth(num);
				}
			}
			if (playerStats != null)
			{
				float num2 = baseMaxHeat + permanentMaxHeatBonus + cachedModifiers.heatCapacityBonus;
				if (Mathf.Abs(playerStats.MaxHeat - num2) > 0.01f)
				{
					playerStats.SetMaxHeat(num2);
				}
			}
		}

		private void SpawnVisualEffect(StatusEffect effect)
		{
			GameObject gameObject = (effect.isDebuff ? debuffEffectPrefab : buffEffectPrefab);
			if (gameObject != null && effectsContainer != null)
			{
				effect.visualEffectInstance = UnityEngine.Object.Instantiate(gameObject, effectsContainer);
				ParticleSystem component = effect.visualEffectInstance.GetComponent<ParticleSystem>();
				if (component != null)
				{
					ParticleSystem.MainModule main = component.main;
					main.startColor = GetEffectColor(effect.effectType);
				}
			}
		}

		private Color GetEffectColor(StatusEffectType type)
		{
			return type switch
			{
				StatusEffectType.Regeneration => Color.green, 
				StatusEffectType.Poison => Color.magenta, 
				StatusEffectType.AttackBoost => Color.red, 
				StatusEffectType.DefenseBoost => Color.blue, 
				StatusEffectType.SpeedBoost => Color.yellow, 
				StatusEffectType.Slow => Color.gray, 
				StatusEffectType.Burn => new Color(1f, 0.5f, 0f), 
				StatusEffectType.Freeze => Color.cyan, 
				StatusEffectType.Stun => Color.white, 
				StatusEffectType.Shield => new Color(0.5f, 0.5f, 1f), 
				_ => Color.white, 
			};
		}

		private void PlaySound(AudioClip clip)
		{
			if (audioSource != null && clip != null)
			{
				audioSource.PlayOneShot(clip);
			}
		}

		[ContextMenu("Test Health Potion (Accumulated Only)")]
		public void TestHealthPotionAccumulatedOnly()
		{
			StatusEffect effect = new StatusEffect
			{
				effectName = "Test Health Potion (Accumulated)",
				effectType = StatusEffectType.Regeneration,
				duration = 5f,
				isDebuff = false,
				modifiers = new StatModifiers
				{
					healthRegenPerSecond = 10f
				}
			};
			if (playerStats != null && playerStats.IsAtFullHealth())
			{
				playerStats.TakeDamage(30);
			}
			ApplyEffect(effect);
		}

		[ContextMenu("Test Instant Heal as Accumulated")]
		public void TestInstantHealAsAccumulated()
		{
			StatusEffect effect = new StatusEffect
			{
				effectName = "Instant Heal (as Accumulated)",
				effectType = StatusEffectType.Regeneration,
				duration = 0.1f,
				isDebuff = false,
				modifiers = new StatModifiers
				{
					healthRegenPerSecond = 250f
				}
			};
			if (playerStats != null && playerStats.IsAtFullHealth())
			{
				playerStats.TakeDamage(30);
			}
			ApplyEffect(effect);
		}

		[ContextMenu("Force Damage Player")]
		public void ForceDamagePlayer()
		{
			if (playerStats != null)
			{
				playerStats.TakeDamage(25);
			}
		}

		[ContextMenu("Test Attack Buff")]
		public void TestAttackBuff()
		{
			StatusEffect effect = new StatusEffect
			{
				effectName = "Test Attack Boost",
				effectType = StatusEffectType.AttackBoost,
				duration = 30f,
				isDebuff = false,
				modifiers = new StatModifiers
				{
					attackMultiplier = 1.5f,
					bonusAttackDamage = 10
				}
			};
			ApplyEffect(effect);
		}

		[ContextMenu("Test Poison Debuff")]
		public void TestPoisonDebuff()
		{
			StatusEffect effect = new StatusEffect
			{
				effectName = "Test Poison",
				effectType = StatusEffectType.Poison,
				duration = 10f,
				isDebuff = true,
				modifiers = new StatModifiers
				{
					healthDrainPerSecond = 5f
				}
			};
			ApplyEffect(effect);
		}

		[ContextMenu("Remove All Effects")]
		public void DebugRemoveAllEffects()
		{
			RemoveAllEffects();
		}

		[ContextMenu("Force Update Effects")]
		public void ForceUpdateEffects()
		{
			UpdateActiveEffects();
		}

		[ContextMenu("Test Speed Boost")]
		public void TestSpeedBoost()
		{
			StatusEffect effect = new StatusEffect
			{
				effectName = "Test Speed Boost",
				effectType = StatusEffectType.SpeedBoost,
				duration = 15f,
				isDebuff = false,
				modifiers = new StatModifiers
				{
					movementSpeedMultiplier = 1.5f
				}
			};
			ApplyEffect(effect);
		}

		[ContextMenu("Test Speed Debuff")]
		public void TestSpeedDebuff()
		{
			StatusEffect effect = new StatusEffect
			{
				effectName = "Test Slow Debuff",
				effectType = StatusEffectType.Slow,
				duration = 10f,
				isDebuff = true,
				modifiers = new StatModifiers
				{
					movementSpeedMultiplier = 0.5f
				}
			};
			ApplyEffect(effect);
		}

		[ContextMenu("Test Extreme Speed")]
		public void TestExtremeSpeed()
		{
			StatusEffect effect = new StatusEffect
			{
				effectName = "Test Extreme Speed",
				effectType = StatusEffectType.SpeedBoost,
				duration = 8f,
				isDebuff = false,
				modifiers = new StatModifiers
				{
					movementSpeedMultiplier = 2.5f
				}
			};
			ApplyEffect(effect);
		}

		private void OnGUI()
		{
			if (!showDebugInfo)
			{
				return;
			}
			GUILayout.BeginArea(new Rect(10f, 200f, 350f, 450f));
			GUILayout.BeginVertical("box");
			GUILayout.Label("Status Effects", GUI.skin.label);
			if (activeEffects.Count == 0)
			{
				GUILayout.Label("No active effects");
			}
			else
			{
				foreach (StatusEffect activeEffect in activeEffects)
				{
					string text = (activeEffect.isDebuff ? "[-]" : "[+]") + " " + activeEffect.effectName;
					text = (activeEffect.isPermanent ? (text + " (Permanent)") : (text + $": {activeEffect.remainingDuration:F1}s"));
					GUILayout.Label(text);
					if (activeEffect.modifiers.healthRegenPerSecond > 0f && activeEffect.accumulatedValues.ContainsKey("health"))
					{
						float num = activeEffect.accumulatedValues["health"];
						GUILayout.Label($"  Accumulated HP: {num:F2} (Rate: {activeEffect.modifiers.healthRegenPerSecond}/sec)");
					}
				}
			}
			GUILayout.Space(10f);
			GUILayout.Label("Current Modifiers:", GUI.skin.label);
			GUILayout.Label($"Attack: x{cachedModifiers.attackMultiplier:F2} +{cachedModifiers.bonusAttackDamage}");
			GUILayout.Label($"Defense: x{cachedModifiers.defenseMultiplier:F2} +{cachedModifiers.bonusDefense}");
			GUILayout.Label($"Speed: x{cachedModifiers.movementSpeedMultiplier:F2}");
			GUILayout.Label($"Attack Speed: x{cachedModifiers.attackSpeedMultiplier:F2}");
			GUILayout.Label($"Health Regen: {cachedModifiers.healthRegenPerSecond}/sec");
			GUILayout.Space(10f);
			if (playerStats != null)
			{
				GUILayout.Label("Player Stats:", GUI.skin.label);
				GUILayout.Label($"Health: {playerStats.CurrentHealth}/{playerStats.MaxHealth}");
				GUILayout.Label($"At Full Health: {playerStats.IsAtFullHealth()}");
				GUILayout.Label($"Heat: {playerStats.CurrentHeat:F1}/{playerStats.MaxHeat}");
			}
			GUILayout.Space(10f);
			GUILayout.Label("Debug Controls:", GUI.skin.label);
			if (GUILayout.Button("Test Health Potion"))
			{
				TestHealthPotionAccumulatedOnly();
			}
			if (GUILayout.Button("Test Instant Heal"))
			{
				TestInstantHealAsAccumulated();
			}
			if (GUILayout.Button("Damage Player"))
			{
				ForceDamagePlayer();
			}
			if (GUILayout.Button("Speed Boost (+50%)"))
			{
				TestSpeedBoost();
			}
			if (GUILayout.Button("Speed Debuff (-50%)"))
			{
				TestSpeedDebuff();
			}
			if (GUILayout.Button("Extreme Speed (+150%)"))
			{
				TestExtremeSpeed();
			}
			if (GUILayout.Button("Clear All Effects"))
			{
				RemoveAllEffects();
			}
			GUILayout.EndVertical();
			GUILayout.EndArea();
		}
	}
}
