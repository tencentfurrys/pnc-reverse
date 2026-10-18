using System;
using System.Collections.Generic;
using PixelCrushers.GridController.Glyphs;
using TMPro;
using UnityEngine;

namespace PixelCrushers.GridController;

public class UnlockPedestal : MonoBehaviour, IPlayerInteractable
{
	[Serializable]
	public class UnlockableItemEntry
	{
		public InventoryItem item;

		public int unlockPrice = 100;
	}

	[Header("Unlock Configuration")]
	[SerializeField]
	private List<UnlockableItemEntry> unlockableItems = new List<UnlockableItemEntry>();

	[SerializeField]
	private Transform itemHolderPosition;

	[SerializeField]
	private SpriteRenderer itemDisplayRenderer;

	[SerializeField]
	private float displaySpriteSize = 0.5f;

	[Header("Interaction")]
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
	private string soldOutText = "Sold Out";

	[SerializeField]
	private string priceFormat = "Price: {0} (You have: {1})";

	[SerializeField]
	private string buyPrompt = "Press {0} to Unlock";

	[SerializeField]
	private string insufficientFundsText = "Insufficient Funds";

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
	private InventoryItem currentItem;

	private int currentPrice;

	private int currentIndex;

	private bool isBeingLookedAt;

	private bool isSoldOut;

	private GameObject player;

	private int lastDisplayedCurrency = -1;

	private InventoryUI playerInventoryUI;

	private GameObject textContainer;

	private CurrencyDisplayUI currencyDisplayUI;

	public bool CanInteract => true;

	public float InteractionRange => 0f;

	public bool HasItem => currentItem != null;

	public InventoryItem CurrentItem => currentItem;

	public bool IsSoldOut => isSoldOut;

	public int RemainingLockedCount
	{
		get
		{
			if (ItemUnlockManager.Instance == null)
			{
				return 0;
			}
			int num = 0;
			for (int i = currentIndex; i < unlockableItems.Count; i++)
			{
				if (unlockableItems[i].item != null && !ItemUnlockManager.Instance.IsItemUnlocked(unlockableItems[i].item))
				{
					num++;
				}
			}
			return num;
		}
	}

	private void Start()
	{
		if (audioSource == null)
		{
			audioSource = GetComponent<AudioSource>();
		}
		currentItem = FindNextLockedItem();
		if (currentItem != null)
		{
			UpdateItemDisplay(currentItem);
		}
		else
		{
			HideItemDisplay();
			isSoldOut = true;
		}
		if (textParentObject != null)
		{
			SetupAssignedTextParent();
		}
		if (isSoldOut)
		{
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
		HideText();
		if (spriteRenderer != null && spriteRenderer.material != null)
		{
			spriteRenderer.material.SetFloat("_OutlineAlpha", outlineAlpha);
		}
	}

	private void Update()
	{
		if ((!(currentItem == null) || isSoldOut) && isBeingLookedAt && !isSoldOut)
		{
			UpdateFloatingText();
		}
	}

	private InventoryItem FindNextLockedItem()
	{
		if (ItemUnlockManager.Instance == null)
		{
			return null;
		}
		for (int i = currentIndex; i < unlockableItems.Count; i++)
		{
			UnlockableItemEntry unlockableItemEntry = unlockableItems[i];
			if (unlockableItemEntry.item != null && !ItemUnlockManager.Instance.IsItemUnlocked(unlockableItemEntry.item))
			{
				currentIndex = i;
				currentPrice = unlockableItemEntry.unlockPrice;
				return unlockableItemEntry.item;
			}
		}
		currentIndex = unlockableItems.Count;
		return null;
	}

	private void UpdateItemDisplay(InventoryItem item)
	{
		if (!(itemDisplayRenderer == null) && !(item == null) && !(item.itemIcon == null))
		{
			itemDisplayRenderer.sprite = item.itemIcon;
			itemDisplayRenderer.transform.localScale = Vector3.one * displaySpriteSize;
			itemDisplayRenderer.gameObject.SetActive(value: true);
		}
	}

	private void HideItemDisplay()
	{
		if (itemDisplayRenderer != null)
		{
			itemDisplayRenderer.gameObject.SetActive(value: false);
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
			playerInventoryUI = UnityEngine.Object.FindAnyObjectByType<InventoryUI>();
		}
		ShowText();
		if (currencyDisplayUI == null)
		{
			currencyDisplayUI = UnityEngine.Object.FindAnyObjectByType<CurrencyDisplayUI>();
		}
		if (currencyDisplayUI != null)
		{
			currencyDisplayUI.ShowForShop();
		}
	}

	public void OnLookedAway()
	{
		isBeingLookedAt = false;
		player = null;
		HideText();
		if (currencyDisplayUI != null)
		{
			currencyDisplayUI.HideForShop();
		}
	}

	public void OnInteracted()
	{
		if (!isSoldOut)
		{
			AttemptUnlock();
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
		if (currentItem == null)
		{
			return;
		}
		if (itemNameText != null)
		{
			itemNameText.text = currentItem.itemName;
		}
		if (!(CurrencyManager.Instance != null))
		{
			return;
		}
		int currency = CurrencyManager.Instance.GetCurrency();
		if (currency != lastDisplayedCurrency)
		{
			lastDisplayedCurrency = currency;
			if (priceText != null)
			{
				priceText.text = string.Format(priceFormat, currentPrice, currency);
			}
		}
		if (!(promptText != null))
		{
			return;
		}
		if (CurrencyManager.Instance.CanAfford(currentPrice))
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

	private void AttemptUnlock()
	{
		if (currentItem == null || CurrencyManager.Instance == null)
		{
			return;
		}
		if (!CurrencyManager.Instance.CanAfford(currentPrice))
		{
			PlayFailSound();
			return;
		}
		if (playerInventoryUI == null)
		{
			playerInventoryUI = UnityEngine.Object.FindAnyObjectByType<InventoryUI>();
		}
		if (playerInventoryUI == null)
		{
			return;
		}
		if (!playerInventoryUI.TryAddItem(currentItem))
		{
			PlayFailSound();
		}
		else
		{
			if (!CurrencyManager.Instance.SpendCurrency(currentPrice))
			{
				return;
			}
			if (ItemUnlockManager.Instance != null)
			{
				ItemUnlockManager.Instance.UnlockItem(currentItem);
			}
			PlaySuccessSound();
			currentIndex++;
			currentItem = FindNextLockedItem();
			lastDisplayedCurrency = -1;
			if (currentItem != null)
			{
				UpdateItemDisplay(currentItem);
				return;
			}
			HideItemDisplay();
			isSoldOut = true;
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
		}
		Collider component = GetComponent<Collider>();
		if (component != null && component is SphereCollider sphereCollider)
		{
			Gizmos.color = Color.green;
			Gizmos.DrawWireSphere(transform.position, sphereCollider.radius);
		}
	}

	[ContextMenu("Test Unlock (Requires Play Mode)")]
	public void TestUnlock()
	{
		if (Application.isPlaying)
		{
			AttemptUnlock();
		}
	}
}
