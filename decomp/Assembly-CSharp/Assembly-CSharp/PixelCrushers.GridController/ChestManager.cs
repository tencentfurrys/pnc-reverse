using System.Collections.Generic;
using UnityEngine;

namespace PixelCrushers.GridController;

public class ChestManager : MonoBehaviour
{
	private class SpawnSlot
	{
		public Transform position;

		public ChestSpawner owningSpawner;

		public ChestItemPool poolOverride;

		public GameObject chestPrefabOverride;

		public bool isMimic;

		public bool canBeMimic;
	}

	[Header("Level-Wide Chest Settings")]
	[SerializeField]
	[Range(0f, 20f)]
	private int minChestsInLevel = 3;

	[SerializeField]
	[Range(0f, 20f)]
	private int maxChestsInLevel = 5;

	[Header("Level-Wide Mimic Settings")]
	[SerializeField]
	[Range(0f, 10f)]
	private int minMimicsInLevel = 1;

	[SerializeField]
	[Range(0f, 10f)]
	private int maxMimicsInLevel = 2;

	[Header("Arena Reward Settings")]
	[SerializeField]
	[Range(0f, 10f)]
	private int minChestsPerArenaReward = 1;

	[SerializeField]
	[Range(0f, 10f)]
	private int maxChestsPerArenaReward = 3;

	[Header("Prefabs")]
	[SerializeField]
	private GameObject chestPrefab;

	[SerializeField]
	private GameObject mimicPrefab;

	[Tooltip("Maps a specific chest prefab to its mimic variant. Chests not listed here will skip the mimic roll and spawn a regular chest.")]
	[SerializeField]
	private ChestMimicPair[] chestMimicVariants;

	[SerializeField]
	private ChestItemPool defaultItemPool;

	[Header("Debug")]
	[SerializeField]
	private bool debugMode;

	private int mimicsSpawnedTotal;

	private List<ChestSpawner> registeredSpawners = new List<ChestSpawner>();

	private bool hasAllocated;

	public static ChestManager Instance { get; private set; }

	public int MimicsSpawnedTotal => mimicsSpawnedTotal;

	public ChestItemPool DefaultItemPool => defaultItemPool;

	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Object.Destroy(gameObject);
			return;
		}
		Instance = this;
		Object.DontDestroyOnLoad(gameObject);
	}

	private void OnDestroy()
	{
		if (Instance == this)
		{
			Instance = null;
		}
	}

	public void RegisterSpawner(ChestSpawner spawner)
	{
		if (!(spawner == null) && !registeredSpawners.Contains(spawner))
		{
			registeredSpawners.Add(spawner);
		}
	}

	public void UnregisterSpawner(ChestSpawner spawner)
	{
		if (!(spawner == null))
		{
			registeredSpawners.Remove(spawner);
		}
	}

	public void AllocateChestsAcrossLevel()
	{
		if (hasAllocated)
		{
			return;
		}
		hasAllocated = true;
		ChestSpawner[] array = Object.FindObjectsByType<ChestSpawner>(FindObjectsSortMode.None);
		foreach (ChestSpawner chestSpawner in array)
		{
			if (chestSpawner.IsLevelSpawner && !registeredSpawners.Contains(chestSpawner))
			{
				registeredSpawners.Add(chestSpawner);
			}
		}
		List<ChestSpawner> list = new List<ChestSpawner>();
		foreach (ChestSpawner registeredSpawner in registeredSpawners)
		{
			if (registeredSpawner != null && registeredSpawner.IsLevelSpawner)
			{
				list.Add(registeredSpawner);
			}
		}
		if (list.Count == 0)
		{
			return;
		}
		List<SpawnSlot> list2 = new List<SpawnSlot>();
		foreach (ChestSpawner item in list)
		{
			ChestSpawnPoint[] spawnPoints = item.GetSpawnPoints();
			if (spawnPoints == null)
			{
				continue;
			}
			ChestSpawnPoint[] array2 = spawnPoints;
			foreach (ChestSpawnPoint chestSpawnPoint in array2)
			{
				if (chestSpawnPoint != null && !(chestSpawnPoint.position == null))
				{
					list2.Add(new SpawnSlot
					{
						position = chestSpawnPoint.position,
						owningSpawner = item,
						poolOverride = chestSpawnPoint.itemPoolOverride,
						chestPrefabOverride = chestSpawnPoint.chestPrefabOverride,
						isMimic = false,
						canBeMimic = item.CanBeMimic
					});
				}
			}
		}
		if (list2.Count == 0)
		{
			return;
		}
		int a = Random.Range(minChestsInLevel, maxChestsInLevel + 1);
		a = Mathf.Min(a, list2.Count);
		int a2 = Random.Range(minMimicsInLevel, maxMimicsInLevel + 1);
		a2 = Mathf.Min(a2, a);
		ShuffleList(list2);
		List<SpawnSlot> range = list2.GetRange(0, a);
		List<SpawnSlot> list3 = new List<SpawnSlot>();
		List<SpawnSlot> list4 = new List<SpawnSlot>();
		foreach (SpawnSlot item2 in range)
		{
			if (item2.canBeMimic)
			{
				list3.Add(item2);
			}
			else
			{
				list4.Add(item2);
			}
		}
		int num = Mathf.Min(a2, list3.Count);
		ShuffleList(list3);
		for (int j = 0; j < num; j++)
		{
			list3[j].isMimic = true;
		}
		a2 = num;
		range.Clear();
		range.AddRange(list3);
		range.AddRange(list4);
		ShuffleList(range);
		Dictionary<ChestSpawner, List<SpawnAllocation>> dictionary = new Dictionary<ChestSpawner, List<SpawnAllocation>>();
		foreach (SpawnSlot item3 in range)
		{
			if (!dictionary.ContainsKey(item3.owningSpawner))
			{
				dictionary[item3.owningSpawner] = new List<SpawnAllocation>();
			}
			ChestItemPool itemPool = ((item3.poolOverride != null) ? item3.poolOverride : defaultItemPool);
			dictionary[item3.owningSpawner].Add(new SpawnAllocation
			{
				position = item3.position,
				isMimic = item3.isMimic,
				itemPool = itemPool,
				chestPrefabOverride = item3.chestPrefabOverride
			});
		}
		foreach (KeyValuePair<ChestSpawner, List<SpawnAllocation>> item4 in dictionary)
		{
			item4.Key.SpawnFromAllocations(item4.Value);
		}
		mimicsSpawnedTotal += a2;
	}

	private void ShuffleList<T>(List<T> list)
	{
		for (int i = 0; i < list.Count; i++)
		{
			T value = list[i];
			int index = Random.Range(i, list.Count);
			list[i] = list[index];
			list[index] = value;
		}
	}

	public GameObject GetChestPrefab()
	{
		return chestPrefab;
	}

	public GameObject GetMimicPrefab(GameObject chestPrefabOverride)
	{
		if (chestPrefabOverride == null)
		{
			return mimicPrefab;
		}
		if (chestMimicVariants != null)
		{
			ChestMimicPair[] array = chestMimicVariants;
			for (int i = 0; i < array.Length; i++)
			{
				ChestMimicPair chestMimicPair = array[i];
				if (chestMimicPair.chestPrefab == chestPrefabOverride)
				{
					return chestMimicPair.mimicPrefab;
				}
			}
		}
		return null;
	}

	public int GetArenaRewardChestCount()
	{
		return Random.Range(minChestsPerArenaReward, maxChestsPerArenaReward + 1);
	}

	public void ResetForNewLevel()
	{
		mimicsSpawnedTotal = 0;
		hasAllocated = false;
		registeredSpawners.Clear();
	}

	private void OnValidate()
	{
		if (minChestsInLevel < 0)
		{
			minChestsInLevel = 0;
		}
		if (maxChestsInLevel < minChestsInLevel)
		{
			maxChestsInLevel = minChestsInLevel;
		}
		if (minMimicsInLevel < 0)
		{
			minMimicsInLevel = 0;
		}
		if (maxMimicsInLevel < minMimicsInLevel)
		{
			maxMimicsInLevel = minMimicsInLevel;
		}
		if (minChestsPerArenaReward < 0)
		{
			minChestsPerArenaReward = 0;
		}
		if (maxChestsPerArenaReward < minChestsPerArenaReward)
		{
			maxChestsPerArenaReward = minChestsPerArenaReward;
		}
	}

	private void OnDrawGizmosSelected()
	{
	}
}
