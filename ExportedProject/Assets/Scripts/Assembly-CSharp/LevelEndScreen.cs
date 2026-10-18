using System.Collections;
using PixelCrushers.GridController;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelEndScreen : MonoBehaviour
{
	[Header("UI Panel")]
	[SerializeField]
	private GameObject endScreenPanel;

	[SerializeField]
	private float fadeInDuration = 1f;

	[Header("Stat Texts")]
	[SerializeField]
	private TextMeshProUGUI timeText;

	[SerializeField]
	private TextMeshProUGUI killsText;

	[SerializeField]
	private TextMeshProUGUI grabsText;

	[SerializeField]
	private TextMeshProUGUI damageTakenText;

	[Header("Score Texts")]
	[SerializeField]
	private TextMeshProUGUI scoreText;

	[SerializeField]
	private TextMeshProUGUI currencyEarnedText;

	[Header("Buttons")]
	[SerializeField]
	private Button mainMenuButton;

	[Header("Settings")]
	[SerializeField]
	private string mainMenuSceneName = "MainMenu";

	[SerializeField]
	private float statRevealDelay = 0.35f;

	[SerializeField]
	private float countUpDuration = 0.5f;

	private CanvasGroup panelCanvasGroup;

	public static bool IsActive { get; private set; }

	private void Awake()
	{
		if (endScreenPanel != null)
		{
			panelCanvasGroup = endScreenPanel.GetComponent<CanvasGroup>();
			if (panelCanvasGroup == null)
			{
				panelCanvasGroup = endScreenPanel.AddComponent<CanvasGroup>();
			}
			panelCanvasGroup.alpha = 0f;
			endScreenPanel.SetActive(value: false);
		}
		if (mainMenuButton != null)
		{
			mainMenuButton.onClick.AddListener(ReturnToMainMenu);
			mainMenuButton.interactable = false;
		}
		ClearAllStats();
		IsActive = false;
	}

	private void OnDestroy()
	{
		IsActive = false;
		if (mainMenuButton != null)
		{
			mainMenuButton.onClick.RemoveListener(ReturnToMainMenu);
		}
	}

	public void ShowEndScreen()
	{
		Time.timeScale = 0f;
		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;
		IsActive = true;
		ClearAllStats();
		if (mainMenuButton != null)
		{
			mainMenuButton.interactable = false;
		}
		if (endScreenPanel != null)
		{
			endScreenPanel.SetActive(value: true);
			StartCoroutine(ShowEndScreenSequence());
		}
	}

	private IEnumerator FadeInPanel()
	{
		if (!(panelCanvasGroup == null))
		{
			float elapsed = 0f;
			while (elapsed < fadeInDuration)
			{
				panelCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
				elapsed += Time.unscaledDeltaTime;
				yield return null;
			}
			panelCanvasGroup.alpha = 1f;
		}
	}

	private void ClearAllStats()
	{
		if (timeText != null)
		{
			timeText.text = "";
		}
		if (killsText != null)
		{
			killsText.text = "";
		}
		if (grabsText != null)
		{
			grabsText.text = "";
		}
		if (damageTakenText != null)
		{
			damageTakenText.text = "";
		}
		if (scoreText != null)
		{
			scoreText.text = "";
		}
		if (currencyEarnedText != null)
		{
			currencyEarnedText.text = "";
		}
	}

	private IEnumerator ShowEndScreenSequence()
	{
		yield return StartCoroutine(FadeInPanel());
		yield return StartCoroutine(RevealStatsSequentially());
		if (mainMenuButton != null)
		{
			mainMenuButton.interactable = true;
			MenuNavigationWiring.WireVertical(endScreenPanel);
			if (EventSystem.current != null)
			{
				EventSystem.current.SetSelectedGameObject(mainMenuButton.gameObject);
			}
		}
	}

	private IEnumerator RevealStatsSequentially()
	{
		LevelStatsTracker instance = LevelStatsTracker.Instance;
		if (!(instance == null))
		{
			float elapsedTime = instance.GetElapsedTime();
			int kills = instance.EnemiesKilled;
			int grabs = instance.TimesGrabbed;
			int damage = instance.TotalDamageTaken;
			int num = Mathf.FloorToInt(elapsedTime / 60f);
			int num2 = Mathf.FloorToInt(elapsedTime % 60f);
			int num3 = Mathf.Max(0, 10000 - Mathf.FloorToInt(elapsedTime * 10f));
			int num4 = kills * 100;
			int num5 = grabs * -100;
			int num6 = damage * -5;
			int totalScore = Mathf.Max(0, num3 + num4 + num5 + num6);
			int currencyEarned = Mathf.CeilToInt((float)totalScore / 1000f);
			int totalSeconds = num * 60 + num2;
			yield return new WaitForSecondsRealtime(statRevealDelay);
			yield return StartCoroutine(CountUpTime(timeText, totalSeconds));
			yield return new WaitForSecondsRealtime(statRevealDelay);
			yield return StartCoroutine(CountUpStat(killsText, kills));
			yield return new WaitForSecondsRealtime(statRevealDelay);
			yield return StartCoroutine(CountUpStat(grabsText, grabs));
			yield return new WaitForSecondsRealtime(statRevealDelay);
			yield return StartCoroutine(CountUpStat(damageTakenText, damage));
			yield return new WaitForSecondsRealtime(statRevealDelay);
			yield return StartCoroutine(CountUpStat(scoreText, totalScore));
			yield return new WaitForSecondsRealtime(statRevealDelay);
			yield return StartCoroutine(CountUpStat(currencyEarnedText, currencyEarned, "+"));
			if (currencyEarned > 0 && CurrencyManager.Instance != null)
			{
				CurrencyManager.Instance.AddCurrency(currencyEarned);
			}
			yield return new WaitForSecondsRealtime(statRevealDelay);
			ProcessCompletionUnlocks();
		}
	}

	private void ProcessCompletionUnlocks()
	{
		CompletionUnlock[] array = Resources.LoadAll<CompletionUnlock>("CompletionUnlocks");
		if (array == null || array.Length == 0)
		{
			return;
		}
		PlayerClass playerClass = ((PlayerClassManager.Instance != null) ? PlayerClassManager.Instance.GetSelectedClass() : null);
		ItemUnlockManager instance = ItemUnlockManager.Instance;
		if (instance == null)
		{
			return;
		}
		CompletionUnlock[] array2 = array;
		foreach (CompletionUnlock completionUnlock in array2)
		{
			if (completionUnlock.requiredClass != null && (playerClass == null || completionUnlock.requiredClass.className != playerClass.className))
			{
				continue;
			}
			if (completionUnlock.itemsToUnlock != null)
			{
				InventoryItem[] itemsToUnlock = completionUnlock.itemsToUnlock;
				foreach (InventoryItem inventoryItem in itemsToUnlock)
				{
					if (inventoryItem != null && !instance.IsItemUnlocked(inventoryItem))
					{
						instance.UnlockItem(inventoryItem);
					}
				}
			}
			if (completionUnlock.classesToUnlock == null)
			{
				continue;
			}
			PlayerClass[] classesToUnlock = completionUnlock.classesToUnlock;
			foreach (PlayerClass playerClass2 in classesToUnlock)
			{
				if (playerClass2 != null && !instance.IsClassUnlocked(playerClass2))
				{
					instance.UnlockClass(playerClass2);
				}
			}
		}
	}

	private IEnumerator CountUpStat(TextMeshProUGUI text, int targetValue, string prefix = "")
	{
		if (text == null)
		{
			yield break;
		}
		if (targetValue == 0)
		{
			text.text = prefix + "0";
			yield break;
		}
		float elapsed = 0f;
		while (elapsed < countUpDuration)
		{
			elapsed += Time.unscaledDeltaTime;
			float t = Mathf.Clamp01(elapsed / countUpDuration);
			int num = Mathf.RoundToInt(Mathf.Lerp(0f, targetValue, t));
			text.text = $"{prefix}{num}";
			yield return null;
		}
		text.text = $"{prefix}{targetValue}";
	}

	private IEnumerator CountUpTime(TextMeshProUGUI text, int totalSeconds)
	{
		if (text == null)
		{
			yield break;
		}
		if (totalSeconds == 0)
		{
			text.text = "00:00";
			yield break;
		}
		float elapsed = 0f;
		while (elapsed < countUpDuration)
		{
			elapsed += Time.unscaledDeltaTime;
			float t = Mathf.Clamp01(elapsed / countUpDuration);
			int num = Mathf.RoundToInt(Mathf.Lerp(0f, totalSeconds, t));
			int num2 = num / 60;
			int num3 = num % 60;
			text.text = $"{num2:00}:{num3:00}";
			yield return null;
		}
		int num4 = totalSeconds / 60;
		int num5 = totalSeconds % 60;
		text.text = $"{num4:00}:{num5:00}";
	}

	private void ReturnToMainMenu()
	{
		Time.timeScale = 1f;
		SceneManager.LoadScene(mainMenuSceneName);
	}
}
