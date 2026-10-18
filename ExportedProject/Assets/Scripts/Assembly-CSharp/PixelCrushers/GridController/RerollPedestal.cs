using System.Collections.Generic;
using PixelCrushers.GridController.Glyphs;
using TMPro;
using UnityEngine;

namespace PixelCrushers.GridController
{
	public class RerollPedestal : MonoBehaviour, IPlayerInteractable
	{
		[Header("Re-Roll Targets")]
		[Tooltip("Pedestals refreshed by this one. Leave empty to auto-find them under the shop root.")]
		[SerializeField]
		private List<ShopItemPedestal> pedestalsToReroll = new List<ShopItemPedestal>();

		[Tooltip("If the list above is empty, collect every ShopItemPedestal under the shop root at Start.")]
		[SerializeField]
		private bool autoFindIfEmpty = true;

		[Tooltip("Root to search when auto-finding. Defaults to this object's topmost parent.")]
		[SerializeField]
		private Transform shopRoot;

		[Header("Cost")]
		[SerializeField]
		private int baseCost = 50;

		[Tooltip("Flat amount added to the cost for each re-roll already used.")]
		[SerializeField]
		private int costIncreasePerUse = 25;

		[Tooltip("Multiplier compounded per use. Leave at 1 for purely additive scaling.")]
		[SerializeField]
		private float costMultiplierPerUse = 1f;

		[Header("Interaction")]
		[SerializeField]
		private float interactionRange = 3f;

		[Header("Re-Roll Effect")]
		[Tooltip("Optional particle effect spawned when a re-roll is paid for.")]
		[SerializeField]
		private GameObject rerollEffect;

		[SerializeField]
		private float rerollEffectDuration = 2f;

		[Tooltip("Optional spawn point for the effect. Defaults to this pedestal's position.")]
		[SerializeField]
		private Transform effectSpawnPoint;

		[Header("Floating Text")]
		[Tooltip("Optional: Assign a parent GameObject containing all text elements. If not assigned, no floating text will be shown.")]
		[SerializeField]
		private GameObject textParentObject;

		[Tooltip("Turn the text to face the player while it is being looked at, so it reads from any approach angle.")]
		[SerializeField]
		private bool billboardText = true;

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
		private string titleText = "Re-Roll";

		[SerializeField]
		private string priceFormat = "Price: {0} (You have: {1})";

		[SerializeField]
		private string buyPrompt = "Press {0} to Re-Roll";

		[SerializeField]
		private string insufficientFundsText = "Insufficient Funds";

		[SerializeField]
		private string nothingToRerollText = "Nothing to Re-Roll";

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

		private int timesUsed;

		private bool isBeingLookedAt;

		private GameObject player;

		private int lastDisplayedCurrency = -1;

		private GameObject textContainer;

		private Color priceTextDefaultColor = Color.white;

		private Camera billboardCamera;

		public bool CanInteract => true;

		public float InteractionRange => interactionRange;

		public int CurrentCost
		{
			get
			{
				float f = (float)(baseCost + costIncreasePerUse * timesUsed) * Mathf.Pow(costMultiplierPerUse, timesUsed);
				return Mathf.Max(0, Mathf.RoundToInt(f));
			}
		}

		public int TimesUsed => timesUsed;

		public bool HasRerollableItems => CountRerollablePedestals() > 0;

		private void Start()
		{
			if (audioSource == null)
			{
				audioSource = GetComponent<AudioSource>();
			}
			CollectPedestals();
			if (textParentObject != null)
			{
				SetupAssignedTextParent();
			}
			HideText();
			if (spriteRenderer != null && spriteRenderer.material != null)
			{
				spriteRenderer.material.SetFloat("_OutlineAlpha", outlineAlpha);
			}
			if (textParentObject != null && (textContainer == null || itemNameText == null || priceText == null))
			{
				Debug.LogWarning("[RerollPedestal] '" + base.name + "' has a text parent assigned but some text references are missing " + $"(container: {textContainer != null}, name: {itemNameText != null}, price: {priceText != null}).", this);
			}
		}

		private void Update()
		{
			if (isBeingLookedAt)
			{
				UpdateFloatingText();
			}
		}

		private void LateUpdate()
		{
			if (billboardText && isBeingLookedAt && !(textContainer == null) && !(billboardCamera == null))
			{
				Vector3 forward = textContainer.transform.position - billboardCamera.transform.position;
				forward.y = 0f;
				if (forward.sqrMagnitude > 0.0001f)
				{
					textContainer.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
				}
			}
		}

		private void CollectPedestals()
		{
			if (!autoFindIfEmpty)
			{
				return;
			}
			bool flag = false;
			for (int i = 0; i < pedestalsToReroll.Count; i++)
			{
				if (pedestalsToReroll[i] != null)
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				Transform transform = ((shopRoot != null) ? shopRoot : base.transform.root);
				pedestalsToReroll.Clear();
				pedestalsToReroll.AddRange(transform.GetComponentsInChildren<ShopItemPedestal>(includeInactive: true));
			}
		}

		private int CountRerollablePedestals()
		{
			int num = 0;
			for (int i = 0; i < pedestalsToReroll.Count; i++)
			{
				ShopItemPedestal shopItemPedestal = pedestalsToReroll[i];
				if (shopItemPedestal != null && shopItemPedestal.CanReroll)
				{
					num++;
				}
			}
			return num;
		}

		private void SetupAssignedTextParent()
		{
			textContainer = textParentObject;
			if (itemNameText == null)
			{
				TextMeshPro[] componentsInChildren = textParentObject.GetComponentsInChildren<TextMeshPro>(includeInactive: true);
				for (int i = 0; i < componentsInChildren.Length; i++)
				{
					if (componentsInChildren[i] != priceText && componentsInChildren[i] != promptText)
					{
						itemNameText = componentsInChildren[i];
						break;
					}
				}
			}
			if (priceText == null)
			{
				TextMeshPro[] componentsInChildren2 = textParentObject.GetComponentsInChildren<TextMeshPro>(includeInactive: true);
				if (componentsInChildren2.Length > 1)
				{
					priceText = componentsInChildren2[1];
				}
			}
			if (promptText == null)
			{
				TextMeshPro[] componentsInChildren3 = textParentObject.GetComponentsInChildren<TextMeshPro>(includeInactive: true);
				if (componentsInChildren3.Length > 2)
				{
					promptText = componentsInChildren3[2];
				}
			}
			if (priceText != null)
			{
				priceTextDefaultColor = priceText.color;
			}
		}

		public void OnLookedAt(GameObject playerObj)
		{
			player = playerObj;
			isBeingLookedAt = true;
			if (billboardCamera == null)
			{
				billboardCamera = Camera.main;
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
			AttemptReroll();
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
			if (itemNameText != null)
			{
				itemNameText.text = titleText;
			}
			TextMeshPro textMeshPro = ((promptText != null) ? promptText : priceText);
			bool flag = textMeshPro == priceText;
			if (CountRerollablePedestals() <= 0)
			{
				if (textMeshPro != null)
				{
					textMeshPro.text = nothingToRerollText;
					textMeshPro.color = unaffordableColor;
				}
				if (priceText != null && !flag)
				{
					priceText.text = string.Empty;
				}
				lastDisplayedCurrency = -1;
				return;
			}
			if (CurrencyManager.Instance == null)
			{
				if (priceText != null)
				{
					priceText.text = $"Price: {CurrentCost} (Currency: ???)";
				}
				return;
			}
			int currency = CurrencyManager.Instance.GetCurrency();
			bool flag2 = CurrencyManager.Instance.CanAfford(CurrentCost);
			bool flag3 = flag2 || !flag;
			if (((priceText != null) & flag3) && currency != lastDisplayedCurrency)
			{
				lastDisplayedCurrency = currency;
				priceText.text = string.Format(priceFormat, CurrentCost, currency);
				priceText.color = priceTextDefaultColor;
			}
			if (textMeshPro == null)
			{
				return;
			}
			if (flag2)
			{
				if (!flag)
				{
					string actionMarkup = InputGlyphs.GetActionMarkup(InputManager.Interact, out var asset);
					if (asset != null)
					{
						textMeshPro.spriteAsset = asset;
					}
					textMeshPro.text = string.Format(buyPrompt, actionMarkup);
					textMeshPro.color = affordableColor;
				}
			}
			else
			{
				textMeshPro.text = insufficientFundsText;
				textMeshPro.color = unaffordableColor;
				if (flag)
				{
					lastDisplayedCurrency = -1;
				}
			}
		}

		private void AttemptReroll()
		{
			if (CountRerollablePedestals() <= 0)
			{
				PlayFailSound();
			}
			else
			{
				if (CurrencyManager.Instance == null)
				{
					return;
				}
				int currentCost = CurrentCost;
				if (!CurrencyManager.Instance.CanAfford(currentCost))
				{
					PlayFailSound();
				}
				else
				{
					if (!CurrencyManager.Instance.SpendCurrency(currentCost))
					{
						return;
					}
					int num = 0;
					for (int i = 0; i < pedestalsToReroll.Count; i++)
					{
						ShopItemPedestal shopItemPedestal = pedestalsToReroll[i];
						if (shopItemPedestal != null && shopItemPedestal.TryReroll())
						{
							num++;
						}
					}
					if (num == 0)
					{
						CurrencyManager.Instance.AddCurrency(currentCost);
						PlayFailSound();
						return;
					}
					timesUsed++;
					lastDisplayedCurrency = -1;
					PlaySuccessSound();
					PlayRerollEffect();
				}
			}
		}

		private void PlayRerollEffect()
		{
			if (rerollEffect == null)
			{
				return;
			}
			Vector3 position = ((effectSpawnPoint != null) ? effectSpawnPoint.position : base.transform.position);
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
					AudioSource.PlayClipAtPoint(purchaseSuccessSound, base.transform.position);
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
					AudioSource.PlayClipAtPoint(purchaseFailSound, base.transform.position);
				}
			}
		}

		public void SetPedestals(List<ShopItemPedestal> pedestals)
		{
			pedestalsToReroll = pedestals ?? new List<ShopItemPedestal>();
		}

		private void OnDrawGizmosSelected()
		{
			Vector3 vector = ((textPosition != null) ? textPosition.position : (base.transform.position + Vector3.up * textHeight));
			Gizmos.color = Color.yellow;
			Gizmos.DrawWireSphere(vector, 0.3f);
			Gizmos.DrawLine(base.transform.position, vector);
			Gizmos.color = Color.cyan;
			if (pedestalsToReroll != null)
			{
				for (int i = 0; i < pedestalsToReroll.Count; i++)
				{
					if (pedestalsToReroll[i] != null)
					{
						Gizmos.DrawLine(base.transform.position, pedestalsToReroll[i].transform.position);
					}
				}
			}
			Collider component = GetComponent<Collider>();
			if (component != null && component is SphereCollider sphereCollider)
			{
				Gizmos.color = Color.green;
				Gizmos.DrawWireSphere(base.transform.position, sphereCollider.radius);
			}
			else
			{
				Gizmos.color = Color.green;
				Gizmos.DrawWireSphere(base.transform.position, interactionRange);
			}
		}

		[ContextMenu("Test Re-Roll (Requires Play Mode)")]
		public void TestReroll()
		{
			if (Application.isPlaying)
			{
				AttemptReroll();
			}
		}

		[ContextMenu("Log Text Setup")]
		public void LogTextSetup()
		{
			Debug.Log(string.Format("[RerollPedestal] '{0}' lookedAt={1} camera={2}", base.name, isBeingLookedAt, (billboardCamera != null) ? billboardCamera.name : "null"), this);
			if (textContainer == null)
			{
				Debug.Log($"[RerollPedestal] textContainer is NULL (textParentObject assigned: {textParentObject != null})", this);
			}
			else
			{
				Transform transform = textContainer.transform;
				Debug.Log($"[RerollPedestal] container '{textContainer.name}' active={textContainer.activeInHierarchy} " + $"pos={transform.position} scale={transform.lossyScale} fwd={transform.forward}", this);
			}
			LogTextObject("itemName", itemNameText);
			LogTextObject("price", priceText);
			LogTextObject("prompt", promptText);
		}

		private void LogTextObject(string label, TextMeshPro text)
		{
			if (text == null)
			{
				Debug.Log("[RerollPedestal]   " + label + ": NULL", this);
				return;
			}
			Renderer component = text.GetComponent<Renderer>();
			Debug.Log($"[RerollPedestal]   {label}: '{text.text}' active={text.gameObject.activeInHierarchy} " + $"enabled={text.enabled} pos={text.transform.position} scale={text.transform.lossyScale} " + string.Format("fontSize={0} visible={1}", text.fontSize, (component != null) ? component.isVisible.ToString() : "no renderer"), this);
		}
	}
}
