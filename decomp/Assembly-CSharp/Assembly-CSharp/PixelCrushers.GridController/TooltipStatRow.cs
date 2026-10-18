using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PixelCrushers.GridController;

public class TooltipStatRow : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
{
	[SerializeField]
	private Image icon;

	[SerializeField]
	private TextMeshProUGUI valueText;

	private string description;

	private Action<string> onHoverEnter;

	private Action onHoverExit;

	public void Setup(Sprite iconSprite, string value, string desc, Action<string> hoverEnter, Action hoverExit)
	{
		if (icon != null)
		{
			icon.sprite = iconSprite;
		}
		if (valueText != null)
		{
			valueText.text = value;
		}
		description = desc;
		onHoverEnter = hoverEnter;
		onHoverExit = hoverExit;
		gameObject.SetActive(value: true);
	}

	public void Hide()
	{
		gameObject.SetActive(value: false);
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		if (!string.IsNullOrEmpty(description))
		{
			onHoverEnter?.Invoke(description);
		}
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		onHoverExit?.Invoke();
	}
}
