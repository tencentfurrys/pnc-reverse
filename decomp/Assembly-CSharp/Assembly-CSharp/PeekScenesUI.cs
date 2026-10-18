using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PeekScenesUI : MonoBehaviour
{
	[Header("Peek Scenes")]
	[SerializeField]
	private List<EnemyGalleryEntry> allPeekScenes = new List<EnemyGalleryEntry>();

	[Header("Navigation")]
	[SerializeField]
	private Button leftArrowButton;

	[SerializeField]
	private Button rightArrowButton;

	[SerializeField]
	private TextMeshProUGUI indexText;

	[Header("Display")]
	[SerializeField]
	private TextMeshProUGUI peekSceneNameText;

	[SerializeField]
	private Image peekSceneIconImage;

	[SerializeField]
	private Animator peekSceneAnimator;

	[SerializeField]
	private GameObject peekSceneModel;

	[Header("Animation Controls")]
	[SerializeField]
	private TextMeshProUGUI animationNameText;

	[SerializeField]
	private Button prevAnimButton;

	[SerializeField]
	private Button nextAnimButton;

	[Header("Locked Display")]
	[SerializeField]
	private GameObject lockedDiorama;

	[SerializeField]
	private string lockedName = "???";

	[Header("Audio")]
	[SerializeField]
	private AudioSource audioSource;

	[Header("View Peek Scene Panel")]
	[SerializeField]
	private Button viewPeekSceneButton;

	[SerializeField]
	private GameObject peekScenePanel;

	[SerializeField]
	private Button exitPeekViewButton;

	[Header("Optional Back Button")]
	[SerializeField]
	private Button backButton;

	[Header("Controller Navigation")]
	[SerializeField]
	private GameObject firstSelected;

	[SerializeField]
	private GameObject peekPanelFirstSelected;

	private readonly List<EnemyGalleryEntry> displayedPeekScenes = new List<EnemyGalleryEntry>();

	private int currentEntryIndex;

	private int currentAnimIndex;

	private GrabAnimationData[] currentAnimations;

	private bool isViewingPeekScene;

	public GameObject FirstSelected => firstSelected;

	private void Start()
	{
		if (leftArrowButton != null)
		{
			leftArrowButton.onClick.AddListener(OnLeftArrowClicked);
		}
		if (rightArrowButton != null)
		{
			rightArrowButton.onClick.AddListener(OnRightArrowClicked);
		}
		if (prevAnimButton != null)
		{
			prevAnimButton.onClick.AddListener(OnPrevAnimClicked);
		}
		if (nextAnimButton != null)
		{
			nextAnimButton.onClick.AddListener(OnNextAnimClicked);
		}
		if (viewPeekSceneButton != null)
		{
			viewPeekSceneButton.onClick.AddListener(OnViewPeekSceneClicked);
		}
		if (exitPeekViewButton != null)
		{
			exitPeekViewButton.onClick.AddListener(OnExitPeekViewClicked);
		}
		if (backButton != null)
		{
			backButton.onClick.AddListener(CloseScreen);
		}
		RefreshDisplayList();
	}

	private void OnEnable()
	{
		RefreshDisplay();
	}

	public void RefreshDisplay()
	{
		RefreshDisplayList();
		DisplayCurrent();
	}

	private void OnDisable()
	{
		ExitPeekView();
		if (audioSource != null && audioSource.isPlaying)
		{
			audioSource.Stop();
		}
	}

	private void RefreshDisplayList()
	{
		displayedPeekScenes.Clear();
		foreach (EnemyGalleryEntry allPeekScene in allPeekScenes)
		{
			if (allPeekScene != null)
			{
				displayedPeekScenes.Add(allPeekScene);
			}
		}
		currentEntryIndex = ((displayedPeekScenes.Count > 0) ? Mathf.Clamp(currentEntryIndex, 0, displayedPeekScenes.Count - 1) : 0);
	}

	private void OnLeftArrowClicked()
	{
		if (displayedPeekScenes.Count != 0)
		{
			ExitPeekView();
			currentEntryIndex--;
			if (currentEntryIndex < 0)
			{
				currentEntryIndex = displayedPeekScenes.Count - 1;
			}
			DisplayCurrent();
		}
	}

	private void OnRightArrowClicked()
	{
		if (displayedPeekScenes.Count != 0)
		{
			ExitPeekView();
			currentEntryIndex++;
			if (currentEntryIndex >= displayedPeekScenes.Count)
			{
				currentEntryIndex = 0;
			}
			DisplayCurrent();
		}
	}

	private void DisplayCurrent()
	{
		if (displayedPeekScenes.Count == 0)
		{
			ShowEmpty();
			return;
		}
		EnemyGalleryEntry enemyGalleryEntry = displayedPeekScenes[currentEntryIndex];
		if (!(enemyGalleryEntry == null))
		{
			if (enemyGalleryEntry.alwaysUnlocked || (GalleryProgressManager.Instance != null && GalleryProgressManager.Instance.IsEnemyUnlocked(enemyGalleryEntry.enemyID)))
			{
				DisplayUnlocked(enemyGalleryEntry);
			}
			else
			{
				DisplayLocked(enemyGalleryEntry);
			}
			UpdateIndexDisplay();
			UpdateNavigationButtons();
		}
	}

	private void DisplayUnlocked(EnemyGalleryEntry entry)
	{
		if (lockedDiorama != null)
		{
			lockedDiorama.SetActive(value: false);
		}
		if (peekSceneModel != null)
		{
			peekSceneModel.SetActive(value: true);
		}
		if (peekSceneNameText != null)
		{
			peekSceneNameText.text = entry.enemyName;
		}
		if (peekSceneIconImage != null)
		{
			peekSceneIconImage.sprite = entry.enemyIcon;
		}
		if (peekSceneAnimator != null && entry.grabAnimatorController != null)
		{
			peekSceneAnimator.runtimeAnimatorController = entry.grabAnimatorController;
		}
		currentAnimations = entry.grabAnimations;
		currentAnimIndex = 0;
		UpdateViewPeekSceneButton(entry);
	}

	private void DisplayLocked(EnemyGalleryEntry entry)
	{
		if (lockedDiorama != null)
		{
			lockedDiorama.SetActive(value: true);
		}
		if (peekSceneModel != null)
		{
			peekSceneModel.SetActive(value: false);
		}
		if (peekSceneNameText != null)
		{
			peekSceneNameText.text = lockedName;
		}
		if (peekSceneIconImage != null)
		{
			peekSceneIconImage.sprite = null;
		}
		if (animationNameText != null)
		{
			animationNameText.text = "";
		}
		if (audioSource != null && audioSource.isPlaying)
		{
			audioSource.Stop();
		}
		currentAnimations = null;
		currentAnimIndex = 0;
		SetAnimButtonsInteractable(interactable: false);
		HideViewPeekSceneButton();
		ExitPeekView();
	}

	private void OnViewPeekSceneClicked()
	{
		if (displayedPeekScenes.Count == 0)
		{
			return;
		}
		EnemyGalleryEntry enemyGalleryEntry = displayedPeekScenes[currentEntryIndex];
		if (!(enemyGalleryEntry == null) && enemyGalleryEntry.HasGrabScene())
		{
			isViewingPeekScene = true;
			if (peekScenePanel != null)
			{
				peekScenePanel.SetActive(value: true);
			}
			if (peekSceneAnimator != null && enemyGalleryEntry.grabAnimatorController != null)
			{
				peekSceneAnimator.runtimeAnimatorController = enemyGalleryEntry.grabAnimatorController;
			}
			currentAnimations = enemyGalleryEntry.grabAnimations;
			currentAnimIndex = 0;
			UpdateAnimationButtons();
			PlayCurrentAnimation();
			if (peekPanelFirstSelected != null && EventSystem.current != null)
			{
				EventSystem.current.SetSelectedGameObject(peekPanelFirstSelected);
			}
		}
	}

	private void OnExitPeekViewClicked()
	{
		ExitPeekView();
		if (firstSelected != null && EventSystem.current != null)
		{
			EventSystem.current.SetSelectedGameObject(firstSelected);
		}
	}

	private void ExitPeekView()
	{
		if (isViewingPeekScene)
		{
			isViewingPeekScene = false;
			if (peekScenePanel != null)
			{
				peekScenePanel.SetActive(value: false);
			}
			if (audioSource != null && audioSource.isPlaying)
			{
				audioSource.Stop();
			}
		}
	}

	private void UpdateViewPeekSceneButton(EnemyGalleryEntry entry)
	{
		if (!(viewPeekSceneButton == null))
		{
			viewPeekSceneButton.gameObject.SetActive(entry != null && entry.HasGrabScene());
		}
	}

	private void HideViewPeekSceneButton()
	{
		if (viewPeekSceneButton != null)
		{
			viewPeekSceneButton.gameObject.SetActive(value: false);
		}
	}

	private void ShowEmpty()
	{
		if (peekSceneNameText != null)
		{
			peekSceneNameText.text = "No Peek Scenes";
		}
		if (animationNameText != null)
		{
			animationNameText.text = "";
		}
		if (indexText != null)
		{
			indexText.text = "0/0";
		}
		if (peekSceneModel != null)
		{
			peekSceneModel.SetActive(value: false);
		}
		if (lockedDiorama != null)
		{
			lockedDiorama.SetActive(value: false);
		}
		SetAnimButtonsInteractable(interactable: false);
		HideViewPeekSceneButton();
		ExitPeekView();
	}

	private void OnPrevAnimClicked()
	{
		if (currentAnimations != null && currentAnimations.Length != 0)
		{
			currentAnimIndex--;
			if (currentAnimIndex < 0)
			{
				currentAnimIndex = currentAnimations.Length - 1;
			}
			PlayCurrentAnimation();
		}
	}

	private void OnNextAnimClicked()
	{
		if (currentAnimations != null && currentAnimations.Length != 0)
		{
			currentAnimIndex++;
			if (currentAnimIndex >= currentAnimations.Length)
			{
				currentAnimIndex = 0;
			}
			PlayCurrentAnimation();
		}
	}

	private void PlayCurrentAnimation()
	{
		if (currentAnimations == null || currentAnimations.Length == 0)
		{
			return;
		}
		GrabAnimationData grabAnimationData = currentAnimations[currentAnimIndex];
		if (grabAnimationData != null)
		{
			if (peekSceneAnimator != null && !string.IsNullOrEmpty(grabAnimationData.animationName))
			{
				peekSceneAnimator.Play(grabAnimationData.animationName);
			}
			if (audioSource != null && grabAnimationData.sound != null)
			{
				audioSource.Stop();
				audioSource.clip = grabAnimationData.sound;
				audioSource.Play();
			}
			UpdateAnimationDisplay();
		}
	}

	private void UpdateAnimationDisplay()
	{
		if (!(animationNameText == null) && currentAnimations != null && currentAnimations.Length != 0)
		{
			GrabAnimationData grabAnimationData = currentAnimations[currentAnimIndex];
			animationNameText.text = ((grabAnimationData != null) ? grabAnimationData.animationName : "");
		}
	}

	private void UpdateAnimationButtons()
	{
		bool animButtonsInteractable = currentAnimations != null && currentAnimations.Length > 1;
		SetAnimButtonsInteractable(animButtonsInteractable);
	}

	private void SetAnimButtonsInteractable(bool interactable)
	{
		if (prevAnimButton != null)
		{
			prevAnimButton.interactable = interactable;
		}
		if (nextAnimButton != null)
		{
			nextAnimButton.interactable = interactable;
		}
	}

	private void UpdateIndexDisplay()
	{
		if (!(indexText == null))
		{
			indexText.text = ((displayedPeekScenes.Count > 0) ? $"{currentEntryIndex + 1}/{displayedPeekScenes.Count}" : "0/0");
		}
	}

	private void UpdateNavigationButtons()
	{
		bool interactable = displayedPeekScenes.Count > 1;
		if (leftArrowButton != null)
		{
			leftArrowButton.interactable = interactable;
		}
		if (rightArrowButton != null)
		{
			rightArrowButton.interactable = interactable;
		}
	}

	public void CloseScreen()
	{
		if (audioSource != null && audioSource.isPlaying)
		{
			audioSource.Stop();
		}
		gameObject.SetActive(value: false);
	}
}
