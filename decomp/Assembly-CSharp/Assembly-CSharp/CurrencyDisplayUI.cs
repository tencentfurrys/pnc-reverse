using PixelCrushers.GridController;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CurrencyDisplayUI : MonoBehaviour
{
	[Header("UI References")]
	[SerializeField]
	private TextMeshProUGUI currencyText;

	[SerializeField]
	private Text legacyText;

	[SerializeField]
	private Image currencyIcon;

	[SerializeField]
	private Animator currencyAnimator;

	[Header("Inventory Integration")]
	[SerializeField]
	private InventoryUI inventoryUI;

	[SerializeField]
	private bool autoFindInventoryUI = true;

	[Header("Display Settings")]
	[SerializeField]
	private string currencyPrefix = "$";

	[SerializeField]
	private string currencySuffix = "";

	[SerializeField]
	private bool useThousandsSeparator = true;

	[SerializeField]
	private bool hideWhenZero;

	[Header("Animation Settings")]
	[SerializeField]
	private bool animateChanges = true;

	[SerializeField]
	private float animationDuration = 0.3f;

	[SerializeField]
	private AnimationCurve animationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

	[SerializeField]
	private Color gainColor = Color.green;

	[SerializeField]
	private Color spendColor = Color.red;

	[SerializeField]
	private float colorFlashDuration = 0.5f;

	[Header("Scale Animation")]
	[SerializeField]
	private bool scaleOnChange = true;

	[SerializeField]
	private float scaleMultiplier = 1.2f;

	[SerializeField]
	private float scaleDuration = 0.2f;

	private int shopNearbyCount;

	private int displayedCurrency;

	private int targetCurrency;

	private float animationTime;

	private bool isAnimating;

	private Color originalTextColor;

	private float colorFlashTime;

	private bool isFlashingColor;

	private Vector3 originalScale;

	private float scaleAnimationTime;

	private bool isScaling;

	private bool wasShowing;

	private void Awake()
	{
		if (currencyText != null)
		{
			originalTextColor = currencyText.color;
			originalScale = currencyText.transform.localScale;
		}
		else if (legacyText != null)
		{
			originalTextColor = legacyText.color;
			originalScale = legacyText.transform.localScale;
		}
		if (animationCurve == null || animationCurve.keys.Length == 0)
		{
			animationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
		}
		if (autoFindInventoryUI && inventoryUI == null)
		{
			inventoryUI = Object.FindAnyObjectByType<InventoryUI>();
		}
		if (currencyAnimator == null)
		{
			currencyAnimator = GetComponent<Animator>();
		}
	}

	private void Start()
	{
		if (CurrencyManager.Instance != null)
		{
			SubscribeToCurrencyManager();
		}
		else
		{
			Invoke("TrySubscribe", 0.1f);
		}
		SubscribeToInventoryEvents();
	}

	private void TrySubscribe()
	{
		if (CurrencyManager.Instance != null)
		{
			SubscribeToCurrencyManager();
		}
	}

	private void SubscribeToCurrencyManager()
	{
		CurrencyManager.Instance.OnCurrencyChanged.AddListener(OnCurrencyChanged);
		CurrencyManager.Instance.OnCurrencyGained.AddListener(OnCurrencyGained);
		CurrencyManager.Instance.OnCurrencySpent.AddListener(OnCurrencySpent);
		UpdateCurrencyDisplay(targetCurrency = (displayedCurrency = CurrencyManager.Instance.GetCurrency()));
	}

	private void SubscribeToInventoryEvents()
	{
	}

	private void OnDestroy()
	{
		if (CurrencyManager.Instance != null)
		{
			CurrencyManager.Instance.OnCurrencyChanged.RemoveListener(OnCurrencyChanged);
			CurrencyManager.Instance.OnCurrencyGained.RemoveListener(OnCurrencyGained);
			CurrencyManager.Instance.OnCurrencySpent.RemoveListener(OnCurrencySpent);
		}
	}

	private void Update()
	{
		if (currencyAnimator != null)
		{
			bool flag = (inventoryUI != null && inventoryUI.IsInventoryOpen) || shopNearbyCount > 0;
			if (flag && !wasShowing)
			{
				currencyAnimator.Play("Show");
			}
			wasShowing = flag;
			currencyAnimator.SetBool("InventoryOpen", flag);
		}
		if (isAnimating && animateChanges)
		{
			animationTime += Time.deltaTime;
			float num = Mathf.Clamp01(animationTime / animationDuration);
			float t = animationCurve.Evaluate(num);
			int num2 = displayedCurrency;
			displayedCurrency = Mathf.RoundToInt(Mathf.Lerp(num2, targetCurrency, t));
			UpdateCurrencyDisplay(displayedCurrency);
			if (num >= 1f)
			{
				displayedCurrency = targetCurrency;
				UpdateCurrencyDisplay(displayedCurrency);
				isAnimating = false;
			}
		}
		if (isFlashingColor)
		{
			colorFlashTime += Time.deltaTime;
			float num3 = Mathf.Clamp01(colorFlashTime / colorFlashDuration);
			Color color = Color.Lerp((currencyText != null) ? currencyText.color : legacyText.color, originalTextColor, num3);
			if (currencyText != null)
			{
				currencyText.color = color;
			}
			else if (legacyText != null)
			{
				legacyText.color = color;
			}
			if (num3 >= 1f)
			{
				isFlashingColor = false;
				if (currencyText != null)
				{
					currencyText.color = originalTextColor;
				}
				else if (legacyText != null)
				{
					legacyText.color = originalTextColor;
				}
			}
		}
		if (!isScaling || !scaleOnChange)
		{
			return;
		}
		scaleAnimationTime += Time.deltaTime;
		float num4 = Mathf.Clamp01(scaleAnimationTime / scaleDuration);
		float num5 = ((!(num4 < 0.5f)) ? Mathf.Lerp(scaleMultiplier, 1f, (num4 - 0.5f) * 2f) : Mathf.Lerp(1f, scaleMultiplier, num4 * 2f));
		Vector3 localScale = originalScale * num5;
		if (currencyText != null)
		{
			currencyText.transform.localScale = localScale;
		}
		else if (legacyText != null)
		{
			legacyText.transform.localScale = localScale;
		}
		if (num4 >= 1f)
		{
			isScaling = false;
			if (currencyText != null)
			{
				currencyText.transform.localScale = originalScale;
			}
			else if (legacyText != null)
			{
				legacyText.transform.localScale = originalScale;
			}
		}
	}

	private void OnCurrencyChanged(int newAmount)
	{
		targetCurrency = newAmount;
		if (animateChanges)
		{
			animationTime = 0f;
			isAnimating = true;
		}
		else
		{
			displayedCurrency = newAmount;
			UpdateCurrencyDisplay(newAmount);
		}
		if (hideWhenZero)
		{
			gameObject.SetActive(newAmount > 0);
		}
	}

	private void OnCurrencyGained(int amount)
	{
		if (currencyAnimator != null)
		{
			currencyAnimator.Play("Show");
		}
		FlashColor(gainColor);
		TriggerScaleAnimation();
	}

	private void OnCurrencySpent(int amount)
	{
		FlashColor(spendColor);
		TriggerScaleAnimation();
	}

	private void UpdateCurrencyDisplay(int amount)
	{
		string text = FormatCurrency(amount);
		if (currencyText != null)
		{
			currencyText.text = text;
		}
		else if (legacyText != null)
		{
			legacyText.text = text;
		}
	}

	private string FormatCurrency(int amount)
	{
		return string.Concat(str1: (!useThousandsSeparator) ? amount.ToString() : amount.ToString("N0"), str0: currencyPrefix, str2: currencySuffix);
	}

	private void FlashColor(Color flashColor)
	{
		if (currencyText != null)
		{
			currencyText.color = flashColor;
		}
		else if (legacyText != null)
		{
			legacyText.color = flashColor;
		}
		colorFlashTime = 0f;
		isFlashingColor = true;
	}

	private void TriggerScaleAnimation()
	{
		if (scaleOnChange)
		{
			scaleAnimationTime = 0f;
			isScaling = true;
		}
	}

	public void ShowForShop()
	{
		shopNearbyCount++;
	}

	public void HideForShop()
	{
		shopNearbyCount = Mathf.Max(0, shopNearbyCount - 1);
	}

	public void SetTextComponent(TextMeshProUGUI textComponent)
	{
		currencyText = textComponent;
		originalTextColor = textComponent.color;
		originalScale = textComponent.transform.localScale;
		if (CurrencyManager.Instance != null)
		{
			UpdateCurrencyDisplay(CurrencyManager.Instance.GetCurrency());
		}
	}

	public void SetLegacyTextComponent(Text textComponent)
	{
		legacyText = textComponent;
		originalTextColor = textComponent.color;
		originalScale = textComponent.transform.localScale;
		if (CurrencyManager.Instance != null)
		{
			UpdateCurrencyDisplay(CurrencyManager.Instance.GetCurrency());
		}
	}

	public void SetCurrencyIcon(Sprite icon)
	{
		if (currencyIcon != null)
		{
			currencyIcon.sprite = icon;
		}
	}

	public void SetCurrencyFormat(string prefix, string suffix)
	{
		currencyPrefix = prefix;
		currencySuffix = suffix;
		UpdateCurrencyDisplay(displayedCurrency);
	}

	public void ForceRefresh()
	{
		if (CurrencyManager.Instance != null)
		{
			int amount = (targetCurrency = (displayedCurrency = CurrencyManager.Instance.GetCurrency()));
			isAnimating = false;
			UpdateCurrencyDisplay(amount);
		}
	}

	public void SetAnimator(Animator animator)
	{
		currencyAnimator = animator;
	}

	public void SetInventoryUI(InventoryUI inventory)
	{
		inventoryUI = inventory;
		SubscribeToInventoryEvents();
	}
}
