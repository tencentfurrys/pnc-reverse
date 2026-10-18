using System.Collections.Generic;
using PixelCrushers.GridController;
using UnityEngine;

[CreateAssetMenu(fileName = "New Player Class", menuName = "Player/Class")]
public class PlayerClass : ScriptableObject
{
	[Header("Class Information")]
	public string className = "Warrior";

	[TextArea(3, 5)]
	public string classDescription = "A strong melee fighter with high defense.";

	public Sprite classIcon;

	[Header("Unlock Settings")]
	public bool startsLocked;

	[TextArea(2, 3)]
	public string unlockConditionText = "";

	[Header("Starting Stats Modifiers")]
	[Tooltip("Multiplier for base health (1.0 = normal)")]
	public float healthMultiplier = 1f;

	[Tooltip("Bonus health added to base")]
	public int bonusHealth;

	[Tooltip("Multiplier for base heat capacity (1.0 = normal)")]
	public float heatCapacityMultiplier = 1f;

	[Tooltip("Bonus heat capacity added to base")]
	public float bonusHeatCapacity;

	[Tooltip("Multiplier for movement speed (1.0 = normal)")]
	public float movementSpeedMultiplier = 1f;

	[Header("Starting Equipment")]
	[Tooltip("Item equipped in upper armor slot")]
	public InventoryItem startingUpperArmor;

	[Tooltip("Item equipped in lower armor slot")]
	public InventoryItem startingLowerArmor;

	[Tooltip("Item equipped in left weapon slot")]
	public InventoryItem startingLeftWeapon;

	[Tooltip("Item equipped in right weapon slot")]
	public InventoryItem startingRightWeapon;

	[Tooltip("Item equipped in first trinket slot")]
	public InventoryItem startingTrinket1;

	[Tooltip("Item equipped in second trinket slot")]
	public InventoryItem startingTrinket2;

	[Header("Starting Inventory Items")]
	[Tooltip("Items that will be placed in the inventory grid at game start")]
	public List<StartingInventoryItem> startingInventoryItems = new List<StartingInventoryItem>();

	[Header("Inventory Configuration")]
	[Tooltip("Which rows are unlocked at start (0-3)")]
	public List<int> unlockedRows = new List<int> { 0, 1 };

	public List<InventoryItem> GetStartingEquipment()
	{
		List<InventoryItem> list = new List<InventoryItem>();
		if (startingUpperArmor != null)
		{
			list.Add(startingUpperArmor);
		}
		if (startingLowerArmor != null)
		{
			list.Add(startingLowerArmor);
		}
		if (startingLeftWeapon != null)
		{
			list.Add(startingLeftWeapon);
		}
		if (startingRightWeapon != null)
		{
			list.Add(startingRightWeapon);
		}
		if (startingTrinket1 != null)
		{
			list.Add(startingTrinket1);
		}
		if (startingTrinket2 != null)
		{
			list.Add(startingTrinket2);
		}
		return list;
	}

	public bool ValidateStartingEquipment(out string errorMessage)
	{
		errorMessage = "";
		if (startingUpperArmor != null && !startingUpperArmor.CanEquipInSlot(EquipmentSlotType.UpperArmor))
		{
			errorMessage = startingUpperArmor.itemName + " cannot be equipped in upper armor slot";
			return false;
		}
		if (startingLowerArmor != null && !startingLowerArmor.CanEquipInSlot(EquipmentSlotType.LowerArmor))
		{
			errorMessage = startingLowerArmor.itemName + " cannot be equipped in lower armor slot";
			return false;
		}
		if (startingLeftWeapon != null && !startingLeftWeapon.CanEquipInSlot(EquipmentSlotType.LeftWeapon))
		{
			errorMessage = startingLeftWeapon.itemName + " cannot be equipped in left weapon slot";
			return false;
		}
		if (startingRightWeapon != null && !startingRightWeapon.CanEquipInSlot(EquipmentSlotType.RightWeapon))
		{
			errorMessage = startingRightWeapon.itemName + " cannot be equipped in right weapon slot";
			return false;
		}
		if (startingTrinket1 != null && !startingTrinket1.CanEquipInSlot(EquipmentSlotType.Trinket1))
		{
			errorMessage = startingTrinket1.itemName + " cannot be equipped in trinket slot 1";
			return false;
		}
		if (startingTrinket2 != null && !startingTrinket2.CanEquipInSlot(EquipmentSlotType.Trinket2))
		{
			errorMessage = startingTrinket2.itemName + " cannot be equipped in trinket slot 2";
			return false;
		}
		return true;
	}
}
