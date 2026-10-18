using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemySpawner : MonoBehaviour
{
	private static readonly Collider[] spawnCheckBuffer = new Collider[16];

	[Header("Enemy Spawn Settings")]
	[SerializeField]
	private EnemyData[] enemyData;

	[SerializeField]
	private Transform[] spawnPositions;

	[SerializeField]
	private int enemiesToSpawn = 3;

	[SerializeField]
	private float spawnDelay = 0.5f;

	[SerializeField]
	private bool spawnRandomEnemies = true;

	[SerializeField]
	private bool oneTimeSpawn = true;

	[Header("Trigger Settings")]
	[SerializeField]
	private Collider[] triggerBoxes;

	[SerializeField]
	private string playerTag = "Player";

	[SerializeField]
	private LayerMask playerLayer = 1;

	[SerializeField]
	private bool destroyTriggersAfterSpawn = true;

	[Header("Spawn Effects")]
	[SerializeField]
	private GameObject spawnEffect;

	[SerializeField]
	private AudioClip spawnSound;

	[SerializeField]
	private AudioSource audioSource;

	[SerializeField]
	private float effectDuration = 2f;

	[Header("Advanced Settings")]
	[SerializeField]
	private bool checkLineOfSight;

	[SerializeField]
	private LayerMask obstacleLayer = 1;

	[SerializeField]
	private float minDistanceFromPlayer = 3f;

	[SerializeField]
	private bool avoidPlayerProximity = true;

	[SerializeField]
	private bool randomizeSpawnOrder = true;

	[Header("Object Pooling")]
	[SerializeField]
	private bool useObjectPooling = true;

	[SerializeField]
	private bool preloadEnemies = true;

	[SerializeField]
	private int poolSizePerPrefab = 10;

	[Header("Debug")]
	[SerializeField]
	private bool debugMode = true;

	[SerializeField]
	private bool showGizmos = true;

	private bool hasSpawned;

	private List<GameObject> spawnedEnemies = new List<GameObject>();

	private bool playerInAnyTrigger;

	public Transform player;

	private Dictionary<GameObject, Queue<GameObject>> enemyPools = new Dictionary<GameObject, Queue<GameObject>>();

	private Transform poolParent;

	public IReadOnlyList<Transform> SpawnPositions => spawnPositions;

	private void Awake()
	{
		ValidateSetup();
		SetupAudioSource();
		SetupTriggers();
		FindPlayer();
		SetupObjectPooling();
	}

	private void Start()
	{
		if (useObjectPooling && preloadEnemies)
		{
			StartCoroutine(PreloadEnemiesCoroutine());
		}
	}

	private void FindPlayer()
	{
		if (player == null)
		{
			GameObject gameObject = GameObject.FindGameObjectWithTag(playerTag);
			if (gameObject != null)
			{
				player = gameObject.transform;
			}
		}
	}

	private void ValidateSetup()
	{
		if (enemyData != null && enemyData.Length != 0 && spawnPositions != null && spawnPositions.Length != 0 && triggerBoxes != null && triggerBoxes.Length != 0)
		{
			if (enemiesToSpawn <= 0)
			{
				enemiesToSpawn = 1;
			}
			if (enemiesToSpawn > spawnPositions.Length)
			{
				enemiesToSpawn = spawnPositions.Length;
			}
		}
	}

	private void SetupObjectPooling()
	{
		if (!useObjectPooling)
		{
			return;
		}
		GameObject gameObject = new GameObject(base.gameObject.name + "_EnemyPool");
		poolParent = gameObject.transform;
		if (this.enemyData == null)
		{
			return;
		}
		EnemyData[] array = this.enemyData;
		foreach (EnemyData enemyData in array)
		{
			if (enemyData == null || enemyData.prefabVariants == null)
			{
				continue;
			}
			GameObject[] prefabVariants = enemyData.prefabVariants;
			foreach (GameObject gameObject2 in prefabVariants)
			{
				if (gameObject2 != null)
				{
					enemyPools[gameObject2] = new Queue<GameObject>();
				}
			}
		}
	}

	private IEnumerator PreloadEnemiesCoroutine()
	{
		EnemyData[] array = this.enemyData;
		foreach (EnemyData enemyData in array)
		{
			if (enemyData == null || enemyData.prefabVariants == null)
			{
				continue;
			}
			GameObject[] prefabVariants = enemyData.prefabVariants;
			foreach (GameObject prefab in prefabVariants)
			{
				if (prefab == null)
				{
					continue;
				}
				Queue<GameObject> pool = enemyPools[prefab];
				for (int k = 0; k < poolSizePerPrefab; k++)
				{
					GameObject item = CreatePooledEnemy(prefab);
					pool.Enqueue(item);
					if (k % 2 == 0)
					{
						yield return null;
					}
				}
			}
		}
	}

	private GameObject CreatePooledEnemy(GameObject prefab)
	{
		GameObject gameObject = Object.Instantiate(prefab, poolParent);
		gameObject.SetActive(value: false);
		return gameObject;
	}

	private void SetupAudioSource()
	{
		if (audioSource == null)
		{
			audioSource = GetComponent<AudioSource>();
		}
		if (audioSource == null)
		{
			audioSource = gameObject.AddComponent<AudioSource>();
		}
		audioSource.playOnAwake = false;
		audioSource.spatialBlend = 1f;
	}

	private void SetupTriggers()
	{
		Collider[] array = triggerBoxes;
		foreach (Collider collider in array)
		{
			if (collider != null)
			{
				collider.isTrigger = true;
				EnemySpawnerTriggerHandler enemySpawnerTriggerHandler = collider.GetComponent<EnemySpawnerTriggerHandler>();
				if (enemySpawnerTriggerHandler == null)
				{
					enemySpawnerTriggerHandler = collider.gameObject.AddComponent<EnemySpawnerTriggerHandler>();
				}
				enemySpawnerTriggerHandler.Initialize(this);
			}
		}
	}

	public void OnPlayerEnterTrigger(Collider trigger)
	{
		if (!oneTimeSpawn || !hasSpawned)
		{
			playerInAnyTrigger = true;
			StartCoroutine(DelayedSpawning());
		}
	}

	private IEnumerator DelayedSpawning()
	{
		yield return new WaitForEndOfFrame();
		StartSpawning();
	}

	public void OnPlayerExitTrigger(Collider trigger)
	{
		playerInAnyTrigger = false;
		Collider[] array = triggerBoxes;
		foreach (Collider collider in array)
		{
			if (collider != null && IsPlayerInTrigger(collider))
			{
				playerInAnyTrigger = true;
				break;
			}
		}
	}

	private bool IsPlayerInTrigger(Collider triggerBox)
	{
		if (player == null)
		{
			return false;
		}
		Bounds playerBounds = GetPlayerBounds();
		return triggerBox.bounds.Intersects(playerBounds);
	}

	private Bounds GetPlayerBounds()
	{
		if (player == null)
		{
			return new Bounds(Vector3.zero, Vector3.one);
		}
		Collider component = player.GetComponent<Collider>();
		if (component != null)
		{
			return component.bounds;
		}
		return new Bounds(player.position, Vector3.one);
	}

	private void StartSpawning()
	{
		if (!oneTimeSpawn || !hasSpawned)
		{
			hasSpawned = true;
			StartCoroutine(SpawnEnemiesCoroutine());
		}
	}

	private IEnumerator SpawnEnemiesCoroutine()
	{
		yield return null;
		List<Transform> availablePositions = GetAvailableSpawnPositions();
		if (availablePositions.Count == 0)
		{
			yield break;
		}
		yield return null;
		if (randomizeSpawnOrder)
		{
			availablePositions = ShuffleList(availablePositions);
		}
		int spawnCount = availablePositions.Count;
		for (int i = 0; i < spawnCount; i++)
		{
			Transform transform = availablePositions[i];
			GameObject enemyPrefab = GetEnemyPrefab();
			if (enemyPrefab != null && transform != null)
			{
				yield return StartCoroutine(SpawnSingleEnemy(enemyPrefab, transform));
				yield return new WaitForSeconds(Mathf.Max(spawnDelay, 0.1f));
			}
		}
		if (destroyTriggersAfterSpawn)
		{
			yield return new WaitForSeconds(0.5f);
			DestroyTriggers();
		}
	}

	private List<Transform> GetAvailableSpawnPositions()
	{
		List<Transform> list = new List<Transform>();
		Transform[] array = spawnPositions;
		foreach (Transform transform in array)
		{
			if (transform == null || !transform.gameObject.activeInHierarchy)
			{
				continue;
			}
			bool flag = true;
			if (avoidPlayerProximity && player != null && Vector3.Distance(transform.position, player.position) < minDistanceFromPlayer)
			{
				flag = false;
			}
			if (flag && checkLineOfSight && player != null && HasLineOfSightToPlayer(transform.position))
			{
				flag = false;
			}
			if (flag)
			{
				int num = Physics.OverlapSphereNonAlloc(transform.position, 1f, spawnCheckBuffer, -1, QueryTriggerInteraction.Ignore);
				for (int j = 0; j < num; j++)
				{
					Collider collider = spawnCheckBuffer[j];
					if (collider.GetComponent<NavMeshAgent>() != null || spawnedEnemies.Contains(collider.gameObject))
					{
						flag = false;
						break;
					}
				}
			}
			if (flag)
			{
				list.Add(transform);
			}
		}
		return list;
	}

	private bool HasLineOfSightToPlayer(Vector3 fromPosition)
	{
		if (player == null)
		{
			return false;
		}
		Vector3 vector = player.position + Vector3.up * 1f;
		Vector3 vector2 = fromPosition + Vector3.up * 1f;
		Vector3 normalized = (vector - vector2).normalized;
		float maxDistance = Vector3.Distance(vector2, vector);
		return !Physics.Raycast(vector2, normalized, maxDistance, obstacleLayer);
	}

	private GameObject GetEnemyPrefab()
	{
		if (this.enemyData == null || this.enemyData.Length == 0)
		{
			return null;
		}
		EnemyData enemyData = ((!spawnRandomEnemies) ? this.enemyData[spawnedEnemies.Count % this.enemyData.Length] : this.enemyData[Random.Range(0, this.enemyData.Length)]);
		if (!(enemyData != null))
		{
			return null;
		}
		return enemyData.GetRandomPrefab();
	}

	private IEnumerator SpawnSingleEnemy(GameObject prefab, Transform spawnPosition)
	{
		if (spawnEffect != null)
		{
			Object.Destroy(Object.Instantiate(spawnEffect, spawnPosition.position, spawnPosition.rotation), effectDuration);
		}
		if (spawnSound != null && audioSource != null)
		{
			audioSource.PlayOneShot(spawnSound);
		}
		yield return new WaitForSeconds(0.1f);
		GameObject gameObject = GetPooledEnemy(prefab);
		if (gameObject == null)
		{
			gameObject = Object.Instantiate(prefab, spawnPosition.position, spawnPosition.rotation);
		}
		else
		{
			gameObject.transform.SetParent(null);
			gameObject.transform.position = spawnPosition.position;
			gameObject.transform.rotation = spawnPosition.rotation;
			gameObject.SetActive(value: true);
			ResetEnemyState(gameObject);
		}
		spawnedEnemies.Add(gameObject);
	}

	private GameObject GetPooledEnemy(GameObject prefab)
	{
		if (!useObjectPooling || !enemyPools.ContainsKey(prefab))
		{
			return null;
		}
		Queue<GameObject> queue = enemyPools[prefab];
		if (queue.Count > 0)
		{
			return queue.Dequeue();
		}
		return null;
	}

	private void ResetEnemyState(GameObject enemy)
	{
		IPoolResettable[] components = enemy.GetComponents<IPoolResettable>();
		for (int i = 0; i < components.Length; i++)
		{
			components[i].PoolReset();
		}
	}

	public void ReturnEnemyToPool(GameObject enemy)
	{
		if (!useObjectPooling)
		{
			Object.Destroy(enemy);
			return;
		}
		GameObject gameObject = null;
		string text = enemy.name.Replace("(Clone)", "");
		EnemyData[] array = this.enemyData;
		foreach (EnemyData enemyData in array)
		{
			if (enemyData == null || enemyData.prefabVariants == null)
			{
				continue;
			}
			GameObject[] prefabVariants = enemyData.prefabVariants;
			foreach (GameObject gameObject2 in prefabVariants)
			{
				if (gameObject2 != null && gameObject2.name == text)
				{
					gameObject = gameObject2;
					break;
				}
			}
			if (gameObject != null)
			{
				break;
			}
		}
		if (gameObject != null && enemyPools.ContainsKey(gameObject))
		{
			enemy.SetActive(value: false);
			enemy.transform.SetParent(poolParent);
			enemyPools[gameObject].Enqueue(enemy);
			spawnedEnemies.Remove(enemy);
		}
		else
		{
			Object.Destroy(enemy);
		}
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

	public void ResetSpawner()
	{
		hasSpawned = false;
		foreach (GameObject spawnedEnemy in spawnedEnemies)
		{
			if (spawnedEnemy != null)
			{
				if (useObjectPooling)
				{
					ReturnEnemyToPool(spawnedEnemy);
				}
				else
				{
					Object.Destroy(spawnedEnemy);
				}
			}
		}
		spawnedEnemies.Clear();
		if (!destroyTriggersAfterSpawn || triggerBoxes != null)
		{
			return;
		}
		EnemySpawnerTriggerHandler[] array = Object.FindObjectsByType<EnemySpawnerTriggerHandler>(FindObjectsSortMode.None);
		List<Collider> list = new List<Collider>();
		EnemySpawnerTriggerHandler[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			Collider component = array2[i].GetComponent<Collider>();
			if (component != null)
			{
				list.Add(component);
			}
		}
		if (list.Count > 0)
		{
			triggerBoxes = list.ToArray();
			SetupTriggers();
		}
	}

	public void ForceSpawn()
	{
		StartSpawning();
	}

	public int GetSpawnedEnemyCount()
	{
		spawnedEnemies.RemoveAll((GameObject enemy) => enemy == null);
		return spawnedEnemies.Count;
	}

	public bool HasSpawned()
	{
		return hasSpawned;
	}

	public bool IsPlayerInAnyTrigger()
	{
		return playerInAnyTrigger;
	}

	private void DestroyTriggers()
	{
		if (triggerBoxes == null)
		{
			return;
		}
		Collider[] array = triggerBoxes;
		foreach (Collider collider in array)
		{
			if (collider != null)
			{
				Object.Destroy(collider.gameObject);
			}
		}
		triggerBoxes = null;
	}

	private void OnDrawGizmos()
	{
		if (!showGizmos)
		{
			return;
		}
		if (triggerBoxes != null)
		{
			Collider[] array = triggerBoxes;
			foreach (Collider collider in array)
			{
				if (collider != null)
				{
					Gizmos.color = (playerInAnyTrigger ? Color.green : Color.yellow);
					Gizmos.matrix = collider.transform.localToWorldMatrix;
					if (collider is BoxCollider boxCollider)
					{
						Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
					}
					else if (collider is SphereCollider sphereCollider)
					{
						Gizmos.DrawWireSphere(sphereCollider.center, sphereCollider.radius);
					}
				}
			}
		}
		if (spawnPositions != null)
		{
			Transform[] array2 = spawnPositions;
			foreach (Transform transform in array2)
			{
				if (transform != null)
				{
					Gizmos.color = (hasSpawned ? Color.red : Color.cyan);
					Gizmos.DrawWireSphere(transform.position, 0.5f);
					Gizmos.DrawRay(transform.position, Vector3.up * 2f);
					if (avoidPlayerProximity)
					{
						Gizmos.color = Color.red * 0.3f;
						Gizmos.DrawWireSphere(transform.position, minDistanceFromPlayer);
					}
				}
			}
		}
		Gizmos.color = Color.white;
		if (spawnPositions != null)
		{
			Transform[] array2 = spawnPositions;
			foreach (Transform transform2 in array2)
			{
				if (transform2 != null)
				{
					Gizmos.DrawLine(base.transform.position, transform2.position);
				}
			}
		}
		if (triggerBoxes != null)
		{
			Collider[] array = triggerBoxes;
			foreach (Collider collider2 in array)
			{
				if (collider2 != null)
				{
					Gizmos.DrawLine(base.transform.position, collider2.transform.position);
				}
			}
		}
		_ = Application.isPlaying;
	}

	private void OnDestroy()
	{
		if (poolParent != null)
		{
			Object.Destroy(poolParent.gameObject);
		}
	}

	private void OnValidate()
	{
		if (enemiesToSpawn < 0)
		{
			enemiesToSpawn = 1;
		}
		if (spawnDelay < 0f)
		{
			spawnDelay = 0f;
		}
		if (minDistanceFromPlayer < 0f)
		{
			minDistanceFromPlayer = 0f;
		}
		if (effectDuration <= 0f)
		{
			effectDuration = 1f;
		}
	}
}
