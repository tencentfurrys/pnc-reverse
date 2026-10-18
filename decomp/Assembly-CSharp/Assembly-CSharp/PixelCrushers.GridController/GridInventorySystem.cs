using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;

namespace PixelCrushers.GridController;

public class GridInventorySystem : MonoBehaviour
{
	[Header("Grid Settings")]
	[SerializeField]
	private int gridWidth = 8;

	[SerializeField]
	private int gridHeight = 4;

	[SerializeField]
	private bool[] unlockedRows = new bool[4] { true, true, false, false };

	[Header("Equipment References")]
	[SerializeField]
	private DualWieldingSystem dualWieldingSystem;

	[SerializeField]
	private PlayerStats playerStats;

	[SerializeField]
	private TrinketManager trinketManager;

	[Header("Audio")]
	[SerializeField]
	private AudioSource audioSource;

	[Tooltip("Fallback equip sound if item has no specific equip sound")]
	[SerializeField]
	private AudioClip defaultEquipSound;

	[Tooltip("Fallback move sound if item has no specific move sound")]
	[SerializeField]
	private AudioClip defaultMoveSound;

	[SerializeField]
	private AudioMixerGroup sfxMixerGroup;

	[Header("Equipment Slots")]
	[SerializeField]
	private InventoryItem upperArmorSlot;

	[SerializeField]
	private InventoryItem lowerArmorSlot;

	[SerializeField]
	private InventoryItem leftWeaponSlot;

	[SerializeField]
	private InventoryItem rightWeaponSlot;

	[SerializeField]
	private InventoryItem trinket1Slot;

	[SerializeField]
	private InventoryItem trinket2Slot;

	[Header("Debug")]
	[SerializeField]
	private bool showDebugCoordinates;

	[Header("Events")]
	public UnityEvent<InventoryItem, Vector2Int> OnItemAdded;

	public UnityEvent<InventoryItem, Vector2Int> OnItemRemoved;

	public UnityEvent<InventoryItem, Vector2Int, Vector2Int> OnItemMoved;

	public UnityEvent<InventoryItem, EquipmentSlotType> OnItemEquipped;

	public UnityEvent<InventoryItem, EquipmentSlotType> OnItemUnequipped;

	public UnityEvent<int> OnRowUnlocked;

	public UnityEvent OnInventoryChanged;

	private InventoryItem[,] inventoryGrid;

	private Dictionary<Vector2Int, InventoryItem> itemPositions;

	public int GridWidth => gridWidth;

	public int GridHeight => gridHeight;

	public bool ShowDebugCoordinates => showDebugCoordinates;

	public int TotalSlots => GetAvailableSlotCount();

	public int UsedSlots => GetUsedSlotCount();

	public bool[] UnlockedRows => unlockedRows;

	public InventoryItem UpperArmor => upperArmorSlot;

	public InventoryItem LowerArmor => lowerArmorSlot;

	public InventoryItem LeftWeapon => leftWeaponSlot;

	public InventoryItem RightWeapon => rightWeaponSlot;

	public InventoryItem Trinket1 => trinket1Slot;

	public InventoryItem Trinket2 => trinket2Slot;

	private void Awake()
	{
		InitializeInventory();
		FindComponents();
	}

	private void InitializeInventory()
	{
		inventoryGrid = new InventoryItem[gridWidth, gridHeight];
		itemPositions = new Dictionary<Vector2Int, InventoryItem>();
	}

	private void FindComponents()
	{
		if (dualWieldingSystem == null)
		{
			dualWieldingSystem = GetComponent<DualWieldingSystem>();
		}
		if (playerStats == null)
		{
			playerStats = GetComponent<PlayerStats>();
		}
		if (trinketManager == null)
		{
			trinketManager = GetComponent<TrinketManager>() ?? Object.FindAnyObjectByType<TrinketManager>();
		}
		if (audioSource == null)
		{
			audioSource = GetComponent<AudioSource>();
		}
		if (audioSource == null)
		{
			audioSource = gameObject.AddComponent<AudioSource>();
		}
		if (audioSource != null && sfxMixerGroup != null)
		{
			audioSource.outputAudioMixerGroup = sfxMixerGroup;
		}
	}

	private void PlaySound(AudioClip clip)
	{
		if (audioSource != null && clip != null)
		{
			audioSource.PlayOneShot(clip);
		}
	}

	private void PlayEquipSound(InventoryItem item)
	{
		AudioClip clip = item?.equipSound ?? defaultEquipSound;
		PlaySound(clip);
	}

	private void PlayMoveSound(InventoryItem item)
	{
		AudioClip clip = item?.pickupDropSound ?? defaultMoveSound;
		PlaySound(clip);
	}

	public bool IsValidPosition(Vector2Int position)
	{
		if (position.x >= 0 && position.x < gridWidth && position.y >= 0)
		{
			return position.y < gridHeight;
		}
		return false;
	}

	public bool IsSlotAvailable(Vector2Int position)
	{
		if (!IsValidPosition(position))
		{
			return false;
		}
		if (!unlockedRows[position.y])
		{
			return false;
		}
		return inventoryGrid[position.x, position.y] == null;
	}

	public bool IsRowUnlocked(int rowIndex)
	{
		if (rowIndex >= 0 && rowIndex < gridHeight)
		{
			return unlockedRows[rowIndex];
		}
		return false;
	}

	public void UnlockRow(int rowIndex)
	{
		if (rowIndex >= 0 && rowIndex < gridHeight && !unlockedRows[rowIndex])
		{
			unlockedRows[rowIndex] = true;
			OnRowUnlocked?.Invoke(rowIndex);
			OnInventoryChanged?.Invoke();
		}
	}

	public void LockRow(int rowIndex)
	{
		if (rowIndex < 0 || rowIndex >= gridHeight || rowIndex < 2 || !unlockedRows[rowIndex])
		{
			return;
		}
		for (int i = 0; i < gridWidth; i++)
		{
			Vector2Int position = new Vector2Int(i, rowIndex);
			if (inventoryGrid[i, rowIndex] != null)
			{
				RemoveItem(position);
			}
		}
		unlockedRows[rowIndex] = false;
		OnInventoryChanged?.Invoke();
	}

	public int GetAvailableSlotCount()
	{
		int num = 0;
		for (int i = 0; i < gridHeight; i++)
		{
			if (unlockedRows[i])
			{
				num += gridWidth;
			}
		}
		return num;
	}

	public int GetUsedSlotCount()
	{
		return itemPositions.Count;
	}

	public bool AddItem(InventoryItem item, Vector2Int position, bool silent = false)
	{
		if (item == null)
		{
			return false;
		}
		if (!IsSlotAvailable(position))
		{
			return false;
		}
		inventoryGrid[position.x, position.y] = item;
		itemPositions[position] = item;
		if (!silent)
		{
			OnItemAdded?.Invoke(item, position);
		}
		OnInventoryChanged?.Invoke();
		return true;
	}

	public bool AddItem(InventoryItem item, bool silent = false)
	{
		if (item == null)
		{
			return false;
		}
		Vector2Int? vector2Int = FindFirstAvailableSlot();
		if (vector2Int.HasValue)
		{
			return AddItem(item, vector2Int.Value, silent);
		}
		return false;
	}

	public InventoryItem RemoveItem(Vector2Int position, bool silent = false)
	{
		if (!IsValidPosition(position))
		{
			return null;
		}
		InventoryItem inventoryItem = inventoryGrid[position.x, position.y];
		if (inventoryItem == null)
		{
			return null;
		}
		inventoryGrid[position.x, position.y] = null;
		itemPositions.Remove(position);
		if (!silent)
		{
			OnItemRemoved?.Invoke(inventoryItem, position);
		}
		OnInventoryChanged?.Invoke();
		return inventoryItem;
	}

	public InventoryItem GetItem(Vector2Int position)
	{
		if (!IsValidPosition(position))
		{
			return null;
		}
		return inventoryGrid[position.x, position.y];
	}

	public bool MoveItem(Vector2Int fromPosition, Vector2Int toPosition)
	{
		InventoryItem item = GetItem(fromPosition);
		if (item == null)
		{
			return false;
		}
		if (!IsSlotAvailable(toPosition))
		{
			return false;
		}
		inventoryGrid[fromPosition.x, fromPosition.y] = null;
		itemPositions.Remove(fromPosition);
		inventoryGrid[toPosition.x, toPosition.y] = item;
		itemPositions[toPosition] = item;
		OnItemMoved?.Invoke(item, fromPosition, toPosition);
		OnInventoryChanged?.Invoke();
		return true;
	}

	private Vector2Int? FindFirstAvailableSlot()
	{
		for (int i = 0; i < gridHeight; i++)
		{
			if (!unlockedRows[i])
			{
				continue;
			}
			for (int j = 0; j < gridWidth; j++)
			{
				Vector2Int vector2Int = new Vector2Int(j, i);
				if (IsSlotAvailable(vector2Int))
				{
					return vector2Int;
				}
			}
		}
		return null;
	}

	public bool EquipItem(InventoryItem item, EquipmentSlotType slotType, bool skipStatModifiers = false, bool silent = false)
	{
		List<InventoryItem> displacedItems;
		return EquipItem(item, slotType, out displacedItems, skipStatModifiers, silent);
	}

	public bool EquipItem(InventoryItem item, EquipmentSlotType slotType, out List<InventoryItem> displacedItems, bool skipStatModifiers = false, bool silent = false)
	{
		displacedItems = new List<InventoryItem>();
		if (item == null)
		{
			return false;
		}
		if (!CanEquipInSlot(item, slotType))
		{
			return false;
		}
		InventoryItem equippedItem = GetEquippedItem(slotType);
		if (equippedItem != null && UnequipItem(slotType, silent) == null)
		{
			DetachEquippedItem(equippedItem, slotType);
			displacedItems.Add(equippedItem);
		}
		SetEquippedItem(item, slotType);
		if (item.IsWeapon && dualWieldingSystem != null)
		{
			HandSlot handSlot;
			if (item.weaponData.handSlot == HandSlot.BothHands || item.weaponData.twoHanded)
			{
				EquipmentSlotType slotType2 = ((slotType == EquipmentSlotType.LeftWeapon) ? EquipmentSlotType.RightWeapon : EquipmentSlotType.LeftWeapon);
				InventoryItem equippedItem2 = GetEquippedItem(slotType2);
				if (equippedItem2 != null && equippedItem2.IsWeapon && !dualWieldingSystem.IsUsingBothHandsWeapon() && UnequipItem(slotType2, silent) == null)
				{
					DetachEquippedItem(equippedItem2, slotType2);
					displacedItems.Add(equippedItem2);
				}
				handSlot = HandSlot.BothHands;
				leftWeaponSlot = item;
				rightWeaponSlot = item;
			}
			else
			{
				handSlot = ((slotType != EquipmentSlotType.LeftWeapon) ? HandSlot.RightHand : HandSlot.LeftHand);
			}
			dualWieldingSystem.EquipWeapon(item.weaponData, handSlot);
		}
		if (!skipStatModifiers && item.IsArmor && item.armorData != null && playerStats != null)
		{
			item.armorData.ApplyStatModifiers(playerStats);
		}
		if (item.IsTrinket && trinketManager != null)
		{
			int trinketIndex = GetTrinketIndex(slotType);
			if (trinketIndex >= 0)
			{
				trinketManager.EquipTrinket(item.trinketData, trinketIndex);
			}
		}
		ApplyEquipmentStats();
		if (!silent)
		{
			OnItemEquipped?.Invoke(item, slotType);
		}
		OnInventoryChanged?.Invoke();
		return true;
	}

	public InventoryItem UnequipItem(EquipmentSlotType slotType, bool silent = false)
	{
		InventoryItem equippedItem = GetEquippedItem(slotType);
		if (equippedItem == null)
		{
			return null;
		}
		if (!AddItem(equippedItem, silent: true))
		{
			return null;
		}
		DetachEquippedItem(equippedItem, slotType);
		ApplyEquipmentStats();
		if (!silent)
		{
			OnItemUnequipped?.Invoke(equippedItem, slotType);
		}
		OnInventoryChanged?.Invoke();
		return equippedItem;
	}

	private void DetachEquippedItem(InventoryItem item, EquipmentSlotType slotType)
	{
		if (item == null)
		{
			return;
		}
		bool num = item.IsWeapon && (item.weaponData.handSlot == HandSlot.BothHands || item.weaponData.twoHanded);
		if (item.IsArmor && item.armorData != null && playerStats != null)
		{
			item.armorData.RemoveStatModifiers(playerStats);
		}
		if (item.IsTrinket && trinketManager != null)
		{
			int trinketIndex = GetTrinketIndex(slotType);
			if (trinketIndex >= 0)
			{
				trinketManager.UnequipTrinket(trinketIndex);
			}
		}
		if (num)
		{
			SetEquippedItem(null, EquipmentSlotType.LeftWeapon);
			SetEquippedItem(null, EquipmentSlotType.RightWeapon);
			if (dualWieldingSystem != null)
			{
				dualWieldingSystem.ForceUnequipBothHandsWeapon();
			}
		}
		else
		{
			SetEquippedItem(null, slotType);
			if (item.IsWeapon && dualWieldingSystem != null)
			{
				HandSlot handSlot = ((slotType != EquipmentSlotType.LeftWeapon) ? HandSlot.RightHand : HandSlot.LeftHand);
				dualWieldingSystem.HandleEmptyWeaponSlot(handSlot);
			}
		}
	}

	public void SyncDualWieldingSystem()
	{
		if (!(dualWieldingSystem == null))
		{
			dualWieldingSystem.SyncWithInventorySystem(leftWeaponSlot, rightWeaponSlot);
		}
	}

	private bool CanEquipInSlot(InventoryItem item, EquipmentSlotType slotType)
	{
		if (item == null)
		{
			return false;
		}
		return item.CanEquipInSlot(slotType);
	}

	private int GetTrinketIndex(EquipmentSlotType slotType)
	{
		return slotType switch
		{
			EquipmentSlotType.Trinket1 => 0, 
			EquipmentSlotType.Trinket2 => 1, 
			_ => -1, 
		};
	}

	public void ConsumeTrinket(int trinketIndex)
	{
		EquipmentSlotType slotType = ((trinketIndex == 0) ? EquipmentSlotType.Trinket1 : EquipmentSlotType.Trinket2);
		if (!(GetEquippedItem(slotType) == null))
		{
			if (trinketManager != null)
			{
				trinketManager.UnequipTrinket(trinketIndex);
			}
			SetEquippedItem(null, slotType);
			OnInventoryChanged?.Invoke();
		}
	}

	private void ClearTrinketSlotForLoad(EquipmentSlotType slotType)
	{
		if (!(GetEquippedItem(slotType) == null))
		{
			int trinketIndex = GetTrinketIndex(slotType);
			if (trinketIndex >= 0 && trinketManager != null)
			{
				trinketManager.UnequipTrinket(trinketIndex);
			}
			SetEquippedItem(null, slotType);
		}
	}

	public InventoryItem GetEquippedItem(EquipmentSlotType slotType)
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

	public void SetEquippedItemToNull(EquipmentSlotType slotType)
	{
		SetEquippedItem(null, slotType);
	}

	public DualWieldingSystem GetDualWieldingSystem()
	{
		return dualWieldingSystem;
	}

	private void SetEquippedItem(InventoryItem item, EquipmentSlotType slotType)
	{
		switch (slotType)
		{
		case EquipmentSlotType.UpperArmor:
			upperArmorSlot = item;
			break;
		case EquipmentSlotType.LowerArmor:
			lowerArmorSlot = item;
			break;
		case EquipmentSlotType.LeftWeapon:
			leftWeaponSlot = item;
			break;
		case EquipmentSlotType.RightWeapon:
			rightWeaponSlot = item;
			break;
		case EquipmentSlotType.Trinket1:
			trinket1Slot = item;
			break;
		case EquipmentSlotType.Trinket2:
			trinket2Slot = item;
			break;
		}
	}

	public void ApplyEquipmentStats()
	{
		BuffDebuffSystem buffDebuffSystem = Object.FindAnyObjectByType<BuffDebuffSystem>();
		if (buffDebuffSystem != null)
		{
			buffDebuffSystem.ReinitializeBaseStats();
		}
	}

	public int GetTotalDefense()
	{
		int num = 0;
		if (upperArmorSlot != null && upperArmorSlot.IsArmor && upperArmorSlot.armorData != null)
		{
			num += upperArmorSlot.armorData.defense;
		}
		if (lowerArmorSlot != null && lowerArmorSlot.IsArmor && lowerArmorSlot.armorData != null)
		{
			num += lowerArmorSlot.armorData.defense;
		}
		return num;
	}

	public int GetTotalThornsDamage()
	{
		int num = 0;
		if (upperArmorSlot != null && upperArmorSlot.IsArmor && upperArmorSlot.armorData != null)
		{
			num += upperArmorSlot.armorData.thornsDamage;
		}
		if (lowerArmorSlot != null && lowerArmorSlot.IsArmor && lowerArmorSlot.armorData != null)
		{
			num += lowerArmorSlot.armorData.thornsDamage;
		}
		return num;
	}

	public bool HasLifeStealOnKill()
	{
		if (upperArmorSlot != null && upperArmorSlot.IsArmor && upperArmorSlot.armorData != null && upperArmorSlot.armorData.lifeStealOnKill)
		{
			return true;
		}
		if (lowerArmorSlot != null && lowerArmorSlot.IsArmor && lowerArmorSlot.armorData != null && lowerArmorSlot.armorData.lifeStealOnKill)
		{
			return true;
		}
		if (trinketManager != null && trinketManager.HasLifeStealOnKill())
		{
			return true;
		}
		return false;
	}

	public bool HasPitfallImmunity()
	{
		if (upperArmorSlot != null && upperArmorSlot.IsArmor && upperArmorSlot.armorData != null && upperArmorSlot.armorData.negatesPitfallDamage)
		{
			return true;
		}
		if (lowerArmorSlot != null && lowerArmorSlot.IsArmor && lowerArmorSlot.armorData != null && lowerArmorSlot.armorData.negatesPitfallDamage)
		{
			return true;
		}
		return false;
	}

	public List<InventoryItem> GetAllItems()
	{
		List<InventoryItem> list = new List<InventoryItem>();
		foreach (InventoryItem value in itemPositions.Values)
		{
			if (value != null)
			{
				list.Add(value);
			}
		}
		return list;
	}

	public int GetItemCount(string itemName)
	{
		int num = 0;
		foreach (InventoryItem value in itemPositions.Values)
		{
			if (value != null && value.itemName == itemName)
			{
				num++;
			}
		}
		return num;
	}

	public bool HasItem(string itemName, int amount = 1)
	{
		return GetItemCount(itemName) >= amount;
	}

	public bool ConsumeItem(string itemName, int amount = 1)
	{
		if (!HasItem(itemName, amount))
		{
			return false;
		}
		int num = amount;
		List<Vector2Int> list = new List<Vector2Int>();
		foreach (KeyValuePair<Vector2Int, InventoryItem> itemPosition in itemPositions)
		{
			if (itemPosition.Value.itemName == itemName && num > 0)
			{
				list.Add(itemPosition.Key);
				num--;
			}
		}
		foreach (Vector2Int item in list)
		{
			RemoveItem(item);
		}
		return num == 0;
	}

	public void ClearInventory()
	{
		for (int i = 0; i < gridWidth; i++)
		{
			for (int j = 0; j < gridHeight; j++)
			{
				inventoryGrid[i, j] = null;
			}
		}
		itemPositions.Clear();
		OnInventoryChanged?.Invoke();
	}

	public void SetDebugMode(bool enabled)
	{
		showDebugCoordinates = enabled;
	}

	public List<(InventoryItem item, Vector2Int position)> GetAllItemsWithPositions()
	{
		List<(InventoryItem, Vector2Int)> list = new List<(InventoryItem, Vector2Int)>();
		foreach (KeyValuePair<Vector2Int, InventoryItem> itemPosition in itemPositions)
		{
			list.Add((itemPosition.Value, itemPosition.Key));
		}
		return list;
	}

	public bool[] GetUnlockedRowsState()
	{
		return (bool[])unlockedRows.Clone();
	}

	public void LoadFromSaveData(List<(InventoryItem item, Vector2Int position)> items, bool[] rows)
	{
		ClearInventory();
		if (rows != null)
		{
			for (int i = 0; i < rows.Length && i < unlockedRows.Length; i++)
			{
				unlockedRows[i] = rows[i];
			}
		}
		foreach (var (inventoryItem, position) in items)
		{
			if (inventoryItem != null && IsValidPosition(position))
			{
				AddItem(inventoryItem, position, silent: true);
			}
		}
		OnInventoryChanged?.Invoke();
	}

	public void LoadEquipmentFromSaveData(InventoryItem upper, InventoryItem lower, InventoryItem left, InventoryItem right, bool skipStatModifiers = false, InventoryItem trinket1 = null, InventoryItem trinket2 = null)
	{
		if (upperArmorSlot != null)
		{
			UnequipItem(EquipmentSlotType.UpperArmor, silent: true);
		}
		if (lowerArmorSlot != null)
		{
			UnequipItem(EquipmentSlotType.LowerArmor, silent: true);
		}
		if (leftWeaponSlot != null)
		{
			UnequipItem(EquipmentSlotType.LeftWeapon, silent: true);
		}
		if (rightWeaponSlot != null)
		{
			UnequipItem(EquipmentSlotType.RightWeapon, silent: true);
		}
		ClearTrinketSlotForLoad(EquipmentSlotType.Trinket1);
		ClearTrinketSlotForLoad(EquipmentSlotType.Trinket2);
		if (upper != null)
		{
			EquipItem(upper, EquipmentSlotType.UpperArmor, skipStatModifiers, silent: true);
		}
		if (lower != null)
		{
			EquipItem(lower, EquipmentSlotType.LowerArmor, skipStatModifiers, silent: true);
		}
		if (trinket1 != null)
		{
			EquipItem(trinket1, EquipmentSlotType.Trinket1, skipStatModifiers, silent: true);
		}
		if (trinket2 != null)
		{
			EquipItem(trinket2, EquipmentSlotType.Trinket2, skipStatModifiers, silent: true);
		}
		bool flag = false;
		if (left != null)
		{
			EquipItem(left, EquipmentSlotType.LeftWeapon, skipStatModifiers, silent: true);
			flag = left.weaponData != null && (left.weaponData.twoHanded || left.weaponData.handSlot == HandSlot.BothHands);
		}
		if (!flag && right != null)
		{
			EquipItem(right, EquipmentSlotType.RightWeapon, skipStatModifiers, silent: true);
		}
	}

	[ContextMenu("Debug: Print Inventory")]
	public void DebugPrintInventory()
	{
		foreach (KeyValuePair<Vector2Int, InventoryItem> itemPosition in itemPositions)
		{
			_ = itemPosition.Key;
			_ = itemPosition.Value;
		}
	}
}
