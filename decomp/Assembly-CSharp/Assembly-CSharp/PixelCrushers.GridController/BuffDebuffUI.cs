using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PixelCrushers.GridController;

public class BuffDebuffUI : MonoBehaviour
{
	private class EffectIcon
	{
		public GameObject gameObject;

		public Image iconImage;

		public Image borderImage;

		public TextMeshProUGUI durationText;

		public TextMeshProUGUI stackText;

		public Image fillImage;

		public StatusEffect effect;

		public float animationTime;

		public bool isAnimating;

		public int lastDisplayedDuration = -1;

		public int lastDisplayedStacks = -1;
	}

	[Header("UI References")]
	[SerializeField]
	private Transform buffContainer;

	[SerializeField]
	private Transform debuffContainer;

	[SerializeField]
	private GameObject effectIconPrefab;

	[Header("Display Settings")]
	[SerializeField]
	private float iconSize = 32f;

	[SerializeField]
	private float iconSpacing = 4f;

	[SerializeField]
	private int maxBuffsDisplay = 8;

	[SerializeField]
	private int maxDebuffsDisplay = 8;

	[SerializeField]
	private bool showDuration = true;

	[SerializeField]
	private bool showStacks = true;

	[Header("Icon Settings")]
	[SerializeField]
	private Sprite defaultBuffIcon;

	[SerializeField]
	private Sprite defaultDebuffIcon;

	[SerializeField]
	private Dictionary<StatusEffectType, Sprite> effectIcons = new Dictionary<StatusEffectType, Sprite>();

	[Header("Colors")]
	[SerializeField]
	private Color buffColor = new Color(0.2f, 0.8f, 0.2f, 1f);

	[SerializeField]
	private Color debuffColor = new Color(0.8f, 0.2f, 0.2f, 1f);

	[SerializeField]
	private Color expiringColor = Color.yellow;

	[SerializeField]
	private float expiringThreshold = 3f;

	[Header("Animation")]
	[SerializeField]
	private bool animateNewEffects = true;

	[SerializeField]
	private float scaleAnimationDuration = 0.3f;

	[SerializeField]
	private AnimationCurve scaleAnimationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

	[Header("Component References")]
	[SerializeField]
	private BuffDebuffSystem buffDebuffSystem;

	private Dictionary<StatusEffect, EffectIcon> activeIcons = new Dictionary<StatusEffect, EffectIcon>();

	private List<EffectIcon> iconPool = new List<EffectIcon>();

	private void Awake()
	{
		if (buffDebuffSystem == null)
		{
			buffDebuffSystem = Object.FindAnyObjectByType<BuffDebuffSystem>();
		}
		if (!(buffDebuffSystem == null))
		{
			buffDebuffSystem.OnBuffApplied.AddListener(OnEffectAdded);
			buffDebuffSystem.OnDebuffApplied.AddListener(OnEffectAdded);
			buffDebuffSystem.OnEffectRemoved.AddListener(OnEffectRemoved);
			buffDebuffSystem.OnEffectExpired.AddListener(OnEffectRemoved);
			buffDebuffSystem.OnEffectsUpdated.AddListener(RefreshDisplay);
			CreateIconPool();
		}
	}

	private void OnDestroy()
	{
		if (buffDebuffSystem != null)
		{
			buffDebuffSystem.OnBuffApplied.RemoveListener(OnEffectAdded);
			buffDebuffSystem.OnDebuffApplied.RemoveListener(OnEffectAdded);
			buffDebuffSystem.OnEffectRemoved.RemoveListener(OnEffectRemoved);
			buffDebuffSystem.OnEffectExpired.RemoveListener(OnEffectRemoved);
			buffDebuffSystem.OnEffectsUpdated.RemoveListener(RefreshDisplay);
		}
	}

	private void Update()
	{
		UpdateActiveIcons();
	}

	private void CreateIconPool()
	{
		if (effectIconPrefab == null)
		{
			CreateDefaultIconPrefab();
		}
		int num = maxBuffsDisplay + maxDebuffsDisplay;
		for (int i = 0; i < num; i++)
		{
			EffectIcon effectIcon = CreateNewIcon();
			effectIcon.gameObject.SetActive(value: false);
			iconPool.Add(effectIcon);
		}
	}

	private void CreateDefaultIconPrefab()
	{
		effectIconPrefab = new GameObject("EffectIcon");
		effectIconPrefab.AddComponent<Image>().preserveAspect = true;
		GameObject gameObject = new GameObject("Border");
		gameObject.transform.SetParent(effectIconPrefab.transform, worldPositionStays: false);
		Image image = gameObject.AddComponent<Image>();
		image.rectTransform.anchorMin = Vector2.zero;
		image.rectTransform.anchorMax = Vector2.one;
		image.rectTransform.sizeDelta = Vector2.zero;
		GameObject gameObject2 = new GameObject("Fill");
		gameObject2.transform.SetParent(effectIconPrefab.transform, worldPositionStays: false);
		Image image2 = gameObject2.AddComponent<Image>();
		image2.type = Image.Type.Filled;
		image2.fillMethod = Image.FillMethod.Radial360;
		image2.fillOrigin = 2;
		image2.fillClockwise = false;
		image2.rectTransform.anchorMin = Vector2.zero;
		image2.rectTransform.anchorMax = Vector2.one;
		image2.rectTransform.sizeDelta = Vector2.zero;
		GameObject gameObject3 = new GameObject("Duration");
		gameObject3.transform.SetParent(effectIconPrefab.transform, worldPositionStays: false);
		TextMeshProUGUI textMeshProUGUI = gameObject3.AddComponent<TextMeshProUGUI>();
		textMeshProUGUI.text = "";
		textMeshProUGUI.alignment = TextAlignmentOptions.Center;
		textMeshProUGUI.fontSize = 12f;
		textMeshProUGUI.rectTransform.anchorMin = Vector2.zero;
		textMeshProUGUI.rectTransform.anchorMax = Vector2.one;
		textMeshProUGUI.rectTransform.sizeDelta = Vector2.zero;
		GameObject gameObject4 = new GameObject("Stack");
		gameObject4.transform.SetParent(effectIconPrefab.transform, worldPositionStays: false);
		TextMeshProUGUI textMeshProUGUI2 = gameObject4.AddComponent<TextMeshProUGUI>();
		textMeshProUGUI2.text = "";
		textMeshProUGUI2.alignment = TextAlignmentOptions.BottomRight;
		textMeshProUGUI2.fontSize = 10f;
		textMeshProUGUI2.rectTransform.anchorMin = new Vector2(0.5f, 0f);
		textMeshProUGUI2.rectTransform.anchorMax = Vector2.one;
		textMeshProUGUI2.rectTransform.sizeDelta = Vector2.zero;
		textMeshProUGUI2.rectTransform.anchoredPosition = new Vector2(-2f, 2f);
		effectIconPrefab.GetComponent<RectTransform>().sizeDelta = new Vector2(iconSize, iconSize);
	}

	private EffectIcon CreateNewIcon()
	{
		GameObject gameObject = Object.Instantiate(effectIconPrefab);
		return new EffectIcon
		{
			gameObject = gameObject,
			iconImage = gameObject.GetComponent<Image>(),
			borderImage = gameObject.transform.Find("Border")?.GetComponent<Image>(),
			fillImage = gameObject.transform.Find("Fill")?.GetComponent<Image>(),
			durationText = gameObject.transform.Find("Duration")?.GetComponent<TextMeshProUGUI>(),
			stackText = gameObject.transform.Find("Stack")?.GetComponent<TextMeshProUGUI>()
		};
	}

	private EffectIcon GetIconFromPool()
	{
		foreach (EffectIcon item in iconPool)
		{
			if (!item.gameObject.activeSelf)
			{
				return item;
			}
		}
		EffectIcon effectIcon = CreateNewIcon();
		iconPool.Add(effectIcon);
		return effectIcon;
	}

	private void ReturnIconToPool(EffectIcon icon)
	{
		icon.gameObject.SetActive(value: false);
		icon.effect = null;
		icon.isAnimating = false;
		icon.animationTime = 0f;
	}

	private void OnEffectAdded(StatusEffect effect)
	{
		if (!activeIcons.ContainsKey(effect))
		{
			EffectIcon iconFromPool = GetIconFromPool();
			SetupIcon(iconFromPool, effect);
			Transform parent = (effect.isDebuff ? debuffContainer : buffContainer);
			iconFromPool.gameObject.transform.SetParent(parent, worldPositionStays: false);
			iconFromPool.gameObject.SetActive(value: true);
			if (animateNewEffects)
			{
				iconFromPool.isAnimating = true;
				iconFromPool.animationTime = 0f;
				iconFromPool.gameObject.transform.localScale = Vector3.zero;
			}
			activeIcons[effect] = iconFromPool;
			ArrangeIcons();
		}
	}

	private void OnEffectRemoved(StatusEffect effect)
	{
		if (activeIcons.TryGetValue(effect, out var value))
		{
			activeIcons.Remove(effect);
			ReturnIconToPool(value);
			ArrangeIcons();
		}
	}

	private void RefreshDisplay(List<StatusEffect> effects)
	{
		List<StatusEffect> list = new List<StatusEffect>();
		foreach (KeyValuePair<StatusEffect, EffectIcon> activeIcon in activeIcons)
		{
			if (!effects.Contains(activeIcon.Key))
			{
				list.Add(activeIcon.Key);
			}
		}
		foreach (StatusEffect item in list)
		{
			OnEffectRemoved(item);
		}
		foreach (StatusEffect effect in effects)
		{
			if (!activeIcons.ContainsKey(effect))
			{
				OnEffectAdded(effect);
			}
		}
	}

	private void SetupIcon(EffectIcon icon, StatusEffect effect)
	{
		icon.effect = effect;
		Sprite iconForEffect = GetIconForEffect(effect);
		if (icon.iconImage != null)
		{
			icon.iconImage.sprite = iconForEffect;
			icon.iconImage.color = Color.white;
		}
		if (icon.borderImage != null)
		{
			icon.borderImage.color = (effect.isDebuff ? debuffColor : buffColor);
		}
		if (icon.fillImage != null)
		{
			Color color = (effect.isDebuff ? debuffColor : buffColor);
			color.a = 0.5f;
			icon.fillImage.color = color;
		}
		UpdateIconTexts(icon);
	}

	private Sprite GetIconForEffect(StatusEffect effect)
	{
		if (effectIcons.TryGetValue(effect.effectType, out var value))
		{
			return value;
		}
		if (!effect.isDebuff)
		{
			return defaultBuffIcon;
		}
		return defaultDebuffIcon;
	}

	private void UpdateActiveIcons()
	{
		foreach (KeyValuePair<StatusEffect, EffectIcon> activeIcon in activeIcons)
		{
			EffectIcon value = activeIcon.Value;
			StatusEffect key = activeIcon.Key;
			if (value.isAnimating)
			{
				value.animationTime += Time.deltaTime;
				float num = value.animationTime / scaleAnimationDuration;
				if (num >= 1f)
				{
					num = 1f;
					value.isAnimating = false;
				}
				float num2 = scaleAnimationCurve.Evaluate(num);
				value.gameObject.transform.localScale = Vector3.one * num2;
			}
			if (!key.isPermanent && value.fillImage != null)
			{
				float fillAmount = key.remainingDuration / key.duration;
				value.fillImage.fillAmount = fillAmount;
				if (key.remainingDuration <= expiringThreshold)
				{
					Color color = Color.Lerp(expiringColor, key.isDebuff ? debuffColor : buffColor, key.remainingDuration / expiringThreshold);
					value.borderImage.color = color;
				}
			}
			UpdateIconTexts(value);
		}
	}

	private void UpdateIconTexts(EffectIcon icon)
	{
		StatusEffect effect = icon.effect;
		if (showDuration && icon.durationText != null)
		{
			if (!effect.isPermanent && effect.remainingDuration > 0f)
			{
				int num = ((!(effect.remainingDuration > 60f)) ? Mathf.CeilToInt(effect.remainingDuration) : (-Mathf.FloorToInt(effect.remainingDuration / 60f)));
				if (num != icon.lastDisplayedDuration)
				{
					icon.lastDisplayedDuration = num;
					if (num < 0)
					{
						icon.durationText.text = $"{-num}m";
					}
					else
					{
						icon.durationText.text = num.ToString();
					}
				}
			}
			else if (icon.lastDisplayedDuration != 0)
			{
				icon.lastDisplayedDuration = 0;
				icon.durationText.text = "";
			}
		}
		if (showStacks && icon.stackText != null && effect.stackCount != icon.lastDisplayedStacks)
		{
			icon.lastDisplayedStacks = effect.stackCount;
			icon.stackText.text = ((effect.stackCount > 1) ? effect.stackCount.ToString() : "");
		}
	}

	private void ArrangeIcons()
	{
		ArrangeIconsInContainer(buffContainer, isDebuff: false);
		ArrangeIconsInContainer(debuffContainer, isDebuff: true);
	}

	private void ArrangeIconsInContainer(Transform container, bool isDebuff)
	{
		if (container == null)
		{
			return;
		}
		int num = 0;
		int num2 = (isDebuff ? maxDebuffsDisplay : maxBuffsDisplay);
		foreach (Transform item in container)
		{
			if (item.gameObject.activeSelf)
			{
				if (num >= num2)
				{
					item.gameObject.SetActive(value: false);
					continue;
				}
				float x = (float)num * (iconSize + iconSpacing);
				item.GetComponent<RectTransform>().anchoredPosition = new Vector2(x, 0f);
				num++;
			}
		}
	}

	public void SetEffectIcon(StatusEffectType type, Sprite icon)
	{
		effectIcons[type] = icon;
	}

	public void ForceRefresh()
	{
		if (buffDebuffSystem != null)
		{
			RefreshDisplay(buffDebuffSystem.GetActiveEffects());
		}
	}
}
