using PixelCrushers.GridController;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RunRestartController : MonoBehaviour
{
	private enum State
	{
		Idle = 0,
		Holding = 1,
		FadingOut = 2,
		Restarting = 3
	}

	private const string MainMenuSceneName = "MainMenu";

	private const string RestartSceneName = "Floor1";

	private const int OverlaySortingOrder = 30000;

	private const float HoldDuration = 1.5f;

	private const float ReleaseFadeDuration = 0.35f;

	private const float RestartWatchdogSeconds = 30f;

	private const float BarFadeInScale = 4f;

	private const string PrefabResourcePath = "RunRestartController";

	[Header("Hold Meter")]
	[Tooltip("Spiral art, revealed by a 360 degree sweep. Left empty, the meter falls back to a plain rectangle that grows left to right.")]
	[SerializeField]
	private Sprite fillSprite;

	[Tooltip("Optional art drawn behind the fill, e.g. a dimmed copy of the spiral. No backing is drawn when this is empty.")]
	[SerializeField]
	private Sprite trackSprite;

	[SerializeField]
	private Color fillColor = Color.white;

	[SerializeField]
	private Color trackColor = new Color(1f, 1f, 1f, 0.25f);

	[Tooltip("Where the sweep starts from.")]
	[SerializeField]
	private Image.Origin360 fillOrigin = Image.Origin360.Top;

	[Tooltip("Match this to the direction your spiral winds.")]
	[SerializeField]
	private bool fillClockwise = true;

	[Tooltip("Size of the meter. Keep it square for a radial sweep.")]
	[SerializeField]
	private Vector2 barSize = new Vector2(160f, 160f);

	[Tooltip("Distance from the bottom of the screen, in canvas units.")]
	[SerializeField]
	private float barBottomOffset = 140f;

	[Tooltip("Inset of the Fill inside the backing art. Ignored when there is no backing.")]
	[SerializeField]
	private float barPadding;

	private State state;

	private float holdTime;

	private float alpha;

	private float restartStartedAt;

	private bool requireRelease;

	private GameObject overlayRoot;

	private CanvasGroup overlayGroup;

	private Image dimImage;

	private CanvasGroup barGroup;

	private Image barFillImage;

	private RectTransform barFillRect;

	private bool fillUsesSprite;

	private PauseMenuManager pauseMenu;

	private InventoryUI inventoryUI;

	private bool blockersCached;

	public static RunRestartController Instance { get; private set; }

	private static bool RestartHeld
	{
		get
		{
			if (InputManager.Restart != null)
			{
				return InputManager.Restart.IsPressed();
			}
			return false;
		}
	}

	private static bool RestartJustPressed
	{
		get
		{
			if (InputManager.Restart != null)
			{
				return InputManager.Restart.WasPressedThisFrame();
			}
			return false;
		}
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStatics()
	{
		Instance = null;
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	private static void Bootstrap()
	{
		if (!(Instance != null))
		{
			GameObject gameObject = Resources.Load<GameObject>("RunRestartController");
			if (gameObject != null && gameObject.GetComponent<RunRestartController>() != null)
			{
				GameObject obj = Object.Instantiate(gameObject);
				obj.name = "RunRestartController";
				obj.SetActive(value: true);
			}
			else
			{
				new GameObject("RunRestartController").AddComponent<RunRestartController>();
			}
		}
	}

	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Object.Destroy(base.gameObject);
			return;
		}
		Instance = this;
		Object.DontDestroyOnLoad(base.gameObject);
		BuildOverlay();
		SceneManager.sceneLoaded += OnSceneLoaded;
	}

	private void OnDestroy()
	{
		SceneManager.sceneLoaded -= OnSceneLoaded;
		if (Instance == this)
		{
			Instance = null;
		}
	}

	private void BuildOverlay()
	{
		overlayRoot = new GameObject("RunRestartOverlay");
		overlayRoot.transform.SetParent(base.transform, worldPositionStays: false);
		overlayRoot.layer = LayerMask.NameToLayer("UI");
		Canvas canvas = overlayRoot.AddComponent<Canvas>();
		canvas.renderMode = RenderMode.ScreenSpaceOverlay;
		canvas.sortingOrder = 30000;
		overlayGroup = overlayRoot.AddComponent<CanvasGroup>();
		overlayGroup.alpha = 1f;
		overlayGroup.interactable = false;
		overlayGroup.blocksRaycasts = false;
		GameObject gameObject = new GameObject("Dim");
		gameObject.transform.SetParent(overlayRoot.transform, worldPositionStays: false);
		gameObject.layer = overlayRoot.layer;
		dimImage = gameObject.AddComponent<Image>();
		dimImage.color = new Color(0f, 0f, 0f, 0f);
		dimImage.raycastTarget = false;
		Stretch(dimImage.rectTransform);
		BuildBar(overlayRoot.transform);
		overlayRoot.SetActive(value: false);
	}

	private void BuildBar(Transform parent)
	{
		GameObject gameObject = new GameObject("Bar");
		gameObject.transform.SetParent(parent, worldPositionStays: false);
		gameObject.layer = overlayRoot.layer;
		RectTransform rectTransform = gameObject.AddComponent<RectTransform>();
		rectTransform.anchorMin = new Vector2(0.5f, 0f);
		rectTransform.anchorMax = new Vector2(0.5f, 0f);
		rectTransform.pivot = new Vector2(0.5f, 0f);
		rectTransform.anchoredPosition = new Vector2(0f, barBottomOffset);
		rectTransform.sizeDelta = barSize;
		barGroup = gameObject.AddComponent<CanvasGroup>();
		barGroup.alpha = 0f;
		barGroup.interactable = false;
		barGroup.blocksRaycasts = false;
		bool flag = trackSprite != null;
		if (flag)
		{
			GameObject obj = new GameObject("Track");
			obj.transform.SetParent(gameObject.transform, worldPositionStays: false);
			obj.layer = overlayRoot.layer;
			Image image = obj.AddComponent<Image>();
			image.raycastTarget = false;
			image.color = trackColor;
			image.sprite = trackSprite;
			Stretch(image.rectTransform);
		}
		GameObject gameObject2 = new GameObject("Fill");
		gameObject2.transform.SetParent(gameObject.transform, worldPositionStays: false);
		gameObject2.layer = overlayRoot.layer;
		barFillImage = gameObject2.AddComponent<Image>();
		barFillImage.raycastTarget = false;
		barFillImage.color = fillColor;
		barFillRect = barFillImage.rectTransform;
		Stretch(barFillRect);
		fillUsesSprite = fillSprite != null;
		if (fillUsesSprite)
		{
			barFillImage.sprite = fillSprite;
			barFillImage.type = Image.Type.Filled;
			barFillImage.fillMethod = Image.FillMethod.Radial360;
			barFillImage.fillOrigin = (int)fillOrigin;
			barFillImage.fillClockwise = fillClockwise;
			if (flag)
			{
				barFillRect.offsetMin = new Vector2(barPadding, barPadding);
				barFillRect.offsetMax = new Vector2(0f - barPadding, 0f - barPadding);
			}
		}
		else
		{
			barFillRect.offsetMin = new Vector2(0f, barPadding);
			barFillRect.offsetMax = new Vector2(0f, 0f - barPadding);
		}
		SetFillProgress(0f);
	}

	private static void Stretch(RectTransform rt)
	{
		rt.anchorMin = Vector2.zero;
		rt.anchorMax = Vector2.one;
		rt.offsetMin = Vector2.zero;
		rt.offsetMax = Vector2.zero;
	}

	private void SetFillProgress(float t)
	{
		if (fillUsesSprite)
		{
			barFillImage.fillAmount = t;
		}
		else
		{
			barFillRect.anchorMax = new Vector2(t, 1f);
		}
	}

	private void SetAlpha(float value)
	{
		alpha = Mathf.Clamp01(value);
		dimImage.color = new Color(0f, 0f, 0f, alpha);
		barGroup.alpha = Mathf.Clamp01(alpha * 4f);
		SetFillProgress(alpha);
	}

	private void ClearOverlay()
	{
		SetAlpha(0f);
		overlayRoot.SetActive(value: false);
		holdTime = 0f;
		state = State.Idle;
	}

	private void Update()
	{
		switch (state)
		{
		case State.Idle:
			TickIdle();
			break;
		case State.Holding:
			TickHolding();
			break;
		case State.FadingOut:
			TickFadingOut();
			break;
		case State.Restarting:
			TickRestarting();
			break;
		}
	}

	private void TickIdle()
	{
		if (requireRelease)
		{
			if (!RestartHeld)
			{
				requireRelease = false;
			}
		}
		else if (RestartJustPressed && IsAvailable())
		{
			holdTime = 0f;
			SetAlpha(0f);
			overlayRoot.SetActive(value: true);
			state = State.Holding;
		}
	}

	private void TickHolding()
	{
		if (!RestartHeld || !IsAvailable())
		{
			state = State.FadingOut;
			return;
		}
		holdTime += Time.unscaledDeltaTime;
		SetAlpha(holdTime / 1.5f);
		if (holdTime >= 1.5f)
		{
			Commit();
		}
	}

	private void TickFadingOut()
	{
		if (RestartJustPressed && IsAvailable())
		{
			holdTime = alpha * 1.5f;
			state = State.Holding;
			return;
		}
		SetAlpha(alpha - Time.unscaledDeltaTime / 0.35f);
		if (alpha <= 0f)
		{
			ClearOverlay();
		}
	}

	private void TickRestarting()
	{
		if (Time.unscaledTime - restartStartedAt > 30f)
		{
			Debug.LogWarning("[RunRestartController] Restart did not complete; clearing the overlay.");
			ClearOverlay();
		}
	}

	private void Commit()
	{
		SetAlpha(1f);
		barGroup.alpha = 0f;
		state = State.Restarting;
		requireRelease = true;
		restartStartedAt = Time.unscaledTime;
		RestartRun();
	}

	private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		blockersCached = false;
		pauseMenu = null;
		inventoryUI = null;
		if (state == State.Restarting)
		{
			ClearOverlay();
		}
	}

	private bool IsAvailable()
	{
		if (IsEnabledInActiveScene())
		{
			return !IsBlocked();
		}
		return false;
	}

	private static bool IsEnabledInActiveScene()
	{
		Scene activeScene = SceneManager.GetActiveScene();
		if (activeScene.IsValid())
		{
			return activeScene.name != "MainMenu";
		}
		return false;
	}

	private bool IsBlocked()
	{
		if (LoadingScreen.IsTransitioning)
		{
			return true;
		}
		if (LevelEndScreen.IsActive)
		{
			return true;
		}
		if (NoteViewer.Instance != null && NoteViewer.Instance.IsOpen)
		{
			return true;
		}
		if (GameOverScreen.Instance != null && GameOverScreen.Instance.IsShowing)
		{
			return true;
		}
		CacheBlockers();
		if (pauseMenu != null && pauseMenu.IsPaused)
		{
			return true;
		}
		if (inventoryUI != null && inventoryUI.IsInventoryOpen)
		{
			return true;
		}
		return false;
	}

	private void CacheBlockers()
	{
		if (!blockersCached)
		{
			blockersCached = true;
			pauseMenu = Object.FindAnyObjectByType<PauseMenuManager>();
			inventoryUI = Object.FindAnyObjectByType<InventoryUI>();
		}
	}

	public static void RestartRun()
	{
		if (PlayerStateSaver.Instance != null)
		{
			PlayerStateSaver.Instance.ClearSavedState();
		}
		if (LevelStatsTracker.Instance != null)
		{
			LevelStatsTracker.Instance.StartRun();
		}
		Time.timeScale = 1f;
		LoadingScreen loadingScreen = Object.FindAnyObjectByType<LoadingScreen>();
		if (loadingScreen != null)
		{
			loadingScreen.LoadSceneWithLoadingScreen("Floor1");
		}
		else
		{
			SceneManager.LoadScene("Floor1");
		}
	}
}
