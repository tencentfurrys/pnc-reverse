using UnityEngine;

namespace PixelCrushers.GridController
{
	[CreateAssetMenu(fileName = "Grab Damage Trinket", menuName = "Trinkets/Grab Damage Bonus")]
	public class GrabDamageBonusTrinketEffect : TrinketEffect
	{
		[Tooltip("Flat bonus damage added to each of the player's attacks while grabbed.")]
		public float bonusDamage = 5f;

		public override float GetGrabDamageBonus()
		{
			return bonusDamage;
		}
	}
}
