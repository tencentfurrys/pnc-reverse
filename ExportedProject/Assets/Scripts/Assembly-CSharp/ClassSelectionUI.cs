using System.Collections.Generic;
using PixelCrushers.GridController;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ClassSelectionUI : MonoBehaviour
{
	protected internal class EquipmentSlotHover : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
	{
		public InventoryItem item;

		public ClassSelectionUI selectionUI;

		public void OnPointerEnter(PointerEventData eventData)
		{
			if (item != null && selectionUI != null)
			{
				selectionUI.ShowEquipmentTooltip(item);
			}
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			if (selectionUI != null)
			{
				selectionUI.HideEquipmentTooltip();
			}
		}
	}

	[Header("Class Configuration")]
	[SerializeField]
	private List<PlayerClass> availableClasses = new List<PlayerClass>();

	[SerializeField]
	private int defaultClassIndex;

	[Header("Navigation Buttons")]
	[SerializeField]
	private Button leftArrowButton;

	[SerializeField]
	private Button rightArrowButton;

	[Header("UI References")]
	[SerializeField]
	private GameObject classSelectionPanel;

	[Header("Class Details Panel")]
	[SerializeField]
	private GameObject classDetailsPanel;

	[SerializeField]
	private TextMeshProUGUI classNameText;

	[SerializeField]
	private TextMeshProUGUI classDescriptionText;

	[SerializeField]
	private Image classIconImage;

	[SerializeField]
	private TextMeshProUGUI classStatsText;

	[SerializeField]
	private TextMeshProUGUI classIndexText;

	[Header("Locked Class Overlay")]
	[SerializeField]
	private GameObject lockedOverlay;

	[SerializeField]
	private TextMeshProUGUI lockedReasonText;

	[Header("Health Bar Preview")]
	[SerializeField]
	private Image healthBarFill;

	[SerializeField]
	private TextMeshProUGUI healthValueText;

	[Header("Heat Bar Preview")]
	[SerializeField]
	private Image heatBarFill;

	[SerializeField]
	private TextMeshProUGUI heatValueText;

	[Header("Equipment Preview Slots")]
	[SerializeField]
	private Image upperArmorIcon;

	[SerializeField]
	private Image lowerArmorIcon;

	[SerializeField]
	private Image leftWeaponIcon;

	[SerializeField]
	private Image rightWeaponIcon;

	[Header("Equipment Tooltip")]
	[SerializeField]
	private GameObject equipmentTooltipPanel;

	[SerializeField]
	private TextMeshProUGUI equipmentTooltipTitle;

	[SerializeField]
	private TextMeshProUGUI equipmentTooltipDescription;

	[SerializeField]
	private TextMeshProUGUI equipmentTooltipStats;

	[Header("Confirmation")]
	[SerializeField]
	private Button confirmButton;

	[SerializeField]
	private Button backButton;

	[Header("Scene Loading")]
	[SerializeField]
	private string gameSceneName = "Floor1";

	[SerializeField]
	private GameObject loadingScreenPrefab;

	[Header("Controller Navigation")]
	[SerializeField]
	private GameObject firstSelected;

	private const int BaseMaxHealth = 100;

	private const float BaseMaxHeat = 100f;

	private int currentClassIndex;

	private float maxEffectiveHealth;

	private float maxEffectiveHeat;

	private void Start()
	{
		if (PlayerClassManager.Instance == null)
		{
			new GameObject("PlayerClassManager").AddComponent<PlayerClassManager>();
		}
		if (availableClasses != null && availableClasses.Count != 0)
		{
			if (leftArrowButton != null)
			{
				leftArrowButton.onClick.AddListener(OnLeftArrowClicked);
			}
			if (rightArrowButton != null)
			{
				rightArrowButton.onClick.AddListener(OnRightArrowClicked);
			}
			if (confirmButton != null)
			{
				confirmButton.onClick.AddListener(OnConfirmClassSelection);
			}
			if (backButton != null)
			{
				backButton.onClick.AddListener(OnBackButton);
			}
			CalculateBarNormalizationValues();
			SetupEquipmentSlotHover(upperArmorIcon);
			SetupEquipmentSlotHover(lowerArmorIcon);
			SetupEquipmentSlotHover(leftWeaponIcon);
			SetupEquipmentSlotHover(rightWeaponIcon);
			HideEquipmentTooltip();
			currentClassIndex = Mathf.Clamp(defaultClassIndex, 0, availableClasses.Count - 1);
			DisplayCurrentClass();
			FocusFirst();
		}
	}

	public void FocusFirst()
	{
		if (firstSelected != null && EventSystem.current != null)
		{
			EventSystem.current.SetSelectedGameObject(firstSelected);
		}
	}

	private void OnLeftArrowClicked()
	{
		currentClassIndex--;
		if (currentClassIndex < 0)
		{
			currentClassIndex = availableClasses.Count - 1;
		}
		DisplayCurrentClass();
	}

	private void OnRightArrowClicked()
	{
		currentClassIndex++;
		if (currentClassIndex >= availableClasses.Count)
		{
			currentClassIndex = 0;
		}
		DisplayCurrentClass();
	}

	private void DisplayCurrentClass()
	{
		if (availableClasses == null || availableClasses.Count == 0)
		{
			return;
		}
		PlayerClass playerClass = availableClasses[currentClassIndex];
		if (playerClass == null)
		{
			return;
		}
		bool flag = IsClassAvailable(playerClass);
		DisplayClassDetails(playerClass);
		UpdateNavigationButtons();
		UpdateClassIndexDisplay();
		if (lockedOverlay != null)
		{
			lockedOverlay.SetActive(!flag);
		}
		if (lockedReasonText != null)
		{
			lockedReasonText.gameObject.SetActive(!flag);
			if (!flag)
			{
				lockedReasonText.text = ((!string.IsNullOrEmpty(playerClass.unlockConditionText)) ? playerClass.unlockConditionText : "This class is locked.");
			}
		}
		if (classIconImage != null)
		{
			classIconImage.color = (flag ? Color.white : new Color(0.3f, 0.3f, 0.3f, 1f));
		}
		if (confirmButton != null)
		{
			confirmButton.interactable = flag;
		}
	}

	private void UpdateNavigationButtons()
	{
		bool interactable = availableClasses.Count > 1;
		if (leftArrowButton != null)
		{
			leftArrowButton.interactable = interactable;
		}
		if (rightArrowButton != null)
		{
			rightArrowButton.interactable = interactable;
		}
	}

	private void UpdateClassIndexDisplay()
	{
		if (classIndexText != null)
		{
			classIndexText.text = $"{currentClassIndex + 1}/{availableClasses.Count}";
		}
	}

	private void DisplayClassDetails(PlayerClass playerClass)
	{
		if (classDetailsPanel != null)
		{
			classDetailsPanel.SetActive(value: true);
		}
		if (classNameText != null)
		{
			classNameText.text = playerClass.className;
		}
		if (classDescriptionText != null)
		{
			classDescriptionText.text = playerClass.classDescription;
		}
		if (classIconImage != null && playerClass.classIcon != null)
		{
			classIconImage.sprite = playerClass.classIcon;
		}
		if (classStatsText != null)
		{
			classStatsText.text = GetClassStatsText(playerClass);
		}
		int num = Mathf.RoundToInt(100f * playerClass.healthMultiplier) + playerClass.bonusHealth + GetArmorHealthBonus(playerClass);
		if (healthBarFill != null)
		{
			healthBarFill.fillAmount = ((maxEffectiveHealth > 0f) ? ((float)num / maxEffectiveHealth) : 1f);
		}
		if (healthValueText != null)
		{
			healthValueText.text = num.ToString();
		}
		float num2 = 100f * playerClass.heatCapacityMultiplier + playerClass.bonusHeatCapacity + GetArmorHeatBonus(playerClass);
		if (heatBarFill != null)
		{
			heatBarFill.fillAmount = ((maxEffectiveHeat > 0f) ? (num2 / maxEffectiveHeat) : 1f);
		}
		if (heatValueText != null)
		{
			heatValueText.text = Mathf.RoundToInt(num2).ToString();
		}
		UpdateEquipmentSlot(upperArmorIcon, playerClass.startingUpperArmor);
		UpdateEquipmentSlot(lowerArmorIcon, playerClass.startingLowerArmor);
		UpdateEquipmentSlot(leftWeaponIcon, playerClass.startingLeftWeapon);
		UpdateEquipmentSlot(rightWeaponIcon, playerClass.startingRightWeapon);
		HideEquipmentTooltip();
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

	private void OnConfirmClassSelection()
	{
		if (availableClasses == null || availableClasses.Count == 0)
		{
			return;
		}
		PlayerClass playerClass = availableClasses[currentClassIndex];
		if (!(playerClass == null) && IsClassAvailable(playerClass))
		{
			if (PlayerClassManager.Instance != null)
			{
				PlayerClassManager.Instance.SelectClass(playerClass);
			}
			LoadGameScene();
		}
	}

	private void OnBackButton()
	{
		if (classSelectionPanel != null)
		{
			classSelectionPanel.SetActive(value: false);
		}
	}

	private void LoadGameScene()
	{
		if (loadingScreenPrefab != null)
		{
			LoadingScreen component = Object.Instantiate(loadingScreenPrefab).GetComponent<LoadingScreen>();
			if (component != null)
			{
				component.LoadSceneWithLoadingScreen(gameSceneName);
			}
			else
			{
				SceneManager.LoadScene(gameSceneName);
			}
		}
		else
		{
			SceneManager.LoadScene(gameSceneName);
		}
	}

	public void OpenClassSelection()
	{
		LoadingScreen.DestroyInstance();
		if (classSelectionPanel != null)
		{
			classSelectionPanel.SetActive(value: true);
		}
		CalculateBarNormalizationValues();
		currentClassIndex = Mathf.Clamp(defaultClassIndex, 0, availableClasses.Count - 1);
		DisplayCurrentClass();
	}

	public void CloseClassSelection()
	{
		if (classSelectionPanel != null)
		{
			classSelectionPanel.SetActive(value: false);
		}
	}

	public void QuickStartWithDefaultClass()
	{
		if (availableClasses != null && availableClasses.Count > 0 && PlayerClassManager.Instance != null)
		{
			int index = Mathf.Clamp(defaultClassIndex, 0, availableClasses.Count - 1);
			PlayerClass playerClass = availableClasses[index];
			if (playerClass == null || !IsClassAvailable(playerClass))
			{
				playerClass = null;
				for (int i = 0; i < availableClasses.Count; i++)
				{
					if (availableClasses[i] != null && IsClassAvailable(availableClasses[i]))
					{
						playerClass = availableClasses[i];
						break;
					}
				}
			}
			if (!(playerClass != null))
			{
				return;
			}
			PlayerClassManager.Instance.SelectClass(playerClass);
		}
		LoadGameScene();
	}

	private bool IsClassAvailable(PlayerClass playerClass)
	{
		if (playerClass == null)
		{
			return false;
		}
		if (ItemUnlockManager.Instance == null)
		{
			return true;
		}
		return ItemUnlockManager.Instance.IsClassUnlocked(playerClass);
	}

	public PlayerClass GetCurrentClass()
	{
		if (availableClasses != null && currentClassIndex >= 0 && currentClassIndex < availableClasses.Count)
		{
			return availableClasses[currentClassIndex];
		}
		return null;
	}

	public void SetClassIndex(int index)
	{
		if (availableClasses != null && availableClasses.Count != 0)
		{
			currentClassIndex = Mathf.Clamp(index, 0, availableClasses.Count - 1);
			DisplayCurrentClass();
		}
	}

	private void CalculateBarNormalizationValues()
	{
		maxEffectiveHealth = 0f;
		maxEffectiveHeat = 0f;
		if (availableClasses == null)
		{
			return;
		}
		foreach (PlayerClass availableClass in availableClasses)
		{
			if (!(availableClass == null))
			{
				float num = Mathf.RoundToInt(100f * availableClass.healthMultiplier) + availableClass.bonusHealth + GetArmorHealthBonus(availableClass);
				float num2 = 100f * availableClass.heatCapacityMultiplier + availableClass.bonusHeatCapacity + GetArmorHeatBonus(availableClass);
				if (num > maxEffectiveHealth)
				{
					maxEffectiveHealth = num;
				}
				if (num2 > maxEffectiveHeat)
				{
					maxEffectiveHeat = num2;
				}
			}
		}
	}

	private int GetArmorHealthBonus(PlayerClass playerClass)
	{
		int num = 0;
		if (playerClass.startingUpperArmor?.armorData != null)
		{
			num += playerClass.startingUpperArmor.armorData.healthBonus;
		}
		if (playerClass.startingLowerArmor?.armorData != null)
		{
			num += playerClass.startingLowerArmor.armorData.healthBonus;
		}
		return num;
	}

	private float GetArmorHeatBonus(PlayerClass playerClass)
	{
		float num = 0f;
		if (playerClass.startingUpperArmor?.armorData != null)
		{
			num += playerClass.startingUpperArmor.armorData.heatCapacityBonus;
		}
		if (playerClass.startingLowerArmor?.armorData != null)
		{
			num += playerClass.startingLowerArmor.armorData.heatCapacityBonus;
		}
		return num;
	}

	private void UpdateEquipmentSlot(Image iconImage, InventoryItem item)
	{
		if (!(iconImage == null))
		{
			if (item != null && item.itemIcon != null)
			{
				iconImage.sprite = item.itemIcon;
				iconImage.color = Color.white;
			}
			else
			{
				iconImage.sprite = null;
				iconImage.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
			}
			EquipmentSlotHover component = iconImage.GetComponent<EquipmentSlotHover>();
			if (component != null)
			{
				component.item = item;
			}
		}
	}

	private void SetupEquipmentSlotHover(Image iconImage)
	{
		if (!(iconImage == null))
		{
			EquipmentSlotHover equipmentSlotHover = iconImage.gameObject.AddComponent<EquipmentSlotHover>();
			equipmentSlotHover.item = null;
			equipmentSlotHover.selectionUI = this;
		}
	}

	public void ShowEquipmentTooltip(InventoryItem item)
	{
		if (!(equipmentTooltipPanel == null) && !(item == null))
		{
			if (equipmentTooltipTitle != null)
			{
				equipmentTooltipTitle.text = item.GetDisplayName();
				equipmentTooltipTitle.color = item.GetRarityColor();
			}
			if (equipmentTooltipDescription != null)
			{
				equipmentTooltipDescription.text = item.itemDescription;
			}
			if (equipmentTooltipStats != null)
			{
				equipmentTooltipStats.text = item.GetTooltipDescription();
			}
			equipmentTooltipPanel.SetActive(value: true);
		}
	}

	public void HideEquipmentTooltip()
	{
		if (equipmentTooltipPanel != null)
		{
			equipmentTooltipPanel.SetActive(value: false);
		}
	}
}
