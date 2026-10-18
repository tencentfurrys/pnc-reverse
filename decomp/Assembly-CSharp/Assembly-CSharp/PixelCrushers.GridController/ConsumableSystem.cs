using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;

namespace PixelCrushers.GridController;

public class ConsumableSystem : MonoBehaviour
{
	[Serializable]
	private class ConsumableEffect
	{
		public EffectType effectType;

		public float remainingDuration;

		public float totalDuration;

		public int effectValue;

		public InventoryItem sourceItem;

		public bool isPermanent;

		public float Progress
		{
			get
			{
				if (!isPermanent)
				{
					if (!(totalDuration > 0f))
					{
						return 1f;
					}
					return (totalDuration - remainingDuration) / totalDuration;
				}
				return 1f;
			}
		}
	}

	[Header("Audio")]
	[SerializeField]
	private AudioSource audioSource;

	[SerializeField]
	private AudioClip consumeSound;

	[SerializeField]
	private AudioClip healSound;

	[SerializeField]
	private AudioClip buffSound;

	[SerializeField]
	private AudioClip failSound;

	[Header("Visual Effects")]
	[SerializeField]
	private GameObject healEffect;

	[SerializeField]
	private GameObject buffEffect;

	[SerializeField]
	private GameObject consumeEffect;

	[Header("Component References")]
	[SerializeField]
	private PlayerStats playerStats;

	[SerializeField]
	private BuffDebuffSystem buffDebuffSystem;

	[SerializeField]
	private Transform playerTransform;

	[Header("Events")]
	public UnityEvent<InventoryItem> OnItemConsumed;

	public UnityEvent<InventoryItem, string> OnConsumeFailed;

	[Header("Debug")]
	private Dictionary<EffectType, ConsumableEffect> activeEffects = new Dictionary<EffectType, ConsumableEffect>();

	private void Awake()
	{
		if (audioSource == null)
		{
			audioSource = GetComponent<AudioSource>();
		}
		if (playerStats == null)
		{
			playerStats = GetComponent<PlayerStats>();
		}
		if (buffDebuffSystem == null)
		{
			buffDebuffSystem = UnityEngine.Object.FindAnyObjectByType<BuffDebuffSystem>();
		}
		if (playerTransform == null)
		{
			playerTransform = transform;
		}
		if (audioSource == null)
		{
			audioSource = gameObject.AddComponent<AudioSource>();
		}
	}

	private void Update()
	{
		UpdateActiveEffects();
	}

	public bool TryUseConsumable(InventoryItem item)
	{
		if (!CanUseConsumable(item, out var reason))
		{
			OnConsumeFailed?.Invoke(item, reason);
			PlayFailSound();
			return false;
		}
		ApplyConsumableEffects(item);
		PlayConsumeEffects(item);
		OnItemConsumed?.Invoke(item);
		return true;
	}

	public bool CanUseConsumable(InventoryItem item, out string reason)
	{
		reason = "";
		if (item == null)
		{
			reason = "Item is null";
			return false;
		}
		if (!item.IsConsumable)
		{
			reason = "Item is not consumable";
			return false;
		}
		if (playerStats != null && !playerStats.IsAlive)
		{
			reason = "Player is dead";
			return false;
		}
		switch (item.effectType)
		{
		case EffectType.Heal:
			if (playerStats != null && playerStats.IsAtFullHealth())
			{
				reason = "Already at full health";
				return false;
			}
			break;
		case EffectType.Buff:
		case EffectType.Temporary:
			if (!(buffDebuffSystem != null))
			{
			}
			break;
		case EffectType.None:
			if (item.instantHeatReduction <= 0f && item.instantHealthChange == 0)
			{
				reason = "Item has no effect";
				return false;
			}
			break;
		}
		return true;
	}

	public string GetActiveEffectsInfo()
	{
		string text = "=== Consumable Effects ===\n";
		if (buffDebuffSystem != null)
		{
			return text + buffDebuffSystem.GetActiveEffectsInfo();
		}
		if (activeEffects.Count == 0)
		{
			return "No active effects";
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("Active Consumable Effects (Legacy):");
		foreach (ConsumableEffect value in activeEffects.Values)
		{
			if (value.isPermanent)
			{
				stringBuilder.AppendLine($"  {value.effectType}: Permanent ({value.sourceItem.itemName})");
			}
			else
			{
				stringBuilder.AppendLine($"  {value.effectType}: {value.remainingDuration:F1}s remaining ({value.sourceItem.itemName})");
			}
		}
		return text + stringBuilder.ToString();
	}

	public void ClearAllEffects()
	{
		if (buffDebuffSystem != null)
		{
			buffDebuffSystem.RemoveAllEffects();
		}
		activeEffects.Clear();
	}

	public bool HasActiveEffect(EffectType effectType)
	{
		if (buffDebuffSystem != null)
		{
			StatusEffectType type = ConvertToStatusEffectType(effectType);
			return buffDebuffSystem.HasEffect(type);
		}
		return activeEffects.ContainsKey(effectType);
	}

	public float GetEffectRemainingDuration(EffectType effectType)
	{
		if (buffDebuffSystem != null)
		{
			StatusEffectType type = ConvertToStatusEffectType(effectType);
			List<StatusEffect> effectsOfType = buffDebuffSystem.GetEffectsOfType(type);
			if (effectsOfType.Count > 0)
			{
				return effectsOfType[0].remainingDuration;
			}
		}
		if (activeEffects.TryGetValue(effectType, out var value))
		{
			return value.remainingDuration;
		}
		return 0f;
	}

	private void ApplyConsumableEffects(InventoryItem item)
	{
		if (item.instantHeatReduction > 0f && playerStats != null)
		{
			bool flag = playerStats.CanAttackNow();
			playerStats.CoolHeat(item.instantHeatReduction);
			playerStats.ClearOverheatIfBelowMax();
			bool flag2 = playerStats.CanAttackNow();
			if ((!flag & flag2) && buffDebuffSystem != null)
			{
				buffDebuffSystem.OnAttacksReenabled?.Invoke();
			}
		}
		switch (item.effectType)
		{
		case EffectType.Heal:
			ApplyHealEffect(item);
			break;
		case EffectType.Mana:
			ApplyManaEffect(item);
			break;
		case EffectType.Buff:
			ApplyBuffEffect(item);
			break;
		case EffectType.Temporary:
			ApplyTemporaryEffect(item);
			break;
		case EffectType.Debuff:
			ApplyDebuffEffect(item);
			break;
		case EffectType.None:
			break;
		}
	}

	private void ApplyHealEffect(InventoryItem item)
	{
		if (item.instantHealthChange != 0 && playerStats != null)
		{
			if (item.instantHealthChange > 0)
			{
				playerStats.Heal(item.instantHealthChange);
				if (healEffect != null)
				{
					UnityEngine.Object.Destroy(UnityEngine.Object.Instantiate(healEffect, playerTransform.position, Quaternion.identity), 3f);
				}
			}
			else
			{
				playerStats.TakeDamage(-item.instantHealthChange);
			}
		}
		else if (playerStats != null && item.healAmount > 0)
		{
			playerStats.Heal(item.healAmount);
			if (healEffect != null)
			{
				UnityEngine.Object.Destroy(UnityEngine.Object.Instantiate(healEffect, playerTransform.position, Quaternion.identity), 3f);
			}
		}
	}

	private void ApplyManaEffect(InventoryItem item)
	{
	}

	private void ApplyBuffEffect(InventoryItem item)
	{
		if (buffDebuffSystem != null)
		{
			buffDebuffSystem.ApplyEffect(item);
		}
		else
		{
			ApplyLegacyEffect(item);
		}
	}

	private void ApplyTemporaryEffect(InventoryItem item)
	{
		if (buffDebuffSystem != null)
		{
			buffDebuffSystem.ApplyEffect(item);
		}
		else
		{
			ApplyLegacyEffect(item);
		}
	}

	private void ApplyDebuffEffect(InventoryItem item)
	{
		if (item.removesAllDebuffs && buffDebuffSystem != null)
		{
			buffDebuffSystem.RemoveAllDebuffs();
			return;
		}
		if (item.removesDebuffTypes.Count > 0 && buffDebuffSystem != null)
		{
			foreach (StatusEffectType removesDebuffType in item.removesDebuffTypes)
			{
				buffDebuffSystem.RemoveEffectsOfType(removesDebuffType);
			}
			return;
		}
		if (item.isDebuff && buffDebuffSystem != null)
		{
			buffDebuffSystem.ApplyEffect(item);
		}
	}

	private void ApplyLegacyEffect(InventoryItem item)
	{
		ConsumableEffect consumableEffect = new ConsumableEffect
		{
			effectType = item.effectType,
			remainingDuration = item.duration,
			totalDuration = item.duration,
			effectValue = item.healAmount,
			sourceItem = item,
			isPermanent = (item.duration <= 0f)
		};
		activeEffects[item.effectType] = consumableEffect;
		if (!consumableEffect.isPermanent)
		{
			_ = $"{item.duration}s";
		}
	}

	private void UpdateActiveEffects()
	{
		if (buffDebuffSystem != null)
		{
			return;
		}
		List<EffectType> list = new List<EffectType>();
		foreach (KeyValuePair<EffectType, ConsumableEffect> activeEffect in activeEffects)
		{
			ConsumableEffect value = activeEffect.Value;
			if (!value.isPermanent)
			{
				value.remainingDuration -= Time.deltaTime;
				if (value.remainingDuration <= 0f)
				{
					list.Add(activeEffect.Key);
				}
			}
		}
		foreach (EffectType item in list)
		{
			activeEffects.Remove(item);
		}
	}

	private StatusEffectType ConvertToStatusEffectType(EffectType effectType)
	{
		return effectType switch
		{
			EffectType.Heal => StatusEffectType.Regeneration, 
			EffectType.Buff => StatusEffectType.AttackBoost, 
			EffectType.Debuff => StatusEffectType.Poison, 
			EffectType.Temporary => StatusEffectType.SpeedBoost, 
			_ => StatusEffectType.AttackBoost, 
		};
	}

	private void PlayConsumeEffects(InventoryItem item)
	{
		AudioClip audioClip = item.consumeSound;
		if (audioClip == null)
		{
			switch (item.effectType)
			{
			case EffectType.Heal:
				audioClip = healSound ?? consumeSound;
				break;
			case EffectType.Buff:
			case EffectType.Temporary:
				audioClip = buffSound ?? consumeSound;
				break;
			default:
				audioClip = consumeSound;
				break;
			}
		}
		PlaySound(audioClip);
		if (consumeEffect != null)
		{
			UnityEngine.Object.Destroy(UnityEngine.Object.Instantiate(consumeEffect, playerTransform.position, Quaternion.identity), 2f);
		}
		GameObject gameObject = null;
		EffectType effectType = item.effectType;
		if (effectType == EffectType.Buff || effectType == EffectType.Temporary)
		{
			gameObject = buffEffect;
		}
		if (gameObject != null)
		{
			UnityEngine.Object.Destroy(UnityEngine.Object.Instantiate(gameObject, playerTransform.position, Quaternion.identity), 3f);
		}
	}

	private void PlaySound(AudioClip clip)
	{
		if (audioSource != null && clip != null)
		{
			audioSource.PlayOneShot(clip);
		}
	}

	private void PlayFailSound()
	{
		PlaySound(failSound);
	}

	[ContextMenu("Clear All Effects")]
	public void DebugClearAllEffects()
	{
		ClearAllEffects();
	}

	public void TestConsumable(InventoryItem testItem)
	{
		if (!(testItem == null))
		{
			TryUseConsumable(testItem);
		}
	}
}
