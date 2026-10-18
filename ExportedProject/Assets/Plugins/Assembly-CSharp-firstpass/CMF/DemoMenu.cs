using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CMF
{
	public class DemoMenu : MonoBehaviour
	{
		public KeyCode menuKey = KeyCode.C;

		private DisableShadows disableShadows;

		private FPSCounter fpsCounter;

		public GameObject demoMenuObject;

		public List<GameObject> controllers = new List<GameObject>();

		public List<Button> buttons = new List<Button>();

		public Toggle shadowToggle;

		public GameObject regularArea;

		public GameObject topDownArea;

		public Color activeButtonColor = Color.cyan;

		private void Start()
		{
			disableShadows = GetComponent<DisableShadows>();
			fpsCounter = GetComponent<FPSCounter>();
			SetMenuEnabled(_isEnabled: false);
			disableShadows.SetShadows(PlayerData.enableShadows);
			shadowToggle.isOn = PlayerData.enableShadows;
			for (int i = 0; i < controllers.Count; i++)
			{
				controllers[i].SetActive(value: false);
			}
			controllers[PlayerData.controllerIndex].SetActive(value: true);
			if (PlayerData.controllerIndex >= 4)
			{
				regularArea.SetActive(value: false);
			}
			else
			{
				topDownArea.SetActive(value: false);
			}
			ColorBlock colors = buttons[PlayerData.controllerIndex].colors;
			colors.normalColor = activeButtonColor;
			colors.highlightedColor = activeButtonColor;
			colors.pressedColor = activeButtonColor;
			buttons[PlayerData.controllerIndex].colors = colors;
		}

		private void Update()
		{
			if (Input.GetKeyDown(menuKey))
			{
				SetMenuEnabled(!demoMenuObject.activeSelf);
			}
			if (Input.GetKeyDown(KeyCode.Escape))
			{
				SetMenuEnabled(!demoMenuObject.activeSelf);
			}
			if (Input.GetMouseButtonDown(0) && !demoMenuObject.activeSelf)
			{
				Cursor.lockState = CursorLockMode.Locked;
			}
		}

		public void RestartScene()
		{
			SceneManager.LoadScene(SceneManager.GetActiveScene().name);
		}

		public void QuitGame()
		{
			Application.Quit();
		}

		public void OnControllerPresetChosen(int _presetIndex)
		{
			PlayerData.controllerIndex = _presetIndex;
			RestartScene();
		}

		public void SetMenuEnabled(bool _isEnabled)
		{
			demoMenuObject.SetActive(_isEnabled);
			if (_isEnabled)
			{
				Cursor.lockState = CursorLockMode.None;
			}
			else
			{
				Cursor.lockState = CursorLockMode.Locked;
			}
		}

		public void SetShadowsEnabled(bool _isEnabled)
		{
			disableShadows.SetShadows(_isEnabled);
			PlayerData.enableShadows = _isEnabled;
		}

		public void SetFrameRateEnabled(bool _isEnabled)
		{
			fpsCounter.enabled = _isEnabled;
		}
	}
}
