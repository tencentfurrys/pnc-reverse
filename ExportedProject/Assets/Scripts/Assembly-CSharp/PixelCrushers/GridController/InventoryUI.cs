using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PixelCrushers.GridController
{
	public class InventoryUI : MonoBehaviour, IDynamicFirstSelectable
	{
		[Header("UI References")]
		[SerializeField]
		private GameObject inventoryPanel;

		[SerializeField]
		private Transform gridContainer;

		[SerializeField]
		private GameObject slotPrefab;

		[SerializeField]
		private KeyCode toggleKey = KeyCode.Tab;

		[Header("Health/Heat/Currency Bar Animations")]
		[SerializeField]
		private Animator healthBarAnimator;

		[SerializeField]
		private Animator heatBarAnimator;

		[SerializeField]
		private Animator currencyAnimator;

		[Header("Player Control Settings")]
		[SerializeField]
		private bool pauseGameWhenOpen;

		[SerializeField]
		private bool disablePlayerMovement = true;

		[SerializeField]
		private bool disablePlayerCombat = true;

		[Header("Equipment UI")]
		[SerializeField]
		private InventorySlotUI upperArmorSlot;

		[SerializeField]
		private InventorySlotUI lowerArmorSlot;

		[SerializeField]
		private InventorySlotUI leftWeaponSlot;

		[SerializeField]
		private InventorySlotUI rightWeaponSlot;

		[SerializeField]
		private InventorySlotUI trinket1Slot;

		[SerializeField]
		private InventorySlotUI trinket2Slot;

		[Header("Slot Highlight")]
		[Tooltip("Single assignment point for the hover / valid-target overlay art. Pushed down into all grid slots and all 6 equipment slots at Initialize time. Leave sprites empty to fall back to a tinted slot frame.")]
		[SerializeField]
		private SlotHighlightTheme slotHighlightTheme = new SlotHighlightTheme();

		[Header("Controller Navigation")]
		[Tooltip("Selectable that the top row of the inventory grid's Up nav exits to (e.g., upper armor slot).")]
		[SerializeField]
		private Selectable equipmentEntryPoint;

		[Header("Stats Display")]
		[SerializeField]
		private TextMeshProUGUI healthText;

		[SerializeField]
		private TextMeshProUGUI attackText;

		[SerializeField]
		private TextMeshProUGUI defenseText;

		[SerializeField]
		private TextMeshProUGUI slotsUsedText;

		[Header("Row Controls")]
		[SerializeField]
		private Transform rowControlsContainer;

		[SerializeField]
		private GameObject rowTogglePrefab;

		[Header("Drag & Drop")]
		[SerializeField]
		private GameObject dragPreview;

		[SerializeField]
		private Image dragPreviewImage;

		[SerializeField]
		private Canvas dragCanvas;

		[Header("Tooltip")]
		[SerializeField]
		private GameObject tooltipPanel;

		[SerializeField]
		private TextMeshProUGUI tooltipTitle;

		[SerializeField]
		private Image tooltipItemImage;

		[SerializeField]
		private Image tooltipRarityImage;

		[SerializeField]
		private TextMeshProUGUI tooltipDescription;

		[SerializeField]
		private Transform tooltipStatsContainer;

		[SerializeField]
		private TooltipStatRow tooltipStatRowPrefab;

		[SerializeField]
		private TooltipIcons tooltipIcons;

		[SerializeField]
		private TextMeshProUGUI tooltipExtraStats;

		[SerializeField]
		private Image tooltipHandTypeIcon;

		[SerializeField]
		private Image tooltipWeaponTypeIcon;

		[SerializeField]
		private RectTransform statDescriptionPanel;

		[SerializeField]
		private TextMeshProUGUI tooltipStatDescription;

		[Header("Audio")]
		[SerializeField]
		private AudioSource audioSource;

		[SerializeField]
		private AudioClip pickupSound;

		[SerializeField]
		private AudioClip dropSound;

		[SerializeField]
		private AudioClip equipSound;

		[SerializeField]
		private AudioClip inventoryMoveSound;

		[SerializeField]
		private AudioClip errorSound;

		[SerializeField]
		private AudioMixerGroup sfxMixerGroup;

		[Header("Item Dropping")]
		[SerializeField]
		private ItemDropper itemDropper;

		[SerializeField]
		private KeyCode dropKey = KeyCode.Q;

		[Header("Consumable System")]
		[SerializeField]
		private ConsumableSystem consumableSystem;

		[Header("Debug Settings")]
		[SerializeField]
		private bool enableDebugLogging = true;

		private GridInventorySystem inventorySystem;

		private PlayerStats playerStats;

		private Canvas mainCanvas;

		private FirstPersonController firstPersonController;

		private DualWieldingSystem dualWieldingSystem;

		private CursorLockMode previousCursorLockState;

		private bool previousCursorVisible;

		private InventorySlotUI[,] gridSlots;

		private List<Toggle> rowToggles;

		private bool isInventoryOpen;

		private float focusRegainedTime = -1f;

		private const float FOCUS_COOLDOWN = 0.2f;

		private InventoryItem draggedItem;

		private Vector2Int draggedFromPosition;

		private EquipmentSlotType draggedFromEquipment;

		private bool isDraggingFromEquipment;

		private InventorySlotUI focusedSlot;

		private bool isControllerHolding;

		private PauseMenuManager pauseMenu;

		private static PointerEventData cachedPointerData;

		private static readonly List<RaycastResult> cachedRaycastResults = new List<RaycastResult>();

		private readonly List<TooltipStatRow> activeStatRows = new List<TooltipStatRow>();

		private InventoryItem pinnedTooltipItem;

		private bool isStatDescriptionVisible;

		public InventorySlotUI FocusedSlot => focusedSlot;

		public Selectable FirstSelectable
		{
			get
			{
				if (gridSlots == null || inventorySystem == null)
				{
					return null;
				}
				int gridWidth = inventorySystem.GridWidth;
				int gridHeight = inventorySystem.GridHeight;
				for (int i = 0; i < gridHeight; i++)
				{
					for (int j = 0; j < gridWidth; j++)
					{
						InventorySlotUI inventorySlotUI = gridSlots[j, i];
						if (inventorySlotUI != null && !inventorySlotUI.IsLocked)
						{
							Selectable component = inventorySlotUI.GetComponent<Selectable>();
							if (component != null && component.IsInteractable())
							{
								return component;
							}
						}
					}
				}
				return null;
			}
		}

		public bool IsInventoryOpen => isInventoryOpen;

		public GridInventorySystem InventorySystem => inventorySystem;

		public void SetFocusedSlot(InventorySlotUI slot)
		{
			focusedSlot = slot;
			RefreshHighlightDriver();
		}

		public void ClearFocusedSlot(InventorySlotUI slot)
		{
			if (!(focusedSlot != slot))
			{
				focusedSlot = null;
				RefreshHighlightDriver();
			}
		}

		public void EnsureBuilt()
		{
		}

		private void Awake()
		{
			inventorySystem = GetComponent<GridInventorySystem>();
			playerStats = GetComponent<PlayerStats>();
			firstPersonController = GetComponent<FirstPersonController>();
			dualWieldingSystem = GetComponent<DualWieldingSystem>();
			mainCanvas = GetComponentInParent<Canvas>();
			if (consumableSystem == null)
			{
				consumableSystem = GetComponent<ConsumableSystem>();
			}
			if (firstPersonController == null)
			{
				firstPersonController = Object.FindAnyObjectByType<FirstPersonController>();
			}
			if (dualWieldingSystem == null)
			{
				dualWieldingSystem = DualWieldingSystem.Instance;
			}
			if (consumableSystem == null)
			{
				consumableSystem = Object.FindAnyObjectByType<ConsumableSystem>();
			}
			if (audioSource == null)
			{
				audioSource = GetComponent<AudioSource>();
			}
			if (audioSource == null)
			{
				audioSource = base.gameObject.AddComponent<AudioSource>();
			}
			if (audioSource != null && sfxMixerGroup != null)
			{
				audioSource.outputAudioMixerGroup = sfxMixerGroup;
			}
			if (healthBarAnimator == null)
			{
				GameObject gameObject = GameObject.Find("HealthBar");
				if (gameObject == null)
				{
					gameObject = GameObject.Find("Health Bar");
				}
				if (gameObject == null)
				{
					gameObject = GameObject.Find("HealthBarContainer");
				}
				if (gameObject != null)
				{
					healthBarAnimator = gameObject.GetComponent<Animator>();
				}
			}
			if (heatBarAnimator == null)
			{
				GameObject gameObject2 = GameObject.Find("HeatBar");
				if (gameObject2 == null)
				{
					gameObject2 = GameObject.Find("Heat Bar");
				}
				if (gameObject2 == null)
				{
					gameObject2 = GameObject.Find("HeatBarContainer");
				}
				if (gameObject2 != null)
				{
					heatBarAnimator = gameObject2.GetComponent<Animator>();
				}
			}
			if (currencyAnimator == null)
			{
				GameObject gameObject3 = GameObject.Find("Currency");
				if (gameObject3 == null)
				{
					gameObject3 = GameObject.Find("CurrencyDisplay");
				}
				if (gameObject3 == null)
				{
					gameObject3 = GameObject.Find("CurrencyUI");
				}
				if (gameObject3 != null)
				{
					currencyAnimator = gameObject3.GetComponent<Animator>();
				}
			}
			previousCursorLockState = Cursor.lockState;
			previousCursorVisible = Cursor.visible;
		}

		private void Start()
		{
			pauseMenu = Object.FindAnyObjectByType<PauseMenuManager>();
			InitializeGridUI();
			InitializeEquipmentUI();
			InitializeRowControls();
			if (inventoryPanel != null)
			{
				inventoryPanel.SetActive(value: false);
			}
			if (dragPreview != null)
			{
				dragPreview.SetActive(value: false);
			}
			if (tooltipPanel != null)
			{
				tooltipPanel.SetActive(value: false);
			}
			if (inventorySystem != null)
			{
				inventorySystem.OnItemAdded.AddListener(OnItemAdded);
				inventorySystem.OnItemRemoved.AddListener(OnItemRemoved);
				inventorySystem.OnItemMoved.AddListener(OnItemMoved);
				inventorySystem.OnItemEquipped.AddListener(OnItemEquipped);
				inventorySystem.OnItemUnequipped.AddListener(OnItemUnequipped);
				inventorySystem.OnRowUnlocked.AddListener(OnRowUnlocked);
				inventorySystem.OnInventoryChanged.AddListener(RefreshUI);
			}
			if (consumableSystem != null)
			{
				consumableSystem.OnItemConsumed.AddListener(OnItemConsumed);
				consumableSystem.OnConsumeFailed.AddListener(OnConsumeFailed);
			}
			RefreshUI();
		}

		private void Update()
		{
			if (Time.unscaledTime - focusRegainedTime < 0.2f)
			{
				return;
			}
			if (isStatDescriptionVisible)
			{
				UpdateStatDescriptionPosition();
			}
			if (InputManager.ToggleInventory != null && InputManager.ToggleInventory.WasPressedThisFrame() && !LevelEndScreen.IsActive)
			{
				if ((NoteViewer.Instance != null && NoteViewer.Instance.IsOpen) || (pauseMenu != null && pauseMenu.IsPaused))
				{
					return;
				}
				ToggleInventory();
			}
			if (isInventoryOpen && InputManager.DropItem != null && InputManager.DropItem.WasPressedThisFrame())
			{
				HandleItemDrop();
			}
			if (isInventoryOpen)
			{
				bool flag = false;
				if (InputManager.InventoryPickUp != null && InputManager.InventoryPickUp.WasPressedThisFrame())
				{
					flag = true;
					if (isControllerHolding)
					{
						PlaceControllerHold(focusedSlot);
					}
					else if (focusedSlot != null && !focusedSlot.IsEmpty && !focusedSlot.IsLocked)
					{
						StartControllerHold(focusedSlot);
					}
				}
				if (!flag && UISubmitPressedThisFrame() && !isControllerHolding && focusedSlot != null)
				{
					focusedSlot.InvokeRightClickAction();
				}
				if (UICancelPressedThisFrame())
				{
					if (isControllerHolding)
					{
						CancelControllerHold();
					}
					else
					{
						CloseInventory();
					}
				}
			}
			if (isDraggingItem())
			{
				UpdateDragPreview();
				if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame && !IsMouseOverValidDropTarget())
				{
					CancelDrag();
					PlaySound(errorSound);
				}
			}
			if (isInventoryOpen && InputManager.Pause != null && InputManager.Pause.WasPressedThisFrame())
			{
				CloseInventory();
			}
		}

		private bool IsMouseOverValidDropTarget()
		{
			EventSystem current = EventSystem.current;
			if (current == null)
			{
				return false;
			}
			if (cachedPointerData == null)
			{
				cachedPointerData = new PointerEventData(current);
			}
			cachedPointerData.position = ((Mouse.current != null) ? ((Vector3)Mouse.current.position.ReadValue()) : Vector3.zero);
			cachedRaycastResults.Clear();
			current.RaycastAll(cachedPointerData, cachedRaycastResults);
			foreach (RaycastResult cachedRaycastResult in cachedRaycastResults)
			{
				if (cachedRaycastResult.gameObject.GetComponent<InventorySlotUI>() != null)
				{
					return true;
				}
			}
			return false;
		}

		private void OnDestroy()
		{
			if (inventorySystem != null)
			{
				inventorySystem.OnItemAdded.RemoveListener(OnItemAdded);
				inventorySystem.OnItemRemoved.RemoveListener(OnItemRemoved);
				inventorySystem.OnItemMoved.RemoveListener(OnItemMoved);
				inventorySystem.OnItemEquipped.RemoveListener(OnItemEquipped);
				inventorySystem.OnItemUnequipped.RemoveListener(OnItemUnequipped);
				inventorySystem.OnRowUnlocked.RemoveListener(OnRowUnlocked);
				inventorySystem.OnInventoryChanged.RemoveListener(RefreshUI);
			}
			if (consumableSystem != null)
			{
				consumableSystem.OnItemConsumed.RemoveListener(OnItemConsumed);
				consumableSystem.OnConsumeFailed.RemoveListener(OnConsumeFailed);
			}
		}

		private void InitializeGridUI()
		{
			if (inventorySystem == null || gridContainer == null || slotPrefab == null)
			{
				return;
			}
			for (int num = gridContainer.childCount - 1; num >= 0; num--)
			{
				if (Application.isPlaying)
				{
					Object.DestroyImmediate(gridContainer.GetChild(num).gameObject);
				}
				else
				{
					Object.DestroyImmediate(gridContainer.GetChild(num).gameObject);
				}
			}
			GridLayoutGroup gridLayoutGroup = gridContainer.GetComponent<GridLayoutGroup>();
			if (gridLayoutGroup == null)
			{
				gridLayoutGroup = gridContainer.gameObject.AddComponent<GridLayoutGroup>();
			}
			gridLayoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
			gridLayoutGroup.constraintCount = inventorySystem.GridWidth;
			gridLayoutGroup.cellSize = new Vector2(25f, 25f);
			gridLayoutGroup.spacing = new Vector2(2f, 2f);
			gridSlots = new InventorySlotUI[inventorySystem.GridWidth, inventorySystem.GridHeight];
			int num2 = 0;
			for (int i = 0; i < inventorySystem.GridHeight; i++)
			{
				for (int j = 0; j < inventorySystem.GridWidth; j++)
				{
					GameObject gameObject = Object.Instantiate(slotPrefab, gridContainer);
					gameObject.name = $"Slot_{j}_{i}";
					InventorySlotUI inventorySlotUI = gameObject.GetComponent<InventorySlotUI>();
					if (inventorySlotUI == null)
					{
						inventorySlotUI = gameObject.AddComponent<InventorySlotUI>();
					}
					inventorySlotUI.ApplyHighlightTheme(slotHighlightTheme);
					inventorySlotUI.Initialize(new Vector2Int(j, i), this);
					gridSlots[j, i] = inventorySlotUI;
					num2++;
					if (inventorySystem.ShowDebugCoordinates)
					{
						inventorySlotUI.SetDebugCoordinates($"{j},{i}");
					}
				}
			}
			WireSlotNavigation();
		}

		private static Selectable SelOf(InventorySlotUI s)
		{
			if (!(s != null))
			{
				return null;
			}
			return s.GetComponent<Selectable>();
		}

		private void WireSlotNavigation()
		{
			if (gridSlots == null || inventorySystem == null)
			{
				return;
			}
			int gridWidth = inventorySystem.GridWidth;
			int gridHeight = inventorySystem.GridHeight;
			for (int i = 0; i < gridHeight; i++)
			{
				for (int j = 0; j < gridWidth; j++)
				{
					InventorySlotUI inventorySlotUI = gridSlots[j, i];
					if (!(inventorySlotUI == null))
					{
						Selectable component = inventorySlotUI.GetComponent<Selectable>();
						if (!(component == null))
						{
							Navigation navigation = component.navigation;
							navigation.mode = Navigation.Mode.Explicit;
							navigation.selectOnLeft = ((j > 0) ? SelOf(gridSlots[j - 1, i]) : null);
							navigation.selectOnRight = ((j < gridWidth - 1) ? SelOf(gridSlots[j + 1, i]) : null);
							navigation.selectOnUp = FindUpNeighbor(j, i);
							navigation.selectOnDown = FindDownNeighbor(j, i, gridHeight);
							component.navigation = navigation;
						}
					}
				}
			}
			WireEquipmentNavigation();
		}

		private void WireEquipmentNavigation()
		{
			Selectable firstSelectable = FirstSelectable;
			SetNav(upperArmorSlot, null, SelOf(lowerArmorSlot), null, firstSelectable);
			SetNav(lowerArmorSlot, SelOf(upperArmorSlot), SelOf(leftWeaponSlot), null, firstSelectable);
			SetNav(leftWeaponSlot, SelOf(lowerArmorSlot), SelOf(trinket1Slot), null, SelOf(rightWeaponSlot));
			SetNav(rightWeaponSlot, SelOf(lowerArmorSlot), SelOf(trinket2Slot), SelOf(leftWeaponSlot), firstSelectable);
			SetNav(trinket1Slot, SelOf(leftWeaponSlot), null, null, SelOf(trinket2Slot));
			SetNav(trinket2Slot, SelOf(rightWeaponSlot), null, SelOf(trinket1Slot), firstSelectable);
		}

		private static void SetNav(InventorySlotUI slot, Selectable up, Selectable down, Selectable left, Selectable right)
		{
			Selectable selectable = SelOf(slot);
			if (!(selectable == null))
			{
				Navigation navigation = selectable.navigation;
				navigation.mode = Navigation.Mode.Explicit;
				navigation.selectOnUp = up;
				navigation.selectOnDown = down;
				navigation.selectOnLeft = left;
				navigation.selectOnRight = right;
				selectable.navigation = navigation;
			}
		}

		private Selectable FindUpNeighbor(int x, int y)
		{
			for (int num = y - 1; num >= 0; num--)
			{
				InventorySlotUI inventorySlotUI = gridSlots[x, num];
				if (inventorySlotUI != null && !inventorySlotUI.IsLocked)
				{
					return SelOf(inventorySlotUI);
				}
			}
			return equipmentEntryPoint;
		}

		private Selectable FindDownNeighbor(int x, int y, int h)
		{
			for (int i = y + 1; i < h; i++)
			{
				InventorySlotUI inventorySlotUI = gridSlots[x, i];
				if (inventorySlotUI != null && !inventorySlotUI.IsLocked)
				{
					return SelOf(inventorySlotUI);
				}
			}
			return null;
		}

		private void InitializeEquipmentUI()
		{
			InitializeEquipmentSlot(upperArmorSlot, EquipmentSlotType.UpperArmor);
			InitializeEquipmentSlot(lowerArmorSlot, EquipmentSlotType.LowerArmor);
			InitializeEquipmentSlot(leftWeaponSlot, EquipmentSlotType.LeftWeapon);
			InitializeEquipmentSlot(rightWeaponSlot, EquipmentSlotType.RightWeapon);
			InitializeEquipmentSlot(trinket1Slot, EquipmentSlotType.Trinket1);
			InitializeEquipmentSlot(trinket2Slot, EquipmentSlotType.Trinket2);
		}

		private void InitializeEquipmentSlot(InventorySlotUI slot, EquipmentSlotType slotType)
		{
			if (!(slot == null))
			{
				slot.ApplyHighlightTheme(slotHighlightTheme);
				slot.Initialize(slotType, this);
			}
		}

		private void InitializeRowControls()
		{
			if (rowControlsContainer == null || rowTogglePrefab == null)
			{
				return;
			}
			foreach (Transform item in rowControlsContainer)
			{
				Object.DestroyImmediate(item.gameObject);
			}
			rowToggles = new List<Toggle>();
			for (int i = 0; i < inventorySystem.GridHeight; i++)
			{
				GameObject gameObject = Object.Instantiate(rowTogglePrefab, rowControlsContainer);
				Toggle toggle = gameObject.GetComponent<Toggle>();
				if (toggle == null)
				{
					toggle = gameObject.AddComponent<Toggle>();
				}
				TextMeshProUGUI componentInChildren = toggle.GetComponentInChildren<TextMeshProUGUI>();
				if (componentInChildren != null)
				{
					componentInChildren.text = $"R{i + 1}";
				}
				toggle.isOn = inventorySystem.IsRowUnlocked(i);
				toggle.interactable = i >= 2;
				int rowIndex = i;
				toggle.onValueChanged.AddListener((bool isOn) =>
				{
					OnRowToggleChanged(rowIndex, isOn);
				});
				rowToggles.Add(toggle);
			}
		}

		public void ToggleInventory()
		{
			if (isInventoryOpen)
			{
				CloseInventory();
			}
			else
			{
				OpenInventory();
			}
		}

		public void OpenInventory()
		{
			if (inventoryPanel != null)
			{
				inventoryPanel.SetActive(value: true);
				isInventoryOpen = true;
				ClearAllSlotHighlights();
				if (healthBarAnimator != null)
				{
					healthBarAnimator.Play("HealthShow");
					healthBarAnimator.SetBool("InventoryOpen", value: true);
				}
				if (heatBarAnimator != null)
				{
					heatBarAnimator.Play("HeatShow");
					heatBarAnimator.SetBool("InventoryOpen", value: true);
				}
				if (currencyAnimator != null)
				{
					currencyAnimator.Play("Shown");
					currencyAnimator.SetBool("InventoryOpen", value: true);
				}
				previousCursorLockState = Cursor.lockState;
				previousCursorVisible = Cursor.visible;
				Cursor.lockState = CursorLockMode.None;
				Cursor.visible = true;
				if (pauseGameWhenOpen)
				{
					Time.timeScale = 0f;
				}
				if (disablePlayerMovement && firstPersonController != null)
				{
					firstPersonController.enabled = false;
				}
				if (disablePlayerCombat && dualWieldingSystem != null)
				{
					dualWieldingSystem.enabled = false;
				}
				RefreshUI();
				WireSlotNavigation();
				Selectable firstSelectable = FirstSelectable;
				if (firstSelectable != null && EventSystem.current != null)
				{
					EventSystem.current.SetSelectedGameObject(firstSelectable.gameObject);
				}
			}
		}

		public void CloseInventory()
		{
			if (inventoryPanel != null)
			{
				inventoryPanel.SetActive(value: false);
				isInventoryOpen = false;
				pinnedTooltipItem = null;
				HideTooltip();
				HideStatDescription();
				if (healthBarAnimator != null)
				{
					healthBarAnimator.SetBool("InventoryOpen", value: false);
				}
				if (heatBarAnimator != null)
				{
					heatBarAnimator.SetBool("InventoryOpen", value: false);
				}
				if (currencyAnimator != null)
				{
					currencyAnimator.SetBool("InventoryOpen", value: false);
				}
				if (isControllerHolding)
				{
					CancelControllerHold();
				}
				if (isDraggingItem())
				{
					CancelDrag();
				}
				if (EventSystem.current != null)
				{
					EventSystem.current.SetSelectedGameObject(null);
				}
				focusedSlot = null;
				ClearAllSlotHighlights();
				Cursor.lockState = previousCursorLockState;
				Cursor.visible = previousCursorVisible;
				if (pauseGameWhenOpen)
				{
					Time.timeScale = 1f;
				}
				if (disablePlayerMovement && firstPersonController != null)
				{
					firstPersonController.enabled = true;
					firstPersonController.ForceUpdateMovementSpeeds();
				}
				if (disablePlayerCombat && dualWieldingSystem != null)
				{
					dualWieldingSystem.enabled = true;
				}
			}
		}

		public void RefreshUI()
		{
			RefreshGridSlots();
			RefreshEquipmentSlots();
			RefreshStats();
			RefreshRowControls();
			RefreshHighlightDriver();
		}

		private void RefreshGridSlots()
		{
			if (gridSlots == null || inventorySystem == null)
			{
				return;
			}
			for (int i = 0; i < inventorySystem.GridWidth; i++)
			{
				for (int j = 0; j < inventorySystem.GridHeight; j++)
				{
					if (gridSlots[i, j] != null)
					{
						Vector2Int position = new Vector2Int(i, j);
						InventoryItem item = inventorySystem.GetItem(position);
						bool locked = !inventorySystem.IsRowUnlocked(j);
						gridSlots[i, j].SetItem(item);
						gridSlots[i, j].SetLocked(locked);
					}
				}
			}
		}

		private void RefreshEquipmentSlots()
		{
			if (!(inventorySystem == null))
			{
				if (upperArmorSlot != null)
				{
					upperArmorSlot.SetItem(inventorySystem.UpperArmor);
				}
				if (lowerArmorSlot != null)
				{
					lowerArmorSlot.SetItem(inventorySystem.LowerArmor);
				}
				if (leftWeaponSlot != null)
				{
					leftWeaponSlot.SetItem(inventorySystem.LeftWeapon);
				}
				if (rightWeaponSlot != null)
				{
					rightWeaponSlot.SetItem(inventorySystem.RightWeapon);
				}
				if (trinket1Slot != null)
				{
					trinket1Slot.SetItem(inventorySystem.Trinket1);
				}
				if (trinket2Slot != null)
				{
					trinket2Slot.SetItem(inventorySystem.Trinket2);
				}
			}
		}

		private void RefreshStats()
		{
			if (playerStats != null && healthText != null)
			{
				healthText.text = $"{playerStats.CurrentHealth}/{playerStats.MaxHealth}";
			}
			if (inventorySystem != null)
			{
				if (slotsUsedText != null)
				{
					slotsUsedText.text = $"{inventorySystem.UsedSlots}/{inventorySystem.TotalSlots}";
				}
				int num = CalculateTotalAttack();
				int num2 = CalculateTotalDefense();
				if (attackText != null)
				{
					attackText.text = num.ToString();
				}
				if (defenseText != null)
				{
					defenseText.text = num2.ToString();
				}
			}
		}

		private void RefreshRowControls()
		{
			if (rowToggles == null || inventorySystem == null)
			{
				return;
			}
			for (int i = 0; i < rowToggles.Count && i < inventorySystem.GridHeight; i++)
			{
				if (rowToggles[i] != null)
				{
					rowToggles[i].isOn = inventorySystem.IsRowUnlocked(i);
				}
			}
		}

		private int CalculateTotalAttack()
		{
			int num = 25;
			InventoryItem leftWeapon = inventorySystem.LeftWeapon;
			if ((object)leftWeapon != null && leftWeapon.IsWeapon)
			{
				num += inventorySystem.LeftWeapon.weaponData.damage;
			}
			InventoryItem rightWeapon = inventorySystem.RightWeapon;
			if ((object)rightWeapon != null && rightWeapon.IsWeapon)
			{
				num += inventorySystem.RightWeapon.weaponData.damage;
			}
			return num;
		}

		private int CalculateTotalDefense()
		{
			int num = 15;
			InventoryItem upperArmor = inventorySystem.UpperArmor;
			if ((object)upperArmor != null && upperArmor.IsArmor)
			{
				num += inventorySystem.UpperArmor.armorData.defense;
			}
			InventoryItem lowerArmor = inventorySystem.LowerArmor;
			if ((object)lowerArmor != null && lowerArmor.IsArmor)
			{
				num += inventorySystem.LowerArmor.armorData.defense;
			}
			return num;
		}

		private void OnItemAdded(InventoryItem item, Vector2Int position)
		{
			PlaySound(item?.pickupDropSound ?? pickupSound);
		}

		private void OnItemRemoved(InventoryItem item, Vector2Int position)
		{
			PlaySound(item?.pickupDropSound ?? dropSound);
		}

		private void OnItemMoved(InventoryItem item, Vector2Int fromPosition, Vector2Int toPosition)
		{
		}

		private void OnItemEquipped(InventoryItem item, EquipmentSlotType slotType)
		{
			PlaySound(item?.equipSound ?? equipSound);
		}

		private void OnItemUnequipped(InventoryItem item, EquipmentSlotType slotType)
		{
			PlaySound(item?.pickupDropSound ?? dropSound);
		}

		private void OnRowUnlocked(int rowIndex)
		{
			RefreshRowControls();
			RefreshGridSlots();
			WireSlotNavigation();
		}

		private void OnRowToggleChanged(int rowIndex, bool isOn)
		{
			if (isOn && !inventorySystem.IsRowUnlocked(rowIndex))
			{
				if (rowToggles[rowIndex] != null)
				{
					rowToggles[rowIndex].isOn = false;
				}
			}
			else if (!isOn && inventorySystem.IsRowUnlocked(rowIndex) && rowIndex >= 2)
			{
				inventorySystem.LockRow(rowIndex);
			}
		}

		private void OnItemConsumed(InventoryItem item)
		{
			PlaySound(pickupSound);
		}

		private void OnConsumeFailed(InventoryItem item, string reason)
		{
			PlaySound(errorSound);
		}

		public bool TryUseConsumable(InventoryItem item, Vector2Int position)
		{
			if (consumableSystem == null)
			{
				return false;
			}
			if (item == null || !item.IsConsumable)
			{
				return false;
			}
			if (inventorySystem == null)
			{
				return false;
			}
			if (inventorySystem.GetItem(position) != item)
			{
				return false;
			}
			bool num = consumableSystem.TryUseConsumable(item);
			if (num)
			{
				inventorySystem.RemoveItem(position, silent: true);
			}
			return num;
		}

		public bool TryUseItem(InventoryItem item, Vector2Int position)
		{
			return TryUseConsumable(item, position);
		}

		private static bool UISubmitPressedThisFrame()
		{
			return UIInput.SubmitPressedThisFrame();
		}

		private static bool UICancelPressedThisFrame()
		{
			return UIInput.CancelPressedThisFrame();
		}

		private void HandleItemDrop()
		{
			InventorySlotUI slotUnderMouse = focusedSlot;
			if (slotUnderMouse == null)
			{
				Vector2 mousePosition = ((Mouse.current != null) ? ((Vector3)Mouse.current.position.ReadValue()) : Vector3.zero);
				slotUnderMouse = GetSlotUnderMouse(mousePosition);
			}
			HandleItemDrop(slotUnderMouse);
		}

		private void HandleItemDrop(InventorySlotUI target)
		{
			if (!(itemDropper == null) && !(target == null) && !(target.CurrentItem == null) && !target.IsLocked)
			{
				DropItemFromSlot(target);
			}
		}

		private InventorySlotUI GetSlotUnderMouse(Vector2 mousePosition)
		{
			PointerEventData eventData = new PointerEventData(EventSystem.current)
			{
				position = mousePosition
			};
			List<RaycastResult> list = new List<RaycastResult>();
			GraphicRaycaster componentInParent = GetComponentInParent<GraphicRaycaster>();
			if (componentInParent != null)
			{
				componentInParent.Raycast(eventData, list);
				foreach (RaycastResult item in list)
				{
					InventorySlotUI component = item.gameObject.GetComponent<InventorySlotUI>();
					if (component != null)
					{
						return component;
					}
				}
			}
			return null;
		}

		private void DropItemFromSlot(InventorySlotUI slot)
		{
			if (slot.CurrentItem == null || itemDropper == null)
			{
				return;
			}
			_ = slot.CurrentItem;
			if (!slot.IsEmpty && !slot.IsLocked)
			{
				Vector2Int gridPosition = slot.GridPosition;
				InventoryItem inventoryItem = inventorySystem.RemoveItem(gridPosition);
				if (inventoryItem != null)
				{
					itemDropper.DropItem(inventoryItem);
					PlaySound(dropSound);
				}
			}
		}

		public void DropEquippedItem(EquipmentSlotType slotType)
		{
			if (itemDropper == null || !(inventorySystem.GetEquippedItem(slotType) != null))
			{
				return;
			}
			InventoryItem inventoryItem = inventorySystem.UnequipItem(slotType);
			if (inventoryItem != null)
			{
				Vector2Int? vector2Int = FindItemInInventory(inventoryItem);
				if (vector2Int.HasValue)
				{
					inventorySystem.RemoveItem(vector2Int.Value);
					itemDropper.DropItem(inventoryItem);
					PlaySound(dropSound);
				}
			}
		}

		public void DropAllItems()
		{
			if (!(itemDropper == null) && !(inventorySystem == null))
			{
				List<InventoryItem> allItems = inventorySystem.GetAllItems();
				if (allItems.Count != 0)
				{
					inventorySystem.ClearInventory();
					StartCoroutine(DropItemsWithDelay(allItems.ToArray()));
				}
			}
		}

		private IEnumerator DropItemsWithDelay(InventoryItem[] items)
		{
			for (int i = 0; i < items.Length; i++)
			{
				if (items[i] != null)
				{
					itemDropper.DropItem(items[i]);
					if (i % 3 == 0)
					{
						yield return new WaitForSeconds(0.1f);
					}
				}
			}
		}

		public void DropItems(InventoryItem[] items)
		{
			if (itemDropper == null || items == null)
			{
				return;
			}
			foreach (InventoryItem inventoryItem in items)
			{
				if (inventoryItem != null)
				{
					itemDropper.DropItem(inventoryItem);
				}
			}
		}

		private void RefreshHighlightDriver()
		{
			InventoryItem item = ((draggedItem != null) ? draggedItem : ((!(focusedSlot != null) || IsEquipmentSlotInstance(focusedSlot)) ? null : focusedSlot.CurrentItem));
			RefreshEquipmentHighlights(item, HighlightSourceSlot());
		}

		public void RefreshEquipmentHighlights(InventoryItem item)
		{
			RefreshEquipmentHighlights(item, HighlightSourceSlot());
		}

		public void ClearEquipmentHighlights()
		{
			RefreshEquipmentHighlights(null, null);
		}

		private void RefreshEquipmentHighlights(InventoryItem item, InventorySlotUI sourceSlot)
		{
			ApplyEquipmentHighlight(upperArmorSlot, EquipmentSlotType.UpperArmor, item, sourceSlot);
			ApplyEquipmentHighlight(lowerArmorSlot, EquipmentSlotType.LowerArmor, item, sourceSlot);
			ApplyEquipmentHighlight(leftWeaponSlot, EquipmentSlotType.LeftWeapon, item, sourceSlot);
			ApplyEquipmentHighlight(rightWeaponSlot, EquipmentSlotType.RightWeapon, item, sourceSlot);
			ApplyEquipmentHighlight(trinket1Slot, EquipmentSlotType.Trinket1, item, sourceSlot);
			ApplyEquipmentHighlight(trinket2Slot, EquipmentSlotType.Trinket2, item, sourceSlot);
		}

		private static void ApplyEquipmentHighlight(InventorySlotUI slot, EquipmentSlotType slotType, InventoryItem item, InventorySlotUI sourceSlot)
		{
			if (!(slot == null))
			{
				bool validTarget = item != null && slot != sourceSlot && !slot.IsLocked && item.CanEquipInSlot(slotType);
				slot.SetValidTarget(validTarget);
			}
		}

		private InventorySlotUI HighlightSourceSlot()
		{
			if (draggedItem != null)
			{
				if (!isDraggingFromEquipment)
				{
					return null;
				}
				return GetEquipmentSlotUI(draggedFromEquipment);
			}
			return focusedSlot;
		}

		private InventorySlotUI GetEquipmentSlotUI(EquipmentSlotType slotType)
		{
			return slotType switch
			{
				EquipmentSlotType.UpperArmor => upperArmorSlot, 
				EquipmentSlotType.LowerArmor => lowerArmorSlot, 
				EquipmentSlotType.LeftWeapon => leftWeaponSlot, 
				EquipmentSlotType.RightWeapon => rightWeaponSlot, 
				EquipmentSlotType.Trinket1 => trinket1Slot, 
				EquipmentSlotType.Trinket2 => trinket2Slot, 
				_ => null, 
			};
		}

		private void ClearAllSlotHighlights()
		{
			if (gridSlots != null)
			{
				for (int i = 0; i < gridSlots.GetLength(0); i++)
				{
					for (int j = 0; j < gridSlots.GetLength(1); j++)
					{
						if (gridSlots[i, j] != null)
						{
							gridSlots[i, j].ClearHighlights();
						}
					}
				}
			}
			if (upperArmorSlot != null)
			{
				upperArmorSlot.ClearHighlights();
			}
			if (lowerArmorSlot != null)
			{
				lowerArmorSlot.ClearHighlights();
			}
			if (leftWeaponSlot != null)
			{
				leftWeaponSlot.ClearHighlights();
			}
			if (rightWeaponSlot != null)
			{
				rightWeaponSlot.ClearHighlights();
			}
			if (trinket1Slot != null)
			{
				trinket1Slot.ClearHighlights();
			}
			if (trinket2Slot != null)
			{
				trinket2Slot.ClearHighlights();
			}
		}

		public void StartDrag(InventoryItem item, Vector2Int fromPosition)
		{
			if (!(item == null))
			{
				draggedItem = item;
				draggedFromPosition = fromPosition;
				isDraggingFromEquipment = false;
				RefreshHighlightDriver();
				PlaySound(item?.inventoryMoveSound ?? inventoryMoveSound);
				StartDragVisual(item);
			}
		}

		public void StartDragFromEquipment(InventoryItem item, EquipmentSlotType fromSlot)
		{
			if (!(item == null))
			{
				draggedItem = item;
				draggedFromEquipment = fromSlot;
				isDraggingFromEquipment = true;
				RefreshHighlightDriver();
				PlaySound(item?.inventoryMoveSound ?? inventoryMoveSound);
				StartDragVisual(item);
			}
		}

		private void StartDragVisual(InventoryItem item)
		{
			if (dragPreview != null && dragPreviewImage != null)
			{
				dragPreview.SetActive(value: true);
				dragPreviewImage.sprite = item.itemIcon;
				dragPreviewImage.color = new Color(1f, 1f, 1f, 0.8f);
				if (dragCanvas != null)
				{
					dragPreview.transform.SetParent(dragCanvas.transform, worldPositionStays: false);
				}
				else
				{
					dragPreview.transform.SetParent(base.transform, worldPositionStays: false);
				}
				RectTransform component = dragPreview.GetComponent<RectTransform>();
				if (component != null)
				{
					component.pivot = new Vector2(0.5f, 0.5f);
					if (dragCanvas != null && dragCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
					{
						component.anchorMin = Vector2.zero;
						component.anchorMax = Vector2.zero;
						component.anchoredPosition = Vector2.zero;
					}
				}
				UpdateDragPreview();
			}
			HideTooltip();
		}

		public void EndDrag(Vector2Int toPosition)
		{
			if (!isDraggingItem())
			{
				return;
			}
			bool flag = false;
			if (isDraggingFromEquipment)
			{
				if (!inventorySystem.IsSlotAvailable(toPosition))
				{
					PlaySound(errorSound);
					CancelDrag();
					return;
				}
				InventoryItem inventoryItem = inventorySystem.UnequipItem(draggedFromEquipment);
				if (inventoryItem != null)
				{
					Vector2Int? vector2Int = FindItemInInventory(inventoryItem);
					flag = !vector2Int.HasValue || !(vector2Int.Value != toPosition) || inventorySystem.MoveItem(vector2Int.Value, toPosition);
					inventorySystem.SyncDualWieldingSystem();
					if (!flag)
					{
						flag = true;
						PlaySound(pickupSound);
					}
				}
				else
				{
					PlaySound(errorSound);
				}
			}
			else if (!inventorySystem.MoveItem(draggedFromPosition, toPosition))
			{
				PlaySound(errorSound);
			}
			CancelDrag();
		}

		private Vector2Int? FindItemInInventory(InventoryItem item)
		{
			if (inventorySystem == null)
			{
				return null;
			}
			for (int i = 0; i < inventorySystem.GridWidth; i++)
			{
				for (int j = 0; j < inventorySystem.GridHeight; j++)
				{
					Vector2Int vector2Int = new Vector2Int(i, j);
					if (inventorySystem.GetItem(vector2Int) == item)
					{
						return vector2Int;
					}
				}
			}
			return null;
		}

		public void EndDragOnEquipment(EquipmentSlotType toSlot)
		{
			if (!isDraggingItem())
			{
				return;
			}
			if (isDraggingFromEquipment)
			{
				PlaySound(errorSound);
			}
			else
			{
				InventoryItem inventoryItem = inventorySystem.RemoveItem(draggedFromPosition, silent: true);
				if (inventoryItem != null)
				{
					if (inventorySystem.EquipItem(inventoryItem, toSlot, out var displacedItems))
					{
						if (displacedItems.Count > 0)
						{
							DropItems(displacedItems.ToArray());
						}
					}
					else
					{
						inventorySystem.AddItem(inventoryItem, draggedFromPosition, silent: true);
						PlaySound(errorSound);
					}
				}
			}
			CancelDrag();
		}

		public void CancelDrag()
		{
			draggedItem = null;
			isDraggingFromEquipment = false;
			isControllerHolding = false;
			RefreshHighlightDriver();
			if (dragPreview != null)
			{
				dragPreview.SetActive(value: false);
				RectTransform component = dragPreview.GetComponent<RectTransform>();
				if (component != null)
				{
					component.anchoredPosition = Vector2.zero;
				}
			}
		}

		public void StartControllerHold(InventorySlotUI source)
		{
			if (!(source == null) && !(source.CurrentItem == null) && !source.IsLocked && !isDraggingItem())
			{
				if (IsEquipmentSlotInstance(source))
				{
					StartDragFromEquipment(source.CurrentItem, source.EquipmentSlotType);
				}
				else
				{
					StartDrag(source.CurrentItem, source.GridPosition);
				}
				isControllerHolding = true;
			}
		}

		public void PlaceControllerHold(InventorySlotUI target)
		{
			if (!isControllerHolding || target == null)
			{
				CancelControllerHold();
				return;
			}
			bool num;
			if (!IsEquipmentSlotInstance(target))
			{
				if (!isDraggingFromEquipment)
				{
					num = target.GridPosition == draggedFromPosition;
					goto IL_0058;
				}
			}
			else if (isDraggingFromEquipment)
			{
				num = target.EquipmentSlotType == draggedFromEquipment;
				goto IL_0058;
			}
			goto IL_0061;
			IL_0058:
			if (num)
			{
				CancelControllerHold();
				return;
			}
			goto IL_0061;
			IL_0061:
			if (IsEquipmentSlotInstance(target))
			{
				EndDragOnEquipment(target.EquipmentSlotType);
			}
			else
			{
				EndDrag(target.GridPosition);
			}
			isControllerHolding = false;
		}

		public void CancelControllerHold()
		{
			if (isControllerHolding)
			{
				CancelDrag();
				isControllerHolding = false;
			}
		}

		private bool IsEquipmentSlotInstance(InventorySlotUI s)
		{
			if (!(s == upperArmorSlot) && !(s == lowerArmorSlot) && !(s == leftWeaponSlot) && !(s == rightWeaponSlot) && !(s == trinket1Slot))
			{
				return s == trinket2Slot;
			}
			return true;
		}

		private bool isDraggingItem()
		{
			return draggedItem != null;
		}

		private void UpdateDragPreview()
		{
			if (!(dragPreview != null) || !isDraggingItem())
			{
				return;
			}
			RectTransform component = dragPreview.GetComponent<RectTransform>();
			if (component == null)
			{
				return;
			}
			Vector3 vector = ((!isControllerHolding || !(focusedSlot != null)) ? ((Mouse.current != null) ? ((Vector3)Mouse.current.position.ReadValue()) : Vector3.zero) : ((Vector3)RectTransformUtility.WorldToScreenPoint(null, focusedSlot.transform.position)));
			if (mainCanvas != null)
			{
				Canvas canvas = mainCanvas;
				if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
				{
					component.position = vector;
					return;
				}
				RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.transform as RectTransform, vector, canvas.worldCamera, out var localPoint);
				component.localPosition = localPoint;
			}
			else
			{
				component.position = vector;
			}
		}

		public void PinTooltip(InventoryItem item, Vector2 screenPosition)
		{
			if (pinnedTooltipItem == item)
			{
				UnpinTooltip();
				return;
			}
			pinnedTooltipItem = item;
			ShowTooltip(item, screenPosition);
		}

		public void UnpinTooltip()
		{
			pinnedTooltipItem = null;
			HideTooltip();
		}

		public void ShowTooltip(InventoryItem item, Vector2 screenPosition)
		{
			if ((pinnedTooltipItem != null && pinnedTooltipItem != item) || tooltipPanel == null || item == null)
			{
				return;
			}
			if (tooltipTitle != null)
			{
				tooltipTitle.text = item.GetDisplayName();
			}
			if (tooltipItemImage != null)
			{
				tooltipItemImage.sprite = item.itemIcon;
			}
			if (tooltipRarityImage != null)
			{
				tooltipRarityImage.color = item.GetRarityColor();
			}
			if (tooltipDescription != null)
			{
				tooltipDescription.text = item.itemDescription;
			}
			bool flag = item.IsWeapon && item.weaponData != null;
			if (tooltipHandTypeIcon != null)
			{
				if (flag && tooltipIcons != null)
				{
					tooltipHandTypeIcon.sprite = tooltipIcons.GetHandTypeIcon(item.weaponData.twoHanded);
					tooltipHandTypeIcon.gameObject.SetActive(value: true);
				}
				else
				{
					tooltipHandTypeIcon.gameObject.SetActive(value: false);
				}
			}
			if (tooltipWeaponTypeIcon != null)
			{
				if (flag && tooltipIcons != null)
				{
					Sprite weaponTypeIcon = tooltipIcons.GetWeaponTypeIcon(item.weaponData.weaponType);
					if (weaponTypeIcon != null)
					{
						tooltipWeaponTypeIcon.sprite = weaponTypeIcon;
						tooltipWeaponTypeIcon.gameObject.SetActive(value: true);
					}
					else
					{
						tooltipWeaponTypeIcon.gameObject.SetActive(value: false);
					}
				}
				else
				{
					tooltipWeaponTypeIcon.gameObject.SetActive(value: false);
				}
			}
			ClearStatRows();
			HideStatDescription();
			bool isConsumable = item.IsConsumable;
			if (tooltipStatsContainer != null && tooltipStatRowPrefab != null && tooltipIcons != null)
			{
				List<TooltipStatEntry> tooltipStatEntries = item.GetTooltipStatEntries();
				for (int i = 0; i < tooltipStatEntries.Count; i++)
				{
					Sprite icon = tooltipIcons.GetIcon(tooltipStatEntries[i].statType);
					if (!(icon == null))
					{
						string description = tooltipIcons.GetDescription(tooltipStatEntries[i].statType, isConsumable);
						TooltipStatRow tooltipStatRow = Object.Instantiate(tooltipStatRowPrefab, tooltipStatsContainer);
						tooltipStatRow.Setup(icon, tooltipStatEntries[i].value, description, ShowStatDescription, HideStatDescription);
						activeStatRows.Add(tooltipStatRow);
					}
				}
			}
			if (tooltipExtraStats != null)
			{
				string tooltipExtraText = item.GetTooltipExtraText();
				tooltipExtraStats.text = tooltipExtraText;
				tooltipExtraStats.gameObject.SetActive(!string.IsNullOrEmpty(tooltipExtraText));
			}
			tooltipPanel.SetActive(value: true);
		}

		public void HideTooltip()
		{
			if (!(pinnedTooltipItem != null))
			{
				if (tooltipPanel != null)
				{
					tooltipPanel.SetActive(value: false);
				}
				ClearStatRows();
			}
		}

		private void ShowStatDescription(string description)
		{
			if (tooltipStatDescription != null)
			{
				tooltipStatDescription.text = description;
			}
			if (statDescriptionPanel != null)
			{
				statDescriptionPanel.gameObject.SetActive(value: true);
				UpdateStatDescriptionPosition();
			}
			isStatDescriptionVisible = true;
		}

		private void HideStatDescription()
		{
			isStatDescriptionVisible = false;
			if (tooltipStatDescription != null)
			{
				tooltipStatDescription.text = "";
			}
			if (statDescriptionPanel != null)
			{
				statDescriptionPanel.gameObject.SetActive(value: false);
			}
		}

		private void UpdateStatDescriptionPosition()
		{
			if (statDescriptionPanel == null)
			{
				return;
			}
			Canvas componentInParent = statDescriptionPanel.GetComponentInParent<Canvas>();
			if (!(componentInParent == null))
			{
				if (componentInParent.renderMode == RenderMode.ScreenSpaceOverlay)
				{
					statDescriptionPanel.position = ((Mouse.current != null) ? ((Vector3)Mouse.current.position.ReadValue()) : Vector3.zero);
				}
				else if (componentInParent.worldCamera != null)
				{
					RectTransformUtility.ScreenPointToLocalPointInRectangle(componentInParent.transform as RectTransform, (Mouse.current != null) ? ((Vector3)Mouse.current.position.ReadValue()) : Vector3.zero, componentInParent.worldCamera, out var localPoint);
					statDescriptionPanel.position = componentInParent.transform.TransformPoint(localPoint);
				}
			}
		}

		private void ClearStatRows()
		{
			for (int num = activeStatRows.Count - 1; num >= 0; num--)
			{
				if (activeStatRows[num] != null)
				{
					Object.Destroy(activeStatRows[num].gameObject);
				}
			}
			activeStatRows.Clear();
		}

		private void PlaySound(AudioClip clip)
		{
			if (audioSource != null && clip != null)
			{
				audioSource.PlayOneShot(clip);
			}
		}

		public void ForceCloseInventory()
		{
			if (isInventoryOpen)
			{
				CloseInventory();
			}
		}

		public bool ArePlayerControlsDisabled()
		{
			if (isInventoryOpen)
			{
				if (!disablePlayerMovement)
				{
					return disablePlayerCombat;
				}
				return true;
			}
			return false;
		}

		public void SetInventoryEnabled(bool enabled)
		{
			base.enabled = enabled;
			if (!enabled && isInventoryOpen)
			{
				ForceCloseInventory();
			}
		}

		public void ConfigurePlayerControls(bool pauseGame, bool disableMovement, bool disableCombat)
		{
			pauseGameWhenOpen = pauseGame;
			disablePlayerMovement = disableMovement;
			disablePlayerCombat = disableCombat;
		}

		public void ConfigureItemDropping(bool pauseDropping, KeyCode newDropKey)
		{
			dropKey = newDropKey;
		}

		public bool CanDropItems()
		{
			if (itemDropper != null)
			{
				return itemDropper.IsConfigured();
			}
			return false;
		}

		public void SetItemDropper(ItemDropper dropper)
		{
			itemDropper = dropper;
		}

		public void SetConsumableSystem(ConsumableSystem system)
		{
			if (consumableSystem != null)
			{
				consumableSystem.OnItemConsumed.RemoveListener(OnItemConsumed);
				consumableSystem.OnConsumeFailed.RemoveListener(OnConsumeFailed);
			}
			consumableSystem = system;
			if (consumableSystem != null)
			{
				consumableSystem.OnItemConsumed.AddListener(OnItemConsumed);
				consumableSystem.OnConsumeFailed.AddListener(OnConsumeFailed);
			}
		}

		public void SetDebugMode(bool enabled)
		{
			enableDebugLogging = enabled;
			if (inventorySystem != null)
			{
				inventorySystem.SetDebugMode(enabled);
			}
			if (gridSlots == null)
			{
				return;
			}
			for (int i = 0; i < inventorySystem.GridWidth; i++)
			{
				for (int j = 0; j < inventorySystem.GridHeight; j++)
				{
					if (gridSlots[i, j] != null)
					{
						if (enabled)
						{
							gridSlots[i, j].SetDebugCoordinates($"{i},{j}");
						}
						else
						{
							gridSlots[i, j].SetDebugCoordinates("");
						}
					}
				}
			}
		}

		public bool TryAddItem(InventoryItem item)
		{
			if (inventorySystem == null)
			{
				return false;
			}
			return inventorySystem.AddItem(item);
		}

		[ContextMenu("Force Refresh UI")]
		public void ForceRefreshUI()
		{
			RefreshUI();
		}

		[ContextMenu("Force Initialize Grid")]
		public void ForceInitializeGrid()
		{
			InitializeGridUI();
			RefreshUI();
		}

		[ContextMenu("Test Grid Creation")]
		private void TestGridCreation()
		{
			if (!(gridContainer == null) && !(slotPrefab == null))
			{
				Object.Instantiate(slotPrefab, gridContainer).name = "TestSlot";
			}
		}

		[ContextMenu("Emergency Cleanup")]
		public void EmergencyCleanup()
		{
			if (inventoryPanel != null)
			{
				inventoryPanel.SetActive(value: false);
			}
			isInventoryOpen = false;
			Cursor.lockState = CursorLockMode.Locked;
			Cursor.visible = false;
			Time.timeScale = 1f;
			if (firstPersonController != null)
			{
				firstPersonController.enabled = true;
			}
			if (dualWieldingSystem != null)
			{
				dualWieldingSystem.enabled = true;
			}
			HideTooltip();
			CancelDrag();
		}

		[ContextMenu("Rebuild Grid")]
		public void RebuildGrid()
		{
			if (gridContainer != null)
			{
				for (int num = gridContainer.childCount - 1; num >= 0; num--)
				{
					Object.DestroyImmediate(gridContainer.GetChild(num).gameObject);
				}
			}
			InitializeGridUI();
			RefreshUI();
		}

		private void OnApplicationFocus(bool hasFocus)
		{
			if (hasFocus)
			{
				focusRegainedTime = Time.unscaledTime;
				if (isInventoryOpen)
				{
					Cursor.lockState = CursorLockMode.None;
					Cursor.visible = true;
					if (inventoryPanel != null)
					{
						inventoryPanel.SetActive(value: true);
					}
					if (pauseGameWhenOpen)
					{
						Time.timeScale = 0f;
					}
				}
			}
			else if (isInventoryOpen)
			{
				previousCursorLockState = CursorLockMode.Locked;
				previousCursorVisible = false;
			}
		}

		private void OnApplicationPause(bool pauseStatus)
		{
			if (!pauseStatus && isInventoryOpen && pauseGameWhenOpen)
			{
				Time.timeScale = 0f;
			}
		}

		private void OnValidate()
		{
			if (!Application.isPlaying)
			{
				if (inventorySystem == null)
				{
					inventorySystem = GetComponent<GridInventorySystem>();
				}
				if (consumableSystem == null)
				{
					consumableSystem = GetComponent<ConsumableSystem>();
				}
			}
		}
	}
}
