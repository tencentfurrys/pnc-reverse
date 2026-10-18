using PixelCrushers.GridController;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ItemUnlockSlotUI : MonoBehaviour, IPointerClickHandler, IEventSystemHandler, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
	[Header("References")]
	[SerializeField]
	private Image itemIcon;

	[SerializeField]
	private Image background;

	[Header("Visual Settings")]
	[SerializeField]
	private Color hoverColor = new Color(1f, 1f, 1f, 0.15f);

	[SerializeField]
	private Color normalBgColor = new Color(0f, 0f, 0f, 0.3f);

	private InventoryItem item;

	private bool isUnlocked;

	private ItemUnlocksScreenUI parentScreen;

	private Button button;

	private void Awake()
	{
		button = GetComponent<Button>();
	}

	public void Setup(InventoryItem item, bool isUnlocked, ItemUnlocksScreenUI parentScreen)
	{
		this.item = item;
		this.isUnlocked = isUnlocked;
		this.parentScreen = parentScreen;
		if (itemIcon != null && item.itemIcon != null)
		{
			itemIcon.sprite = item.itemIcon;
			itemIcon.color = (isUnlocked ? Color.white : Color.black);
		}
		if (background != null)
		{
			background.color = normalBgColor;
		}
		if (button == null)
		{
			button = GetComponent<Button>();
		}
		if (button != null)
		{
			button.onClick.RemoveAllListeners();
			button.onClick.AddListener(ShowDetails);
		}
	}

	private void ShowDetails()
	{
		if (parentScreen != null && item != null)
		{
			parentScreen.ShowItemDetails(item, isUnlocked);
		}
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		ShowDetails();
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		if (background != null)
		{
			background.color = hoverColor;
		}
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		if (background != null)
		{
			background.color = normalBgColor;
		}
	}

	public void OnSelect(BaseEventData eventData)
	{
		if (background != null)
		{
			background.color = hoverColor;
		}
		ShowDetails();
	}

	public void OnDeselect(BaseEventData eventData)
	{
		if (background != null)
		{
			background.color = normalBgColor;
		}
	}
}
