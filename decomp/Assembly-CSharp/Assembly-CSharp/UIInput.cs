using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

public static class UIInput
{
	private static InputSystemUIInputModule Module
	{
		get
		{
			EventSystem current = EventSystem.current;
			if (!(current != null))
			{
				return null;
			}
			return current.currentInputModule as InputSystemUIInputModule;
		}
	}

	public static bool SubmitPressedThisFrame()
	{
		InputSystemUIInputModule module = Module;
		if (module == null || module.submit == null)
		{
			return false;
		}
		return module.submit.action?.WasPerformedThisFrame() ?? false;
	}

	public static bool CancelPressedThisFrame()
	{
		InputSystemUIInputModule module = Module;
		if (module == null || module.cancel == null)
		{
			return false;
		}
		return module.cancel.action?.WasPerformedThisFrame() ?? false;
	}

	public static bool NavigatePressedThisFrame()
	{
		InputSystemUIInputModule module = Module;
		if (module == null || module.move == null)
		{
			return false;
		}
		return module.move.action?.WasPerformedThisFrame() ?? false;
	}

	public static Vector2 NavigateDirectionThisFrame()
	{
		InputSystemUIInputModule module = Module;
		if (module == null || module.move == null)
		{
			return Vector2.zero;
		}
		InputAction action = module.move.action;
		if (action == null || !action.WasPerformedThisFrame())
		{
			return Vector2.zero;
		}
		Vector2 vector = action.ReadValue<Vector2>();
		if (vector.sqrMagnitude < 0.25f)
		{
			return Vector2.zero;
		}
		if (!(Mathf.Abs(vector.x) > Mathf.Abs(vector.y)))
		{
			return new Vector2(0f, Mathf.Sign(vector.y));
		}
		return new Vector2(Mathf.Sign(vector.x), 0f);
	}

	public static bool NavigateOrSubmitPressedThisFrame()
	{
		if (!NavigatePressedThisFrame())
		{
			return SubmitPressedThisFrame();
		}
		return true;
	}
}
