using System.Collections.Generic;
using PixelCrushers.GridController;
using UnityEngine;

public class BuffScreenEffect : MonoBehaviour
{
	[Header("Material")]
	[SerializeField]
	private Material buffMaterial;

	[Header("Effect Settings")]
	[SerializeField]
	private float buffAlpha = 0.3f;

	[SerializeField]
	private float fadeInSpeed = 3f;

	[SerializeField]
	private float fadeOutSpeed = 5f;

	[Header("References")]
	[SerializeField]
	private BuffDebuffSystem buffDebuffSystem;

	private readonly List<StatusEffect> activeBuffs = new List<StatusEffect>();

	private float currentAlpha;

	private float targetAlpha;

	private static readonly int VignetteAlphaID = Shader.PropertyToID("_Vingette_Alpha");

	private void Start()
	{
		SetAlpha(0f);
		currentAlpha = 0f;
		targetAlpha = 0f;
		if (buffDebuffSystem != null)
		{
			buffDebuffSystem.OnBuffApplied.AddListener(HandleBuffApplied);
			buffDebuffSystem.OnEffectRemoved.AddListener(HandleEffectRemoved);
			buffDebuffSystem.OnEffectExpired.AddListener(HandleEffectExpired);
		}
	}

	private void Update()
	{
		float num = ((targetAlpha > currentAlpha) ? fadeInSpeed : fadeOutSpeed);
		currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, num * Time.deltaTime);
		SetAlpha(currentAlpha);
	}

	private void HandleBuffApplied(StatusEffect effect)
	{
		if (!effect.isDebuff && !(effect.screenEffectMaterial == null))
		{
			activeBuffs.Remove(effect);
			activeBuffs.Add(effect);
			ApplyMaterial(effect.screenEffectMaterial);
			targetAlpha = buffAlpha;
		}
	}

	private void HandleEffectRemoved(StatusEffect effect)
	{
		RemoveBuff(effect);
	}

	private void HandleEffectExpired(StatusEffect effect)
	{
		RemoveBuff(effect);
	}

	private void RemoveBuff(StatusEffect effect)
	{
		if (!effect.isDebuff)
		{
			activeBuffs.Remove(effect);
			if (activeBuffs.Count > 0)
			{
				StatusEffect statusEffect = activeBuffs[activeBuffs.Count - 1];
				ApplyMaterial(statusEffect.screenEffectMaterial);
				targetAlpha = buffAlpha;
			}
			else
			{
				targetAlpha = 0f;
			}
		}
	}

	private void SetAlpha(float alpha)
	{
		if (buffMaterial != null)
		{
			buffMaterial.SetFloat(VignetteAlphaID, alpha);
		}
	}

	private void ApplyMaterial(Material source)
	{
		if (buffMaterial != null && source != null)
		{
			float value = buffMaterial.GetFloat(VignetteAlphaID);
			buffMaterial.CopyPropertiesFromMaterial(source);
			buffMaterial.SetFloat(VignetteAlphaID, value);
		}
	}

	private void OnDisable()
	{
		SetAlpha(0f);
	}

	private void OnDestroy()
	{
		SetAlpha(0f);
		if (buffDebuffSystem != null)
		{
			buffDebuffSystem.OnBuffApplied.RemoveListener(HandleBuffApplied);
			buffDebuffSystem.OnEffectRemoved.RemoveListener(HandleEffectRemoved);
			buffDebuffSystem.OnEffectExpired.RemoveListener(HandleEffectExpired);
		}
	}
}
