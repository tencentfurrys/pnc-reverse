using UnityEngine;

namespace CMF
{
	public class CharacterKeyboardInput : CharacterInput
	{
		public string horizontalInputAxis = "Horizontal";

		public string verticalInputAxis = "Vertical";

		public KeyCode jumpKey = KeyCode.Space;

		public bool useRawInput = true;

		public override float GetHorizontalMovementInput()
		{
			if (useRawInput)
			{
				return Input.GetAxisRaw(horizontalInputAxis);
			}
			return Input.GetAxis(horizontalInputAxis);
		}

		public override float GetVerticalMovementInput()
		{
			if (useRawInput)
			{
				return Input.GetAxisRaw(verticalInputAxis);
			}
			return Input.GetAxis(verticalInputAxis);
		}

		public override bool IsJumpKeyPressed()
		{
			return Input.GetKey(jumpKey);
		}
	}
}
