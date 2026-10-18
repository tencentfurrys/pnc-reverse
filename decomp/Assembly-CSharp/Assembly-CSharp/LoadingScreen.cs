using System.Collections;
using PixelCrushers.GridController;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadingScreen : MonoBehaviour
{
	private static LoadingScreen instance;

	[Header("UI Elements")]
	[SerializeField]
	private GameObject loadingPanel;

	[SerializeField]
	private TextMeshProUGUI loadingText;

	[Header("Loading Messages")]
	[SerializeField]
	private string[] loadingMessages = new string[5] { "Generating dungeon layout...", "Placing enemies...", "Loading assets...", "Initializing systems...", "Almost ready..." };

	[Header("Settings")]
	[SerializeField]
	private float fadeInDuration = 0.5f;

	[SerializeField]
	private float minimumLoadTime = 2f;

	[SerializeField]
	private float messageChangeInterval = 0.5f;

	[SerializeField]
	private float textFadeOutDuration = 1f;

	[SerializeField]
	private float backgroundFadeOutDuration = 1f;

	[SerializeField]
	private bool waitForDungeonGeneration = true;

	[SerializeField]
	private bool waitForAStarIntegration = true;

	[SerializeField]
	private float maxWaitTimeForDungeon = 30f;

	public static bool IsTransitioning { get; private set; }

	private void Awake()
	{
		if (instance != null && instance != this)
		{
			Object.Destroy(gameObject);
			return;
		}
		instance = this;
		Object.DontDestroyOnLoad(gameObject);
		if (loadingPanel != null)
		{
			loadingPanel.SetActive(value: false);
		}
	}

	private void OnDestroy()
	{
		if (instance == this)
		{
			instance = null;
			IsTransitioning = false;
		}
	}

	public static void DestroyInstance()
	{
		if (instance != null)
		{
			Object.Destroy(instance.gameObject);
			instance = null;
		}
		IsTransitioning = false;
	}

	public static bool HasInstance()
	{
		return instance != null;
	}

	public void LoadSceneWithLoadingScreen(string sceneName)
	{
		StartCoroutine(LoadSceneAsync(sceneName));
	}

	private IEnumerator LoadSceneAsync(string sceneName)
	{
		IsTransitioning = true;
		if (loadingPanel != null)
		{
			loadingPanel.SetActive(value: true);
			CanvasGroup canvasGroup = loadingPanel.GetComponent<CanvasGroup>();
			if (canvasGroup == null)
			{
				canvasGroup = loadingPanel.AddComponent<CanvasGroup>();
			}
			canvasGroup.alpha = 0f;
			float elapsed = 0f;
			while (elapsed < fadeInDuration)
			{
				canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
				elapsed += Time.deltaTime;
				yield return null;
			}
			canvasGroup.alpha = 1f;
		}
		float startTime = Time.time;
		AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
		asyncLoad.allowSceneActivation = false;
		float messageTimer = 0f;
		int currentMessageIndex = 0;
		while (!asyncLoad.isDone)
		{
			messageTimer += Time.deltaTime;
			if (messageTimer >= messageChangeInterval && loadingMessages.Length != 0)
			{
				messageTimer = 0f;
				if (loadingText != null)
				{
					loadingText.text = loadingMessages[currentMessageIndex];
				}
				currentMessageIndex = (currentMessageIndex + 1) % loadingMessages.Length;
			}
			if (asyncLoad.progress >= 0.9f && Time.time - startTime >= minimumLoadTime)
			{
				asyncLoad.allowSceneActivation = true;
			}
			yield return null;
		}
		if (waitForDungeonGeneration || waitForAStarIntegration)
		{
			yield return StartCoroutine(WaitForDungeonAndAStarIntegration());
		}
		yield return StartCoroutine(PrewarmGameplayPools());
		InputManager.SwitchToGameplay();
		yield return StartCoroutine(FadeOutSequence());
		IsTransitioning = false;
	}

	private IEnumerator WaitForDungeonAndAStarIntegration()
	{
		if (loadingText != null)
		{
			loadingText.text = "Preparing dungeon...";
		}
		yield return new WaitForSeconds(0.2f);
		DunGenAStarIntegration integration = Object.FindAnyObjectByType<DunGenAStarIntegration>();
		if (integration != null)
		{
			float waitStartTime = Time.time;
			bool dungeonGenerated = false;
			bool fullyReady = false;
			while (!fullyReady && Time.time - waitStartTime < maxWaitTimeForDungeon)
			{
				if (!dungeonGenerated && integration.IsDungeonGenerated())
				{
					dungeonGenerated = true;
					if (loadingText != null)
					{
						loadingText.text = "Calculating pathfinding...";
					}
				}
				if (integration.IsFullyReady())
				{
					fullyReady = true;
					if (loadingText != null)
					{
						loadingText.text = "Ready!";
					}
				}
				else
				{
					yield return null;
				}
			}
			if (!fullyReady)
			{
				Debug.LogWarning($"LoadingScreen: dungeon wait timed out after {maxWaitTimeForDungeon}s. " + $"dungeonGenerated={integration.IsDungeonGenerated()}, " + $"aStarScanComplete={integration.IsAStarScanComplete()}, " + $"generatorStatus={integration.GeneratorStatus}");
				if (loadingText != null)
				{
					loadingText.text = "Loading timeout - continuing anyway...";
				}
				yield return new WaitForSeconds(1f);
			}
		}
		else if (loadingText != null)
		{
			loadingText.text = "Ready!";
		}
		yield return new WaitForSeconds(0.5f);
	}

	private IEnumerator PrewarmGameplayPools()
	{
		if (PickupPoolManager.Instance != null)
		{
			yield return StartCoroutine(PickupPoolManager.Instance.ResetForNewLevel());
			yield return StartCoroutine(PickupPoolManager.Instance.PrewarmPools());
		}
		if (VFXPoolManager.Instance != null)
		{
			yield return StartCoroutine(VFXPoolManager.Instance.ResetForNewLevel());
		}
		if (AudioPool.Instance != null)
		{
			yield return StartCoroutine(AudioPool.Instance.Prewarm(16));
		}
		DamageNumberManager damageNumberManager = Object.FindAnyObjectByType<DamageNumberManager>();
		if (damageNumberManager != null)
		{
			yield return StartCoroutine(damageNumberManager.PrewarmPool());
		}
		yield return null;
	}

	private IEnumerator FadeOutSequence()
	{
		if (loadingText != null)
		{
			float elapsed = 0f;
			Color originalColor = loadingText.color;
			while (elapsed < textFadeOutDuration)
			{
				float a = Mathf.Lerp(1f, 0f, elapsed / textFadeOutDuration);
				loadingText.color = new Color(originalColor.r, originalColor.g, originalColor.b, a);
				elapsed += Time.deltaTime;
				yield return null;
			}
			loadingText.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
		}
		if (!(loadingPanel != null))
		{
			yield break;
		}
		CanvasGroup canvasGroup = loadingPanel.GetComponent<CanvasGroup>();
		if (canvasGroup != null)
		{
			float elapsed = 0f;
			while (elapsed < backgroundFadeOutDuration)
			{
				canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / backgroundFadeOutDuration);
				elapsed += Time.deltaTime;
				yield return null;
			}
			canvasGroup.alpha = 0f;
		}
	}

	public void ShowLoadingScreen()
	{
		if (loadingPanel != null)
		{
			loadingPanel.SetActive(value: true);
		}
	}

	public void HideLoadingScreen()
	{
		if (loadingPanel != null)
		{
			loadingPanel.SetActive(value: false);
		}
	}

	public void UpdateProgress(float progress, string message = "")
	{
		if (!string.IsNullOrEmpty(message) && loadingText != null)
		{
			loadingText.text = message;
		}
	}
}
