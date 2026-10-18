using CMF;
using UnityEngine;

public class InputSystemCharacterInput : CharacterInput
{
	private Vector2 move;

	private void Update()
	{
		if (InputManager.Move != null)
		{
			move = InputManager.Move.ReadValue<Vector2>();
		}
	}

	public override float GetHorizontalMovementInput()
	{
		return move.x;
	}

	public override float GetVerticalMovementInput()
	{
		return move.y;
	}

	public override bool IsJumpKeyPressed()
	{
		return false;
	}
}
