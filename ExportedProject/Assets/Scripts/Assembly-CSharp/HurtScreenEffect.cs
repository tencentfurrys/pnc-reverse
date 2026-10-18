using System.Collections;
using UnityEngine;

public class HurtScreenEffect : MonoBehaviour
{
	[Header("Material")]
	[SerializeField]
	private Material hurtMaterial;

	[Header("Flash Settings")]
	[SerializeField]
	private float flashAlpha = 0.6f;

	[SerializeField]
	private float flashDuration = 0.3f;

	[Header("Low Health Settings")]
	[SerializeField]
	private float lowHealthAlpha = 0.4f;

	[SerializeField]
	private float lowHealthThreshold = 0.2f;

	private PlayerStats playerStats;

	private Coroutine flashCoroutine;

	private bool isLowHealth;

	private static readonly int VignetteAlphaID = Shader.PropertyToID("_Vingette_Alpha");

	private void Start()
	{
		playerStats = GetComponent<PlayerStats>();
		SetAlpha(0f);
		playerStats.OnDamageTaken.AddListener(HandleDamageTaken);
		playerStats.OnHealthBarUpdated.AddListener(HandleHealthBarUpdated);
	}

	private void HandleDamageTaken(int damage)
	{
		if (!isLowHealth)
		{
			StartFlash();
		}
	}

	private void HandleHealthBarUpdated(float percentage)
	{
		if (percentage <= lowHealthThreshold)
		{
			if (!isLowHealth)
			{
				isLowHealth = true;
				StopFlash();
				SetAlpha(lowHealthAlpha);
			}
		}
		else if (isLowHealth)
		{
			isLowHealth = false;
			StartFlash();
		}
	}

	private void StartFlash()
	{
		StopFlash();
		flashCoroutine = StartCoroutine(FlashCoroutine());
	}

	private void StopFlash()
	{
		if (flashCoroutine != null)
		{
			StopCoroutine(flashCoroutine);
			flashCoroutine = null;
		}
	}

	private IEnumerator FlashCoroutine()
	{
		float elapsed = 0f;
		while (elapsed < flashDuration)
		{
			elapsed += Time.deltaTime;
			float alpha = Mathf.Lerp(flashAlpha, 0f, elapsed / flashDuration);
			SetAlpha(alpha);
			yield return null;
		}
		SetAlpha(0f);
		flashCoroutine = null;
	}

	private void SetAlpha(float alpha)
	{
		if (hurtMaterial != null)
		{
			hurtMaterial.SetFloat(VignetteAlphaID, alpha);
		}
	}

	private void OnDisable()
	{
		SetAlpha(0f);
	}

	private void OnDestroy()
	{
		SetAlpha(0f);
		if (playerStats != null)
		{
			playerStats.OnDamageTaken.RemoveListener(HandleDamageTaken);
			playerStats.OnHealthBarUpdated.RemoveListener(HandleHealthBarUpdated);
		}
	}
}
