using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace PixelCrushers.GridController;

[CreateAssetMenu(fileName = "New Chest Item Pool", menuName = "Inventory/Chest Item Pool")]
public class ChestItemPool : ScriptableObject
{
	[Serializable]
	public class WeightedItem
	{
		[SerializeField]
		public InventoryItem item;

		[SerializeField]
		[Range(0.1f, 100f)]
		public float weight = 1f;

		[SerializeField]
		public bool enabled = true;

		[Header("Override Settings")]
		[SerializeField]
		public bool overrideRarity;

		[SerializeField]
		public ItemRarity customRarity;

		public bool IsValid
		{
			get
			{
				if (item == null || !enabled || weight <= 0f)
				{
					return false;
				}
				if (item.maxTimesPerRun > 0 && GetSeenCount(item) + GetReservedCount(item) >= item.maxTimesPerRun)
				{
					return false;
				}
				return true;
			}
		}

		public ItemRarity EffectiveRarity
		{
			get
			{
				if (!overrideRarity)
				{
					return item?.rarity ?? ItemRarity.Common;
				}
				return customRarity;
			}
		}
	}

	[Header("Basic Pool Settings")]
	[SerializeField]
	private string poolName = "Basic Chest Pool";

	[TextArea(2, 4)]
	[SerializeField]
	private string description = "A collection of items that can spawn from chests";

	[Header("Common Items (High Chance)")]
	[SerializeField]
	private List<WeightedItem> commonItems = new List<WeightedItem>();

	[Header("Uncommon Items (Medium Chance)")]
	[SerializeField]
	private List<WeightedItem> uncommonItems = new List<WeightedItem>();

	[Header("Rare Items (Low Chance)")]
	[SerializeField]
	private List<WeightedItem> rareItems = new List<WeightedItem>();

	[Header("Epic Items (Very Low Chance)")]
	[SerializeField]
	private List<WeightedItem> epicItems = new List<WeightedItem>();

	[Header("Legendary Items (Extremely Rare)")]
	[SerializeField]
	private List<WeightedItem> legendaryItems = new List<WeightedItem>();

	[Header("Pool Statistics")]
	[SerializeField]
	private bool showStatistics = true;

	private float totalCommonWeight = -1f;

	private float totalUncommonWeight = -1f;

	private float totalRareWeight = -1f;

	private float totalEpicWeight = -1f;

	private float totalLegendaryWeight = -1f;

	private float totalAllWeight = -1f;

	private static Dictionary<InventoryItem, int> runSeenCounts = new Dictionary<InventoryItem, int>();

	private static Dictionary<InventoryItem, int> reservedCounts = new Dictionary<InventoryItem, int>();

	public static void RecordItemSeen(InventoryItem item)
	{
		if (!(item == null))
		{
			if (runSeenCounts.ContainsKey(item))
			{
				runSeenCounts[item]++;
			}
			else
			{
				runSeenCounts[item] = 1;
			}
		}
	}

	public static int GetSeenCount(InventoryItem item)
	{
		if (item == null)
		{
			return 0;
		}
		if (!runSeenCounts.TryGetValue(item, out var value))
		{
			return 0;
		}
		return value;
	}

	public static void ReserveItem(InventoryItem item)
	{
		if (!(item == null))
		{
			if (reservedCounts.ContainsKey(item))
			{
				reservedCounts[item]++;
			}
			else
			{
				reservedCounts[item] = 1;
			}
		}
	}

	public static void ReleaseItem(InventoryItem item)
	{
		if (!(item == null) && reservedCounts.TryGetValue(item, out var value))
		{
			if (value <= 1)
			{
				reservedCounts.Remove(item);
			}
			else
			{
				reservedCounts[item] = value - 1;
			}
		}
	}

	public static int GetReservedCount(InventoryItem item)
	{
		if (item == null)
		{
			return 0;
		}
		if (!reservedCounts.TryGetValue(item, out var value))
		{
			return 0;
		}
		return value;
	}

	public static void ResetRunTracker()
	{
		runSeenCounts.Clear();
		reservedCounts.Clear();
	}

	private void OnValidate()
	{
		InvalidateWeightCache();
		if (!Application.isPlaying)
		{
			ValidateItemCategories();
		}
	}

	private void ValidateItemCategories()
	{
		CheckCategoryConsistency(commonItems, ItemRarity.Common, "Common");
		CheckCategoryConsistency(uncommonItems, ItemRarity.Uncommon, "Uncommon");
		CheckCategoryConsistency(rareItems, ItemRarity.Rare, "Rare");
		CheckCategoryConsistency(epicItems, ItemRarity.Epic, "Epic");
		CheckCategoryConsistency(legendaryItems, ItemRarity.Legendary, "Legendary");
	}

	private void CheckCategoryConsistency(List<WeightedItem> items, ItemRarity expectedRarity, string categoryName)
	{
	}

	private void InvalidateWeightCache()
	{
		totalCommonWeight = -1f;
		totalUncommonWeight = -1f;
		totalRareWeight = -1f;
		totalEpicWeight = -1f;
		totalLegendaryWeight = -1f;
		totalAllWeight = -1f;
	}

	private float GetTotalWeight(List<WeightedItem> items)
	{
		float num = 0f;
		foreach (WeightedItem item in items)
		{
			if (item.IsValid)
			{
				num += item.weight;
			}
		}
		return num;
	}

	private float GetCategoryWeight(ItemRarity rarity)
	{
		switch (rarity)
		{
		case ItemRarity.Common:
			if (totalCommonWeight < 0f)
			{
				totalCommonWeight = GetTotalWeight(commonItems);
			}
			return totalCommonWeight;
		case ItemRarity.Uncommon:
			if (totalUncommonWeight < 0f)
			{
				totalUncommonWeight = GetTotalWeight(uncommonItems);
			}
			return totalUncommonWeight;
		case ItemRarity.Rare:
			if (totalRareWeight < 0f)
			{
				totalRareWeight = GetTotalWeight(rareItems);
			}
			return totalRareWeight;
		case ItemRarity.Epic:
			if (totalEpicWeight < 0f)
			{
				totalEpicWeight = GetTotalWeight(epicItems);
			}
			return totalEpicWeight;
		case ItemRarity.Legendary:
			if (totalLegendaryWeight < 0f)
			{
				totalLegendaryWeight = GetTotalWeight(legendaryItems);
			}
			return totalLegendaryWeight;
		default:
			return 0f;
		}
	}

	private float GetTotalAllWeight()
	{
		if (totalAllWeight < 0f)
		{
			totalAllWeight = GetCategoryWeight(ItemRarity.Common) + GetCategoryWeight(ItemRarity.Uncommon) + GetCategoryWeight(ItemRarity.Rare) + GetCategoryWeight(ItemRarity.Epic) + GetCategoryWeight(ItemRarity.Legendary);
		}
		return totalAllWeight;
	}

	public InventoryItem GetRandomItem(bool recordAsSeen = true)
	{
		InvalidateWeightCache();
		float num = GetTotalAllWeight();
		if (num <= 0f)
		{
			return null;
		}
		float num2 = UnityEngine.Random.Range(0f, num);
		float num3 = 0f;
		InventoryItem inventoryItem = null;
		num3 += GetCategoryWeight(ItemRarity.Common);
		if (inventoryItem == null && num2 <= num3)
		{
			inventoryItem = GetRandomItemFromCategory(commonItems);
		}
		num3 += GetCategoryWeight(ItemRarity.Uncommon);
		if (inventoryItem == null && num2 <= num3)
		{
			inventoryItem = GetRandomItemFromCategory(uncommonItems);
		}
		num3 += GetCategoryWeight(ItemRarity.Rare);
		if (inventoryItem == null && num2 <= num3)
		{
			inventoryItem = GetRandomItemFromCategory(rareItems);
		}
		num3 += GetCategoryWeight(ItemRarity.Epic);
		if (inventoryItem == null && num2 <= num3)
		{
			inventoryItem = GetRandomItemFromCategory(epicItems);
		}
		if (inventoryItem == null)
		{
			inventoryItem = GetRandomItemFromCategory(legendaryItems);
		}
		if (recordAsSeen)
		{
			RecordItemSeen(inventoryItem);
		}
		return inventoryItem;
	}

	public InventoryItem GetRandomItemOfRarity(ItemRarity minRarity)
	{
		List<WeightedItem> list = new List<WeightedItem>();
		if (minRarity <= ItemRarity.Common)
		{
			list.AddRange(GetValidItems(commonItems));
		}
		if (minRarity <= ItemRarity.Uncommon)
		{
			list.AddRange(GetValidItems(uncommonItems));
		}
		if (minRarity <= ItemRarity.Rare)
		{
			list.AddRange(GetValidItems(rareItems));
		}
		if (minRarity <= ItemRarity.Epic)
		{
			list.AddRange(GetValidItems(epicItems));
		}
		if (minRarity <= ItemRarity.Legendary)
		{
			list.AddRange(GetValidItems(legendaryItems));
		}
		return GetRandomItemFromCategory(list);
	}

	public InventoryItem GetHighestRarityItem()
	{
		InvalidateWeightCache();
		List<WeightedItem>[] array = new List<WeightedItem>[5] { legendaryItems, epicItems, rareItems, uncommonItems, commonItems };
		foreach (List<WeightedItem> items in array)
		{
			if (GetValidItems(items).Count > 0)
			{
				InventoryItem randomItemFromCategory = GetRandomItemFromCategory(items);
				RecordItemSeen(randomItemFromCategory);
				return randomItemFromCategory;
			}
		}
		return null;
	}

	public InventoryItem GetRandomRareItem()
	{
		return GetRandomItemOfRarity(ItemRarity.Rare);
	}

	public InventoryItem GetRandomCommonItem()
	{
		return GetRandomItemFromCategory(commonItems);
	}

	public List<InventoryItem> GetRandomItems(int count, bool allowDuplicates = true)
	{
		List<InventoryItem> list = new List<InventoryItem>();
		List<InventoryItem> list2 = new List<InventoryItem>();
		for (int i = 0; i < count; i++)
		{
			InventoryItem randomItem = GetRandomItem();
			if (!(randomItem != null))
			{
				continue;
			}
			if (allowDuplicates || !list2.Contains(randomItem))
			{
				list.Add(randomItem);
				if (!allowDuplicates)
				{
					list2.Add(randomItem);
				}
				continue;
			}
			int num = 0;
			while (list2.Contains(randomItem) && num < 10)
			{
				randomItem = GetRandomItem();
				num++;
			}
			if (randomItem != null && !list2.Contains(randomItem))
			{
				list.Add(randomItem);
				list2.Add(randomItem);
			}
		}
		return list;
	}

	private InventoryItem GetRandomItemFromCategory(List<WeightedItem> items)
	{
		List<WeightedItem> validItems = GetValidItems(items);
		if (validItems.Count == 0)
		{
			return null;
		}
		float num = 0f;
		foreach (WeightedItem item in validItems)
		{
			num += item.weight;
		}
		float num2 = UnityEngine.Random.Range(0f, num);
		float num3 = 0f;
		foreach (WeightedItem item2 in validItems)
		{
			num3 += item2.weight;
			if (num2 <= num3)
			{
				return item2.item;
			}
		}
		return validItems[validItems.Count - 1].item;
	}

	private List<WeightedItem> GetValidItems(List<WeightedItem> items)
	{
		List<WeightedItem> list = new List<WeightedItem>();
		foreach (WeightedItem item in items)
		{
			if (item.IsValid && IsItemUnlocked(item.item))
			{
				list.Add(item);
			}
		}
		return list;
	}

	private bool IsItemUnlocked(InventoryItem item)
	{
		if (ItemUnlockManager.Instance == null)
		{
			return true;
		}
		return ItemUnlockManager.Instance.IsItemUnlocked(item);
	}

	public int GetTotalItems()
	{
		HashSet<InventoryItem> hashSet = new HashSet<InventoryItem>();
		AddUniqueItems(hashSet, commonItems);
		AddUniqueItems(hashSet, uncommonItems);
		AddUniqueItems(hashSet, rareItems);
		AddUniqueItems(hashSet, epicItems);
		AddUniqueItems(hashSet, legendaryItems);
		return hashSet.Count;
	}

	private void AddUniqueItems(HashSet<InventoryItem> set, List<WeightedItem> items)
	{
		foreach (WeightedItem item in items)
		{
			if (item.IsValid)
			{
				set.Add(item.item);
			}
		}
	}

	public Dictionary<ItemRarity, int> GetRarityCounts()
	{
		return new Dictionary<ItemRarity, int>
		{
			{
				ItemRarity.Common,
				GetValidItems(commonItems).Count
			},
			{
				ItemRarity.Uncommon,
				GetValidItems(uncommonItems).Count
			},
			{
				ItemRarity.Rare,
				GetValidItems(rareItems).Count
			},
			{
				ItemRarity.Epic,
				GetValidItems(epicItems).Count
			},
			{
				ItemRarity.Legendary,
				GetValidItems(legendaryItems).Count
			}
		};
	}

	public Dictionary<ItemRarity, float> GetRarityProbabilities()
	{
		float num = GetTotalAllWeight();
		Dictionary<ItemRarity, float> dictionary = new Dictionary<ItemRarity, float>();
		if (num > 0f)
		{
			dictionary[ItemRarity.Common] = GetCategoryWeight(ItemRarity.Common) / num;
			dictionary[ItemRarity.Uncommon] = GetCategoryWeight(ItemRarity.Uncommon) / num;
			dictionary[ItemRarity.Rare] = GetCategoryWeight(ItemRarity.Rare) / num;
			dictionary[ItemRarity.Epic] = GetCategoryWeight(ItemRarity.Epic) / num;
			dictionary[ItemRarity.Legendary] = GetCategoryWeight(ItemRarity.Legendary) / num;
		}
		return dictionary;
	}

	public bool HasItems()
	{
		return GetTotalItems() > 0;
	}

	public bool ContainsItem(InventoryItem item)
	{
		if (!ContainsItemInCategory(commonItems, item) && !ContainsItemInCategory(uncommonItems, item) && !ContainsItemInCategory(rareItems, item) && !ContainsItemInCategory(epicItems, item))
		{
			return ContainsItemInCategory(legendaryItems, item);
		}
		return true;
	}

	private bool ContainsItemInCategory(List<WeightedItem> items, InventoryItem target)
	{
		foreach (WeightedItem item in items)
		{
			if (item.item == target)
			{
				return true;
			}
		}
		return false;
	}

	public bool HasItemsOfRarity(ItemRarity rarity)
	{
		return GetCategoryWeight(rarity) > 0f;
	}

	public List<InventoryItem> GetAllItems()
	{
		List<InventoryItem> list = new List<InventoryItem>();
		AddItemsFromCategory(list, commonItems);
		AddItemsFromCategory(list, uncommonItems);
		AddItemsFromCategory(list, rareItems);
		AddItemsFromCategory(list, epicItems);
		AddItemsFromCategory(list, legendaryItems);
		return list;
	}

	private void AddItemsFromCategory(List<InventoryItem> list, List<WeightedItem> items)
	{
		foreach (WeightedItem item in items)
		{
			if (item.IsValid)
			{
				list.Add(item.item);
			}
		}
	}

	public void AddItem(InventoryItem item, float weight = 1f, bool overrideRarity = false, ItemRarity customRarity = ItemRarity.Common)
	{
		if (!(item == null))
		{
			WeightedItem item2 = new WeightedItem
			{
				item = item,
				weight = weight,
				enabled = true,
				overrideRarity = overrideRarity,
				customRarity = customRarity
			};
			switch (overrideRarity ? customRarity : item.rarity)
			{
			case ItemRarity.Common:
				commonItems.Add(item2);
				break;
			case ItemRarity.Uncommon:
				uncommonItems.Add(item2);
				break;
			case ItemRarity.Rare:
				rareItems.Add(item2);
				break;
			case ItemRarity.Epic:
				epicItems.Add(item2);
				break;
			case ItemRarity.Legendary:
				legendaryItems.Add(item2);
				break;
			}
			InvalidateWeightCache();
		}
	}

	public bool RemoveItem(InventoryItem item)
	{
		int num = (int)(0u | (RemoveFromCategory(commonItems, item) ? 1u : 0u) | (RemoveFromCategory(uncommonItems, item) ? 1u : 0u) | (RemoveFromCategory(rareItems, item) ? 1u : 0u) | (RemoveFromCategory(epicItems, item) ? 1u : 0u)) | (RemoveFromCategory(legendaryItems, item) ? 1 : 0);
		if (num != 0)
		{
			InvalidateWeightCache();
		}
		return (byte)num != 0;
	}

	private bool RemoveFromCategory(List<WeightedItem> items, InventoryItem targetItem)
	{
		for (int num = items.Count - 1; num >= 0; num--)
		{
			if (items[num].item == targetItem)
			{
				items.RemoveAt(num);
				return true;
			}
		}
		return false;
	}

	public bool SetItemEnabled(InventoryItem item, bool enabled)
	{
		if (item == null)
		{
			return false;
		}
		int num;
		if (!SetEnabledInCategory(commonItems, item, enabled) && !SetEnabledInCategory(uncommonItems, item, enabled) && !SetEnabledInCategory(rareItems, item, enabled) && !SetEnabledInCategory(epicItems, item, enabled))
		{
			num = (SetEnabledInCategory(legendaryItems, item, enabled) ? 1 : 0);
			if (num == 0)
			{
				goto IL_0065;
			}
		}
		else
		{
			num = 1;
		}
		InvalidateWeightCache();
		goto IL_0065;
		IL_0065:
		return (byte)num != 0;
	}

	private bool SetEnabledInCategory(List<WeightedItem> items, InventoryItem target, bool enabled)
	{
		foreach (WeightedItem item in items)
		{
			if (item.item == target)
			{
				item.enabled = enabled;
				return true;
			}
		}
		return false;
	}

	public void ClearPool()
	{
		commonItems.Clear();
		uncommonItems.Clear();
		rareItems.Clear();
		epicItems.Clear();
		legendaryItems.Clear();
		InvalidateWeightCache();
	}

	public ChestItemPool CreateCopy(string newName = null)
	{
		ChestItemPool chestItemPool = UnityEngine.Object.Instantiate(this);
		chestItemPool.poolName = newName ?? (poolName + " (Copy)");
		return chestItemPool;
	}

	public string GetDebugInfo()
	{
		StringBuilder stringBuilder = new StringBuilder();
		Dictionary<ItemRarity, int> rarityCounts = GetRarityCounts();
		Dictionary<ItemRarity, float> rarityProbabilities = GetRarityProbabilities();
		stringBuilder.AppendLine("=== Chest Item Pool: " + poolName + " ===");
		stringBuilder.AppendLine("Description: " + description);
		stringBuilder.AppendLine($"Total Items: {GetTotalItems()}");
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("Items by Rarity:");
		foreach (ItemRarity item in Enum.GetValues(typeof(ItemRarity)).Cast<ItemRarity>())
		{
			int num = (rarityCounts.ContainsKey(item) ? rarityCounts[item] : 0);
			float num2 = (rarityProbabilities.ContainsKey(item) ? rarityProbabilities[item] : 0f);
			stringBuilder.AppendLine($"  {item}: {num} items ({num2:P1} chance)");
		}
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("Weight Distribution:");
		stringBuilder.AppendLine($"  Common: {GetCategoryWeight(ItemRarity.Common):F1}");
		stringBuilder.AppendLine($"  Uncommon: {GetCategoryWeight(ItemRarity.Uncommon):F1}");
		stringBuilder.AppendLine($"  Rare: {GetCategoryWeight(ItemRarity.Rare):F1}");
		stringBuilder.AppendLine($"  Epic: {GetCategoryWeight(ItemRarity.Epic):F1}");
		stringBuilder.AppendLine($"  Legendary: {GetCategoryWeight(ItemRarity.Legendary):F1}");
		stringBuilder.AppendLine($"  Total: {GetTotalAllWeight():F1}");
		return stringBuilder.ToString();
	}

	public List<string> ValidatePool()
	{
		List<string> list = new List<string>();
		if (GetTotalItems() == 0)
		{
			list.Add("Pool contains no valid items");
		}
		if (GetTotalAllWeight() <= 0f)
		{
			list.Add("Pool has no weight (all items disabled or weight 0)");
		}
		foreach (InventoryItem item in from x in GetAllItems()
			group x by x into g
			where g.Count() > 1
			select g.Key)
		{
			list.Add("Item '" + item.itemName + "' appears multiple times in the pool");
		}
		CheckMissingItems(commonItems, "Common", list);
		CheckMissingItems(uncommonItems, "Uncommon", list);
		CheckMissingItems(rareItems, "Rare", list);
		CheckMissingItems(epicItems, "Epic", list);
		CheckMissingItems(legendaryItems, "Legendary", list);
		return list;
	}

	private void CheckMissingItems(List<WeightedItem> items, string categoryName, List<string> issues)
	{
		foreach (WeightedItem item in items)
		{
			if (item.item == null)
			{
				issues.Add(categoryName + " category has empty item slot");
			}
		}
	}

	[ContextMenu("Validate Pool")]
	public void ValidatePoolFromMenu()
	{
		ValidatePool();
	}

	[ContextMenu("Test Random Selection")]
	public void TestRandomSelection()
	{
		if (!Application.isPlaying)
		{
			return;
		}
		Dictionary<ItemRarity, int> dictionary = new Dictionary<ItemRarity, int>();
		for (int i = 0; i < 100; i++)
		{
			InventoryItem randomItem = GetRandomItem();
			if (randomItem != null)
			{
				if (!dictionary.ContainsKey(randomItem.rarity))
				{
					dictionary[randomItem.rarity] = 0;
				}
				dictionary[randomItem.rarity]++;
			}
		}
		foreach (KeyValuePair<ItemRarity, int> item in dictionary)
		{
			_ = (float)item.Value / 100f;
		}
	}

	[ContextMenu("Organize Items by Rarity")]
	public void OrganizeItemsByRarity()
	{
		List<WeightedItem> list = new List<WeightedItem>();
		list.AddRange(commonItems);
		list.AddRange(uncommonItems);
		list.AddRange(rareItems);
		list.AddRange(epicItems);
		list.AddRange(legendaryItems);
		commonItems.Clear();
		uncommonItems.Clear();
		rareItems.Clear();
		epicItems.Clear();
		legendaryItems.Clear();
		foreach (WeightedItem item in list)
		{
			if (item.IsValid)
			{
				switch (item.EffectiveRarity)
				{
				case ItemRarity.Common:
					commonItems.Add(item);
					break;
				case ItemRarity.Uncommon:
					uncommonItems.Add(item);
					break;
				case ItemRarity.Rare:
					rareItems.Add(item);
					break;
				case ItemRarity.Epic:
					epicItems.Add(item);
					break;
				case ItemRarity.Legendary:
					legendaryItems.Add(item);
					break;
				}
			}
		}
		InvalidateWeightCache();
	}
}
