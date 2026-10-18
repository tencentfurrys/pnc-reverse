using System;
using UnityEngine;

[Serializable]
public class ArenaSpawnEntry
{
	[Tooltip("Which enemy to spawn. A random prefab variant is rolled unless Prefab Override is set.")]
	public EnemyData enemy;

	[Tooltip("ID of the ArenaSpawnPoint this enemy spawns on.")]
	public string spawnPointId;

	[Tooltip("Optional: pin one exact prefab variant instead of rolling from the EnemyData.")]
	public GameObject prefabOverride;

	public bool IsValid
	{
		get
		{
			if (!string.IsNullOrWhiteSpace(spawnPointId))
			{
				if (!(prefabOverride != null))
				{
					return enemy != null;
				}
				return true;
			}
			return false;
		}
	}

	public string DisplayName
	{
		get
		{
			if (prefabOverride != null)
			{
				return prefabOverride.name;
			}
			if (!(enemy != null))
			{
				return "<empty>";
			}
			return enemy.enemyName;
		}
	}

	public GameObject ResolvePrefab()
	{
		if (prefabOverride != null)
		{
			return prefabOverride;
		}
		if (!(enemy != null))
		{
			return null;
		}
		return enemy.GetRandomPrefab();
	}
}
