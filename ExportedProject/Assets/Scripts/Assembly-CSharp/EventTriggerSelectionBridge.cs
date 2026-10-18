using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class EventTriggerSelectionBridge : MonoBehaviour, ISelectHandler, IEventSystemHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler
{
	private EventTrigger eventTrigger;

	private bool pointerInside;

	private bool selected;

	private bool showingForSelection;

	public static void AttachAll(GameObject root)
	{
		if (root == null)
		{
			return;
		}
		Selectable[] componentsInChildren = root.GetComponentsInChildren<Selectable>(includeInactive: true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			GameObject gameObject = componentsInChildren[i].gameObject;
			if (!(gameObject.GetComponent<EventTrigger>() == null) && !(gameObject.GetComponent<EventTriggerSelectionBridge>() != null))
			{
				gameObject.AddComponent<EventTriggerSelectionBridge>();
			}
		}
	}

	private void Awake()
	{
		eventTrigger = GetComponent<EventTrigger>();
	}

	public void OnSelect(BaseEventData eventData)
	{
		selected = true;
		if (!pointerInside)
		{
			Fire(EventTriggerType.PointerEnter);
		}
	}

	public void OnDeselect(BaseEventData eventData)
	{
		selected = false;
		if (!pointerInside)
		{
			Fire(EventTriggerType.PointerExit);
		}
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		pointerInside = true;
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		pointerInside = false;
		if (selected)
		{
			Fire(EventTriggerType.PointerEnter);
		}
	}

	private void OnDisable()
	{
		if (showingForSelection)
		{
			Fire(EventTriggerType.PointerExit);
		}
		pointerInside = false;
		selected = false;
	}

	private void Fire(EventTriggerType type)
	{
		showingForSelection = type == EventTriggerType.PointerEnter;
		if (eventTrigger == null || eventTrigger.triggers == null)
		{
			return;
		}
		BaseEventData arg = new BaseEventData(EventSystem.current);
		for (int i = 0; i < eventTrigger.triggers.Count; i++)
		{
			EventTrigger.Entry entry = eventTrigger.triggers[i];
			if (entry != null && entry.eventID == type)
			{
				entry.callback.Invoke(arg);
			}
		}
	}
}
