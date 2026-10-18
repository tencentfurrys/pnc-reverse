using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DioramaGalleryUI : MonoBehaviour
{
	[Serializable]
	public struct DioramaEntry
	{
		public string galleryID;

		public GameObject diorama;
	}

	[Header("Dioramas")]
	[SerializeField]
	private List<DioramaEntry> dioramas = new List<DioramaEntry>();

	[SerializeField]
	private GameObject lockedDiorama;

	[Header("Navigation")]
	[SerializeField]
	private Button leftArrowButton;

	[SerializeField]
	private Button rightArrowButton;

	[SerializeField]
	private TextMeshProUGUI indexText;

	[Header("Controller Navigation")]
	[SerializeField]
	private GameObject firstSelected;

	private int currentIndex;

	public GameObject FirstSelected => firstSelected;

	private void Start()
	{
		if (leftArrowButton != null)
		{
			leftArrowButton.onClick.AddListener(ShowPrevious);
		}
		if (rightArrowButton != null)
		{
			rightArrowButton.onClick.AddListener(ShowNext);
		}
		if (dioramas == null || dioramas.Count == 0)
		{
			Debug.LogWarning("DioramaGalleryUI: dioramas list is empty — populate it in the Inspector.", this);
		}
	}

	private void OnEnable()
	{
		if (GalleryProgressManager.Instance == null)
		{
			Debug.LogWarning("DioramaGalleryUI: GalleryProgressManager.Instance is null — no unlock state available.", this);
		}
		currentIndex = ((dioramas.Count > 0) ? Mathf.Clamp(currentIndex, 0, dioramas.Count - 1) : 0);
		RefreshDisplay();
	}

	public void RefreshDisplay()
	{
		DisplayCurrent();
	}

	private void OnDisable()
	{
		HideAllDioramas();
	}

	private void DisplayCurrent()
	{
		HideAllDioramas();
		if (dioramas.Count > 0)
		{
			DioramaEntry dioramaEntry = dioramas[currentIndex];
			if (dioramaEntry.diorama == null)
			{
				Debug.LogWarning($"DioramaGalleryUI: entry {currentIndex + 1} has no diorama GameObject assigned.", this);
			}
			else if (string.IsNullOrEmpty(dioramaEntry.galleryID))
			{
				Debug.LogWarning($"DioramaGalleryUI: entry {currentIndex + 1} has an empty galleryID.", this);
				ShowLocked();
			}
			else if (GalleryProgressManager.Instance != null && GalleryProgressManager.Instance.IsEnemyUnlocked(dioramaEntry.galleryID))
			{
				dioramaEntry.diorama.SetActive(value: true);
			}
			else
			{
				ShowLocked();
			}
		}
		UpdateIndexDisplay();
		UpdateNavigationButtons();
	}

	private void ShowLocked()
	{
		if (lockedDiorama != null)
		{
			lockedDiorama.SetActive(value: true);
		}
	}

	private void HideAllDioramas()
	{
		foreach (DioramaEntry diorama in dioramas)
		{
			if (diorama.diorama != null)
			{
				diorama.diorama.SetActive(value: false);
			}
		}
		if (lockedDiorama != null)
		{
			lockedDiorama.SetActive(value: false);
		}
	}

	public void ShowPrevious()
	{
		if (dioramas.Count != 0)
		{
			currentIndex--;
			if (currentIndex < 0)
			{
				currentIndex = dioramas.Count - 1;
			}
			DisplayCurrent();
		}
	}

	public void ShowNext()
	{
		if (dioramas.Count != 0)
		{
			currentIndex++;
			if (currentIndex >= dioramas.Count)
			{
				currentIndex = 0;
			}
			DisplayCurrent();
		}
	}

	private void UpdateIndexDisplay()
	{
		if (!(indexText == null))
		{
			indexText.text = ((dioramas.Count > 0) ? $"{currentIndex + 1}/{dioramas.Count}" : "0/0");
		}
	}

	private void UpdateNavigationButtons()
	{
		bool interactable = dioramas.Count > 1;
		if (leftArrowButton != null)
		{
			leftArrowButton.interactable = interactable;
		}
		if (rightArrowButton != null)
		{
			rightArrowButton.interactable = interactable;
		}
	}
}
