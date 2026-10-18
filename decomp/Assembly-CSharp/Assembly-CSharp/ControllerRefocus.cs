using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ControllerRefocus : MonoBehaviour
{
	[Tooltip("Fallback target to focus when nothing is selected and controller/keyboard nav input fires.")]
	[SerializeField]
	private GameObject firstSelectedStatic;

	[Tooltip("Optional dynamic source. If it implements IDynamicFirstSelectable, its FirstSelectable.gameObject is used in preference to firstSelectedStatic.")]
	[SerializeField]
	private MonoBehaviour firstSelectedProvider;

	private static readonly List<ControllerRefocus> stack = new List<ControllerRefocus>();

	private void OnEnable()
	{
		stack.Remove(this);
		stack.Add(this);
	}

	private void OnDisable()
	{
		stack.Remove(this);
	}

	private void Update()
	{
		if (stack.Count == 0 || stack[stack.Count - 1] != this)
		{
			return;
		}
		EventSystem current = EventSystem.current;
		if (current == null)
		{
			return;
		}
		GameObject currentSelectedGameObject = current.currentSelectedGameObject;
		if ((!(currentSelectedGameObject != null) || !currentSelectedGameObject.activeInHierarchy) && UIInput.NavigateOrSubmitPressedThisFrame())
		{
			GameObject gameObject = ResolveTarget();
			if (gameObject != null && gameObject.activeInHierarchy)
			{
				current.SetSelectedGameObject(gameObject);
			}
		}
	}

	private GameObject ResolveTarget()
	{
		if (firstSelectedProvider is IDynamicFirstSelectable { FirstSelectable: var firstSelectable } && firstSelectable != null)
		{
			return firstSelectable.gameObject;
		}
		return firstSelectedStatic;
	}
}
