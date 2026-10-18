using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace PixelCrushers.GridController;

public class ItemUnlockManager : MonoBehaviour
{
	[Header("Settings")]
	[SerializeField]
	private string prefsKeyPrefix = "ItemUnlock_";

	[SerializeField]
	private string classPrefsKeyPrefix = "ClassUnlock_";

	[SerializeField]
	private List<ChestItemPool> targetPools = new List<ChestItemPool>();

	[SerializeField]
	private bool debugMode;

	[Header("Events")]
	public UnityEvent<InventoryItem> OnItemUnlocked;

	public UnityEvent OnUnlockProgressChanged;

	public UnityEvent<PlayerClass> OnClassUnlocked;

	public UnityEvent OnClassUnlockProgressChanged;

	private Dictionary<string, InventoryItem> lockableItems = new Dictionary<string, InventoryItem>();

	private Dictionary<string, InventoryItem> allItems = new Dictionary<string, InventoryItem>();

	private HashSet<string> unlockedItemNames = new HashSet<string>();

	private Dictionary<string, PlayerClass> lockableClasses = new Dictionary<string, PlayerClass>();

	private Dictionary<string, PlayerClass> allClasses = new Dictionary<string, PlayerClass>();

	private HashSet<string> unlockedClassNames = new HashSet<string>();

	public static ItemUnlockManager Instance { get; private set; }

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
			Object.DontDestroyOnLoad(gameObject);
			BuildLockableItemsCache();
			LoadProgress();
			BuildLockableClassesCache();
			LoadClassProgress();
		}
		else
		{
			Object.Destroy(gameObject);
		}
	}

	private void BuildLockableItemsCache()
	{
		lockableItems.Clear();
		allItems.Clear();
		InventoryItem[] array = Resources.LoadAll<InventoryItem>("ItemData");
		foreach (InventoryItem inventoryItem in array)
		{
			allItems[inventoryItem.itemName] = inventoryItem;
			if (inventoryItem.startsLocked)
			{
				lockableItems[inventoryItem.itemName] = inventoryItem;
			}
		}
	}

	private void LoadProgress()
	{
		unlockedItemNames.Clear();
		foreach (KeyValuePair<string, InventoryItem> lockableItem in lockableItems)
		{
			if (PlayerPrefs.GetInt(prefsKeyPrefix + lockableItem.Key, 0) == 1)
			{
				unlockedItemNames.Add(lockableItem.Key);
			}
		}
	}

	private void SaveProgress(string itemName, bool unlocked)
	{
		PlayerPrefs.SetInt(prefsKeyPrefix + itemName, unlocked ? 1 : 0);
		PlayerPrefs.Save();
	}

	public bool IsItemUnlocked(string itemName)
	{
		if (!lockableItems.ContainsKey(itemName))
		{
			return true;
		}
		return unlockedItemNames.Contains(itemName);
	}

	public bool IsItemUnlocked(InventoryItem item)
	{
		if (item == null)
		{
			return false;
		}
		return IsItemUnlocked(item.itemName);
	}

	public void UnlockItem(string itemName)
	{
		if (lockableItems.ContainsKey(itemName) && !unlockedItemNames.Contains(itemName))
		{
			unlockedItemNames.Add(itemName);
			SaveProgress(itemName, unlocked: true);
			InventoryItem inventoryItem = lockableItems[itemName];
			InjectItemIntoPools(inventoryItem);
			OnItemUnlocked?.Invoke(inventoryItem);
			OnUnlockProgressChanged?.Invoke();
		}
	}

	public void UnlockItem(InventoryItem item)
	{
		if (!(item == null))
		{
			UnlockItem(item.itemName);
		}
	}

	public void LockItem(string itemName)
	{
		if (!lockableItems.ContainsKey(itemName) || !unlockedItemNames.Contains(itemName))
		{
			return;
		}
		unlockedItemNames.Remove(itemName);
		SaveProgress(itemName, unlocked: false);
		InventoryItem item = lockableItems[itemName];
		foreach (ChestItemPool targetPool in targetPools)
		{
			if (targetPool != null)
			{
				targetPool.SetItemEnabled(item, enabled: false);
			}
		}
		OnUnlockProgressChanged?.Invoke();
	}

	public void InjectUnlockedItems()
	{
		foreach (KeyValuePair<string, InventoryItem> lockableItem in lockableItems)
		{
			foreach (ChestItemPool targetPool in targetPools)
			{
				if (targetPool != null)
				{
					targetPool.SetItemEnabled(lockableItem.Value, enabled: false);
				}
			}
		}
		int num = 0;
		foreach (string unlockedItemName in unlockedItemNames)
		{
			if (!lockableItems.TryGetValue(unlockedItemName, out var value))
			{
				continue;
			}
			foreach (ChestItemPool targetPool2 in targetPools)
			{
				if (targetPool2 != null && targetPool2.SetItemEnabled(value, enabled: true))
				{
					num++;
				}
			}
		}
	}

	public int GetUnlockedCount()
	{
		return unlockedItemNames.Count;
	}

	public int GetTotalLockableCount()
	{
		return lockableItems.Count;
	}

	public List<(InventoryItem item, bool isUnlocked)> GetItemsByType(ItemType type)
	{
		List<(InventoryItem, bool)> list = new List<(InventoryItem, bool)>();
		foreach (KeyValuePair<string, InventoryItem> allItem in allItems)
		{
			if (allItem.Value.itemType == type)
			{
				list.Add((allItem.Value, IsItemUnlocked(allItem.Key)));
			}
		}
		return list;
	}

	public List<(InventoryItem item, bool isUnlocked)> GetAllLockableItems()
	{
		List<(InventoryItem, bool)> list = new List<(InventoryItem, bool)>();
		foreach (KeyValuePair<string, InventoryItem> lockableItem in lockableItems)
		{
			list.Add((lockableItem.Value, unlockedItemNames.Contains(lockableItem.Key)));
		}
		return list;
	}

	public void ResetAllProgress()
	{
		foreach (KeyValuePair<string, InventoryItem> lockableItem in lockableItems)
		{
			PlayerPrefs.DeleteKey(prefsKeyPrefix + lockableItem.Key);
			foreach (ChestItemPool targetPool in targetPools)
			{
				if (targetPool != null)
				{
					targetPool.SetItemEnabled(lockableItem.Value, enabled: false);
				}
			}
		}
		unlockedItemNames.Clear();
		foreach (KeyValuePair<string, PlayerClass> lockableClass in lockableClasses)
		{
			PlayerPrefs.DeleteKey(classPrefsKeyPrefix + lockableClass.Key);
		}
		unlockedClassNames.Clear();
		PlayerPrefs.Save();
		OnUnlockProgressChanged?.Invoke();
		OnClassUnlockProgressChanged?.Invoke();
	}

	public void UnlockAllItems()
	{
		foreach (KeyValuePair<string, InventoryItem> lockableItem in lockableItems)
		{
			if (!unlockedItemNames.Contains(lockableItem.Key))
			{
				unlockedItemNames.Add(lockableItem.Key);
				SaveProgress(lockableItem.Key, unlocked: true);
			}
		}
		InjectUnlockedItems();
		OnUnlockProgressChanged?.Invoke();
	}

	public bool IsClassUnlocked(string className)
	{
		if (!lockableClasses.ContainsKey(className))
		{
			return true;
		}
		return unlockedClassNames.Contains(className);
	}

	public bool IsClassUnlocked(PlayerClass playerClass)
	{
		if (playerClass == null)
		{
			return false;
		}
		return IsClassUnlocked(playerClass.className);
	}

	public void UnlockClass(string className)
	{
		if (lockableClasses.ContainsKey(className) && !unlockedClassNames.Contains(className))
		{
			unlockedClassNames.Add(className);
			SaveClassProgress(className, unlocked: true);
			PlayerClass arg = lockableClasses[className];
			OnClassUnlocked?.Invoke(arg);
			OnClassUnlockProgressChanged?.Invoke();
		}
	}

	public void UnlockClass(PlayerClass playerClass)
	{
		if (!(playerClass == null))
		{
			UnlockClass(playerClass.className);
		}
	}

	public void LockClass(string className)
	{
		if (lockableClasses.ContainsKey(className) && unlockedClassNames.Contains(className))
		{
			unlockedClassNames.Remove(className);
			SaveClassProgress(className, unlocked: false);
			OnClassUnlockProgressChanged?.Invoke();
		}
	}

	public List<(PlayerClass playerClass, bool isUnlocked)> GetAllClasses()
	{
		List<(PlayerClass, bool)> list = new List<(PlayerClass, bool)>();
		foreach (KeyValuePair<string, PlayerClass> allClass in allClasses)
		{
			list.Add((allClass.Value, IsClassUnlocked(allClass.Key)));
		}
		return list;
	}

	public int GetUnlockedClassCount()
	{
		int num = 0;
		foreach (KeyValuePair<string, PlayerClass> allClass in allClasses)
		{
			if (IsClassUnlocked(allClass.Key))
			{
				num++;
			}
		}
		return num;
	}

	public int GetTotalLockableClassCount()
	{
		return lockableClasses.Count;
	}

	private void InjectItemIntoPools(InventoryItem item)
	{
		foreach (ChestItemPool targetPool in targetPools)
		{
			if (targetPool != null)
			{
				targetPool.SetItemEnabled(item, enabled: true);
			}
		}
	}

	private void BuildLockableClassesCache()
	{
		lockableClasses.Clear();
		allClasses.Clear();
		PlayerClass[] array = Resources.LoadAll<PlayerClass>("PlayerClassData");
		foreach (PlayerClass playerClass in array)
		{
			allClasses[playerClass.className] = playerClass;
			if (playerClass.startsLocked)
			{
				lockableClasses[playerClass.className] = playerClass;
			}
		}
	}

	private void LoadClassProgress()
	{
		unlockedClassNames.Clear();
		foreach (KeyValuePair<string, PlayerClass> lockableClass in lockableClasses)
		{
			if (PlayerPrefs.GetInt(classPrefsKeyPrefix + lockableClass.Key, 0) == 1)
			{
				unlockedClassNames.Add(lockableClass.Key);
			}
		}
	}

	private void SaveClassProgress(string className, bool unlocked)
	{
		PlayerPrefs.SetInt(classPrefsKeyPrefix + className, unlocked ? 1 : 0);
		PlayerPrefs.Save();
	}

	[ContextMenu("Unlock All Items")]
	private void DebugUnlockAll()
	{
		UnlockAllItems();
	}

	[ContextMenu("Reset All Unlock Progress")]
	private void DebugResetAll()
	{
		ResetAllProgress();
	}

	[ContextMenu("Inject Unlocked Items Now")]
	private void DebugInjectNow()
	{
		InjectUnlockedItems();
	}

	[ContextMenu("Rebuild Cache")]
	private void DebugRebuildCache()
	{
		BuildLockableItemsCache();
		LoadProgress();
	}
}
