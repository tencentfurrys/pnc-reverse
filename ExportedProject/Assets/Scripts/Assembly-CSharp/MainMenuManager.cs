using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
	[Header("Loading Screen")]
	[SerializeField]
	private GameObject loadingScreenPrefab;

	[Header("Camera Animation")]
	[SerializeField]
	private Animator cameraAnimator;

	[Header("Item Unlocks")]
	[SerializeField]
	private ItemUnlocksScreenUI itemUnlocksScreen;

	[Header("Sub-menus")]
	[SerializeField]
	private OptionsMenu optionsMenu;

	[SerializeField]
	private EnemyGalleryUI enemyGalleryUI;

	[SerializeField]
	private GalleryTabController galleryTabController;

	[Header("Controller Navigation")]
	[SerializeField]
	private GameObject firstSelected;

	[Header("Cheat Code")]
	[SerializeField]
	private AudioSource cheatAudioSource;

	[SerializeField]
	private AudioClip cheatSound;

	[SerializeField]
	private DioramaGalleryUI dioramaGalleryUI;

	[SerializeField]
	private PeekScenesUI peekScenesUI;

	private bool isLoading;

	private static readonly Vector2[] CheatSequence = new Vector2[8]
	{
		Vector2.up,
		Vector2.up,
		Vector2.down,
		Vector2.down,
		Vector2.left,
		Vector2.right,
		Vector2.left,
		Vector2.right
	};

	private int cheatProgress;

	private void Start()
	{
		LoadingScreen.DestroyInstance();
		if (PlayerStateSaver.Instance != null)
		{
			PlayerStateSaver.Instance.ClearSavedState();
		}
		if (LevelStatsTracker.Instance != null)
		{
			LevelStatsTracker.Instance.ResetStats();
		}
		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;
		InputManager.SwitchToUI();
		if (firstSelected != null && EventSystem.current != null)
		{
			EventSystem.current.SetSelectedGameObject(firstSelected);
		}
	}

	private void Update()
	{
		Vector2 vector = UIInput.NavigateDirectionThisFrame();
		if (vector == Vector2.zero)
		{
			return;
		}
		if (vector == CheatSequence[cheatProgress])
		{
			cheatProgress++;
			if (cheatProgress >= CheatSequence.Length)
			{
				cheatProgress = 0;
				ToggleGalleryCheat();
			}
		}
		else
		{
			cheatProgress = ((vector == CheatSequence[0]) ? 1 : 0);
		}
	}

	private void ToggleGalleryCheat()
	{
		if (!(GalleryProgressManager.Instance == null))
		{
			GalleryProgressManager.Instance.ToggleCheatUnlockAll();
			if (cheatAudioSource != null && cheatSound != null)
			{
				cheatAudioSource.PlayOneShot(cheatSound);
			}
			if (enemyGalleryUI != null && enemyGalleryUI.isActiveAndEnabled)
			{
				enemyGalleryUI.RefreshDisplay();
			}
			if (dioramaGalleryUI != null && dioramaGalleryUI.isActiveAndEnabled)
			{
				dioramaGalleryUI.RefreshDisplay();
			}
			if (peekScenesUI != null && peekScenesUI.isActiveAndEnabled)
			{
				peekScenesUI.RefreshDisplay();
			}
		}
	}

	public void StartGame()
	{
		if (isLoading)
		{
			return;
		}
		isLoading = true;
		if (PlayerStateSaver.Instance != null)
		{
			PlayerStateSaver.Instance.ClearSavedState();
		}
		if (LevelStatsTracker.Instance != null)
		{
			LevelStatsTracker.Instance.StartRun();
		}
		if (loadingScreenPrefab != null)
		{
			LoadingScreen component = Object.Instantiate(loadingScreenPrefab).GetComponent<LoadingScreen>();
			if (component != null)
			{
				component.LoadSceneWithLoadingScreen("Floor1");
			}
			else
			{
				SceneManager.LoadScene("Floor1");
			}
		}
		else
		{
			SceneManager.LoadScene("Floor1");
		}
	}

	public void ExitGame()
	{
		Application.Quit();
	}

	public void OpenOptions()
	{
		if (cameraAnimator != null)
		{
			cameraAnimator.SetBool("Options", value: true);
		}
		if (optionsMenu != null && optionsMenu.FirstSelected != null && EventSystem.current != null)
		{
			EventSystem.current.SetSelectedGameObject(optionsMenu.FirstSelected);
		}
	}

	public void CloseOptions()
	{
		if (cameraAnimator != null)
		{
			cameraAnimator.SetBool("Options", value: false);
		}
		if (firstSelected != null && EventSystem.current != null)
		{
			EventSystem.current.SetSelectedGameObject(firstSelected);
		}
	}

	public void OpenEnemyGallery()
	{
		if (cameraAnimator != null)
		{
			cameraAnimator.SetBool("EnemyGallery", value: true);
		}
		if (galleryTabController != null && galleryTabController.FirstSelected != null && EventSystem.current != null)
		{
			EventSystem.current.SetSelectedGameObject(galleryTabController.FirstSelected);
		}
	}

	public void CloseEnemyGallery()
	{
		if (cameraAnimator != null)
		{
			cameraAnimator.SetBool("EnemyGallery", value: false);
		}
		if (firstSelected != null && EventSystem.current != null)
		{
			EventSystem.current.SetSelectedGameObject(firstSelected);
		}
	}

	public void OpenDioramaGallery()
	{
		if (cameraAnimator != null)
		{
			cameraAnimator.SetBool("DioramaGallery", value: true);
		}
	}

	public void CloseDioramaGallery()
	{
		if (cameraAnimator != null)
		{
			cameraAnimator.SetBool("DioramaGallery", value: false);
		}
	}

	public void OpenItemUnlocks()
	{
		if (itemUnlocksScreen != null)
		{
			itemUnlocksScreen.OpenScreen();
			if (itemUnlocksScreen.FirstSelected != null && EventSystem.current != null)
			{
				EventSystem.current.SetSelectedGameObject(itemUnlocksScreen.FirstSelected);
			}
		}
	}

	public void CloseItemUnlocks()
	{
		if (itemUnlocksScreen != null)
		{
			itemUnlocksScreen.CloseScreen();
		}
		if (firstSelected != null && EventSystem.current != null)
		{
			EventSystem.current.SetSelectedGameObject(firstSelected);
		}
	}

	public void OpenTwitter()
	{
		Application.OpenURL("https://x.com/PostNCalamity");
	}

	public void OpenSubscribeStar()
	{
		Application.OpenURL("https://subscribestar.adult/post-nut-calamity");
	}
}
