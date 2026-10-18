using System;
using UnityEngine;
using UnityEngine.UI;

public class OptionsTabs : MonoBehaviour
{
	[Serializable]
	public class Tab
	{
		public Button tabButton;

		public Selectable firstInContent;

		[Tooltip("Optional: a component implementing IDynamicFirstSelectable. If set, its FirstSelectable overrides firstInContent at runtime.")]
		public MonoBehaviour dynamicSource;
	}

	[SerializeField]
	private Tab[] tabs;

	[SerializeField]
	private int defaultTabIndex;

	private void Start()
	{
		if (tabs != null)
		{
			for (int i = 0; i < tabs.Length; i++)
			{
				if (tabs[i].dynamicSource is IDynamicFirstSelectable dynamicFirstSelectable)
				{
					dynamicFirstSelectable.EnsureBuilt();
				}
			}
		}
		SwitchTab(defaultTabIndex);
	}

	public void SwitchTab(int index)
	{
		if (tabs == null || index < 0 || index >= tabs.Length)
		{
			return;
		}
		Selectable selectOnDown = tabs[index].firstInContent;
		if (tabs[index].dynamicSource is IDynamicFirstSelectable { FirstSelectable: var firstSelectable } && firstSelectable != null)
		{
			selectOnDown = firstSelectable;
		}
		for (int i = 0; i < tabs.Length; i++)
		{
			Button tabButton = tabs[i].tabButton;
			if (!(tabButton == null))
			{
				Navigation navigation = tabButton.navigation;
				navigation.selectOnDown = selectOnDown;
				tabButton.navigation = navigation;
			}
		}
	}
}
