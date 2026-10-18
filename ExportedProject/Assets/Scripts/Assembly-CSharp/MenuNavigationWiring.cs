using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class MenuNavigationWiring
{
	private class Row
	{
		public float Centre;

		public readonly List<Selectable> Items = new List<Selectable>();
	}

	public static void WireVertical(GameObject root, bool wrap = true)
	{
		if (root == null || !root.activeInHierarchy)
		{
			return;
		}
		Canvas.ForceUpdateCanvases();
		List<Row> list = BuildRows(Collect(root));
		if (list.Count == 0)
		{
			return;
		}
		for (int i = 0; i < list.Count; i++)
		{
			List<Selectable> items = list[i].Items;
			Row row = ((i > 0) ? list[i - 1] : (wrap ? list[list.Count - 1] : null));
			Row row2 = ((i < list.Count - 1) ? list[i + 1] : (wrap ? list[0] : null));
			if (list.Count == 1)
			{
				row = null;
				row2 = null;
			}
			for (int j = 0; j < items.Count; j++)
			{
				Navigation navigation = items[j].navigation;
				navigation.mode = Navigation.Mode.Explicit;
				navigation.selectOnUp = ((row != null) ? NearestByX(row, items[j]) : null);
				navigation.selectOnDown = ((row2 != null) ? NearestByX(row2, items[j]) : null);
				if (items[j] is Slider)
				{
					navigation.selectOnLeft = null;
					navigation.selectOnRight = null;
				}
				else
				{
					navigation.selectOnLeft = ((j > 0) ? items[j - 1] : null);
					navigation.selectOnRight = ((j < items.Count - 1) ? items[j + 1] : null);
				}
				items[j].navigation = navigation;
			}
		}
	}

	public static Selectable FirstSelectable(GameObject root)
	{
		if (root == null)
		{
			return null;
		}
		List<Selectable> list = Collect(root);
		if (list.Count <= 0)
		{
			return null;
		}
		return list[0];
	}

	private static List<Selectable> Collect(GameObject root)
	{
		List<Selectable> list = new List<Selectable>();
		Selectable[] componentsInChildren = root.GetComponentsInChildren<Selectable>(includeInactive: false);
		foreach (Selectable selectable in componentsInChildren)
		{
			if (!(selectable == null) && selectable.IsInteractable() && selectable.navigation.mode != Navigation.Mode.None && !(selectable is Scrollbar) && selectable.transform is RectTransform)
			{
				list.Add(selectable);
			}
		}
		return list;
	}

	private static List<Row> BuildRows(List<Selectable> selectables)
	{
		List<Row> list = new List<Row>();
		if (selectables.Count == 0)
		{
			return list;
		}
		selectables.Sort((Selectable a, Selectable b) =>
		{
			int num5 = CentreY(b).CompareTo(CentreY(a));
			return (num5 == 0) ? CentreX(a).CompareTo(CentreX(b)) : num5;
		});
		for (int num = 0; num < selectables.Count; num++)
		{
			float num2 = CentreY(selectables[num]);
			float num3 = Mathf.Max(8f, Height(selectables[num]) * 0.5f);
			if (list.Count == 0 || Mathf.Abs(list[list.Count - 1].Centre - num2) > num3)
			{
				list.Add(new Row
				{
					Centre = num2
				});
			}
			list[list.Count - 1].Items.Add(selectables[num]);
		}
		for (int num4 = 0; num4 < list.Count; num4++)
		{
			list[num4].Items.Sort((Selectable a, Selectable b) => CentreX(a).CompareTo(CentreX(b)));
		}
		return list;
	}

	private static Selectable NearestByX(Row row, Selectable from)
	{
		Selectable result = null;
		float num = float.MaxValue;
		float num2 = CentreX(from);
		for (int i = 0; i < row.Items.Count; i++)
		{
			float num3 = Mathf.Abs(CentreX(row.Items[i]) - num2);
			if (num3 < num)
			{
				num = num3;
				result = row.Items[i];
			}
		}
		return result;
	}

	private static float CentreY(Selectable s)
	{
		return s.transform.position.y;
	}

	private static float CentreX(Selectable s)
	{
		return s.transform.position.x;
	}

	private static float Height(Selectable s)
	{
		RectTransform rectTransform = (RectTransform)s.transform;
		return rectTransform.rect.height * rectTransform.lossyScale.y;
	}
}
