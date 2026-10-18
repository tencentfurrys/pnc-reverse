using System;
using System.Collections.Generic;
using PixelCrushers.GridController;
using UnityEngine;

public class PlayerStateSaver : MonoBehaviour
{
	[Serializable]
	public struct InventorySlotData
	{
		public string itemName;

		public Vector2Int gridPosition;
	}

	[Serializable]
	public struct EquipmentData
	{
		public string upperArmorName;

		public string lowerArmorName;

		public string leftWeaponName;

		public string rightWeaponName;

		public string trinket1Name;

		public string trinket2Name;
	}

	[Serializable]
	public struct PlayerSaveData
	{
		public PlayerStats.PlayerStatsData statsData;

		public EquipmentData equipmentData;

		public List<InventorySlotData> inventoryItems;

		public bool[] unlockedRows;
	}

	private PlayerSaveData? savedData;

	private Dictionary<string, InventoryItem> itemLookupCache;

	public static PlayerStateSaver Instance { get; private set; }

	public bool HasSavedState => savedData.HasValue;

	public bool IsFirstFloor => !HasSavedState;

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
			UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
		}
		else
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}

	public void SavePlayerState()
	{
		PlayerStats playerStats = UnityEngine.Object.FindAnyObjectByType<PlayerStats>();
		GridInventorySystem gridInventorySystem = UnityEngine.Object.FindAnyObjectByType<GridInventorySystem>();
		if (playerStats == null || gridInventorySystem == null)
		{
			return;
		}
		PlayerSaveData value = new PlayerSaveData
		{
			statsData = playerStats.GetSaveData()
		};
		List<(InventoryItem item, Vector2Int position)> allItemsWithPositions = gridInventorySystem.GetAllItemsWithPositions();
		value.inventoryItems = new List<InventorySlotData>();
		foreach (var (inventoryItem, gridPosition) in allItemsWithPositions)
		{
			if (inventoryItem != null)
			{
				value.inventoryItems.Add(new InventorySlotData
				{
					itemName = inventoryItem.itemName,
					gridPosition = gridPosition
				});
			}
		}
		value.unlockedRows = gridInventorySystem.GetUnlockedRowsState();
		value.equipmentData = new EquipmentData
		{
			upperArmorName = (gridInventorySystem.UpperArmor?.itemName ?? ""),
			lowerArmorName = (gridInventorySystem.LowerArmor?.itemName ?? ""),
			leftWeaponName = (gridInventorySystem.LeftWeapon?.itemName ?? ""),
			rightWeaponName = (gridInventorySystem.RightWeapon?.itemName ?? ""),
			trinket1Name = (gridInventorySystem.Trinket1?.itemName ?? ""),
			trinket2Name = (gridInventorySystem.Trinket2?.itemName ?? "")
		};
		savedData = value;
	}

	public void LoadPlayerState()
	{
		if (!HasSavedState)
		{
			return;
		}
		PlayerStats playerStats = UnityEngine.Object.FindAnyObjectByType<PlayerStats>();
		GridInventorySystem gridInventorySystem = UnityEngine.Object.FindAnyObjectByType<GridInventorySystem>();
		if (playerStats == null || gridInventorySystem == null)
		{
			return;
		}
		PlayerSaveData value = savedData.Value;
		playerStats.LoadSaveData(value.statsData);
		BuildItemLookupCache();
		List<(InventoryItem, Vector2Int)> list = new List<(InventoryItem, Vector2Int)>();
		foreach (InventorySlotData inventoryItem2 in value.inventoryItems)
		{
			InventoryItem inventoryItem = FindItemByName(inventoryItem2.itemName);
			if (inventoryItem != null)
			{
				list.Add((inventoryItem, inventoryItem2.gridPosition));
			}
		}
		gridInventorySystem.LoadFromSaveData(list, value.unlockedRows);
		InventoryItem upper = FindItemByName(value.equipmentData.upperArmorName);
		InventoryItem lower = FindItemByName(value.equipmentData.lowerArmorName);
		InventoryItem left = FindItemByName(value.equipmentData.leftWeaponName);
		InventoryItem right = FindItemByName(value.equipmentData.rightWeaponName);
		InventoryItem trinket = FindItemByName(value.equipmentData.trinket1Name);
		InventoryItem trinket2 = FindItemByName(value.equipmentData.trinket2Name);
		gridInventorySystem.LoadEquipmentFromSaveData(upper, lower, left, right, skipStatModifiers: true, trinket, trinket2);
		BuffDebuffSystem buffDebuffSystem = UnityEngine.Object.FindAnyObjectByType<BuffDebuffSystem>();
		if (buffDebuffSystem != null)
		{
			buffDebuffSystem.ReinitializeBaseStats();
		}
	}

	public void ClearSavedState()
	{
		savedData = null;
		itemLookupCache = null;
	}

	private void BuildItemLookupCache()
	{
		if (itemLookupCache != null)
		{
			return;
		}
		itemLookupCache = new Dictionary<string, InventoryItem>();
		InventoryItem[] array = Resources.LoadAll<InventoryItem>("ItemData");
		foreach (InventoryItem inventoryItem in array)
		{
			if (!string.IsNullOrEmpty(inventoryItem.itemName) && !itemLookupCache.ContainsKey(inventoryItem.itemName))
			{
				itemLookupCache[inventoryItem.itemName] = inventoryItem;
			}
		}
	}

	private InventoryItem FindItemByName(string itemName)
	{
		if (string.IsNullOrEmpty(itemName))
		{
			return null;
		}
		if (itemLookupCache != null && itemLookupCache.TryGetValue(itemName, out var value))
		{
			return value;
		}
		return null;
	}
}
