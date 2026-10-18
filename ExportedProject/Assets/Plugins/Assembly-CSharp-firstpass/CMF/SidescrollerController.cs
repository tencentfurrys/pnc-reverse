using UnityEngine;

namespace CMF
{
	public class SidescrollerController : AdvancedWalkerController
	{
		protected override Vector3 CalculateMovementDirection()
		{
			if (characterInput == null)
			{
				return Vector3.zero;
			}
			Vector3 zero = Vector3.zero;
			if (cameraTransform == null)
			{
				return zero + tr.right * characterInput.GetHorizontalMovementInput();
			}
			return zero + Vector3.ProjectOnPlane(cameraTransform.right, tr.up).normalized * characterInput.GetHorizontalMovementInput();
		}
	}
}
