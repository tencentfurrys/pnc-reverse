using System.Collections;
using UnityEngine;

namespace PixelCrushers.GridController;

public class ChestManagerInitializer : MonoBehaviour
{
	private IEnumerator Start()
	{
		if (ChestManager.Instance == null)
		{
			yield break;
		}
		ChestManager.Instance.ResetForNewLevel();
		if (PickupPoolManager.Instance != null)
		{
			yield return PickupPoolManager.Instance.ResetForNewLevel();
			yield return PickupPoolManager.Instance.PrewarmPools();
		}
		DunGenAStarIntegration dungenIntegration = Object.FindAnyObjectByType<DunGenAStarIntegration>();
		if (dungenIntegration != null)
		{
			while (!dungenIntegration.IsFullyReady())
			{
				yield return null;
			}
		}
		yield return null;
		if (ItemUnlockManager.Instance != null)
		{
			ItemUnlockManager.Instance.InjectUnlockedItems();
		}
		ChestManager.Instance.AllocateChestsAcrossLevel();
	}
}
