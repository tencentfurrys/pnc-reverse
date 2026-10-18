using PixelCrushers.GridController;
using UnityEngine;

public class LevelTransition : MonoBehaviour
{
	[Header("Scene Settings")]
	[Tooltip("Name of the scene to load (must match the scene name in Build Settings)")]
	[SerializeField]
	private string nextSceneName;

	[Header("End Screen")]
	[SerializeField]
	private bool isLastLevel;

	[SerializeField]
	private LevelEndScreen levelEndScreen;

	[Header("LoadingScreen Reference")]
	[Tooltip("Reference to the LoadingScreen prefab. Will try to find in Resources if not set.")]
	[SerializeField]
	private GameObject loadingScreenPrefab;

	private bool isTransitioning;

	private void Start()
	{
		Collider component = GetComponent<Collider>();
		if (component != null)
		{
			component.isTrigger = true;
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (IsPlayer(other) && !isTransitioning)
		{
			isTransitioning = true;
			TransitionToNextLevel();
		}
	}

	private bool IsPlayer(Collider other)
	{
		if (!other.CompareTag("Player") && !(other.GetComponent<FirstPersonController>() != null) && !(other.GetComponent<CharacterController>() != null) && !(other.GetComponent<GridInventorySystem>() != null))
		{
			return other.GetComponent<PlayerStats>() != null;
		}
		return true;
	}

	private void TransitionToNextLevel()
	{
		if (PlayerStateSaver.Instance != null)
		{
			PlayerStateSaver.Instance.SavePlayerState();
		}
		if (isLastLevel)
		{
			if (levelEndScreen == null)
			{
				levelEndScreen = Object.FindAnyObjectByType<LevelEndScreen>();
			}
			if (levelEndScreen != null)
			{
				levelEndScreen.ShowEndScreen();
			}
			return;
		}
		LoadingScreen loadingScreen = Object.FindAnyObjectByType<LoadingScreen>();
		if (loadingScreen == null)
		{
			GameObject gameObject = null;
			if (loadingScreenPrefab != null)
			{
				gameObject = Object.Instantiate(loadingScreenPrefab);
			}
			else
			{
				Debug.LogWarning("LevelTransition: loadingScreenPrefab not assigned! Falling back to Resources.Load which causes a frame spike. Assign the prefab in the Inspector.");
				GameObject gameObject2 = Resources.Load<GameObject>("LoadingScreen");
				if (gameObject2 != null)
				{
					gameObject = Object.Instantiate(gameObject2);
				}
			}
			if (gameObject != null)
			{
				loadingScreen = gameObject.GetComponent<LoadingScreen>();
			}
		}
		if (loadingScreen != null)
		{
			loadingScreen.LoadSceneWithLoadingScreen(nextSceneName);
		}
	}
}
