using UnityEngine;

namespace PixelCrushers.GridController;

public class SimpleItemPedestal : MonoBehaviour
{
	[Header("Pedestal Settings")]
	[SerializeField]
	private ChestItemPool itemPool;

	[SerializeField]
	private GameObject itemPickupPrefab;

	[Header("Item Holder")]
	[Tooltip("Empty GameObject where the item will be placed on the pedestal")]
	[SerializeField]
	private Transform itemHolderPosition;

	[Header("Outline Effect (Optional)")]
	[SerializeField]
	private SpriteRenderer spriteRenderer;

	[SerializeField]
	private float outlineAlpha = 1f;

	private bool hasSpawnedItem;

	private GameObject spawnedItem;

	public bool HasSpawnedItem => hasSpawnedItem;

	private void Start()
	{
		SpawnItemOnPedestal();
		if (spriteRenderer != null && spriteRenderer.material != null)
		{
			spriteRenderer.material.SetFloat("_OutlineAlpha", outlineAlpha);
		}
	}

	private void SpawnItemOnPedestal()
	{
		if (!hasSpawnedItem && !(itemPool == null) && itemPool.HasItems())
		{
			InventoryItem randomItem = itemPool.GetRandomItem();
			if (randomItem != null)
			{
				SpawnSingleItem(randomItem);
				hasSpawnedItem = true;
			}
		}
	}

	private void SpawnSingleItem(InventoryItem itemToSpawn)
	{
		if (itemToSpawn == null || itemHolderPosition == null)
		{
			return;
		}
		Vector3 position = itemHolderPosition.position;
		Quaternion rotation = itemHolderPosition.rotation;
		if (itemPickupPrefab != null)
		{
			if (PickupPoolManager.Instance != null)
			{
				spawnedItem = PickupPoolManager.Instance.GetItemPickup(position, rotation);
			}
			else
			{
				spawnedItem = Object.Instantiate(itemPickupPrefab, position, rotation);
			}
			InventoryItemPickup component = spawnedItem.GetComponent<InventoryItemPickup>();
			if (component != null)
			{
				component.SetItemData(itemToSpawn);
				component.SetHeldInChest(held: true);
			}
			Rigidbody component2 = spawnedItem.GetComponent<Rigidbody>();
			if (component2 != null)
			{
				component2.isKinematic = true;
				component2.useGravity = false;
			}
			spawnedItem.transform.SetParent(itemHolderPosition);
		}
	}

	public void ResetPedestal()
	{
		hasSpawnedItem = false;
		if (spawnedItem != null)
		{
			Object.Destroy(spawnedItem);
			spawnedItem = null;
		}
	}

	public void SetItemPool(ChestItemPool pool)
	{
		itemPool = pool;
	}

	public void SpawnItem()
	{
		if (!hasSpawnedItem)
		{
			SpawnItemOnPedestal();
		}
	}

	private void OnDrawGizmosSelected()
	{
		if (itemHolderPosition != null)
		{
			Gizmos.color = Color.cyan;
			Gizmos.DrawWireSphere(itemHolderPosition.position, 0.2f);
			Gizmos.color = Color.magenta;
			Gizmos.DrawLine(transform.position, itemHolderPosition.position);
			Gizmos.color = Color.red;
			Gizmos.DrawRay(itemHolderPosition.position, itemHolderPosition.right * 0.3f);
			Gizmos.color = Color.green;
			Gizmos.DrawRay(itemHolderPosition.position, itemHolderPosition.up * 0.3f);
			Gizmos.color = Color.blue;
			Gizmos.DrawRay(itemHolderPosition.position, itemHolderPosition.forward * 0.3f);
		}
	}

	[ContextMenu("Test Spawn")]
	public void TestSpawn()
	{
		SpawnItem();
	}

	[ContextMenu("Test Reset")]
	public void TestReset()
	{
		ResetPedestal();
	}
}
