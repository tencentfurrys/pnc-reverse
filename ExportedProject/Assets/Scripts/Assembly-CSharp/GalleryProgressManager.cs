using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GalleryProgressManager : MonoBehaviour
{
	[Header("Gallery Configuration")]
	[SerializeField]
	private string prefsKeyPrefix = "Gallery_";

	[SerializeField]
	private List<EnemyGalleryEntry> allEnemies = new List<EnemyGalleryEntry>();

	[Header("Events")]
	public UnityEvent<string> OnEnemyUnlocked;

	public UnityEvent OnProgressChanged;

	private HashSet<string> unlockedEnemies = new HashSet<string>();

	private bool cheatUnlockAll;

	private const string UNLOCKED_LIST_KEY = "Gallery_UnlockedList";

	public static GalleryProgressManager Instance { get; private set; }

	public bool CheatUnlockAll => cheatUnlockAll;

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
			Object.DontDestroyOnLoad(base.gameObject);
			LoadProgress();
		}
		else
		{
			Object.Destroy(base.gameObject);
		}
	}

	public bool IsEnemyUnlocked(string enemyID)
	{
		if (string.IsNullOrEmpty(enemyID))
		{
			return false;
		}
		if (cheatUnlockAll)
		{
			return true;
		}
		return unlockedEnemies.Contains(enemyID);
	}

	public bool ToggleCheatUnlockAll()
	{
		cheatUnlockAll = !cheatUnlockAll;
		OnProgressChanged?.Invoke();
		return cheatUnlockAll;
	}

	public bool UnlockEnemy(string enemyID)
	{
		if (string.IsNullOrEmpty(enemyID))
		{
			return false;
		}
		if (unlockedEnemies.Contains(enemyID))
		{
			return false;
		}
		unlockedEnemies.Add(enemyID);
		SaveEnemyUnlock(enemyID);
		OnEnemyUnlocked?.Invoke(enemyID);
		OnProgressChanged?.Invoke();
		return true;
	}

	public List<EnemyGalleryEntry> GetUnlockedEnemies()
	{
		List<EnemyGalleryEntry> list = new List<EnemyGalleryEntry>();
		foreach (EnemyGalleryEntry allEnemy in allEnemies)
		{
			if (allEnemy != null && IsEnemyUnlocked(allEnemy.enemyID))
			{
				list.Add(allEnemy);
			}
		}
		return list;
	}

	public int GetUnlockedCount()
	{
		return unlockedEnemies.Count;
	}

	public int GetTotalCount()
	{
		return allEnemies.Count;
	}

	public List<EnemyGalleryEntry> GetAllEnemies()
	{
		return new List<EnemyGalleryEntry>(allEnemies);
	}

	private void SaveEnemyUnlock(string enemyID)
	{
		PlayerPrefs.SetInt(prefsKeyPrefix + enemyID, 1);
		SaveUnlockedList();
	}

	private void SaveUnlockedList()
	{
		string value = string.Join(",", unlockedEnemies);
		PlayerPrefs.SetString("Gallery_UnlockedList", value);
		PlayerPrefs.Save();
	}

	private void LoadProgress()
	{
		unlockedEnemies.Clear();
		string text = PlayerPrefs.GetString("Gallery_UnlockedList", "");
		if (!string.IsNullOrEmpty(text))
		{
			string[] array = text.Split(',');
			foreach (string text2 in array)
			{
				if (!string.IsNullOrEmpty(text2))
				{
					unlockedEnemies.Add(text2);
				}
			}
		}
		foreach (EnemyGalleryEntry allEnemy in allEnemies)
		{
			if (!(allEnemy == null) && !string.IsNullOrEmpty(allEnemy.enemyID))
			{
				if (PlayerPrefs.GetInt(prefsKeyPrefix + allEnemy.enemyID, 0) == 1)
				{
					unlockedEnemies.Add(allEnemy.enemyID);
				}
				if (allEnemy.alwaysUnlocked)
				{
					unlockedEnemies.Add(allEnemy.enemyID);
				}
			}
		}
	}

	public void ResetAllProgress()
	{
		foreach (EnemyGalleryEntry allEnemy in allEnemies)
		{
			if (allEnemy != null && !string.IsNullOrEmpty(allEnemy.enemyID))
			{
				PlayerPrefs.DeleteKey(prefsKeyPrefix + allEnemy.enemyID);
			}
		}
		PlayerPrefs.DeleteKey("Gallery_UnlockedList");
		PlayerPrefs.Save();
		unlockedEnemies.Clear();
		OnProgressChanged?.Invoke();
	}

	public void UnlockAllEnemies()
	{
		foreach (EnemyGalleryEntry allEnemy in allEnemies)
		{
			if (allEnemy != null && !string.IsNullOrEmpty(allEnemy.enemyID))
			{
				UnlockEnemy(allEnemy.enemyID);
			}
		}
	}

	[ContextMenu("Unlock All Enemies")]
	private void Debug_UnlockAllEnemies()
	{
		UnlockAllEnemies();
	}

	[ContextMenu("Reset All Progress")]
	private void Debug_ResetAllProgress()
	{
		ResetAllProgress();
	}
}
