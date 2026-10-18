using PixelCrushers.GridController.Glyphs;
using TMPro;
using UnityEngine;

namespace PixelCrushers.GridController;

public class ShopItemPedestal : MonoBehaviour, IPlayerInteractable
{
	[Header("Item Configuration")]
	[SerializeField]
	private ChestItemPool itemPool;

	[Tooltip("How many extra rolls to try when a re-roll returns the item that is already on the pedestal.")]
	[SerializeField]
	private int maxRerollAttempts = 5;

	private InventoryItem itemForSale;

	[SerializeField]
	private Transform itemHolderPosition;

	[SerializeField]
	private SpriteRenderer itemDisplayRenderer;

	[SerializeField]
	private float displaySpriteSize = 0.5f;

	[Header("Interaction")]
	[SerializeField]
	private float interactionRange = 3f;

	[Header("Re-Roll Effect")]
	[Tooltip("Optional particle effect spawned at the item position when this pedestal is re-rolled.")]
	[SerializeField]
	private GameObject rerollEffect;

	[SerializeField]
	private float rerollEffectDuration = 2f;

	[Header("Floating Text")]
	[Tooltip("Optional: Assign a parent GameObject containing all text elements. If not assigned, no floating text will be shown.")]
	[SerializeField]
	private GameObject textParentObject;

	[SerializeField]
	private Transform textPosition;

	[SerializeField]
	private float textHeight = 1.5f;

	[SerializeField]
	private TextMeshPro itemNameText;

	[SerializeField]
	private TextMeshPro priceText;

	[SerializeField]
	private TextMeshPro promptText;

	[SerializeField]
	private float fontSize = 0.5f;

	[SerializeField]
	private Color affordableColor = Color.green;

	[SerializeField]
	private Color unaffordableColor = Color.red;

	[Header("Text Content")]
	[SerializeField]
	private string priceFormat = "Price: {0} (You have: {1})";

	[SerializeField]
	private string buyPrompt = "Press {0} to Buy";

	[SerializeField]
	private string insufficientFundsText = "Insufficient Funds";

	[SerializeField]
	private string soldOutText = "Sold Out";

	[Header("Audio")]
	[SerializeField]
	private AudioClip purchaseSuccessSound;

	[SerializeField]
	private AudioClip purchaseFailSound;

	[SerializeField]
	private AudioSource audioSource;

	[Header("Outline Effect (Optional)")]
	[SerializeField]
	private SpriteRenderer spriteRenderer;

	[SerializeField]
	private float outlineAlpha = 1f;

	[Header("Debug")]
	private bool hasSpawnedItem;

	private bool isBeingLookedAt;

	private bool itemSold;

	private bool itemReserved;

	private GameObject player;

	private int lastDisplayedCurrency = -1;

	private InventoryUI playerInventoryUI;

	private GameObject textContainer;

	public bool CanInteract => true;

	public float InteractionRange => interactionRange;

	public bool ItemSold => itemSold;

	public bool HasItem
	{
		get
		{
			if (hasSpawnedItem && itemDisplayRenderer != null)
			{
				return itemDisplayRenderer.gameObject.activeSelf;
			}
			return false;
		}
	}

	public bool CanReroll
	{
		get
		{
			if (!itemSold && itemPool != null)
			{
				return itemPool.HasItems();
			}
			return false;
		}
	}

	private void Start()
	{
		if (audioSource == null)
		{
			audioSource = GetComponent<AudioSource>();
		}
		if (itemPool != null && itemPool.HasItems())
		{
			itemForSale = itemPool.GetRandomItem(recordAsSeen: false);
			ReserveCurrentItem();
		}
		CreateItemDisplay();
		if (textParentObject != null)
		{
			SetupAssignedTextParent();
		}
		HideText();
		if (spriteRenderer != null && spriteRenderer.material != null)
		{
			spriteRenderer.material.SetFloat("_OutlineAlpha", outlineAlpha);
		}
	}

	private void Update()
	{
		if (isBeingLookedAt && !itemSold)
		{
			UpdateFloatingText();
		}
	}

	private void OnDestroy()
	{
		ReleaseCurrentItem();
	}

	private void ReserveCurrentItem()
	{
		if (!itemReserved && !(itemForSale == null))
		{
			ChestItemPool.ReserveItem(itemForSale);
			itemReserved = true;
		}
	}

	private void ReleaseCurrentItem()
	{
		if (itemReserved)
		{
			ChestItemPool.ReleaseItem(itemForSale);
			itemReserved = false;
		}
	}

	private void CreateItemDisplay()
	{
		if (!hasSpawnedItem && !itemSold && !(itemForSale == null) && !(itemForSale.itemIcon == null) && !(itemDisplayRenderer == null))
		{
			itemDisplayRenderer.sprite = itemForSale.itemIcon;
			itemDisplayRenderer.transform.localScale = Vector3.one * displaySpriteSize;
			itemDisplayRenderer.gameObject.SetActive(value: true);
			hasSpawnedItem = true;
		}
	}

	private void HideItemDisplay()
	{
		if (itemDisplayRenderer != null)
		{
			itemDisplayRenderer.gameObject.SetActive(value: false);
		}
	}

	private void PlayRerollEffect()
	{
		if (rerollEffect == null)
		{
			return;
		}
		Vector3 position = transform.position;
		if (itemHolderPosition != null)
		{
			position = itemHolderPosition.position;
		}
		else if (itemDisplayRenderer != null)
		{
			position = itemDisplayRenderer.transform.position;
		}
		GameObject gameObject = Object.Instantiate(rerollEffect, position, Quaternion.identity);
		Object.Destroy(gameObject, rerollEffectDuration);
		ParticleSystem component = gameObject.GetComponent<ParticleSystem>();
		if (component != null)
		{
			component.Play();
			if (component.main.duration > rerollEffectDuration)
			{
				Object.Destroy(gameObject, component.main.duration + component.main.startLifetime.constantMax);
			}
		}
	}

	private void SetupAssignedTextParent()
	{
		textContainer = textParentObject;
		if (itemNameText == null)
		{
			itemNameText = textParentObject.GetComponentInChildren<TextMeshPro>();
		}
		if (priceText == null)
		{
			TextMeshPro[] componentsInChildren = textParentObject.GetComponentsInChildren<TextMeshPro>();
			if (componentsInChildren.Length > 1)
			{
				priceText = componentsInChildren[1];
			}
		}
		if (promptText == null)
		{
			TextMeshPro[] componentsInChildren2 = textParentObject.GetComponentsInChildren<TextMeshPro>();
			if (componentsInChildren2.Length > 2)
			{
				promptText = componentsInChildren2[2];
			}
		}
	}

	public void OnLookedAt(GameObject playerObj)
	{
		player = playerObj;
		isBeingLookedAt = true;
		playerInventoryUI = playerObj.GetComponent<InventoryUI>();
		if (playerInventoryUI == null)
		{
			playerInventoryUI = Object.FindAnyObjectByType<InventoryUI>();
		}
		ShowText();
	}

	public void OnLookedAway()
	{
		isBeingLookedAt = false;
		player = null;
		HideText();
	}

	public void OnInteracted()
	{
		if (!itemSold)
		{
			AttemptPurchase();
		}
	}

	private void ShowText()
	{
		if (textContainer != null)
		{
			textContainer.SetActive(value: true);
		}
	}

	private void HideText()
	{
		if (textContainer != null)
		{
			textContainer.SetActive(value: false);
		}
	}

	private void UpdateFloatingText()
	{
		if (itemForSale == null)
		{
			return;
		}
		if (itemNameText != null)
		{
			itemNameText.text = itemForSale.itemName;
		}
		if (CurrencyManager.Instance != null)
		{
			int currency = CurrencyManager.Instance.GetCurrency();
			if (currency != lastDisplayedCurrency)
			{
				lastDisplayedCurrency = currency;
				if (priceText != null)
				{
					priceText.text = string.Format(priceFormat, itemForSale.price, currency);
				}
			}
			if (!(promptText != null))
			{
				return;
			}
			if (CurrencyManager.Instance.CanAfford(itemForSale.price))
			{
				string actionMarkup = InputGlyphs.GetActionMarkup(InputManager.Interact, out var asset);
				if (asset != null)
				{
					promptText.spriteAsset = asset;
				}
				promptText.text = string.Format(buyPrompt, actionMarkup);
				promptText.color = affordableColor;
			}
			else
			{
				promptText.text = insufficientFundsText;
				promptText.color = unaffordableColor;
			}
		}
		else if (priceText != null)
		{
			priceText.text = $"Price: {itemForSale.price} (Currency: ???)";
		}
	}

	private void AttemptPurchase()
	{
		if (!hasSpawnedItem || itemDisplayRenderer == null || itemForSale == null || itemForSale.price <= 0 || CurrencyManager.Instance == null)
		{
			return;
		}
		if (!CurrencyManager.Instance.CanAfford(itemForSale.price))
		{
			PlayFailSound();
			return;
		}
		if (playerInventoryUI == null)
		{
			playerInventoryUI = Object.FindAnyObjectByType<InventoryUI>();
		}
		if (!(playerInventoryUI == null))
		{
			if (!playerInventoryUI.TryAddItem(itemForSale))
			{
				PlayFailSound();
			}
			else if (CurrencyManager.Instance.SpendCurrency(itemForSale.price))
			{
				CompletePurchase();
			}
		}
	}

	private void CompletePurchase()
	{
		PlaySuccessSound();
		HideItemDisplay();
		ReleaseCurrentItem();
		ChestItemPool.RecordItemSeen(itemForSale);
		itemSold = true;
		hasSpawnedItem = false;
		if (itemNameText != null)
		{
			itemNameText.text = soldOutText;
		}
		if (priceText != null)
		{
			priceText.gameObject.SetActive(value: false);
		}
		if (promptText != null)
		{
			promptText.gameObject.SetActive(value: false);
		}
	}

	private void PlaySuccessSound()
	{
		if (!(purchaseSuccessSound == null))
		{
			if (audioSource != null)
			{
				audioSource.PlayOneShot(purchaseSuccessSound);
			}
			else
			{
				AudioSource.PlayClipAtPoint(purchaseSuccessSound, transform.position);
			}
		}
	}

	private void PlayFailSound()
	{
		if (!(purchaseFailSound == null))
		{
			if (audioSource != null)
			{
				audioSource.PlayOneShot(purchaseFailSound);
			}
			else
			{
				AudioSource.PlayClipAtPoint(purchaseFailSound, transform.position);
			}
		}
	}

	public void ResetPedestal()
	{
		HideItemDisplay();
		ReleaseCurrentItem();
		hasSpawnedItem = false;
		itemSold = false;
		isBeingLookedAt = false;
		player = null;
		lastDisplayedCurrency = -1;
		if (itemPool != null && itemPool.HasItems())
		{
			itemForSale = itemPool.GetRandomItem(recordAsSeen: false);
			ReserveCurrentItem();
		}
		else
		{
			itemForSale = null;
		}
		CreateItemDisplay();
		if (textParentObject != null)
		{
			SetupAssignedTextParent();
		}
		HideText();
	}

	public bool TryReroll()
	{
		if (!CanReroll)
		{
			return false;
		}
		InventoryItem inventoryItem = itemForSale;
		InventoryItem randomItem = itemPool.GetRandomItem(recordAsSeen: false);
		for (int i = 0; i < maxRerollAttempts; i++)
		{
			if (!(randomItem != null))
			{
				break;
			}
			if (!(randomItem == inventoryItem))
			{
				break;
			}
			randomItem = itemPool.GetRandomItem(recordAsSeen: false);
		}
		if (randomItem == null)
		{
			return false;
		}
		ReleaseCurrentItem();
		itemForSale = randomItem;
		ReserveCurrentItem();
		hasSpawnedItem = false;
		CreateItemDisplay();
		if (!hasSpawnedItem)
		{
			HideItemDisplay();
		}
		PlayRerollEffect();
		lastDisplayedCurrency = -1;
		if (isBeingLookedAt)
		{
			UpdateFloatingText();
		}
		return true;
	}

	public void SetItemPool(ChestItemPool pool)
	{
		itemPool = pool;
		ResetPedestal();
	}

	private void OnDrawGizmosSelected()
	{
		if (itemHolderPosition != null)
		{
			Gizmos.color = Color.cyan;
			Gizmos.DrawWireSphere(itemHolderPosition.position, 0.2f);
			Gizmos.color = Color.magenta;
			Gizmos.DrawLine(transform.position, itemHolderPosition.position);
			Vector3 vector = ((textPosition != null) ? textPosition.position : (itemHolderPosition.position + Vector3.up * textHeight));
			Gizmos.color = Color.yellow;
			Gizmos.DrawWireSphere(vector, 0.3f);
			Gizmos.DrawLine(itemHolderPosition.position, vector);
			Gizmos.color = Color.red;
			Gizmos.DrawRay(itemHolderPosition.position, itemHolderPosition.right * 0.3f);
			Gizmos.color = Color.green;
			Gizmos.DrawRay(itemHolderPosition.position, itemHolderPosition.up * 0.3f);
			Gizmos.color = Color.blue;
			Gizmos.DrawRay(itemHolderPosition.position, itemHolderPosition.forward * 0.3f);
		}
		Collider component = GetComponent<Collider>();
		if (component != null && component is SphereCollider sphereCollider)
		{
			Gizmos.color = Color.green;
			Gizmos.DrawWireSphere(transform.position, sphereCollider.radius);
		}
		else
		{
			Gizmos.color = Color.green;
			Gizmos.DrawWireSphere(transform.position, interactionRange);
		}
	}

	[ContextMenu("Test Reset")]
	public void TestReset()
	{
		ResetPedestal();
	}

	[ContextMenu("Test Purchase (Requires Play Mode)")]
	public void TestPurchase()
	{
		if (Application.isPlaying)
		{
			AttemptPurchase();
		}
	}
}
