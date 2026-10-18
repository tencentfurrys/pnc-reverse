using System;
using UnityEngine;
using UnityEngine.UI;

public class GalleryTabController : MonoBehaviour
{
	[Serializable]
	public class Tab
	{
		public Button tabButton;

		public GameObject content;

		public Selectable firstInContent;

		[Header("Camera Animator Bools (set per tab)")]
		public bool enemyGalleryBool;

		public bool dioramaGalleryBool;
	}

	[Header("Tabs (Enemies, Diorama, Peek Scenes)")]
	[SerializeField]
	private Tab[] tabs;

	[SerializeField]
	private int defaultTabIndex;

	[Header("Camera Animation")]
	[SerializeField]
	private Animator cameraAnimator;

	[Header("Controller Navigation")]
	[SerializeField]
	private GameObject firstSelected;

	private const string EnemyGalleryParam = "EnemyGallery";

	private const string DioramaGalleryParam = "DioramaGallery";

	public GameObject FirstSelected => firstSelected;

	private void OnEnable()
	{
		SwitchTab(defaultTabIndex);
	}

	private void OnDisable()
	{
		if (cameraAnimator != null)
		{
			cameraAnimator.SetBool("EnemyGallery", value: false);
			cameraAnimator.SetBool("DioramaGallery", value: false);
		}
	}

	public void SwitchTab(int index)
	{
		if (tabs == null || index < 0 || index >= tabs.Length)
		{
			return;
		}
		for (int i = 0; i < tabs.Length; i++)
		{
			if (tabs[i].content != null)
			{
				tabs[i].content.SetActive(i == index);
			}
		}
		Selectable firstInContent = tabs[index].firstInContent;
		for (int j = 0; j < tabs.Length; j++)
		{
			Button tabButton = tabs[j].tabButton;
			if (!(tabButton == null))
			{
				Navigation navigation = tabButton.navigation;
				navigation.selectOnDown = firstInContent;
				tabButton.navigation = navigation;
			}
		}
		if (cameraAnimator != null)
		{
			cameraAnimator.SetBool("EnemyGallery", tabs[index].enemyGalleryBool);
			cameraAnimator.SetBool("DioramaGallery", tabs[index].dioramaGalleryBool);
		}
	}

	public void ShowEnemiesTab()
	{
		SwitchTab(0);
	}

	public void ShowDioramasTab()
	{
		SwitchTab(1);
	}

	public void ShowPeekScenesTab()
	{
		SwitchTab(2);
	}
}
