using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace PixelCrushers.GridController;

[Serializable]
public class StatusEffect
{
	public string effectName = "Status Effect";

	public StatusEffectType effectType = StatusEffectType.AttackBoost;

	public float duration = 10f;

	public bool isPermanent;

	public bool isDebuff;

	public int stackCount = 1;

	public StatModifiers modifiers = new StatModifiers();

	public List<StatusEffectType> grantsImmunityTo = new List<StatusEffectType>();

	public bool preventsActions;

	public bool preventsMovement;

	public bool refreshDurationOnReapply;

	public Material screenEffectMaterial;

	public float timeApplied;

	public float remainingDuration;

	public GameObject visualEffectInstance;

	public InventoryItem sourceItem;

	public Dictionary<string, float> accumulatedValues = new Dictionary<string, float>();

	public UnityEvent OnEffectTick;
}
