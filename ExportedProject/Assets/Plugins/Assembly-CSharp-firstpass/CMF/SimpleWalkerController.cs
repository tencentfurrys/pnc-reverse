using UnityEngine;

namespace CMF
{
	public class SimpleWalkerController : Controller
	{
		private Mover mover;

		private float currentVerticalSpeed;

		private bool isGrounded;

		public float movementSpeed = 7f;

		public float jumpSpeed = 10f;

		public float gravity = 10f;

		private Vector3 lastVelocity = Vector3.zero;

		public Transform cameraTransform;

		private CharacterInput characterInput;

		private Transform tr;

		private void Start()
		{
			tr = base.transform;
			mover = GetComponent<Mover>();
			characterInput = GetComponent<CharacterInput>();
		}

		private void FixedUpdate()
		{
			mover.CheckForGround();
			if (!isGrounded && mover.IsGrounded())
			{
				OnGroundContactRegained(lastVelocity);
			}
			isGrounded = mover.IsGrounded();
			Vector3 zero = Vector3.zero;
			zero += CalculateMovementDirection() * movementSpeed;
			if (!isGrounded)
			{
				currentVerticalSpeed -= gravity * Time.deltaTime;
			}
			else if (currentVerticalSpeed <= 0f)
			{
				currentVerticalSpeed = 0f;
			}
			if (characterInput != null && isGrounded && characterInput.IsJumpKeyPressed())
			{
				OnJumpStart();
				currentVerticalSpeed = jumpSpeed;
				isGrounded = false;
			}
			zero += tr.up * currentVerticalSpeed;
			lastVelocity = zero;
			mover.SetExtendSensorRange(isGrounded);
			mover.SetVelocity(zero);
		}

		private Vector3 CalculateMovementDirection()
		{
			if (characterInput == null)
			{
				return Vector3.zero;
			}
			Vector3 zero = Vector3.zero;
			if (cameraTransform == null)
			{
				zero += tr.right * characterInput.GetHorizontalMovementInput();
				zero += tr.forward * characterInput.GetVerticalMovementInput();
			}
			else
			{
				zero += Vector3.ProjectOnPlane(cameraTransform.right, tr.up).normalized * characterInput.GetHorizontalMovementInput();
				zero += Vector3.ProjectOnPlane(cameraTransform.forward, tr.up).normalized * characterInput.GetVerticalMovementInput();
			}
			if (zero.magnitude > 1f)
			{
				zero.Normalize();
			}
			return zero;
		}

		private void OnGroundContactRegained(Vector3 _collisionVelocity)
		{
			if (OnLand != null)
			{
				OnLand(_collisionVelocity);
			}
		}

		private void OnJumpStart()
		{
			if (OnJump != null)
			{
				OnJump(lastVelocity);
			}
		}

		public override Vector3 GetVelocity()
		{
			return lastVelocity;
		}

		public override Vector3 GetMovementVelocity()
		{
			return lastVelocity;
		}

		public override bool IsGrounded()
		{
			return isGrounded;
		}
	}
}
