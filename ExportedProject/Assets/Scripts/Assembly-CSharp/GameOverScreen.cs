using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverScreen : MonoBehaviour
{
	[Header("UI Elements")]
	[SerializeField]
	private GameObject promptPanel;

	[SerializeField]
	private GameObject gameOverPanel;

	[SerializeField]
	private TextMeshProUGUI promptText;

	[Header("Buttons")]
	[SerializeField]
	private Button restartButton;

	[SerializeField]
	private Button mainMenuButton;

	[Header("Animation Settings")]
	[SerializeField]
	private float promptFadeInDuration = 2f;

	[SerializeField]
	private float panelFadeInDuration = 1f;

	[SerializeField]
	private float delayBeforePrompt = 1f;

	[Header("Player Reference")]
	[SerializeField]
	private PlayerStats playerStats;

	[Header("Settings")]
	[SerializeField]
	private bool lockCursorOnStart;

	[SerializeField]
	private string mainMenuSceneName = "MainMenu";

	private bool isWaitingForInput;

	private bool hasShownGameOver;

	private CanvasGroup promptCanvasGroup;

	private CanvasGroup panelCanvasGroup;

	public static GameOverScreen Instance { get; private set; }

	public bool IsShowing
	{
		get
		{
			if (!isWaitingForInput)
			{
				return hasShownGameOver;
			}
			return true;
		}
	}

	private void Awake()
	{
		Instance = this;
		if (promptPanel != null)
		{
			promptCanvasGroup = promptPanel.GetComponent<CanvasGroup>();
			if (promptCanvasGroup == null)
			{
				promptCanvasGroup = promptPanel.AddComponent<CanvasGroup>();
			}
			promptCanvasGroup.alpha = 0f;
			promptPanel.SetActive(value: false);
		}
		if (gameOverPanel != null)
		{
			panelCanvasGroup = gameOverPanel.GetComponent<CanvasGroup>();
			if (panelCanvasGroup == null)
			{
				panelCanvasGroup = gameOverPanel.AddComponent<CanvasGroup>();
			}
			panelCanvasGroup.alpha = 0f;
			gameOverPanel.SetActive(value: false);
		}
		if (playerStats == null)
		{
			playerStats = Object.FindAnyObjectByType<PlayerStats>();
		}
		if (restartButton != null)
		{
			restartButton.onClick.AddListener(RestartLevel);
		}
		if (mainMenuButton != null)
		{
			mainMenuButton.onClick.AddListener(ReturnToMainMenu);
		}
	}

	private void Start()
	{
		if (playerStats != null)
		{
			playerStats.OnPlayerDeath.AddListener(OnPlayerDied);
		}
	}

	private void OnDestroy()
	{
		if (playerStats != null)
		{
			playerStats.OnPlayerDeath.RemoveListener(OnPlayerDied);
		}
		if (restartButton != null)
		{
			restartButton.onClick.RemoveListener(RestartLevel);
		}
		if (mainMenuButton != null)
		{
			mainMenuButton.onClick.RemoveListener(ReturnToMainMenu);
		}
	}

	private void Update()
	{
		if (isWaitingForInput && !hasShownGameOver && InputManager.AnyContinue != null && InputManager.AnyContinue.WasPressedThisFrame())
		{
			ShowGameOverPanel();
		}
	}

	private void OnPlayerDied()
	{
		if (!hasShownGameOver)
		{
			if (!lockCursorOnStart)
			{
				Cursor.lockState = CursorLockMode.None;
				Cursor.visible = true;
			}
			StartCoroutine(ShowGameOverSequence());
		}
	}

	private IEnumerator ShowGameOverSequence()
	{
		yield return new WaitForSeconds(delayBeforePrompt);
		if (promptPanel != null && promptCanvasGroup != null)
		{
			promptPanel.SetActive(value: true);
			isWaitingForInput = true;
			float elapsed = 0f;
			while (elapsed < promptFadeInDuration)
			{
				promptCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / promptFadeInDuration);
				elapsed += Time.deltaTime;
				yield return null;
			}
			promptCanvasGroup.alpha = 1f;
		}
		else
		{
			yield return new WaitForSeconds(1f);
			ShowGameOverPanel();
		}
	}

	private void ShowGameOverPanel()
	{
		if (!hasShownGameOver)
		{
			hasShownGameOver = true;
			isWaitingForInput = false;
			StartCoroutine(FadeInGameOverPanel());
		}
	}

	private IEnumerator FadeInGameOverPanel()
	{
		if (promptPanel != null && promptCanvasGroup != null)
		{
			float elapsed = 0f;
			float fadeOutDuration = 0.5f;
			while (elapsed < fadeOutDuration)
			{
				promptCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
				elapsed += Time.deltaTime;
				yield return null;
			}
			promptPanel.SetActive(value: false);
		}
		if (gameOverPanel != null && panelCanvasGroup != null)
		{
			gameOverPanel.SetActive(value: true);
			Cursor.lockState = CursorLockMode.None;
			Cursor.visible = true;
			float fadeOutDuration = 0f;
			while (fadeOutDuration < panelFadeInDuration)
			{
				panelCanvasGroup.alpha = Mathf.Lerp(0f, 1f, fadeOutDuration / panelFadeInDuration);
				fadeOutDuration += Time.deltaTime;
				yield return null;
			}
			panelCanvasGroup.alpha = 1f;
			FocusFirstButton();
		}
	}

	private void FocusFirstButton()
	{
		MenuNavigationWiring.WireVertical(gameOverPanel);
		Button button = ((restartButton != null) ? restartButton : mainMenuButton);
		if (!(button == null) && button.IsInteractable() && !(EventSystem.current == null))
		{
			EventSystem.current.SetSelectedGameObject(button.gameObject);
		}
	}

	public void RestartLevel()
	{
		RunRestartController.RestartRun();
	}

	public void ReturnToMainMenu()
	{
		SceneManager.LoadScene(mainMenuSceneName);
	}

	public void TriggerGameOver()
	{
		OnPlayerDied();
	}

	public void SetMainMenuSceneName(string sceneName)
	{
		mainMenuSceneName = sceneName;
	}
}
