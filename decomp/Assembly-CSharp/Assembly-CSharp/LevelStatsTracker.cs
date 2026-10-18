using PixelCrushers.GridController;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelStatsTracker : MonoBehaviour
{
	private float runStartTime;

	private int enemiesKilled;

	private int timesGrabbed;

	private int totalDamageTaken;

	private PlayerStats subscribedPlayerStats;

	public static LevelStatsTracker Instance { get; private set; }

	public int EnemiesKilled => enemiesKilled;

	public int TimesGrabbed => timesGrabbed;

	public int TotalDamageTaken => totalDamageTaken;

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
			Object.DontDestroyOnLoad(gameObject);
			SceneManager.sceneLoaded += OnSceneLoaded;
		}
		else
		{
			Object.Destroy(gameObject);
		}
	}

	private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		SubscribeToPlayer();
	}

	public void StartRun()
	{
		runStartTime = Time.time;
		enemiesKilled = 0;
		timesGrabbed = 0;
		totalDamageTaken = 0;
		subscribedPlayerStats = null;
		ChestItemPool.ResetRunTracker();
	}

	public static void RecordEnemyKill()
	{
		if (Instance != null)
		{
			Instance.enemiesKilled++;
		}
	}

	public static void RecordGrab()
	{
		if (Instance != null)
		{
			Instance.timesGrabbed++;
		}
	}

	public float GetElapsedTime()
	{
		return Time.time - runStartTime;
	}

	public void SubscribeToPlayer()
	{
		if (subscribedPlayerStats != null)
		{
			subscribedPlayerStats.OnDamageTaken.RemoveListener(OnPlayerDamageTaken);
			subscribedPlayerStats = null;
		}
		PlayerStats playerStats = Object.FindAnyObjectByType<PlayerStats>();
		if (playerStats != null)
		{
			subscribedPlayerStats = playerStats;
			subscribedPlayerStats.OnDamageTaken.AddListener(OnPlayerDamageTaken);
		}
	}

	private void OnPlayerDamageTaken(int damage)
	{
		totalDamageTaken += damage;
	}

	public void ResetStats()
	{
		runStartTime = Time.time;
		enemiesKilled = 0;
		timesGrabbed = 0;
		totalDamageTaken = 0;
		if (subscribedPlayerStats != null)
		{
			subscribedPlayerStats.OnDamageTaken.RemoveListener(OnPlayerDamageTaken);
			subscribedPlayerStats = null;
		}
	}

	private void OnDestroy()
	{
		SceneManager.sceneLoaded -= OnSceneLoaded;
		if (subscribedPlayerStats != null)
		{
			subscribedPlayerStats.OnDamageTaken.RemoveListener(OnPlayerDamageTaken);
		}
		if (Instance == this)
		{
			Instance = null;
		}
	}
}
