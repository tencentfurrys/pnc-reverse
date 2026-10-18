using UnityEngine;

namespace CMF
{
	public class CameraJoystickInput : CameraInput
	{
		public string joystickHorizontalAxis = "Joystick X";

		public string joystickVerticalAxis = "Joystick Y";

		public bool invertHorizontalInput;

		public bool invertVerticalInput;

		public float deadZoneThreshold = 0.2f;

		public override float GetHorizontalCameraInput()
		{
			float num = Input.GetAxisRaw(joystickHorizontalAxis);
			if (Mathf.Abs(num) < deadZoneThreshold)
			{
				num = 0f;
			}
			if (invertHorizontalInput)
			{
				return num *= -1f;
			}
			return num;
		}

		public override float GetVerticalCameraInput()
		{
			float num = Input.GetAxisRaw(joystickVerticalAxis);
			if (Mathf.Abs(num) < deadZoneThreshold)
			{
				num = 0f;
			}
			if (invertVerticalInput)
			{
				return num;
			}
			return num *= -1f;
		}
	}
}
