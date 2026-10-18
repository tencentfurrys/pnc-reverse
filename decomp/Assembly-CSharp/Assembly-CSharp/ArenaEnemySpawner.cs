using System;
using System.Collections;
using System.Collections.Generic;
using PixelCrushers.GridController;
using UnityEngine;

public class ArenaEnemySpawner : MonoBehaviour
{
	private static readonly Collider[] spawnCheckBuffer = new Collider[16];

	[Header("Arena Door Settings")]
	[SerializeField]
	private Animator[] doorAnimators;

	[SerializeField]
	private string doorCloseAnimation = "Close";

	[SerializeField]
	private string doorOpenAnimation = "Open";

	[SerializeField]
	private AudioClip doorCloseSound;

	[SerializeField]
	private AudioClip doorOpenSound;

	[Header("Dragon Freeze Settings")]
	[SerializeField]
	private bool freezeDragonsUntilCleared = true;

	[Header("Enemy Spawn Settings")]
	[SerializeField]
	private EnemyData[] enemyData;

	[SerializeField]
	private Transform[] spawnPositions;

	[SerializeField]
	private float spawnDelay = 0.5f;

	[SerializeField]
	private bool spawnRandomEnemies = true;

	[Header("Spawn Groups")]
	[Tooltip("Authored encounters. One is picked by weight when the arena activates. If this is empty, falls back to the legacy one-random-enemy-per-spawn-point behaviour.")]
	[SerializeField]
	private WeightedSpawnGroup[] spawnGroups;

	[Header("Trigger Settings")]
	[SerializeField]
	private Collider[] triggerBoxes;

	[SerializeField]
	private string playerTag = "Player";

	[SerializeField]
	private bool destroyTriggersAfterActivation = true;

	[Header("Spawn Effects")]
	[SerializeField]
	private GameObject spawnEffect;

	[SerializeField]
	private AudioClip spawnSound;

	[SerializeField]
	private AudioSource audioSource;

	[SerializeField]
	private float effectDuration = 2f;

	[Header("Room Clear Rewards")]
	[SerializeField]
	private ChestSpawner chestSpawner;

	[Header("Arena Music")]
	[SerializeField]
	private AudioClip arenaMusic;

	[SerializeField]
	private AudioSource musicSource;

	[SerializeField]
	private float musicFadeDuration = 1.5f;

	[Header("Room Clear Effects")]
	[SerializeField]
	private AudioClip roomClearSound;

	[SerializeField]
	private AudioSource roomClearAudioSource;

	[SerializeField]
	private GameObject roomClearEffect;

	[Header("Advanced Settings")]
	[SerializeField]
	private float minDistanceFromPlayer = 3f;

	[SerializeField]
	private bool avoidPlayerProximity = true;

	[SerializeField]
	private bool randomizeSpawnOrder = true;

	[Header("Debug")]
	[SerializeField]
	private bool debugMode = true;

	[SerializeField]
	private bool showGizmos = true;

	[Tooltip("Which spawn group to preview in the Scene view. Set to -1 to preview none.")]
	[SerializeField]
	private int gizmoPreviewGroup;

	[Header("Panic-Teleport Hook")]
	[Tooltip("Radius around each spawn position counted as 'inside this arena' by ContainsPoint. Matches the existing IsDragonInArena heuristic.")]
	[SerializeField]
	private float containsPointRadius = 20f;

	private bool isLocked;

	private bool hasActivated;

	private bool hasSpawnedAllEnemies;

	private List<GameObject> spawnedEnemies = new List<GameObject>();

	private List<DragonEnemyAI> frozenDragons = new List<DragonEnemyAI>();

	private List<ProximityDragonEnemyAI> frozenProximityDragons = new List<ProximityDragonEnemyAI>();

	private Transform player;

	private string activeGroupName;

	private ArenaSpawnGroup selectedGroup;

	private Dictionary<string, Transform> spawnPointsById;

	public bool IsRoomLocked => isLocked;

	public bool HasActivated => hasActivated;

	public string ActiveGroupName => activeGroupName;

	public IReadOnlyList<Transform> SpawnPositions => spawnPositions;

	private AudioClip ActiveArenaMusic
	{
		get
		{
			if (!(selectedGroup != null) || !(selectedGroup.arenaMusic != null))
			{
				return arenaMusic;
			}
			return selectedGroup.arenaMusic;
		}
	}

	private AudioClip ActiveExitSting
	{
		get
		{
			if (!(selectedGroup != null) || !(selectedGroup.exitSting != null))
			{
				return roomClearSound;
			}
			return selectedGroup.exitSting;
		}
	}

	public bool ContainsPoint(Vector3 point)
	{
		if (spawnPositions == null)
		{
			return false;
		}
		float num = containsPointRadius * containsPointRadius;
		for (int i = 0; i < spawnPositions.Length; i++)
		{
			Transform transform = spawnPositions[i];
			if (!(transform == null) && (transform.position - point).sqrMagnitude <= num)
			{
				return true;
			}
		}
		return false;
	}

	private void Awake()
	{
		BuildSpawnPointMap();
		ValidateSetup();
		SetupAudioSource();
		SetupTriggers();
		FindPlayer();
	}

	private void BuildSpawnPointMap()
	{
		spawnPointsById = new Dictionary<string, Transform>(StringComparer.OrdinalIgnoreCase);
		ArenaSpawnPoint[] componentsInChildren = GetComponentsInChildren<ArenaSpawnPoint>(includeInactive: true);
		foreach (ArenaSpawnPoint arenaSpawnPoint in componentsInChildren)
		{
			if (!arenaSpawnPoint.HasId)
			{
				if (debugMode)
				{
					Debug.LogWarning("[ArenaEnemySpawner] '" + name + "' has a spawn point ('" + arenaSpawnPoint.name + "') with a blank ID. Groups can't reference it.", arenaSpawnPoint);
				}
			}
			else if (spawnPointsById.ContainsKey(arenaSpawnPoint.id))
			{
				if (debugMode)
				{
					Debug.LogWarning("[ArenaEnemySpawner] '" + name + "' has duplicate spawn point ID '" + arenaSpawnPoint.id + "'. Keeping the first; '" + arenaSpawnPoint.name + "' is unreachable.", arenaSpawnPoint);
				}
			}
			else
			{
				spawnPointsById[arenaSpawnPoint.id] = arenaSpawnPoint.transform;
			}
		}
	}

	private void Update()
	{
		if (isLocked && hasSpawnedAllEnemies && AreAllEnemiesDead())
		{
			OpenRoom();
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
		ValidateSpawnGroups();
	}

	private void ValidateSpawnGroups()
	{
		if (!debugMode)
		{
			return;
		}
		ArenaSpawnPoint[] componentsInChildren = GetComponentsInChildren<ArenaSpawnPoint>(includeInactive: true);
		foreach (ArenaSpawnPoint arenaSpawnPoint in componentsInChildren)
		{
			if (arenaSpawnPoint.HasId && !IsRegisteredSpawnPosition(arenaSpawnPoint.transform))
			{
				Debug.LogWarning("[ArenaEnemySpawner] '" + name + "' spawn point '" + arenaSpawnPoint.id + "' is not listed in Spawn Positions. Groups can still use it, but ContainsPoint and the legacy random path won't see it.", arenaSpawnPoint);
			}
		}
		if (spawnGroups == null)
		{
			return;
		}
		for (int j = 0; j < spawnGroups.Length; j++)
		{
			WeightedSpawnGroup weightedSpawnGroup = spawnGroups[j];
			if (weightedSpawnGroup == null)
			{
				continue;
			}
			if (weightedSpawnGroup.group == null)
			{
				Debug.LogWarning($"[ArenaEnemySpawner] '{name}' spawn group slot {j} has no group asset assigned.", this);
				continue;
			}
			ArenaSpawnGroup arenaSpawnGroup = weightedSpawnGroup.group;
			if (arenaSpawnGroup.entries == null)
			{
				continue;
			}
			for (int k = 0; k < arenaSpawnGroup.entries.Length; k++)
			{
				ArenaSpawnEntry arenaSpawnEntry = arenaSpawnGroup.entries[k];
				string text = $"[ArenaEnemySpawner] '{name}' group '{arenaSpawnGroup.DisplayName}' entry {k}";
				if (arenaSpawnEntry == null || !arenaSpawnEntry.IsValid)
				{
					Debug.LogWarning(text + " is incomplete (needs a spawn point ID plus an EnemyData or prefab override) and will be skipped.", this);
				}
				else if (!spawnPointsById.ContainsKey(arenaSpawnEntry.spawnPointId))
				{
					Debug.LogWarning(text + " references spawn point '" + arenaSpawnEntry.spawnPointId + "', which doesn't exist in this arena. It will be skipped.", this);
				}
			}
		}
	}

	private bool IsRegisteredSpawnPosition(Transform point)
	{
		if (spawnPositions == null || point == null)
		{
			return false;
		}
		for (int i = 0; i < spawnPositions.Length; i++)
		{
			if (spawnPositions[i] == point)
			{
				return true;
			}
		}
		return false;
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
		if (roomClearAudioSource != null)
		{
			roomClearAudioSource.spatialBlend = 0f;
		}
	}

	private void SetupTriggers()
	{
		if (triggerBoxes == null || triggerBoxes.Length == 0)
		{
			return;
		}
		Collider[] array = triggerBoxes;
		foreach (Collider collider in array)
		{
			if (collider != null)
			{
				collider.isTrigger = true;
				ArenaSpawnerTriggerHandler arenaSpawnerTriggerHandler = collider.GetComponent<ArenaSpawnerTriggerHandler>();
				if (arenaSpawnerTriggerHandler == null)
				{
					arenaSpawnerTriggerHandler = collider.gameObject.AddComponent<ArenaSpawnerTriggerHandler>();
				}
				arenaSpawnerTriggerHandler.Initialize(this);
			}
		}
	}

	public void OnPlayerEnterTrigger(Collider trigger)
	{
		if (!hasActivated)
		{
			ActivateArena();
			if (destroyTriggersAfterActivation)
			{
				StartCoroutine(DestroyTriggersDelayed());
			}
		}
	}

	public void ActivateArena()
	{
		if (!hasActivated)
		{
			hasActivated = true;
			StartCoroutine(ActivateArenaSequence());
		}
	}

	private IEnumerator ActivateArenaSequence()
	{
		isLocked = true;
		selectedGroup = SelectSpawnGroup();
		activeGroupName = ((selectedGroup != null) ? selectedGroup.DisplayName : "(legacy random)");
		if (doorAnimators != null)
		{
			Animator[] array = doorAnimators;
			foreach (Animator animator in array)
			{
				if (animator != null)
				{
					animator.Play(doorCloseAnimation);
				}
			}
		}
		if (doorCloseSound != null && roomClearAudioSource != null)
		{
			roomClearAudioSource.PlayOneShot(doorCloseSound);
			yield return new WaitForSeconds(doorCloseSound.length);
		}
		if (freezeDragonsUntilCleared)
		{
			FreezeExistingDragons();
		}
		AudioClip activeArenaMusic = ActiveArenaMusic;
		if (activeArenaMusic != null && musicSource != null)
		{
			musicSource.clip = activeArenaMusic;
			musicSource.loop = true;
			musicSource.Play();
		}
		yield return new WaitForSeconds(3f);
		yield return StartCoroutine(SpawnEnemiesCoroutine());
	}

	private void OpenRoom()
	{
		if (isLocked)
		{
			isLocked = false;
			StartCoroutine(OpenRoomSequence());
		}
	}

	private IEnumerator OpenRoomSequence()
	{
		if (musicSource != null && musicSource.isPlaying)
		{
			musicSource.Stop();
		}
		AudioClip activeExitSting = ActiveExitSting;
		PlayRoomClearEffects(activeExitSting);
		if (activeExitSting != null)
		{
			yield return new WaitForSeconds(activeExitSting.length);
		}
		if (doorAnimators != null)
		{
			Animator[] array = doorAnimators;
			foreach (Animator animator in array)
			{
				if (animator != null)
				{
					animator.Play(doorOpenAnimation);
				}
			}
		}
		if (doorOpenSound != null && roomClearAudioSource != null)
		{
			roomClearAudioSource.PlayOneShot(doorOpenSound);
		}
		UnfreezeAllDragons();
		if (chestSpawner != null)
		{
			chestSpawner.SpawnChests();
		}
	}

	private void FreezeExistingDragons()
	{
		foreach (DragonEnemyAI activeDragon in DragonEnemyAI.ActiveDragons)
		{
			if (activeDragon != null && !activeDragon.IsDead && IsDragonInArena(activeDragon.transform.position))
			{
				activeDragon.frozenByArena = true;
				frozenDragons.Add(activeDragon);
			}
		}
		foreach (ProximityDragonEnemyAI activeDragon2 in ProximityDragonEnemyAI.ActiveDragons)
		{
			if (activeDragon2 != null && !activeDragon2.IsDead && IsDragonInArena(activeDragon2.transform.position))
			{
				activeDragon2.frozenByArena = true;
				frozenProximityDragons.Add(activeDragon2);
			}
		}
	}

	private bool IsDragonInArena(Vector3 position)
	{
		float num = 20f;
		Transform[] array = spawnPositions;
		foreach (Transform transform in array)
		{
			if (transform != null && Vector3.Distance(position, transform.position) < num)
			{
				return true;
			}
		}
		return false;
	}

	private void FreezeSpawnedDragon(GameObject enemy)
	{
		if (!freezeDragonsUntilCleared)
		{
			return;
		}
		DragonEnemyAI component = enemy.GetComponent<DragonEnemyAI>();
		if (component != null)
		{
			component.frozenByArena = true;
			frozenDragons.Add(component);
			return;
		}
		ProximityDragonEnemyAI component2 = enemy.GetComponent<ProximityDragonEnemyAI>();
		if (component2 != null)
		{
			component2.frozenByArena = true;
			frozenProximityDragons.Add(component2);
		}
	}

	private void UnfreezeAllDragons()
	{
		foreach (DragonEnemyAI frozenDragon in frozenDragons)
		{
			if (frozenDragon != null)
			{
				frozenDragon.frozenByArena = false;
			}
		}
		frozenDragons.Clear();
		foreach (ProximityDragonEnemyAI frozenProximityDragon in frozenProximityDragons)
		{
			if (frozenProximityDragon != null)
			{
				frozenProximityDragon.frozenByArena = false;
			}
		}
		frozenProximityDragons.Clear();
	}

	private IEnumerator SpawnEnemiesCoroutine()
	{
		yield return null;
		if (selectedGroup != null)
		{
			yield return StartCoroutine(SpawnGroupCoroutine(selectedGroup));
		}
		else
		{
			yield return StartCoroutine(SpawnLegacyRandomCoroutine());
		}
		hasSpawnedAllEnemies = true;
	}

	private ArenaSpawnGroup SelectSpawnGroup()
	{
		if (spawnGroups == null || spawnGroups.Length == 0)
		{
			return null;
		}
		float num = 0f;
		for (int i = 0; i < spawnGroups.Length; i++)
		{
			if (spawnGroups[i] != null && spawnGroups[i].IsValid)
			{
				num += spawnGroups[i].weight;
			}
		}
		if (num <= 0f)
		{
			return null;
		}
		float num2 = UnityEngine.Random.Range(0f, num);
		for (int j = 0; j < spawnGroups.Length; j++)
		{
			WeightedSpawnGroup weightedSpawnGroup = spawnGroups[j];
			if (weightedSpawnGroup != null && weightedSpawnGroup.IsValid)
			{
				num2 -= weightedSpawnGroup.weight;
				if (num2 <= 0f)
				{
					return weightedSpawnGroup.group;
				}
			}
		}
		return null;
	}

	private IEnumerator SpawnGroupCoroutine(ArenaSpawnGroup group)
	{
		for (int i = 0; i < group.entries.Length; i++)
		{
			ArenaSpawnEntry arenaSpawnEntry = group.entries[i];
			if (arenaSpawnEntry == null || !arenaSpawnEntry.IsValid)
			{
				continue;
			}
			if (!spawnPointsById.TryGetValue(arenaSpawnEntry.spawnPointId, out var value) || value == null)
			{
				if (debugMode)
				{
					Debug.LogWarning($"[ArenaEnemySpawner] '{name}' group '{group.DisplayName}' entry {i} references unknown spawn point '{arenaSpawnEntry.spawnPointId}'. Skipped.", this);
				}
			}
			else
			{
				GameObject gameObject = arenaSpawnEntry.ResolvePrefab();
				if (!(gameObject == null))
				{
					yield return StartCoroutine(SpawnSingleEnemy(gameObject, value));
					yield return new WaitForSeconds(Mathf.Max(spawnDelay, 0.1f));
				}
			}
		}
	}

	private IEnumerator SpawnLegacyRandomCoroutine()
	{
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
			if (flag)
			{
				int num = Physics.OverlapSphereNonAlloc(transform.position, 1f, spawnCheckBuffer, -1, QueryTriggerInteraction.Ignore);
				for (int j = 0; j < num; j++)
				{
					Collider collider = spawnCheckBuffer[j];
					if (spawnedEnemies.Contains(collider.gameObject))
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

	private GameObject GetEnemyPrefab()
	{
		if (this.enemyData == null || this.enemyData.Length == 0)
		{
			return null;
		}
		EnemyData enemyData = ((!spawnRandomEnemies) ? this.enemyData[spawnedEnemies.Count % this.enemyData.Length] : this.enemyData[UnityEngine.Random.Range(0, this.enemyData.Length)]);
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
			UnityEngine.Object.Destroy(UnityEngine.Object.Instantiate(spawnEffect, spawnPosition.position, spawnPosition.rotation), effectDuration);
		}
		if (spawnSound != null && audioSource != null)
		{
			audioSource.PlayOneShot(spawnSound);
		}
		yield return new WaitForSeconds(0.1f);
		GameObject gameObject = UnityEngine.Object.Instantiate(prefab, spawnPosition.position, spawnPosition.rotation);
		spawnedEnemies.Add(gameObject);
		FreezeSpawnedDragon(gameObject);
	}

	private bool AreAllEnemiesDead()
	{
		for (int num = spawnedEnemies.Count - 1; num >= 0; num--)
		{
			if (spawnedEnemies[num] == null)
			{
				spawnedEnemies.RemoveAt(num);
			}
		}
		return spawnedEnemies.Count == 0;
	}

	private void PlayRoomClearEffects(AudioClip sting)
	{
		if (sting != null && roomClearAudioSource != null)
		{
			roomClearAudioSource.PlayOneShot(sting);
		}
		if (roomClearEffect != null)
		{
			UnityEngine.Object.Destroy(UnityEngine.Object.Instantiate(roomClearEffect, transform.position, Quaternion.identity), effectDuration);
		}
	}

	private IEnumerator DestroyTriggersDelayed()
	{
		yield return new WaitForSeconds(0.5f);
		Collider[] array = triggerBoxes;
		foreach (Collider collider in array)
		{
			if (collider != null)
			{
				UnityEngine.Object.Destroy(collider.gameObject);
			}
		}
	}

	private List<T> ShuffleList<T>(List<T> list)
	{
		List<T> list2 = new List<T>(list);
		for (int i = 0; i < list2.Count; i++)
		{
			T value = list2[i];
			int index = UnityEngine.Random.Range(i, list2.Count);
			list2[i] = list2[index];
			list2[index] = value;
		}
		return list2;
	}

	public int GetAliveEnemyCount()
	{
		spawnedEnemies.RemoveAll((GameObject enemy) => enemy == null);
		return spawnedEnemies.Count;
	}

	public void ForceOpenRoom()
	{
		OpenRoom();
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
					Color color;
					if (isLocked)
					{
						color = Color.red;
					}
					else
					{
						color = (hasActivated ? Color.green : Color.yellow);
					}
					Gizmos.color = color;
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
					Gizmos.color = (hasActivated ? Color.red : Color.cyan);
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
		if (doorAnimators != null)
		{
			Gizmos.color = Color.magenta;
			Animator[] array3 = doorAnimators;
			foreach (Animator animator in array3)
			{
				if (animator != null)
				{
					Gizmos.DrawLine(base.transform.position, animator.transform.position);
				}
			}
		}
		DrawSpawnGroupPreview();
		_ = Application.isPlaying;
	}

	private void DrawSpawnGroupPreview()
	{
		if (spawnGroups == null || gizmoPreviewGroup < 0 || gizmoPreviewGroup >= spawnGroups.Length)
		{
			return;
		}
		WeightedSpawnGroup weightedSpawnGroup = spawnGroups[gizmoPreviewGroup];
		if (weightedSpawnGroup == null || weightedSpawnGroup.group == null || weightedSpawnGroup.group.entries == null)
		{
			return;
		}
		ArenaSpawnGroup arenaSpawnGroup = weightedSpawnGroup.group;
		for (int i = 0; i < arenaSpawnGroup.entries.Length; i++)
		{
			ArenaSpawnEntry arenaSpawnEntry = arenaSpawnGroup.entries[i];
			if (arenaSpawnEntry != null && !string.IsNullOrWhiteSpace(arenaSpawnEntry.spawnPointId))
			{
				Transform transform = FindSpawnPointById(arenaSpawnEntry.spawnPointId);
				if (!(transform == null))
				{
					Gizmos.color = (weightedSpawnGroup.enabled ? Color.yellow : Color.grey);
					Gizmos.DrawWireSphere(transform.position, 0.9f);
					Gizmos.DrawLine(base.transform.position, transform.position);
				}
			}
		}
	}

	private Transform FindSpawnPointById(string id)
	{
		if (string.IsNullOrWhiteSpace(id))
		{
			return null;
		}
		if (spawnPointsById != null)
		{
			if (!spawnPointsById.TryGetValue(id, out var value))
			{
				return null;
			}
			return value;
		}
		ArenaSpawnPoint[] componentsInChildren = GetComponentsInChildren<ArenaSpawnPoint>(includeInactive: true);
		foreach (ArenaSpawnPoint arenaSpawnPoint in componentsInChildren)
		{
			if (arenaSpawnPoint.HasId && string.Equals(arenaSpawnPoint.id, id, StringComparison.OrdinalIgnoreCase))
			{
				return arenaSpawnPoint.transform;
			}
		}
		return null;
	}

	private void OnValidate()
	{
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
