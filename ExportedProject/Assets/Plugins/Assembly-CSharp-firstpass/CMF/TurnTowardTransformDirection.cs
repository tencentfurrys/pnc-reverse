using UnityEngine;

namespace CMF
{
	public class TurnTowardTransformDirection : MonoBehaviour
	{
		public Transform targetTransform;

		private Transform tr;

		private Transform parentTransform;

		private void Start()
		{
			tr = base.transform;
			parentTransform = base.transform.parent;
			if (targetTransform == null)
			{
				Debug.LogWarning("No target transform has been assigned to this script.", this);
			}
		}

		private void LateUpdate()
		{
			if ((bool)targetTransform)
			{
				Vector3 normalized = Vector3.ProjectOnPlane(targetTransform.forward, parentTransform.up).normalized;
				Vector3 up = parentTransform.up;
				tr.rotation = Quaternion.LookRotation(normalized, up);
			}
		}
	}
}
