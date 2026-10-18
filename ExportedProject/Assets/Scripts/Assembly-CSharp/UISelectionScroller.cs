using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(ScrollRect))]
public class UISelectionScroller : MonoBehaviour
{
	[SerializeField]
	private float padding = 8f;

	private ScrollRect scrollRect;

	private RectTransform viewport;

	private RectTransform content;

	private GameObject lastSelected;

	private void Awake()
	{
		scrollRect = GetComponent<ScrollRect>();
		viewport = ((scrollRect.viewport != null) ? scrollRect.viewport : ((RectTransform)scrollRect.transform));
		content = scrollRect.content;
	}

	private void LateUpdate()
	{
		if (content == null || viewport == null)
		{
			return;
		}
		GameObject gameObject = ((EventSystem.current != null) ? EventSystem.current.currentSelectedGameObject : null);
		if (gameObject == null || gameObject == lastSelected)
		{
			return;
		}
		if (!gameObject.transform.IsChildOf(content))
		{
			lastSelected = gameObject;
			return;
		}
		lastSelected = gameObject;
		RectTransform component = gameObject.GetComponent<RectTransform>();
		if (component != null)
		{
			SnapTo(component);
		}
	}

	private void SnapTo(RectTransform target)
	{
		Canvas.ForceUpdateCanvases();
		float height = viewport.rect.height;
		float height2 = content.rect.height;
		if (height2 <= height)
		{
			return;
		}
		Vector3[] array = new Vector3[4];
		Vector3[] array2 = new Vector3[4];
		viewport.GetWorldCorners(array);
		target.GetWorldCorners(array2);
		float y = array[1].y;
		float y2 = array[0].y;
		float y3 = array2[1].y;
		float y4 = array2[0].y;
		float num = y3 - (y - padding);
		float num2 = y2 + padding - y4;
		if (num <= 0f && num2 <= 0f)
		{
			return;
		}
		float num3 = height2 - height;
		if (!(num3 <= 0f))
		{
			float num4 = 0f;
			if (num > 0f)
			{
				num4 = num;
			}
			else if (num2 > 0f)
			{
				num4 = 0f - num2;
			}
			float verticalNormalizedPosition = scrollRect.verticalNormalizedPosition;
			float num5 = num4 / num3;
			scrollRect.verticalNormalizedPosition = Mathf.Clamp01(verticalNormalizedPosition + num5);
		}
	}
}
