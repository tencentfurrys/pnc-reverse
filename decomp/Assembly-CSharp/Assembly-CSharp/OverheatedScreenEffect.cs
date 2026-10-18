using UnityEngine;

public class OverheatedScreenEffect : MonoBehaviour
{
	[Header("Material")]
	[SerializeField]
	private Material overheatedMaterial;

	[Header("Effect Settings")]
	[SerializeField]
	private float maxAlpha = 0.6f;

	[SerializeField]
	[Range(0f, 0.99f)]
	private float heatThreshold = 0.75f;

	[SerializeField]
	private float smoothSpeed = 5f;

	private PlayerStats playerStats;

	private float currentAlpha;

	private static readonly int VignetteAlphaID = Shader.PropertyToID("_Vingette_Alpha");

	private void Start()
	{
		playerStats = GetComponent<PlayerStats>();
		SetAlpha(0f);
	}

	private void Update()
	{
		if (!(playerStats == null))
		{
			float heatPercentage = playerStats.HeatPercentage;
			float target;
			if (playerStats.IsAttackLocked())
			{
				target = maxAlpha * Mathf.Clamp01(heatPercentage);
			}
			else if (heatPercentage >= heatThreshold)
			{
				float num = Mathf.InverseLerp(heatThreshold, 1f, heatPercentage);
				target = maxAlpha * num;
			}
			else
			{
				target = 0f;
			}
			currentAlpha = Mathf.MoveTowards(currentAlpha, target, smoothSpeed * Time.deltaTime);
			SetAlpha(currentAlpha);
		}
	}

	private void SetAlpha(float alpha)
	{
		if (overheatedMaterial != null)
		{
			overheatedMaterial.SetFloat(VignetteAlphaID, alpha);
		}
	}

	private void OnDisable()
	{
		SetAlpha(0f);
	}

	private void OnDestroy()
	{
		SetAlpha(0f);
	}
}
