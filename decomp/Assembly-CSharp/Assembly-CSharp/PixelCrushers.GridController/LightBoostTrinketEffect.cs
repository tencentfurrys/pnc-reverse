using UnityEngine;

namespace PixelCrushers.GridController;

[CreateAssetMenu(fileName = "Light Boost Trinket", menuName = "Trinkets/Bright Light")]
public class LightBoostTrinketEffect : TrinketEffect
{
	[Tooltip("Multiplier applied to the light's range (its size/reach).")]
	public float rangeMultiplier = 1.5f;

	[Tooltip("Multiplier applied to the light's intensity (its brightness).")]
	public float intensityMultiplier = 1.5f;

	private Light targetLight;

	private float originalRange;

	private float originalIntensity;

	private bool applied;

	public override void OnEquip(TrinketManager manager)
	{
		targetLight = ((manager != null) ? manager.ResolvePlayerLight() : null);
		if (!(targetLight == null))
		{
			originalRange = targetLight.range;
			originalIntensity = targetLight.intensity;
			targetLight.range = originalRange * rangeMultiplier;
			targetLight.intensity = originalIntensity * intensityMultiplier;
			applied = true;
		}
	}

	public override void OnUnequip(TrinketManager manager)
	{
		if (applied && !(targetLight == null))
		{
			targetLight.range = originalRange;
			targetLight.intensity = originalIntensity;
			applied = false;
		}
	}
}
