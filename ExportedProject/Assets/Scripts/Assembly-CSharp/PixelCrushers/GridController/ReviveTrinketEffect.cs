using UnityEngine;

namespace PixelCrushers.GridController
{
	[CreateAssetMenu(fileName = "Revive Trinket", menuName = "Trinkets/Revive")]
	public class ReviveTrinketEffect : TrinketEffect
	{
		[Tooltip("Fraction of max HP restored when the player is revived (0.5 = 50%).")]
		[Range(0f, 1f)]
		public float reviveHealthFraction = 0.5f;

		private bool chargeUsed;

		public override bool TryConsumeRevive(out float healthFraction)
		{
			if (chargeUsed)
			{
				healthFraction = 0f;
				return false;
			}
			chargeUsed = true;
			healthFraction = reviveHealthFraction;
			return true;
		}
	}
}
