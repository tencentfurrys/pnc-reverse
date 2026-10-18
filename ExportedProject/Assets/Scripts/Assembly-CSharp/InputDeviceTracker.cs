using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Utilities;

public class InputDeviceTracker : MonoBehaviour
{
	private static InputDeviceTracker instance;

	[SerializeField]
	private float mouseMoveThresholdSqr = 0.25f;

	private bool cursorHiddenByTracker;

	private InputDevice lastActiveGamepad;

	public static bool UsingGamepad { get; private set; }

	public static event Action OnDeviceChanged;

	private void Awake()
	{
		if (instance != null && instance != this)
		{
			UnityEngine.Object.Destroy(this);
			return;
		}
		instance = this;
		UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
		if (EventSystem.current == null)
		{
			Debug.LogWarning("[InputDeviceTracker] No EventSystem in scene — controller navigation will not work until one is added.");
		}
	}

	private void Update()
	{
		if (DetectGamepadActivity())
		{
			if (!UsingGamepad)
			{
				SetUsingGamepad(value: true);
			}
			Gamepad current = Gamepad.current;
			if (current != null && current != lastActiveGamepad)
			{
				bool num = lastActiveGamepad == null;
				lastActiveGamepad = current;
				if (!num)
				{
					OnDeviceChanged?.Invoke();
				}
			}
		}
		else if (DetectMouseActivity() && UsingGamepad)
		{
			SetUsingGamepad(value: false);
		}
	}

	private static bool DetectGamepadActivity()
	{
		Gamepad current = Gamepad.current;
		if (current == null)
		{
			return false;
		}
		if (current.leftStick.ReadValue().sqrMagnitude > 0.25f)
		{
			return true;
		}
		if (current.rightStick.ReadValue().sqrMagnitude > 0.25f)
		{
			return true;
		}
		if (current.dpad.ReadValue().sqrMagnitude > 0.1f)
		{
			return true;
		}
		ReadOnlyArray<InputControl> allControls = current.allControls;
		for (int i = 0; i < allControls.Count; i++)
		{
			if (allControls[i] is ButtonControl { wasPressedThisFrame: not false })
			{
				return true;
			}
		}
		return false;
	}

	private bool DetectMouseActivity()
	{
		Mouse current = Mouse.current;
		if (current == null)
		{
			return false;
		}
		if (current.delta.ReadValue().sqrMagnitude > mouseMoveThresholdSqr)
		{
			return true;
		}
		if (current.leftButton.wasPressedThisFrame)
		{
			return true;
		}
		if (current.rightButton.wasPressedThisFrame)
		{
			return true;
		}
		if (current.middleButton.wasPressedThisFrame)
		{
			return true;
		}
		return false;
	}

	private void SetUsingGamepad(bool value)
	{
		UsingGamepad = value;
		OnDeviceChanged?.Invoke();
		if (Cursor.lockState == CursorLockMode.Locked)
		{
			return;
		}
		if (value)
		{
			if (Cursor.visible)
			{
				Cursor.visible = false;
				cursorHiddenByTracker = true;
			}
		}
		else if (cursorHiddenByTracker)
		{
			Cursor.visible = true;
			cursorHiddenByTracker = false;
		}
	}
}
