using PixelCrushers.GridController;
using UnityEngine;

public class ArenaTriggerChest : MonoBehaviour, IPlayerInteractable
{
	[Header("Arena Reference")]
	[SerializeField]
	private ArenaEnemySpawner arenaSpawner;

	[Header("Item Settings")]
	[SerializeField]
	private ChestItemPool itemPool;

	[SerializeField]
	private GameObject itemPickupPrefab;

	[SerializeField]
	private Transform itemHolderPosition;

	[Header("Chest Visuals - 3D")]
	[SerializeField]
	private Animator chestAnimator;

	[SerializeField]
	private string openAnimationTrigger = "Open";

	[Header("Chest Visuals - 2D")]
	[SerializeField]
	private SpriteRenderer spriteRenderer;

	[SerializeField]
	private Sprite closedSprite;

	[SerializeField]
	private Sprite openSprite;

	[Header("Outline Effect")]
	[SerializeField]
	private float outlineAlphaInRange = 1f;

	[SerializeField]
	private float outlineAlphaOutOfRange;

	[Header("Audio")]
	[SerializeField]
	private AudioClip openSound;

	[Header("UI")]
	[SerializeField]
	private GameObject interactPrompt;

	private bool isOpened;

	private bool isBeingLookedAt;

	private AudioSource audioSource;

	private GameObject spawnedItem;

	public bool CanInteract => !isOpened;

	public float InteractionRange => 0f;

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
		if (interactPrompt != null)
		{
			interactPrompt.SetActive(value: false);
		}
	}

	private void Update()
	{
		if (spriteRenderer != null && spriteRenderer.material != null)
		{
			float value = ((isBeingLookedAt && !isOpened) ? outlineAlphaInRange : outlineAlphaOutOfRange);
			spriteRenderer.material.SetFloat("_OutlineAlpha", value);
		}
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
				AudioSource.PlayClipAtPoint(openSound, transform.position);
			}
		}
		if (chestAnimator != null && !string.IsNullOrEmpty(openAnimationTrigger))
		{
			chestAnimator.SetTrigger(openAnimationTrigger);
		}
		if (spriteRenderer != null && openSprite != null)
		{
			spriteRenderer.sprite = openSprite;
		}
		if (interactPrompt != null)
		{
			interactPrompt.SetActive(value: false);
		}
		SpawnItem();
		if (arenaSpawner != null)
		{
			arenaSpawner.ActivateArena();
		}
	}

	private void SpawnItem()
	{
		if (itemPool == null || !itemPool.HasItems() || itemHolderPosition == null)
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
		if (inventoryItem == null)
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
				component.SetItemData(inventoryItem);
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
		if (interactPrompt != null && !isOpened)
		{
			interactPrompt.SetActive(value: true);
		}
	}

	public void OnLookedAway()
	{
		isBeingLookedAt = false;
		if (interactPrompt != null)
		{
			interactPrompt.SetActive(value: false);
		}
	}

	public void OnInteracted()
	{
		if (!isOpened)
		{
			OpenChest();
		}
	}

	private void OnValidate()
	{
		if (spriteRenderer == null)
		{
			spriteRenderer = GetComponent<SpriteRenderer>();
		}
	}
}
