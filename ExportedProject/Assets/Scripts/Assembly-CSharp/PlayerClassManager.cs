using PixelCrushers.GridController;
using UnityEngine;
using UnityEngine.Events;

public class PlayerClassManager : MonoBehaviour
{
	[Header("Current Class")]
	[SerializeField]
	private PlayerClass selectedClass;

	[Header("Events")]
	public UnityEvent<PlayerClass> OnClassSelected;

	public UnityEvent<PlayerClass> OnClassApplied;

	public static PlayerClassManager Instance { get; private set; }

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
			Object.DontDestroyOnLoad(base.gameObject);
		}
		else
		{
			Object.Destroy(base.gameObject);
		}
	}

	public void SelectClass(PlayerClass playerClass)
	{
		if (!(playerClass == null))
		{
			selectedClass = playerClass;
			OnClassSelected?.Invoke(selectedClass);
		}
	}

	public PlayerClass GetSelectedClass()
	{
		return selectedClass;
	}

	public void ApplyClassToPlayer()
	{
		if (selectedClass == null || !selectedClass.ValidateStartingEquipment(out var _))
		{
			return;
		}
		PlayerStats playerStats = Object.FindAnyObjectByType<PlayerStats>();
		GridInventorySystem gridInventorySystem = Object.FindAnyObjectByType<GridInventorySystem>();
		if (!(playerStats == null) && !(gridInventorySystem == null))
		{
			ApplyStatModifiers(playerStats);
			ApplyInventoryConfiguration(gridInventorySystem);
			EquipStartingEquipment(gridInventorySystem);
			AddStartingInventoryItems(gridInventorySystem);
			RecordStartingItemsSeen();
			BuffDebuffSystem buffDebuffSystem = Object.FindAnyObjectByType<BuffDebuffSystem>();
			if (buffDebuffSystem != null)
			{
				buffDebuffSystem.ReinitializeBaseStats();
			}
			OnClassApplied?.Invoke(selectedClass);
		}
	}

	private void ApplyStatModifiers(PlayerStats playerStats)
	{
		int newMaxHealth = Mathf.RoundToInt((float)playerStats.MaxHealth * selectedClass.healthMultiplier) + selectedClass.bonusHealth;
		playerStats.SetMaxHealth(newMaxHealth);
		float maxHeat = playerStats.MaxHeat * selectedClass.heatCapacityMultiplier + selectedClass.bonusHeatCapacity;
		playerStats.SetMaxHeat(maxHeat);
		if (selectedClass.movementSpeedMultiplier != 1f)
		{
			playerStats.SetExternalMovementMultiplier(selectedClass.movementSpeedMultiplier);
		}
	}

	private void ApplyInventoryConfiguration(GridInventorySystem inventorySystem)
	{
		foreach (int unlockedRow in selectedClass.unlockedRows)
		{
			if (unlockedRow >= 0 && unlockedRow < 4)
			{
				inventorySystem.UnlockRow(unlockedRow);
			}
		}
	}

	private void EquipStartingEquipment(GridInventorySystem inventorySystem)
	{
		if (selectedClass.startingUpperArmor != null)
		{
			inventorySystem.EquipItem(selectedClass.startingUpperArmor, EquipmentSlotType.UpperArmor, skipStatModifiers: false, silent: true);
		}
		if (selectedClass.startingLowerArmor != null)
		{
			inventorySystem.EquipItem(selectedClass.startingLowerArmor, EquipmentSlotType.LowerArmor, skipStatModifiers: false, silent: true);
		}
		if (selectedClass.startingLeftWeapon != null)
		{
			inventorySystem.EquipItem(selectedClass.startingLeftWeapon, EquipmentSlotType.LeftWeapon, skipStatModifiers: false, silent: true);
		}
		if (selectedClass.startingRightWeapon != null)
		{
			inventorySystem.EquipItem(selectedClass.startingRightWeapon, EquipmentSlotType.RightWeapon, skipStatModifiers: false, silent: true);
		}
		if (selectedClass.startingTrinket1 != null)
		{
			inventorySystem.EquipItem(selectedClass.startingTrinket1, EquipmentSlotType.Trinket1, skipStatModifiers: false, silent: true);
		}
		if (selectedClass.startingTrinket2 != null)
		{
			inventorySystem.EquipItem(selectedClass.startingTrinket2, EquipmentSlotType.Trinket2, skipStatModifiers: false, silent: true);
		}
	}

	private void AddStartingInventoryItems(GridInventorySystem inventorySystem)
	{
		foreach (StartingInventoryItem startingInventoryItem in selectedClass.startingInventoryItems)
		{
			if (!(startingInventoryItem.item == null) && !inventorySystem.AddItem(startingInventoryItem.item, startingInventoryItem.gridPosition, silent: true) && startingInventoryItem.autoPlaceIfOccupied)
			{
				TryAddToFirstAvailableSlot(inventorySystem, startingInventoryItem.item);
			}
		}
	}

	private void RecordStartingItemsSeen()
	{
		if (selectedClass == null)
		{
			return;
		}
		if (selectedClass.startingUpperArmor != null)
		{
			ChestItemPool.RecordItemSeen(selectedClass.startingUpperArmor);
		}
		if (selectedClass.startingLowerArmor != null)
		{
			ChestItemPool.RecordItemSeen(selectedClass.startingLowerArmor);
		}
		if (selectedClass.startingLeftWeapon != null)
		{
			ChestItemPool.RecordItemSeen(selectedClass.startingLeftWeapon);
		}
		if (selectedClass.startingRightWeapon != null)
		{
			ChestItemPool.RecordItemSeen(selectedClass.startingRightWeapon);
		}
		if (selectedClass.startingTrinket1 != null)
		{
			ChestItemPool.RecordItemSeen(selectedClass.startingTrinket1);
		}
		if (selectedClass.startingTrinket2 != null)
		{
			ChestItemPool.RecordItemSeen(selectedClass.startingTrinket2);
		}
		foreach (StartingInventoryItem startingInventoryItem in selectedClass.startingInventoryItems)
		{
			if (startingInventoryItem.item != null)
			{
				ChestItemPool.RecordItemSeen(startingInventoryItem.item);
			}
		}
	}

	private bool TryAddToFirstAvailableSlot(GridInventorySystem inventorySystem, InventoryItem item)
	{
		for (int i = 0; i < 4; i++)
		{
			for (int j = 0; j < 8; j++)
			{
				Vector2Int position = new Vector2Int(j, i);
				if (inventorySystem.AddItem(item, position, silent: true))
				{
					return true;
				}
			}
		}
		return false;
	}

	public void ClearSelectedClass()
	{
		selectedClass = null;
	}
}
