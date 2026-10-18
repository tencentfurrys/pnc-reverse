using UnityEngine;

namespace CMF
{
	public class ThirdPersonCameraController : CameraController
	{
		public bool turnCameraTowardMovementDirection = true;

		public Controller controller;

		public float maximumMovementSpeed = 7f;

		public float cameraTurnSpeed = 120f;

		protected override void Setup()
		{
			if (controller == null)
			{
				Debug.LogWarning("No controller reference has been assigned to this script.", base.gameObject);
			}
		}

		protected override void HandleCameraRotation()
		{
			base.HandleCameraRotation();
			if (!(controller == null) && turnCameraTowardMovementDirection && controller != null)
			{
				Vector3 velocity = controller.GetVelocity();
				RotateTowardsVelocity(velocity, cameraTurnSpeed);
			}
		}

		public void RotateTowardsVelocity(Vector3 _velocity, float _speed)
		{
			_velocity = VectorMath.RemoveDotVector(_velocity, GetUpDirection());
			float angle = VectorMath.GetAngle(GetFacingDirection(), _velocity, GetUpDirection());
			float num = Mathf.Sign(angle);
			float num2 = Time.deltaTime * _speed * num * Mathf.Abs(angle / 90f);
			if (Mathf.Abs(angle) > 90f)
			{
				num2 = Time.deltaTime * _speed * num * (Mathf.Abs(180f - Mathf.Abs(angle)) / 90f);
			}
			if (Mathf.Abs(num2) > Mathf.Abs(angle))
			{
				num2 = angle;
			}
			num2 *= Mathf.InverseLerp(0f, maximumMovementSpeed, _velocity.magnitude);
			SetRotationAngles(GetCurrentXAngle(), GetCurrentYAngle() + num2);
		}
	}
}
