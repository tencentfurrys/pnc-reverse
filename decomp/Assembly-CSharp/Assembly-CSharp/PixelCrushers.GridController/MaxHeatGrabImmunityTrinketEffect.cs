using UnityEngine;

namespace PixelCrushers.GridController;

[CreateAssetMenu(fileName = "Max Heat Grab Shield Trinket", menuName = "Trinkets/Max Heat Grab Shield")]
public class MaxHeatGrabImmunityTrinketEffect : TrinketEffect
{
	private bool chargeUsed;

	public override bool TryConsumeMaxHeatGrabImmunity()
	{
		if (chargeUsed)
		{
			return false;
		}
		chargeUsed = true;
		return true;
	}
}
