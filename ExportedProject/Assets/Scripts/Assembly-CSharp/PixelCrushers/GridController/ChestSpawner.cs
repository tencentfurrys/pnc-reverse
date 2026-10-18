using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PixelCrushers.GridController
{
	public class ChestSpawner : MonoBehaviour
	{
		[Header("Spawn Timing")]
		[Tooltip("If true, this is a level spawner managed by ChestManager. If false, it's an arena reward spawner.")]
		[SerializeField]
		private bool spawnOnStart = true;

		[Header("Spawn Points")]
		[SerializeField]
		private ChestSpawnPoint[] spawnPoints;

		[Header("Mimic Settings")]
		[Tooltip("If false, this spawner's positions will never be assigned a mimic (e.g. reward rooms, safe rooms).")]
		[SerializeField]
		private bool canBeMimic = true;

		[Header("Debug")]
		[SerializeField]
		private bool debugMode;

		[SerializeField]
		private bool showGizmos = true;

		private List<GameObject> spawnedObjects = new List<GameObject>();

		private bool hasSpawned;

		public bool IsLevelSpawner => spawnOnStart;

		public bool CanBeMimic => canBeMimic;

		public bool HasSpawned => hasSpawned;

		public ChestSpawnPoint[] GetSpawnPoints()
		{
			return spawnPoints;
		}

		private void Awake()
		{
			if (spawnOnStart && ChestManager.Instance != null)
			{
				ChestManager.Instance.RegisterSpawner(this);
			}
		}

		private void OnEnable()
		{
			if (spawnOnStart && ChestManager.Instance != null)
			{
				ChestManager.Instance.RegisterSpawner(this);
			}
		}

		private void OnDestroy()
		{
			if (ChestManager.Instance != null)
			{
				ChestManager.Instance.UnregisterSpawner(this);
			}
		}

		public void SpawnFromAllocations(List<SpawnAllocation> allocations)
		{
			if (allocations != null && allocations.Count != 0)
			{
				hasSpawned = true;
				StartCoroutine(SpawnFromAllocationsCoroutine(allocations));
			}
		}

		private IEnumerator SpawnFromAllocationsCoroutine(List<SpawnAllocation> allocations)
		{
			ChestManager manager = ChestManager.Instance;
			if (manager == null)
			{
				yield break;
			}
			foreach (SpawnAllocation allocation in allocations)
			{
				if (!(allocation.position == null))
				{
					if (allocation.isMimic)
					{
						SpawnMimicAtPosition(allocation.position, manager, allocation.itemPool, allocation.chestPrefabOverride);
					}
					else
					{
						SpawnChestAtPosition(allocation.position, manager, allocation.itemPool, allocation.chestPrefabOverride);
					}
					yield return null;
				}
			}
		}

		public void SpawnChests()
		{
			if (!(ChestManager.Instance == null))
			{
				StartCoroutine(SpawnArenaRewardCoroutine());
			}
		}

		private IEnumerator SpawnArenaRewardCoroutine()
		{
			ChestManager manager = ChestManager.Instance;
			if (manager == null || spawnPoints == null || spawnPoints.Length == 0)
			{
				yield break;
			}
			int chestCount = manager.GetArenaRewardChestCount();
			chestCount = Mathf.Min(chestCount, spawnPoints.Length);
			List<ChestSpawnPoint> shuffledPoints = ShuffleList(new List<ChestSpawnPoint>(spawnPoints));
			for (int i = 0; i < chestCount; i++)
			{
				ChestSpawnPoint chestSpawnPoint = shuffledPoints[i];
				if (!(chestSpawnPoint.position == null))
				{
					ChestItemPool pool = ((chestSpawnPoint.itemPoolOverride != null) ? chestSpawnPoint.itemPoolOverride : manager.DefaultItemPool);
					SpawnChestAtPosition(chestSpawnPoint.position, manager, pool, chestSpawnPoint.chestPrefabOverride);
					yield return null;
				}
			}
			hasSpawned = true;
		}

		private void SpawnChestAtPosition(Transform spawnPos, ChestManager manager, ChestItemPool pool, GameObject chestPrefabOverride)
		{
			GameObject gameObject = ((chestPrefabOverride != null) ? chestPrefabOverride : manager.GetChestPrefab());
			if (!(gameObject == null))
			{
				GameObject gameObject2 = Object.Instantiate(gameObject, spawnPos.position, spawnPos.rotation);
				spawnedObjects.Add(gameObject2);
				SimpleItemChest component = gameObject2.GetComponent<SimpleItemChest>();
				if (component != null && pool != null)
				{
					component.SetItemPool(pool);
				}
			}
		}

		private void SpawnMimicAtPosition(Transform spawnPos, ChestManager manager, ChestItemPool pool, GameObject chestPrefabOverride)
		{
			GameObject mimicPrefab = manager.GetMimicPrefab(chestPrefabOverride);
			if (mimicPrefab == null)
			{
				SpawnChestAtPosition(spawnPos, manager, pool, chestPrefabOverride);
				return;
			}
			GameObject item = Object.Instantiate(mimicPrefab, spawnPos.position, spawnPos.rotation);
			spawnedObjects.Add(item);
		}

		private List<T> ShuffleList<T>(List<T> list)
		{
			List<T> list2 = new List<T>(list);
			for (int i = 0; i < list2.Count; i++)
			{
				T value = list2[i];
				int index = Random.Range(i, list2.Count);
				list2[i] = list2[index];
				list2[index] = value;
			}
			return list2;
		}

		public int GetSpawnedCount()
		{
			spawnedObjects.RemoveAll((GameObject obj) => obj == null);
			return spawnedObjects.Count;
		}

		public void ResetAndRespawn()
		{
			foreach (GameObject spawnedObject in spawnedObjects)
			{
				if (spawnedObject != null)
				{
					Object.Destroy(spawnedObject);
				}
			}
			spawnedObjects.Clear();
			hasSpawned = false;
			if (!spawnOnStart)
			{
				SpawnChests();
			}
		}

		private void OnDrawGizmos()
		{
			if (!showGizmos)
			{
				return;
			}
			ChestSpawnPoint[] array;
			if (spawnPoints != null)
			{
				array = spawnPoints;
				foreach (ChestSpawnPoint chestSpawnPoint in array)
				{
					if (chestSpawnPoint != null && chestSpawnPoint.position != null)
					{
						Gizmos.color = (hasSpawned ? Color.green : Color.yellow);
						Gizmos.DrawWireCube(chestSpawnPoint.position.position, new Vector3(0.8f, 0.5f, 0.8f));
						Gizmos.DrawRay(chestSpawnPoint.position.position, Vector3.up * 1f);
					}
				}
			}
			Gizmos.color = Color.white * 0.5f;
			if (spawnPoints == null)
			{
				return;
			}
			array = spawnPoints;
			foreach (ChestSpawnPoint chestSpawnPoint2 in array)
			{
				if (chestSpawnPoint2 != null && chestSpawnPoint2.position != null)
				{
					Gizmos.DrawLine(base.transform.position, chestSpawnPoint2.position.position);
				}
			}
		}

		private void OnDrawGizmosSelected()
		{
		}

		[ContextMenu("Force Spawn")]
		public void ForceSpawn()
		{
			if (Application.isPlaying)
			{
				ResetAndRespawn();
			}
		}
	}
}
