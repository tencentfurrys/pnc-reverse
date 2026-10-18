using System;
using UnityEngine;

public class SweeperEnemySpawner : MonoBehaviour
{
	[Serializable]
	public class EnemySpawnData
	{
		public GameObject enemyPrefab;

		[TextArea(2, 4)]
		public string spawnText = "An enemy approaches!";
	}

	[Header("Spawning Settings")]
	public EnemySpawnData[] enemySpawnData;

	public Transform[] spawnPoints;

	public float spawnIntervalMinutes = 1f;

	[Header("Text Display Settings")]
	public float textDisplayDuration = 3f;

	public float typewriterSpeed = 0.05f;

	[Header("Optional Settings")]
	public bool startSpawningOnStart = true;

	private float lastSpawnTime;

	private bool isSpawning;

	private GameObject currentEnemy;

	private bool hasSpawned;

	private EnemySpawnData selectedEnemyData;

	private AudioSource enemyAudioSource;

	private Transform selectedSpawnPoint;

	private GameObject followerEntity;

	private void Start()
	{
		if (startSpawningOnStart)
		{
			StartSpawning();
		}
		PickAndSpawnHiddenEnemy();
	}

	private void Update()
	{
		if (isSpawning && !hasSpawned && Time.time >= lastSpawnTime + spawnIntervalMinutes * 60f)
		{
			ActivateEnemy();
		}
	}

	private void PickAndSpawnHiddenEnemy()
	{
		if (enemySpawnData == null || enemySpawnData.Length == 0 || spawnPoints == null || spawnPoints.Length == 0)
		{
			return;
		}
		selectedEnemyData = enemySpawnData[UnityEngine.Random.Range(0, enemySpawnData.Length)];
		selectedSpawnPoint = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];
		GameObject gameObject = GameObject.Find("ChaserHolder");
		if (!(gameObject == null))
		{
			currentEnemy = UnityEngine.Object.Instantiate(selectedEnemyData.enemyPrefab, gameObject.transform.position, gameObject.transform.rotation);
			followerEntity = currentEnemy.transform.Find("Follower Entity")?.gameObject;
			if (followerEntity != null)
			{
				followerEntity.SetActive(value: false);
			}
			enemyAudioSource = currentEnemy.GetComponent<AudioSource>();
			if (enemyAudioSource != null)
			{
				enemyAudioSource.mute = true;
			}
		}
	}

	private void ActivateEnemy()
	{
		if (!hasSpawned && !(currentEnemy == null))
		{
			currentEnemy.transform.position = selectedSpawnPoint.position;
			currentEnemy.transform.rotation = selectedSpawnPoint.rotation;
			if (followerEntity != null)
			{
				followerEntity.SetActive(value: true);
			}
			if (enemyAudioSource != null)
			{
				enemyAudioSource.mute = false;
			}
			lastSpawnTime = Time.time;
			hasSpawned = true;
			ShowSpawnText(selectedEnemyData.spawnText);
		}
	}

	public void StartSpawning()
	{
		isSpawning = true;
		lastSpawnTime = Time.time;
	}

	public void StopSpawning()
	{
		isSpawning = false;
	}

	private void ShowSpawnText(string text)
	{
		AlertPanel alertPanel = UnityEngine.Object.FindAnyObjectByType<AlertPanel>();
		if (!(alertPanel == null))
		{
			alertPanel.ShowAlert(text, typewriterSpeed, textDisplayDuration);
		}
	}
}
