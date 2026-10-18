using CMF;
using UnityEngine;

namespace PixelCrushers.GridController
{
	[CreateAssetMenu(fileName = "Coyote Time Trinket", menuName = "Trinkets/Coyote Time")]
	public class CoyoteTimeTrinketEffect : TrinketEffect
	{
		[Tooltip("Grace period (seconds) after leaving the ground during which the player keeps walking before falling.")]
		public float coyoteSeconds = 0.2f;

		public override void OnEquip(TrinketManager manager)
		{
			AdvancedWalkerController advancedWalkerController = ResolveController(manager);
			if (advancedWalkerController != null)
			{
				advancedWalkerController.SetCoyoteTime(coyoteSeconds);
			}
		}

		public override void OnUnequip(TrinketManager manager)
		{
			AdvancedWalkerController advancedWalkerController = ResolveController(manager);
			if (advancedWalkerController != null)
			{
				advancedWalkerController.SetCoyoteTime(0f);
			}
		}

		private static AdvancedWalkerController ResolveController(TrinketManager manager)
		{
			if (manager == null)
			{
				return null;
			}
			return manager.GetComponent<AdvancedWalkerController>() ?? manager.GetComponentInParent<AdvancedWalkerController>();
		}
	}
}
