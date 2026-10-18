using CMF;
using UnityEngine;

public class InputSystemCameraInput : CameraInput
{
	public bool invertHorizontalInput;

	public bool invertVerticalInput;

	public float mouseInputMultiplier = 0.01f;

	public override float GetHorizontalCameraInput()
	{
		float num = ReadLookAxis(0);
		if (Time.timeScale > 0f && Time.deltaTime > 0f)
		{
			num /= Time.deltaTime;
			num *= Time.timeScale;
		}
		else
		{
			num = 0f;
		}
		num *= mouseInputMultiplier;
		if (invertHorizontalInput)
		{
			num *= -1f;
		}
		return num;
	}

	public override float GetVerticalCameraInput()
	{
		float num = 0f - ReadLookAxis(1);
		if (Time.timeScale > 0f && Time.deltaTime > 0f)
		{
			num /= Time.deltaTime;
			num *= Time.timeScale;
		}
		else
		{
			num = 0f;
		}
		num *= mouseInputMultiplier;
		if (invertVerticalInput)
		{
			num *= -1f;
		}
		return num;
	}

	private float ReadLookAxis(int axis)
	{
		if (InputManager.Look == null)
		{
			return 0f;
		}
		Vector2 vector = InputManager.Look.ReadValue<Vector2>();
		if (axis != 0)
		{
			return vector.y;
		}
		return vector.x;
	}
}
