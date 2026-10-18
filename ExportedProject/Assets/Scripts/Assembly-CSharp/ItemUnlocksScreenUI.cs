using System;
using System.Collections.Generic;
using System.Linq;
using PixelCrushers.GridController;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ItemUnlocksScreenUI : MonoBehaviour, IDynamicFirstSelectable
{
	public enum UnlockTab
	{
		Weapons = 0,
		Consumables = 1,
		Armor = 2,
		Classes = 3,
		Trinkets = 4
	}

	[Serializable]
	public class Tab
	{
		public UnlockTab tabType;

		public Button tabButton;

		public Selectable firstInContent;

		[Tooltip("Optional: a component implementing IDynamicFirstSelectable. If set, its FirstSelectable overrides firstInContent at runtime.")]
		public MonoBehaviour dynamicSource;
	}

	[Header("Screen")]
	[Tooltip("Panel to toggle on open/close. Leave null to toggle this GameObject itself.")]
	[SerializeField]
	private GameObject screenPanel;

	[Header("Tabs")]
	[SerializeField]
	private Tab[] tabs;

	[Header("Tab Visual Settings")]
	[SerializeField]
	private Color activeTabColor = Color.white;

	[SerializeField]
	private Color inactiveTabColor = new Color(0.5f, 0.5f, 0.5f, 1f);

	[Header("Grid")]
	[SerializeField]
	private Transform gridContainer;

	[SerializeField]
	private GameObject slotPrefab;

	[SerializeField]
	private GameObject classSlotPrefab;

	[Header("Progress")]
	[SerializeField]
	private TextMeshProUGUI progressText;

	[Header("Details Panel")]
	[SerializeField]
	private GameObject detailsPanel;

	[SerializeField]
	private TextMeshProUGUI detailsNameText;

	[SerializeField]
	private TextMeshProUGUI detailsDescriptionText;

	[SerializeField]
	private TextMeshProUGUI detailsStatsText;

	[SerializeField]
	private Image detailsIcon;

	[SerializeField]
	private TextMeshProUGUI unlockConditionLabel;

	[Header("Back Button")]
	[SerializeField]
	private Button backButton;

	private UnlockTab currentTab;

	private List<GameObject> spawnedSlots = new List<GameObject>();

	private GameObject PanelTarget
	{
		get
		{
			if (!(screenPanel != null))
			{
				return base.gameObject;
			}
			return screenPanel;
		}
	}

	public GameObject FirstSelected
	{
		get
		{
			int num = ActiveTabIndex();
			if (num < 0)
			{
				return null;
			}
			Selectable selectable = ResolveTabTarget(tabs[num]);
			if (selectable != null)
			{
				return selectable.gameObject;
			}
			if (tabs[num].tabButton != null)
			{
				return tabs[num].tabButton.gameObject;
			}
			return null;
		}
	}

	public Selectable FirstSelectable
	{
		get
		{
			for (int i = 0; i < spawnedSlots.Count; i++)
			{
				if (!(spawnedSlots[i] == null))
				{
					Selectable component = spawnedSlots[i].GetComponent<Selectable>();
					if (component != null)
					{
						return component;
					}
				}
			}
			return null;
		}
	}

	public void EnsureBuilt()
	{
		if (spawnedSlots.Count == 0)
		{
			PopulateGrid();
		}
	}

	private Selectable ResolveTabTarget(Tab tab)
	{
		if (tab == null)
		{
			return null;
		}
		if (tab.dynamicSource is IDynamicFirstSelectable dynamicFirstSelectable)
		{
			dynamicFirstSelectable.EnsureBuilt();
			Selectable firstSelectable = dynamicFirstSelectable.FirstSelectable;
			if (firstSelectable != null)
			{
				return firstSelectable;
			}
		}
		return tab.firstInContent;
	}

	private int GetGridColumns()
	{
		if (gridContainer == null)
		{
			if (spawnedSlots.Count <= 0)
			{
				return 1;
			}
			return spawnedSlots.Count;
		}
		GridLayoutGroup component = gridContainer.GetComponent<GridLayoutGroup>();
		if (component != null && component.constraint == GridLayoutGroup.Constraint.FixedColumnCount && component.constraintCount > 0)
		{
			return component.constraintCount;
		}
		if (spawnedSlots.Count <= 0)
		{
			return 1;
		}
		return spawnedSlots.Count;
	}

	private Selectable SlotAt(int index)
	{
		if (index < 0 || index >= spawnedSlots.Count)
		{
			return null;
		}
		GameObject gameObject = spawnedSlots[index];
		if (!(gameObject != null))
		{
			return null;
		}
		return gameObject.GetComponent<Selectable>();
	}

	private void WireSlotNavigation()
	{
		int count = spawnedSlots.Count;
		if (count == 0)
		{
			return;
		}
		int num = GetGridColumns();
		if (num <= 0)
		{
			num = 1;
		}
		int num2 = ActiveTabIndex();
		Selectable selectable = ((num2 >= 0) ? tabs[num2].tabButton : null);
		Selectable selectable2 = backButton;
		for (int i = 0; i < count; i++)
		{
			Selectable selectable3 = SlotAt(i);
			if (!(selectable3 == null))
			{
				Selectable selectOnLeft = SlotAt((i - 1 + count) % count);
				Selectable selectOnRight = SlotAt((i + 1) % count);
				Selectable selectOnUp = ((i - num >= 0) ? SlotAt(i - num) : selectable);
				Selectable selectOnDown = ((i + num < count) ? SlotAt(i + num) : selectable2);
				Navigation navigation = selectable3.navigation;
				navigation.mode = Navigation.Mode.Explicit;
				navigation.selectOnLeft = selectOnLeft;
				navigation.selectOnRight = selectOnRight;
				navigation.selectOnUp = selectOnUp;
				navigation.selectOnDown = selectOnDown;
				selectable3.navigation = navigation;
			}
		}
	}

	private void OnEnable()
	{
		if (tabs != null)
		{
			Tab[] array = tabs;
			foreach (Tab tab in array)
			{
				if (tab != null && !(tab.tabButton == null))
				{
					UnlockTab captured = tab.tabType;
					tab.tabButton.onClick.AddListener(() =>
					{
						SwitchTab(captured);
					});
				}
			}
		}
		backButton?.onClick.AddListener(CloseScreen);
	}

	private void OnDisable()
	{
		if (tabs != null)
		{
			Tab[] array = tabs;
			foreach (Tab tab in array)
			{
				if (tab != null && !(tab.tabButton == null))
				{
					tab.tabButton.onClick.RemoveAllListeners();
				}
			}
		}
		backButton?.onClick.RemoveAllListeners();
	}

	public void OpenScreen()
	{
		PanelTarget.SetActive(value: true);
		currentTab = UnlockTab.Weapons;
		HideDetails();
		SwitchTab(currentTab);
		if (FirstSelected != null && EventSystem.current != null)
		{
			EventSystem.current.SetSelectedGameObject(FirstSelected);
		}
	}

	public void CloseScreen()
	{
		PanelTarget.SetActive(value: false);
	}

	private int ActiveTabIndex()
	{
		if (tabs == null)
		{
			return -1;
		}
		for (int i = 0; i < tabs.Length; i++)
		{
			if (tabs[i] != null && tabs[i].tabType == currentTab)
			{
				return i;
			}
		}
		return -1;
	}

	private void SwitchTab(UnlockTab tab)
	{
		currentTab = tab;
		int num = ActiveTabIndex();
		if (num < 0)
		{
			return;
		}
		UpdateTabVisuals();
		HideDetails();
		PopulateGrid();
		WireSlotNavigation();
		Selectable selectOnDown = ResolveTabTarget(tabs[num]);
		for (int i = 0; i < tabs.Length; i++)
		{
			Button button = tabs[i]?.tabButton;
			if (!(button == null))
			{
				Navigation navigation = button.navigation;
				navigation.mode = Navigation.Mode.Explicit;
				navigation.selectOnDown = selectOnDown;
				navigation.selectOnLeft = ((i > 0) ? tabs[i - 1].tabButton : null);
				navigation.selectOnRight = ((i < tabs.Length - 1) ? tabs[i + 1].tabButton : null);
				button.navigation = navigation;
			}
		}
	}

	private void UpdateTabVisuals()
	{
		if (tabs == null)
		{
			return;
		}
		Tab[] array = tabs;
		foreach (Tab tab in array)
		{
			if (tab != null)
			{
				SetTabColor(tab.tabButton, tab.tabType == currentTab);
			}
		}
	}

	private void SetTabColor(Button tab, bool isActive)
	{
		if (!(tab == null))
		{
			TextMeshProUGUI componentInChildren = tab.GetComponentInChildren<TextMeshProUGUI>();
			if (componentInChildren != null)
			{
				componentInChildren.color = (isActive ? activeTabColor : inactiveTabColor);
			}
		}
	}

	private void PopulateGrid()
	{
		foreach (GameObject spawnedSlot in spawnedSlots)
		{
			UnityEngine.Object.Destroy(spawnedSlot);
		}
		spawnedSlots.Clear();
		if (!(ItemUnlockManager.Instance == null) && !(gridContainer == null))
		{
			if (currentTab == UnlockTab.Classes)
			{
				PopulateClassGrid();
			}
			else
			{
				PopulateItemGrid();
			}
		}
	}

	private void PopulateItemGrid()
	{
		if (slotPrefab == null)
		{
			return;
		}
		ItemType type = currentTab switch
		{
			UnlockTab.Consumables => ItemType.Consumable, 
			UnlockTab.Armor => ItemType.Armor, 
			UnlockTab.Trinkets => ItemType.Trinket, 
			_ => ItemType.Weapon, 
		};
		List<(InventoryItem, bool)> itemsByType = ItemUnlockManager.Instance.GetItemsByType(type);
		itemsByType.Sort(((InventoryItem item, bool isUnlocked) a, (InventoryItem item, bool isUnlocked) b) => (a.isUnlocked != b.isUnlocked) ? b.isUnlocked.CompareTo(a.isUnlocked) : string.Compare(a.item.itemName, b.item.itemName, StringComparison.OrdinalIgnoreCase));
		int num = itemsByType.Count(((InventoryItem item, bool isUnlocked) i) => i.isUnlocked);
		if (progressText != null)
		{
			progressText.text = $"{num}/{itemsByType.Count} Unlocked";
		}
		foreach (var item3 in itemsByType)
		{
			InventoryItem item = item3.Item1;
			bool item2 = item3.Item2;
			GameObject gameObject = UnityEngine.Object.Instantiate(slotPrefab, gridContainer);
			ItemUnlockSlotUI component = gameObject.GetComponent<ItemUnlockSlotUI>();
			if (component != null)
			{
				component.Setup(item, item2, this);
			}
			spawnedSlots.Add(gameObject);
		}
	}

	private void PopulateClassGrid()
	{
		GameObject gameObject = ((classSlotPrefab != null) ? classSlotPrefab : slotPrefab);
		if (gameObject == null)
		{
			return;
		}
		List<(PlayerClass, bool)> allClasses = ItemUnlockManager.Instance.GetAllClasses();
		allClasses.Sort(((PlayerClass playerClass, bool isUnlocked) a, (PlayerClass playerClass, bool isUnlocked) b) => (a.isUnlocked != b.isUnlocked) ? b.isUnlocked.CompareTo(a.isUnlocked) : string.Compare(a.playerClass.className, b.playerClass.className, StringComparison.OrdinalIgnoreCase));
		int num = allClasses.Count(((PlayerClass playerClass, bool isUnlocked) c) => c.isUnlocked);
		if (progressText != null)
		{
			progressText.text = $"{num}/{allClasses.Count} Unlocked";
		}
		foreach (var item3 in allClasses)
		{
			PlayerClass item = item3.Item1;
			bool item2 = item3.Item2;
			GameObject gameObject2 = UnityEngine.Object.Instantiate(gameObject, gridContainer);
			ClassUnlockSlotUI component = gameObject2.GetComponent<ClassUnlockSlotUI>();
			if (component != null)
			{
				component.Setup(item, item2, this);
			}
			spawnedSlots.Add(gameObject2);
		}
	}

	public void ShowItemDetails(InventoryItem item, bool isUnlocked)
	{
		if (detailsPanel != null)
		{
			detailsPanel.SetActive(value: true);
		}
		if (isUnlocked)
		{
			if (detailsNameText != null)
			{
				detailsNameText.text = item.GetDisplayName();
				detailsNameText.color = Color.white;
			}
			if (detailsDescriptionText != null)
			{
				detailsDescriptionText.gameObject.SetActive(value: true);
				detailsDescriptionText.text = item.itemDescription;
			}
			if (detailsStatsText != null)
			{
				string tooltipDescription = item.GetTooltipDescription();
				detailsStatsText.gameObject.SetActive(!string.IsNullOrEmpty(tooltipDescription));
				detailsStatsText.text = tooltipDescription;
			}
			if (detailsIcon != null && item.itemIcon != null)
			{
				detailsIcon.sprite = item.itemIcon;
				detailsIcon.color = Color.white;
			}
			if (unlockConditionLabel != null)
			{
				unlockConditionLabel.gameObject.SetActive(value: false);
			}
		}
		else
		{
			if (detailsNameText != null)
			{
				detailsNameText.text = "???";
				detailsNameText.color = Color.gray;
			}
			if (detailsDescriptionText != null)
			{
				detailsDescriptionText.gameObject.SetActive(value: false);
			}
			if (detailsStatsText != null)
			{
				detailsStatsText.gameObject.SetActive(value: false);
			}
			if (detailsIcon != null && item.itemIcon != null)
			{
				detailsIcon.sprite = item.itemIcon;
				detailsIcon.color = Color.black;
			}
			if (unlockConditionLabel != null)
			{
				unlockConditionLabel.gameObject.SetActive(value: true);
				string text = ((!string.IsNullOrEmpty(item.unlockConditionText)) ? item.unlockConditionText : "Unlock conditions unknown.");
				unlockConditionLabel.text = text;
			}
		}
	}

	public void ShowClassDetails(PlayerClass playerClass, bool isUnlocked)
	{
		if (detailsPanel != null)
		{
			detailsPanel.SetActive(value: true);
		}
		if (isUnlocked)
		{
			if (detailsNameText != null)
			{
				detailsNameText.text = playerClass.className;
				detailsNameText.color = Color.white;
			}
			if (detailsDescriptionText != null)
			{
				detailsDescriptionText.gameObject.SetActive(value: true);
				detailsDescriptionText.text = playerClass.classDescription;
			}
			if (detailsStatsText != null)
			{
				string classStatsText = GetClassStatsText(playerClass);
				detailsStatsText.gameObject.SetActive(!string.IsNullOrEmpty(classStatsText));
				detailsStatsText.text = classStatsText;
			}
			if (detailsIcon != null && playerClass.classIcon != null)
			{
				detailsIcon.sprite = playerClass.classIcon;
				detailsIcon.color = Color.white;
			}
			if (unlockConditionLabel != null)
			{
				unlockConditionLabel.gameObject.SetActive(value: false);
			}
		}
		else
		{
			if (detailsNameText != null)
			{
				detailsNameText.text = "???";
				detailsNameText.color = Color.gray;
			}
			if (detailsDescriptionText != null)
			{
				detailsDescriptionText.gameObject.SetActive(value: false);
			}
			if (detailsStatsText != null)
			{
				detailsStatsText.gameObject.SetActive(value: false);
			}
			if (detailsIcon != null && playerClass.classIcon != null)
			{
				detailsIcon.sprite = playerClass.classIcon;
				detailsIcon.color = Color.black;
			}
			if (unlockConditionLabel != null)
			{
				unlockConditionLabel.gameObject.SetActive(value: true);
				string text = ((!string.IsNullOrEmpty(playerClass.unlockConditionText)) ? playerClass.unlockConditionText : "Unlock conditions unknown.");
				unlockConditionLabel.text = text;
			}
		}
	}

	private string GetClassStatsText(PlayerClass playerClass)
	{
		string text = "<b>Starting Stats:</b>\n\n";
		if (playerClass.healthMultiplier != 1f || playerClass.bonusHealth != 0)
		{
			string text2 = "";
			if (playerClass.healthMultiplier != 1f)
			{
				text2 += $"{playerClass.healthMultiplier:P0}";
			}
			if (playerClass.bonusHealth != 0)
			{
				text2 += string.Format(" {0}{1}", (playerClass.bonusHealth > 0) ? "+" : "", playerClass.bonusHealth);
			}
			text = text + "Health: " + text2 + "\n";
		}
		if (playerClass.heatCapacityMultiplier != 1f || playerClass.bonusHeatCapacity != 0f)
		{
			string text3 = "";
			if (playerClass.heatCapacityMultiplier != 1f)
			{
				text3 += $"{playerClass.heatCapacityMultiplier:P0}";
			}
			if (playerClass.bonusHeatCapacity != 0f)
			{
				text3 += string.Format(" {0}{1:F0}", (playerClass.bonusHeatCapacity > 0f) ? "+" : "", playerClass.bonusHeatCapacity);
			}
			text = text + "Heat Capacity: " + text3 + "\n";
		}
		if (playerClass.movementSpeedMultiplier != 1f)
		{
			text += $"Movement Speed: {playerClass.movementSpeedMultiplier:P0}\n";
		}
		return text + $"\nInventory Rows: {playerClass.unlockedRows.Count} unlocked\n";
	}

	private void HideDetails()
	{
		if (detailsPanel != null)
		{
			detailsPanel.SetActive(value: false);
		}
	}
}
