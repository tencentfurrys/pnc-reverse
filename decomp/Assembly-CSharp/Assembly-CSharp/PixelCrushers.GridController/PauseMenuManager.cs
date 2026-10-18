using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PixelCrushers.GridController;

public class PauseMenuManager : MonoBehaviour
{
	[Header("UI References")]
	[SerializeField]
	private GameObject pauseMenuPanel;

	[SerializeField]
	private GameObject optionsPanel;

	[Header("Sub-menus")]
	[Tooltip("Optional. Resolved from the options panel's hierarchy when left empty.")]
	[SerializeField]
	private OptionsMenu optionsMenu;

	[Header("Controller Navigation")]
	[Tooltip("Optional. Left empty, the first interactable Selectable under the pause panel is focused. The pause buttons live in a different prefab to this component, so leaving this empty avoids a per-scene override that has to be redone whenever either prefab changes.")]
	[SerializeField]
	private GameObject pauseFirstSelected;

	private InventoryUI inventoryUI;

	private bool isPaused;

	private CursorLockMode previousCursorLockState;

	private bool previousCursorVisible;

	private float focusRegainedTime = -1f;

	private const float FOCUS_COOLDOWN = 0.2f;

	private bool optionsWasActive;

	private readonly HashSet<GameObject> preparedPanels = new HashSet<GameObject>();

	public bool IsPaused => isPaused;

	private void Awake()
	{
		inventoryUI = Object.FindAnyObjectByType<InventoryUI>();
		if (optionsMenu == null && optionsPanel != null)
		{
			optionsMenu = optionsPanel.GetComponentInParent<OptionsMenu>(includeInactive: true);
		}
		if (pauseMenuPanel != null)
		{
			pauseMenuPanel.SetActive(value: false);
		}
		if (optionsPanel != null)
		{
			optionsPanel.SetActive(value: false);
		}
		optionsWasActive = false;
		previousCursorLockState = Cursor.lockState;
		previousCursorVisible = Cursor.visible;
	}

	private void OnApplicationFocus(bool hasFocus)
	{
		if (hasFocus)
		{
			focusRegainedTime = Time.unscaledTime;
			if (isPaused)
			{
				Time.timeScale = 0f;
				Cursor.lockState = CursorLockMode.None;
				Cursor.visible = true;
				ResetToDefaultPanels();
				optionsWasActive = false;
				FocusActivePanel();
			}
		}
	}

	private void Update()
	{
		if (!(Time.unscaledTime - focusRegainedTime < 0.2f))
		{
			bool num = InputManager.Pause != null && InputManager.Pause.WasPressedThisFrame();
			bool flag = isPaused && UIInput.CancelPressedThisFrame();
			if (num | flag)
			{
				HandleBackPressed();
			}
			else if (isPaused)
			{
				TrackPanelSelection();
			}
		}
	}

	private void HandleBackPressed()
	{
		if (!LevelEndScreen.IsActive && (!(NoteViewer.Instance != null) || (!NoteViewer.Instance.IsOpen && !NoteViewer.Instance.WasRecentlyClosed())) && (!(inventoryUI != null) || !inventoryUI.IsInventoryOpen))
		{
			if (optionsPanel != null && optionsPanel.activeSelf)
			{
				CloseOptions();
			}
			else if (isPaused)
			{
				ResumeGame();
			}
			else
			{
				PauseGame();
			}
		}
	}

	public void PauseGame()
	{
		if (!(inventoryUI != null) || !inventoryUI.IsInventoryOpen)
		{
			isPaused = true;
			Time.timeScale = 0f;
			ResetToDefaultPanels();
			optionsWasActive = false;
			previousCursorLockState = Cursor.lockState;
			previousCursorVisible = Cursor.visible;
			Cursor.lockState = CursorLockMode.None;
			Cursor.visible = true;
			InputManager.SwitchToUI(keepPauseAction: true);
			FocusActivePanel();
		}
	}

	public void ResumeGame()
	{
		isPaused = false;
		Time.timeScale = 1f;
		if (pauseMenuPanel != null)
		{
			pauseMenuPanel.SetActive(value: false);
		}
		if (optionsPanel != null)
		{
			optionsPanel.SetActive(value: false);
		}
		optionsWasActive = false;
		Cursor.lockState = previousCursorLockState;
		Cursor.visible = previousCursorVisible;
		if (EventSystem.current != null)
		{
			EventSystem.current.SetSelectedGameObject(null);
		}
		InputManager.SwitchToGameplay();
	}

	public void OpenOptions()
	{
		if (optionsPanel != null)
		{
			optionsPanel.SetActive(value: true);
		}
		if (pauseMenuPanel != null)
		{
			pauseMenuPanel.SetActive(value: false);
		}
		optionsWasActive = true;
		FocusActivePanel();
	}

	public void CloseOptions()
	{
		ResetToDefaultPanels();
		optionsWasActive = false;
		FocusActivePanel();
	}

	private void ResetToDefaultPanels()
	{
		if (pauseMenuPanel != null)
		{
			pauseMenuPanel.SetActive(value: true);
		}
		if (optionsPanel != null)
		{
			optionsPanel.SetActive(value: false);
		}
	}

	public void GoToMainMenu()
	{
		Time.timeScale = 1f;
		SceneManager.LoadScene("MainMenu");
	}

	private void TrackPanelSelection()
	{
		bool flag = optionsPanel != null && optionsPanel.activeSelf;
		if (flag != optionsWasActive)
		{
			optionsWasActive = flag;
			FocusActivePanel();
			return;
		}
		EventSystem current = EventSystem.current;
		if (!(current == null))
		{
			GameObject currentSelectedGameObject = current.currentSelectedGameObject;
			if ((!(currentSelectedGameObject != null) || !currentSelectedGameObject.activeInHierarchy) && UIInput.NavigateOrSubmitPressedThisFrame())
			{
				FocusActivePanel();
			}
		}
	}

	private void FocusActivePanel()
	{
		GameObject gameObject = ((optionsPanel != null && optionsPanel.activeSelf) ? optionsPanel : pauseMenuPanel);
		if (!(gameObject == null) && gameObject.activeInHierarchy)
		{
			PreparePanel(gameObject);
			GameObject gameObject2 = ResolveFirstSelected(gameObject);
			if (gameObject2 != null && EventSystem.current != null)
			{
				EventSystem.current.SetSelectedGameObject(gameObject2);
			}
		}
	}

	private void PreparePanel(GameObject panel)
	{
		if (preparedPanels.Add(panel))
		{
			EventTriggerSelectionBridge.AttachAll(panel);
			MenuNavigationWiring.WireVertical(panel);
		}
	}

	private GameObject ResolveFirstSelected(GameObject panel)
	{
		if (panel == pauseMenuPanel && pauseFirstSelected != null && pauseFirstSelected.activeInHierarchy)
		{
			return pauseFirstSelected;
		}
		if (panel == optionsPanel && optionsMenu != null && optionsMenu.FirstSelected != null && optionsMenu.FirstSelected.activeInHierarchy)
		{
			return optionsMenu.FirstSelected;
		}
		Selectable selectable = MenuNavigationWiring.FirstSelectable(panel);
		if (!(selectable != null))
		{
			return null;
		}
		return selectable.gameObject;
	}
}
