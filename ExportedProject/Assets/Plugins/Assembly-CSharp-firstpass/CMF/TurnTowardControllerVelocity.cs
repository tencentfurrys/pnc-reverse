using UnityEngine;

namespace CMF
{
	public class TurnTowardControllerVelocity : MonoBehaviour
	{
		public Controller controller;

		public float turnSpeed = 500f;

		private Transform parentTransform;

		private Transform tr;

		private float currentYRotation;

		private float fallOffAngle = 90f;

		public bool ignoreControllerMomentum;

		private void Start()
		{
			tr = base.transform;
			parentTransform = tr.parent;
			if (controller == null)
			{
				Debug.LogWarning("No controller script has been assigned to this 'TurnTowardControllerVelocity' component!", this);
				base.enabled = false;
			}
		}

		private void LateUpdate()
		{
			Vector3 vector = ((!ignoreControllerMomentum) ? controller.GetVelocity() : controller.GetMovementVelocity());
			vector = Vector3.ProjectOnPlane(vector, parentTransform.up);
			float num = 0.001f;
			if (!(vector.magnitude < num))
			{
				vector.Normalize();
				float angle = VectorMath.GetAngle(tr.forward, vector, parentTransform.up);
				float num2 = Mathf.InverseLerp(0f, fallOffAngle, Mathf.Abs(angle));
				float num3 = Mathf.Sign(angle) * num2 * Time.deltaTime * turnSpeed;
				if (angle < 0f && num3 < angle)
				{
					num3 = angle;
				}
				else if (angle > 0f && num3 > angle)
				{
					num3 = angle;
				}
				currentYRotation += num3;
				if (currentYRotation > 360f)
				{
					currentYRotation -= 360f;
				}
				if (currentYRotation < -360f)
				{
					currentYRotation += 360f;
				}
				tr.localRotation = Quaternion.Euler(0f, currentYRotation, 0f);
			}
		}

		private void OnDisable()
		{
		}

		private void OnEnable()
		{
			currentYRotation = base.transform.localEulerAngles.y;
		}
	}
}
