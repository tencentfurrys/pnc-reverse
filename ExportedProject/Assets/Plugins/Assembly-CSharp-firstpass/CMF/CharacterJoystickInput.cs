using UnityEngine;

namespace CMF
{
	public class CharacterJoystickInput : CharacterInput
	{
		public string horizontalInputAxis = "Horizontal";

		public string verticalInputAxis = "Vertical";

		public KeyCode jumpKey = KeyCode.Joystick1Button0;

		public bool useRawInput = true;

		public float deadZoneThreshold = 0.2f;

		public override float GetHorizontalMovementInput()
		{
			float num = ((!useRawInput) ? Input.GetAxis(horizontalInputAxis) : Input.GetAxisRaw(horizontalInputAxis));
			if (Mathf.Abs(num) < deadZoneThreshold)
			{
				num = 0f;
			}
			return num;
		}

		public override float GetVerticalMovementInput()
		{
			float num = ((!useRawInput) ? Input.GetAxis(verticalInputAxis) : Input.GetAxisRaw(verticalInputAxis));
			if (Mathf.Abs(num) < deadZoneThreshold)
			{
				num = 0f;
			}
			return num;
		}

		public override bool IsJumpKeyPressed()
		{
			return Input.GetKey(jumpKey);
		}
	}
}
