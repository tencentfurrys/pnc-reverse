using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PixelCrushers.GridController
{
	[RequireComponent(typeof(Button))]
	public class InventorySlotUI : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, ISelectHandler, IDeselectHandler
	{
		[Header("UI Components")]
		[SerializeField]
		private Image slotBackground;

		[SerializeField]
		private Image itemIcon;

		[SerializeField]
		private TextMeshProUGUI stackText;

		[SerializeField]
		private TextMeshProUGUI debugCoordinates;

		[SerializeField]
		private GameObject lockedOverlay;

		[Header("Visual States")]
		[SerializeField]
		private Color normalColor = Color.white;

		[SerializeField]
		private Color hoverColor = new Color(0.8f, 0.8f, 1f, 1f);

		[SerializeField]
		private Color lockedColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

		[SerializeField]
		private Color occupiedColor = new Color(0.9f, 1f, 0.9f, 1f);

		private Image hoverOverlay;

		private Image validTargetOverlay;

		private SlotHighlightTheme highlightTheme;

		private bool isHoverShown;

		private bool isValidTargetShown;

		private InventoryItem currentItem;

		private Vector2Int gridPosition;

		private EquipmentSlotType equipmentSlotType;

		private InventoryUI inventoryUI;

		private Button button;

		private bool isLocked;

		private bool isEquipmentSlot;

		private bool isDragging;

		public InventoryItem CurrentItem => currentItem;

		public Vector2Int GridPosition => gridPosition;

		public EquipmentSlotType EquipmentSlotType => equipmentSlotType;

		public bool IsEmpty => currentItem == null;

		public bool IsLocked => isLocked;

		private void Awake()
		{
			button = GetComponent<Button>();
			if (slotBackground == null)
			{
				slotBackground = GetComponent<Image>();
			}
			if (itemIcon == null)
			{
				itemIcon = base.transform.Find("ItemIcon")?.GetComponent<Image>();
			}
			if (stackText == null)
			{
				stackText = GetComponentInChildren<TextMeshProUGUI>();
			}
			if (debugCoordinates == null)
			{
				debugCoordinates = base.transform.Find("DebugCoordinates")?.GetComponent<TextMeshProUGUI>();
			}
			if (lockedOverlay == null)
			{
				lockedOverlay = base.transform.Find("LockedOverlay")?.gameObject;
			}
			if (button != null)
			{
				button.transition = Selectable.Transition.None;
				if (button.targetGraphic != null)
				{
					button.targetGraphic.canvasRenderer.SetColor(Color.white);
				}
			}
			EnsureOverlays();
			SetVisualState(SlotVisualState.Normal);
		}

		public void Initialize(Vector2Int position, InventoryUI ui)
		{
			gridPosition = position;
			inventoryUI = ui;
			isEquipmentSlot = false;
			SetItem(null);
			SetLocked(locked: false);
		}

		public void Initialize(EquipmentSlotType slotType, InventoryUI ui)
		{
			equipmentSlotType = slotType;
			inventoryUI = ui;
			isEquipmentSlot = true;
			SetItem(null);
			SetLocked(locked: false);
		}

		public void SetItem(InventoryItem item)
		{
			currentItem = item;
			UpdateVisuals();
		}

		public void SetLocked(bool locked)
		{
			isLocked = locked;
			if (button == null)
			{
				button = GetComponent<Button>();
			}
			if (button != null)
			{
				button.interactable = !locked;
			}
			SetHovered(isHoverShown);
			SetValidTarget(isValidTargetShown);
			UpdateVisuals();
		}

		public void SetDebugCoordinates(string coordinates)
		{
			if (debugCoordinates != null)
			{
				debugCoordinates.text = coordinates;
				debugCoordinates.gameObject.SetActive(!string.IsNullOrEmpty(coordinates));
			}
		}

		private void UpdateVisuals()
		{
			if (itemIcon != null)
			{
				if (currentItem != null && currentItem.itemIcon != null)
				{
					itemIcon.sprite = currentItem.itemIcon;
					itemIcon.gameObject.SetActive(value: true);
					itemIcon.color = (isLocked ? Color.gray : Color.white);
				}
				else
				{
					itemIcon.gameObject.SetActive(value: false);
				}
			}
			if (stackText != null)
			{
				stackText.gameObject.SetActive(value: false);
			}
			if (lockedOverlay != null)
			{
				lockedOverlay.SetActive(isLocked);
			}
			if (!isDragging)
			{
				if (isLocked)
				{
					SetVisualState(SlotVisualState.Locked);
				}
				else if (currentItem != null)
				{
					SetVisualState(SlotVisualState.Occupied);
				}
				else
				{
					SetVisualState(SlotVisualState.Normal);
				}
			}
		}

		private void SetVisualState(SlotVisualState state)
		{
			if (!(slotBackground == null))
			{
				Color color = normalColor;
				switch (state)
				{
				case SlotVisualState.Normal:
					color = normalColor;
					break;
				case SlotVisualState.Hover:
					color = hoverColor;
					break;
				case SlotVisualState.Locked:
					color = lockedColor;
					break;
				case SlotVisualState.Occupied:
					color = occupiedColor;
					break;
				}
				slotBackground.color = color;
			}
		}

		public void ApplyHighlightTheme(SlotHighlightTheme theme)
		{
			if (theme != null)
			{
				highlightTheme = theme;
			}
			EnsureOverlays();
			RefreshOverlayVisuals();
		}

		public void SetHovered(bool hovered)
		{
			isHoverShown = hovered;
			EnsureOverlays();
			if (hoverOverlay != null)
			{
				hoverOverlay.enabled = hovered && !isLocked;
			}
		}

		public void SetValidTarget(bool valid)
		{
			isValidTargetShown = valid;
			EnsureOverlays();
			if (validTargetOverlay != null)
			{
				validTargetOverlay.enabled = valid && !isLocked;
			}
		}

		public void ClearHighlights()
		{
			SetHovered(hovered: false);
			SetValidTarget(valid: false);
		}

		private void EnsureOverlays()
		{
			if (highlightTheme == null)
			{
				highlightTheme = new SlotHighlightTheme();
			}
			bool flag = false;
			if (validTargetOverlay == null)
			{
				validTargetOverlay = CreateOverlay("ValidTargetOverlay");
				flag = true;
			}
			if (hoverOverlay == null)
			{
				hoverOverlay = CreateOverlay("HoverOverlay");
				flag = true;
			}
			if (flag)
			{
				if (itemIcon != null)
				{
					validTargetOverlay.transform.SetSiblingIndex(itemIcon.transform.GetSiblingIndex());
					hoverOverlay.transform.SetSiblingIndex(itemIcon.transform.GetSiblingIndex() + 1);
				}
				else
				{
					validTargetOverlay.transform.SetAsLastSibling();
					hoverOverlay.transform.SetAsLastSibling();
				}
				if (lockedOverlay != null)
				{
					lockedOverlay.transform.SetAsLastSibling();
				}
				if (debugCoordinates != null)
				{
					debugCoordinates.transform.SetAsLastSibling();
				}
				RefreshOverlayVisuals();
			}
		}

		private Image CreateOverlay(string overlayName)
		{
			Transform transform = base.transform.Find(overlayName);
			if (transform != null)
			{
				Image component = transform.GetComponent<Image>();
				if (component != null)
				{
					return component;
				}
			}
			GameObject obj = new GameObject(overlayName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image))
			{
				layer = base.gameObject.layer
			};
			RectTransform component2 = obj.GetComponent<RectTransform>();
			component2.SetParent(base.transform, worldPositionStays: false);
			component2.anchorMin = Vector2.zero;
			component2.anchorMax = Vector2.one;
			component2.offsetMin = Vector2.zero;
			component2.offsetMax = Vector2.zero;
			component2.pivot = new Vector2(0.5f, 0.5f);
			component2.localScale = Vector3.one;
			Image component3 = obj.GetComponent<Image>();
			component3.type = Image.Type.Simple;
			component3.preserveAspect = false;
			component3.raycastTarget = false;
			component3.enabled = false;
			return component3;
		}

		private void RefreshOverlayVisuals()
		{
			if (highlightTheme == null)
			{
				highlightTheme = new SlotHighlightTheme();
			}
			Sprite sprite = ((slotBackground != null) ? slotBackground.sprite : null);
			if (sprite == null)
			{
				Image component = GetComponent<Image>();
				if (component != null)
				{
					sprite = component.sprite;
				}
			}
			if (hoverOverlay != null)
			{
				bool flag = highlightTheme.hoverSprite != null;
				hoverOverlay.sprite = (flag ? highlightTheme.hoverSprite : sprite);
				hoverOverlay.color = (flag ? highlightTheme.hoverTint : highlightTheme.hoverFallbackColor);
			}
			if (validTargetOverlay != null)
			{
				bool flag2 = highlightTheme.validTargetSprite != null;
				validTargetOverlay.sprite = (flag2 ? highlightTheme.validTargetSprite : sprite);
				validTargetOverlay.color = (flag2 ? highlightTheme.validTargetTint : highlightTheme.validTargetFallbackColor);
			}
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			SetHovered(hovered: true);
			if (inventoryUI != null)
			{
				inventoryUI.SetFocusedSlot(this);
			}
			if (currentItem != null && inventoryUI != null)
			{
				inventoryUI.ShowTooltip(currentItem, (Mouse.current != null) ? ((Vector3)Mouse.current.position.ReadValue()) : Vector3.zero);
			}
		}

		public void OnSelect(BaseEventData eventData)
		{
			SetHovered(hovered: true);
			if (inventoryUI != null)
			{
				inventoryUI.SetFocusedSlot(this);
				if (currentItem != null)
				{
					Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(null, base.transform.position);
					inventoryUI.ShowTooltip(currentItem, screenPosition);
				}
			}
		}

		public void OnDeselect(BaseEventData eventData)
		{
			SetHovered(hovered: false);
			if (!isDragging)
			{
				UpdateVisuals();
			}
			if (inventoryUI != null)
			{
				inventoryUI.ClearFocusedSlot(this);
				inventoryUI.HideTooltip();
			}
		}

		public void InvokeRightClickAction()
		{
			if (!isLocked && !(currentItem == null))
			{
				HandleRightClick();
			}
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			SetHovered(hovered: false);
			if (!isDragging)
			{
				UpdateVisuals();
			}
			if (inventoryUI != null)
			{
				inventoryUI.ClearFocusedSlot(this);
				inventoryUI.HideTooltip();
			}
		}

		public void OnPointerDown(PointerEventData eventData)
		{
			if (eventData.button == PointerEventData.InputButton.Left)
			{
				if (currentItem != null && inventoryUI != null)
				{
					inventoryUI.PinTooltip(currentItem, (Mouse.current != null) ? ((Vector3)Mouse.current.position.ReadValue()) : Vector3.zero);
				}
				else if (inventoryUI != null)
				{
					inventoryUI.UnpinTooltip();
				}
			}
			if (!isLocked && !(currentItem == null) && eventData.button == PointerEventData.InputButton.Right)
			{
				HandleRightClick();
			}
		}

		public void OnPointerUp(PointerEventData eventData)
		{
		}

		private void HandleRightClick()
		{
			if (currentItem == null)
			{
				return;
			}
			if (isEquipmentSlot)
			{
				if (inventoryUI != null && inventoryUI.InventorySystem != null)
				{
					inventoryUI.InventorySystem.UnequipItem(equipmentSlotType);
				}
			}
			else if (currentItem.IsConsumable)
			{
				if (inventoryUI != null)
				{
					inventoryUI.TryUseItem(currentItem, gridPosition);
				}
			}
			else if (currentItem.IsWeapon || currentItem.IsArmor || currentItem.IsTrinket)
			{
				TryAutoEquip();
			}
		}

		private void TryAutoEquip()
		{
			if (inventoryUI?.InventorySystem == null)
			{
				return;
			}
			EquipmentSlotType slotType;
			if (currentItem.IsWeapon)
			{
				slotType = ((!(inventoryUI.InventorySystem.RightWeapon == null)) ? EquipmentSlotType.LeftWeapon : EquipmentSlotType.RightWeapon);
			}
			else if (currentItem.IsArmor)
			{
				slotType = ((currentItem.armorData.armorType != ArmorType.UpperBody) ? EquipmentSlotType.LowerArmor : EquipmentSlotType.UpperArmor);
			}
			else
			{
				if (!currentItem.IsTrinket)
				{
					return;
				}
				slotType = ((inventoryUI.InventorySystem.Trinket1 == null) ? EquipmentSlotType.Trinket1 : ((!(inventoryUI.InventorySystem.Trinket2 == null)) ? EquipmentSlotType.Trinket1 : EquipmentSlotType.Trinket2));
			}
			InventoryItem inventoryItem = inventoryUI.InventorySystem.RemoveItem(gridPosition);
			if (!(inventoryItem != null))
			{
				return;
			}
			if (inventoryUI.InventorySystem.EquipItem(inventoryItem, slotType, out var displacedItems))
			{
				if (displacedItems.Count > 0)
				{
					inventoryUI.DropItems(displacedItems.ToArray());
				}
			}
			else
			{
				inventoryUI.InventorySystem.AddItem(inventoryItem, gridPosition);
			}
		}

		public void OnBeginDrag(PointerEventData eventData)
		{
			if (!isLocked && !(currentItem == null) && !(inventoryUI == null))
			{
				isDragging = true;
				if (isEquipmentSlot)
				{
					inventoryUI.StartDragFromEquipment(currentItem, equipmentSlotType);
				}
				else
				{
					inventoryUI.StartDrag(currentItem, gridPosition);
				}
				if (slotBackground != null)
				{
					Color color = slotBackground.color;
					color.a = 0.5f;
					slotBackground.color = color;
				}
				if (itemIcon != null)
				{
					Color color2 = itemIcon.color;
					color2.a = 0.3f;
					itemIcon.color = color2;
				}
			}
		}

		public void OnDrag(PointerEventData eventData)
		{
		}

		public void OnEndDrag(PointerEventData eventData)
		{
			isDragging = false;
			if (slotBackground != null)
			{
				Color color = slotBackground.color;
				color.a = 1f;
				slotBackground.color = color;
			}
			if (itemIcon != null)
			{
				Color color2 = itemIcon.color;
				color2.a = 1f;
				itemIcon.color = color2;
			}
			UpdateVisuals();
			inventoryUI?.CancelDrag();
		}

		public void OnDrop(PointerEventData eventData)
		{
			if (isLocked || inventoryUI == null)
			{
				inventoryUI?.CancelDrag();
			}
			else if (isEquipmentSlot)
			{
				inventoryUI.EndDragOnEquipment(equipmentSlotType);
			}
			else
			{
				inventoryUI.EndDrag(gridPosition);
			}
		}
	}
}
