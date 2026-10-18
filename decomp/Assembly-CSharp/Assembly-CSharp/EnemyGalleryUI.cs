using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class EnemyGalleryUI : MonoBehaviour
{
	[Header("Gallery Configuration")]
	[SerializeField]
	private List<EnemyGalleryEntry> allEnemies = new List<EnemyGalleryEntry>();

	[Header("Navigation Buttons")]
	[SerializeField]
	private Button leftArrowButton;

	[SerializeField]
	private Button rightArrowButton;

	[Header("UI References")]
	[SerializeField]
	private GameObject galleryPanel;

	[Header("Enemy Details Panel")]
	[SerializeField]
	private TextMeshProUGUI enemyNameText;

	[SerializeField]
	private TextMeshProUGUI enemyDescriptionText;

	[SerializeField]
	private Image enemyIconImage;

	[SerializeField]
	private TextMeshProUGUI indexText;

	[Header("Animation Display")]
	[SerializeField]
	private Animator displayAnimator;

	[SerializeField]
	private GameObject displayModel;

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

	[SerializeField]
	private string lockedDescription = "Encounter this enemy to unlock.";

	[Header("Back Button")]
	[SerializeField]
	private Button backButton;

	[Header("Controller Navigation")]
	[SerializeField]
	private GameObject firstSelected;

	[SerializeField]
	private GameObject grabPanelFirstSelected;

	[Header("Grab Scene Display")]
	[SerializeField]
	private GameObject grabScenePanel;

	[SerializeField]
	private Animator grabSceneAnimator;

	[SerializeField]
	private GameObject grabDisplayModel;

	[Header("Grab Scene Controls")]
	[SerializeField]
	private Button viewGrabSceneButton;

	[SerializeField]
	private TextMeshProUGUI grabAnimNameText;

	[SerializeField]
	private Button prevGrabAnimButton;

	[SerializeField]
	private Button nextGrabAnimButton;

	[SerializeField]
	private Button exitGrabViewButton;

	[Header("Grab Scene Audio")]
	[SerializeField]
	private AudioSource grabAudioSource;

	private List<EnemyGalleryEntry> displayedEnemies = new List<EnemyGalleryEntry>();

	private int currentEnemyIndex;

	private bool showLockedEnemies = true;

	private int currentAnimationIndex;

	private string[] currentAnimations;

	private bool isViewingGrabScene;

	private int currentGrabAnimIndex;

	private GrabAnimationData[] currentGrabAnimations;

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
		if (backButton != null)
		{
			backButton.onClick.AddListener(CloseGallery);
		}
		if (prevAnimButton != null)
		{
			prevAnimButton.onClick.AddListener(OnPrevAnimationClicked);
		}
		if (nextAnimButton != null)
		{
			nextAnimButton.onClick.AddListener(OnNextAnimationClicked);
		}
		if (viewGrabSceneButton != null)
		{
			viewGrabSceneButton.onClick.AddListener(OnViewGrabSceneClicked);
		}
		if (exitGrabViewButton != null)
		{
			exitGrabViewButton.onClick.AddListener(OnExitGrabViewClicked);
		}
		if (prevGrabAnimButton != null)
		{
			prevGrabAnimButton.onClick.AddListener(OnPrevGrabAnimClicked);
		}
		if (nextGrabAnimButton != null)
		{
			nextGrabAnimButton.onClick.AddListener(OnNextGrabAnimClicked);
		}
		RefreshDisplayList();
	}

	public void RefreshDisplay()
	{
		RefreshDisplayList();
		DisplayCurrentEnemy();
	}

	private void RefreshDisplayList()
	{
		displayedEnemies.Clear();
		if (showLockedEnemies)
		{
			displayedEnemies.AddRange(allEnemies);
		}
		else if (GalleryProgressManager.Instance != null)
		{
			displayedEnemies = GalleryProgressManager.Instance.GetUnlockedEnemies();
		}
		if (displayedEnemies.Count > 0)
		{
			currentEnemyIndex = Mathf.Clamp(currentEnemyIndex, 0, displayedEnemies.Count - 1);
		}
		else
		{
			currentEnemyIndex = 0;
		}
	}

	private void OnLeftArrowClicked()
	{
		if (displayedEnemies.Count != 0)
		{
			ExitGrabView();
			currentEnemyIndex--;
			if (currentEnemyIndex < 0)
			{
				currentEnemyIndex = displayedEnemies.Count - 1;
			}
			DisplayCurrentEnemy();
		}
	}

	private void OnRightArrowClicked()
	{
		if (displayedEnemies.Count != 0)
		{
			ExitGrabView();
			currentEnemyIndex++;
			if (currentEnemyIndex >= displayedEnemies.Count)
			{
				currentEnemyIndex = 0;
			}
			DisplayCurrentEnemy();
		}
	}

	private void DisplayCurrentEnemy()
	{
		if (displayedEnemies == null || displayedEnemies.Count == 0)
		{
			ShowEmptyGallery();
			return;
		}
		EnemyGalleryEntry enemyGalleryEntry = displayedEnemies[currentEnemyIndex];
		if (!(enemyGalleryEntry == null))
		{
			if (enemyGalleryEntry.alwaysUnlocked || (GalleryProgressManager.Instance != null && GalleryProgressManager.Instance.IsEnemyUnlocked(enemyGalleryEntry.enemyID)))
			{
				DisplayUnlockedEnemy(enemyGalleryEntry);
			}
			else
			{
				DisplayLockedEnemy(enemyGalleryEntry);
			}
			UpdateNavigationButtons();
			UpdateIndexDisplay();
		}
	}

	private void DisplayUnlockedEnemy(EnemyGalleryEntry enemy)
	{
		if (lockedDiorama != null)
		{
			lockedDiorama.SetActive(value: false);
		}
		if (displayModel != null)
		{
			displayModel.SetActive(value: true);
		}
		if (enemyNameText != null)
		{
			enemyNameText.text = enemy.enemyName;
		}
		if (enemyDescriptionText != null)
		{
			enemyDescriptionText.text = enemy.enemyDescription;
		}
		if (enemyIconImage != null && enemy.enemyIcon != null)
		{
			enemyIconImage.sprite = enemy.enemyIcon;
		}
		if (displayAnimator != null && enemy.animatorController != null)
		{
			displayAnimator.runtimeAnimatorController = enemy.animatorController;
		}
		UpdateAnimationButtons(enemy);
		PlayCurrentAnimation();
		UpdateGrabSceneButton();
	}

	private void DisplayLockedEnemy(EnemyGalleryEntry enemy)
	{
		if (lockedDiorama != null)
		{
			lockedDiorama.SetActive(value: true);
		}
		if (displayModel != null)
		{
			displayModel.SetActive(value: false);
		}
		if (enemyNameText != null)
		{
			enemyNameText.text = lockedName;
		}
		if (enemyDescriptionText != null)
		{
			enemyDescriptionText.text = lockedDescription;
		}
		if (enemyIconImage != null)
		{
			enemyIconImage.sprite = null;
		}
		SetAnimationButtonsInteractable(interactable: false);
		HideGrabSceneButton();
	}

	private void ShowEmptyGallery()
	{
		if (enemyNameText != null)
		{
			enemyNameText.text = "No Enemies";
		}
		if (enemyDescriptionText != null)
		{
			enemyDescriptionText.text = "Encounter enemies in the game to unlock them for the gallery.";
		}
		if (indexText != null)
		{
			indexText.text = "0/0";
		}
		if (displayModel != null)
		{
			displayModel.SetActive(value: false);
		}
		SetAnimationButtonsInteractable(interactable: false);
	}

	private void UpdateAnimationButtons(EnemyGalleryEntry enemy)
	{
		currentAnimations = enemy.availableAnimations;
		currentAnimationIndex = 0;
		bool interactable = currentAnimations != null && currentAnimations.Length > 1;
		if (prevAnimButton != null)
		{
			prevAnimButton.interactable = interactable;
		}
		if (nextAnimButton != null)
		{
			nextAnimButton.interactable = interactable;
		}
		UpdateAnimationDisplay();
	}

	private void SetAnimationButtonsInteractable(bool interactable)
	{
		if (prevAnimButton != null)
		{
			prevAnimButton.interactable = interactable;
		}
		if (nextAnimButton != null)
		{
			nextAnimButton.interactable = interactable;
		}
		if (!interactable)
		{
			currentAnimations = null;
			currentAnimationIndex = 0;
			if (animationNameText != null)
			{
				animationNameText.text = "";
			}
		}
	}

	private void OnPrevAnimationClicked()
	{
		if (currentAnimations != null && currentAnimations.Length != 0)
		{
			currentAnimationIndex--;
			if (currentAnimationIndex < 0)
			{
				currentAnimationIndex = currentAnimations.Length - 1;
			}
			PlayCurrentAnimation();
		}
	}

	private void OnNextAnimationClicked()
	{
		if (currentAnimations != null && currentAnimations.Length != 0)
		{
			currentAnimationIndex++;
			if (currentAnimationIndex >= currentAnimations.Length)
			{
				currentAnimationIndex = 0;
			}
			PlayCurrentAnimation();
		}
	}

	private void PlayCurrentAnimation()
	{
		if (currentAnimations != null && currentAnimations.Length != 0)
		{
			string animationName = currentAnimations[currentAnimationIndex];
			PlayAnimation(animationName);
			UpdateAnimationDisplay();
		}
	}

	private void UpdateAnimationDisplay()
	{
		if (animationNameText != null && currentAnimations != null && currentAnimations.Length != 0)
		{
			animationNameText.text = currentAnimations[currentAnimationIndex];
		}
	}

	private void PlayAnimation(string animationName)
	{
		if (!(displayAnimator == null) && !string.IsNullOrEmpty(animationName))
		{
			displayAnimator.Play(animationName);
		}
	}

	private void UpdateNavigationButtons()
	{
		bool interactable = displayedEnemies.Count > 1;
		if (leftArrowButton != null)
		{
			leftArrowButton.interactable = interactable;
		}
		if (rightArrowButton != null)
		{
			rightArrowButton.interactable = interactable;
		}
	}

	private void UpdateIndexDisplay()
	{
		if (indexText != null)
		{
			indexText.text = $"{currentEnemyIndex + 1}/{displayedEnemies.Count}";
		}
	}

	public void OpenGallery()
	{
		if (galleryPanel != null)
		{
			galleryPanel.SetActive(value: true);
		}
		RefreshDisplayList();
		currentEnemyIndex = 0;
		DisplayCurrentEnemy();
		if (firstSelected != null && EventSystem.current != null)
		{
			EventSystem.current.SetSelectedGameObject(firstSelected);
		}
	}

	public void CloseGallery()
	{
		ExitGrabView();
		if (galleryPanel != null)
		{
			galleryPanel.SetActive(value: false);
		}
	}

	public void SetShowLockedEnemies(bool show)
	{
		showLockedEnemies = show;
		RefreshDisplayList();
		DisplayCurrentEnemy();
	}

	public EnemyGalleryEntry GetCurrentEnemy()
	{
		if (displayedEnemies != null && currentEnemyIndex >= 0 && currentEnemyIndex < displayedEnemies.Count)
		{
			EnemyGalleryEntry enemyGalleryEntry = displayedEnemies[currentEnemyIndex];
			if (enemyGalleryEntry.alwaysUnlocked || (GalleryProgressManager.Instance != null && GalleryProgressManager.Instance.IsEnemyUnlocked(enemyGalleryEntry.enemyID)))
			{
				return enemyGalleryEntry;
			}
		}
		return null;
	}

	private void UpdateGrabSceneButton()
	{
		if (!(viewGrabSceneButton == null))
		{
			EnemyGalleryEntry currentEnemy = GetCurrentEnemy();
			bool active = currentEnemy != null && currentEnemy.HasGrabScene();
			viewGrabSceneButton.gameObject.SetActive(active);
		}
	}

	private void HideGrabSceneButton()
	{
		if (viewGrabSceneButton != null)
		{
			viewGrabSceneButton.gameObject.SetActive(value: false);
		}
	}

	private void OnViewGrabSceneClicked()
	{
		EnemyGalleryEntry currentEnemy = GetCurrentEnemy();
		if (!(currentEnemy == null) && currentEnemy.HasGrabScene())
		{
			isViewingGrabScene = true;
			currentGrabAnimIndex = 0;
			currentGrabAnimations = currentEnemy.grabAnimations;
			if (grabScenePanel != null)
			{
				grabScenePanel.SetActive(value: true);
			}
			if (grabDisplayModel != null)
			{
				grabDisplayModel.SetActive(value: true);
			}
			if (grabSceneAnimator != null && currentEnemy.grabAnimatorController != null)
			{
				grabSceneAnimator.runtimeAnimatorController = currentEnemy.grabAnimatorController;
			}
			UpdateGrabAnimationButtons();
			PlayCurrentGrabAnimation();
			if (grabPanelFirstSelected != null && EventSystem.current != null)
			{
				EventSystem.current.SetSelectedGameObject(grabPanelFirstSelected);
			}
		}
	}

	private void OnExitGrabViewClicked()
	{
		ExitGrabView();
		if (firstSelected != null && EventSystem.current != null)
		{
			EventSystem.current.SetSelectedGameObject(firstSelected);
		}
	}

	private void ExitGrabView()
	{
		if (isViewingGrabScene)
		{
			isViewingGrabScene = false;
			currentGrabAnimIndex = 0;
			currentGrabAnimations = null;
			if (grabScenePanel != null)
			{
				grabScenePanel.SetActive(value: false);
			}
			if (grabDisplayModel != null)
			{
				grabDisplayModel.SetActive(value: false);
			}
			if (grabAudioSource != null && grabAudioSource.isPlaying)
			{
				grabAudioSource.Stop();
			}
		}
	}

	private void OnPrevGrabAnimClicked()
	{
		if (currentGrabAnimations != null && currentGrabAnimations.Length != 0)
		{
			currentGrabAnimIndex--;
			if (currentGrabAnimIndex < 0)
			{
				currentGrabAnimIndex = currentGrabAnimations.Length - 1;
			}
			PlayCurrentGrabAnimation();
		}
	}

	private void OnNextGrabAnimClicked()
	{
		if (currentGrabAnimations != null && currentGrabAnimations.Length != 0)
		{
			currentGrabAnimIndex++;
			if (currentGrabAnimIndex >= currentGrabAnimations.Length)
			{
				currentGrabAnimIndex = 0;
			}
			PlayCurrentGrabAnimation();
		}
	}

	private void PlayCurrentGrabAnimation()
	{
		if (currentGrabAnimations == null || currentGrabAnimations.Length == 0)
		{
			return;
		}
		GrabAnimationData grabAnimationData = currentGrabAnimations[currentGrabAnimIndex];
		if (grabAnimationData != null)
		{
			if (grabSceneAnimator != null && !string.IsNullOrEmpty(grabAnimationData.animationName))
			{
				grabSceneAnimator.Play(grabAnimationData.animationName);
			}
			if (grabAudioSource != null && grabAnimationData.sound != null)
			{
				grabAudioSource.Stop();
				grabAudioSource.clip = grabAnimationData.sound;
				grabAudioSource.Play();
			}
			UpdateGrabAnimationDisplay();
		}
	}

	private void UpdateGrabAnimationDisplay()
	{
		if (grabAnimNameText != null && currentGrabAnimations != null && currentGrabAnimations.Length != 0)
		{
			GrabAnimationData grabAnimationData = currentGrabAnimations[currentGrabAnimIndex];
			grabAnimNameText.text = ((grabAnimationData != null) ? grabAnimationData.animationName : "");
		}
	}

	private void UpdateGrabAnimationButtons()
	{
		bool interactable = currentGrabAnimations != null && currentGrabAnimations.Length > 1;
		if (prevGrabAnimButton != null)
		{
			prevGrabAnimButton.interactable = interactable;
		}
		if (nextGrabAnimButton != null)
		{
			nextGrabAnimButton.interactable = interactable;
		}
	}
}
