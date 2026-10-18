using UnityEngine;

namespace PixelCrushers.GridController;

public class TestItemUnlock : MonoBehaviour
{
	[SerializeField]
	private InventoryItem itemToUnlock;

	[SerializeField]
	private KeyCode unlockKey = KeyCode.U;

	private void Update()
	{
		if (InputManager.DebugUnlock != null && InputManager.DebugUnlock.WasPressedThisFrame() && !(itemToUnlock == null) && !(ItemUnlockManager.Instance == null))
		{
			ItemUnlockManager.Instance.UnlockItem(itemToUnlock);
		}
	}
}
