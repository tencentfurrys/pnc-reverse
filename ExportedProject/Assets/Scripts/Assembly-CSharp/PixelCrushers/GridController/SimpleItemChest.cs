using UnityEngine;

namespace PixelCrushers.GridController
{
	public class SimpleItemChest : MonoBehaviour, IPlayerInteractable
	{
		[Header("Chest Settings")]
		[SerializeField]
		private ChestItemPool itemPool;

		[SerializeField]
		private GameObject itemPickupPrefab;

		[Header("2D Chest Sprites")]
		[SerializeField]
		private SpriteRenderer spriteRenderer;

		[SerializeField]
		private Sprite closedSprite;

		[SerializeField]
		private Sprite openSprite;

		[Tooltip("Empty GameObject where the item will be held above the chest")]
		[SerializeField]
		private Transform itemHolderPosition;

		[Header("Outline Effect")]
		[SerializeField]
		private float outlineAlphaInRange = 1f;

		[SerializeField]
		private float outlineAlphaOutOfRange;

		[Header("Audio")]
		[SerializeField]
		private AudioClip openSound;

		private bool isOpened;

		private bool isBeingLookedAt;

		private GameObject spawnedItem;

		private AudioSource audioSource;

		public bool CanInteract => !isOpened;

		public float InteractionRange => 0f;

		public bool IsOpened => isOpened;

		public bool PlayerInRange => isBeingLookedAt;

		private void Awake()
		{
			audioSource = GetComponent<AudioSource>();
			if (spriteRenderer == null)
			{
				spriteRenderer = GetComponent<SpriteRenderer>();
			}
			if (spriteRenderer != null && closedSprite != null)
			{
				spriteRenderer.sprite = closedSprite;
			}
			if (spriteRenderer != null && spriteRenderer.material != null)
			{
				spriteRenderer.material.SetFloat("_OutlineAlpha", outlineAlphaOutOfRange);
			}
		}

		private void Update()
		{
			UpdateOutlineAlpha();
		}

		private void OpenChest()
		{
			if (isOpened)
			{
				return;
			}
			isOpened = true;
			if (openSound != null)
			{
				if (audioSource != null)
				{
					audioSource.PlayOneShot(openSound);
				}
				else
				{
					AudioSource.PlayClipAtPoint(openSound, base.transform.position);
				}
			}
			if (spriteRenderer != null && openSprite != null)
			{
				spriteRenderer.sprite = openSprite;
			}
			SpawnItems();
		}

		private void SpawnItems()
		{
			if (itemPool == null || !itemPool.HasItems())
			{
				return;
			}
			InventoryItem inventoryItem = null;
			if (TrinketManager.Instance != null && TrinketManager.Instance.TryConsumeBestChestRarity())
			{
				inventoryItem = itemPool.GetHighestRarityItem();
			}
			if (inventoryItem == null)
			{
				inventoryItem = itemPool.GetRandomItem();
				if (TrinketManager.Instance != null && TrinketManager.Instance.HasChestRerollKeepBest())
				{
					InventoryItem randomItem = itemPool.GetRandomItem();
					if (randomItem != null && (inventoryItem == null || randomItem.rarity > inventoryItem.rarity))
					{
						inventoryItem = randomItem;
					}
				}
			}
			if (inventoryItem != null)
			{
				SpawnSingleItem(inventoryItem);
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

		public void OnLookedAt(GameObject player)
		{
			isBeingLookedAt = true;
		}

		public void OnLookedAway()
		{
			isBeingLookedAt = false;
		}

		public void OnInteracted()
		{
			if (!isOpened)
			{
				OpenChest();
			}
		}

		private void UpdateOutlineAlpha()
		{
			if (!(spriteRenderer == null) && !(spriteRenderer.material == null))
			{
				float value = ((isBeingLookedAt && !isOpened) ? outlineAlphaInRange : outlineAlphaOutOfRange);
				spriteRenderer.material.SetFloat("_OutlineAlpha", value);
			}
		}

		public void OpenChestManually()
		{
			if (!isOpened)
			{
				OpenChest();
			}
		}

		public void ResetChest()
		{
			isOpened = false;
			if (spawnedItem != null)
			{
				Object.Destroy(spawnedItem);
				spawnedItem = null;
			}
			if (spriteRenderer != null && closedSprite != null)
			{
				spriteRenderer.sprite = closedSprite;
			}
		}

		public void SetItemPool(ChestItemPool pool)
		{
			itemPool = pool;
		}

		private void OnValidate()
		{
			if (spriteRenderer == null)
			{
				spriteRenderer = GetComponent<SpriteRenderer>();
			}
		}

		private void OnDrawGizmosSelected()
		{
			if (itemHolderPosition != null)
			{
				Gizmos.color = Color.green;
				Gizmos.DrawWireSphere(itemHolderPosition.position, 0.2f);
				Gizmos.color = Color.blue;
				Gizmos.DrawLine(base.transform.position, itemHolderPosition.position);
				Gizmos.color = Color.red;
				Gizmos.DrawRay(itemHolderPosition.position, itemHolderPosition.right * 0.3f);
				Gizmos.color = Color.green;
				Gizmos.DrawRay(itemHolderPosition.position, itemHolderPosition.up * 0.3f);
				Gizmos.color = Color.blue;
				Gizmos.DrawRay(itemHolderPosition.position, itemHolderPosition.forward * 0.3f);
			}
			Collider component = GetComponent<Collider>();
			if (component != null)
			{
				Gizmos.color = Color.yellow;
				if (component is SphereCollider sphereCollider)
				{
					Gizmos.DrawWireSphere(base.transform.position, sphereCollider.radius);
				}
				else if (component is BoxCollider boxCollider)
				{
					Gizmos.DrawWireCube(base.transform.position, boxCollider.size);
				}
			}
		}

		[ContextMenu("Test Open")]
		public void TestOpen()
		{
			OpenChestManually();
		}

		[ContextMenu("Test Reset")]
		public void TestReset()
		{
			ResetChest();
		}
	}
}
