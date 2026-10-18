using UnityEngine;

namespace CMF
{
	public class TurnTowardCameraDirection : MonoBehaviour
	{
		public CameraController cameraController;

		private Transform tr;

		private void Start()
		{
			tr = base.transform;
			if (cameraController == null)
			{
				Debug.LogWarning("No camera controller reference has been assigned to this script.", this);
			}
		}

		private void LateUpdate()
		{
			if ((bool)cameraController)
			{
				Vector3 facingDirection = cameraController.GetFacingDirection();
				Vector3 upDirection = cameraController.GetUpDirection();
				tr.rotation = Quaternion.LookRotation(facingDirection, upDirection);
			}
		}
	}
}
