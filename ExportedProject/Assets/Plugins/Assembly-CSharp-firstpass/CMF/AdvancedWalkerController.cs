using UnityEngine;

namespace CMF
{
	public class AdvancedWalkerController : Controller
	{
		public enum ControllerState
		{
			Grounded = 0,
			Sliding = 1,
			Falling = 2,
			Rising = 3,
			Jumping = 4
		}

		protected Transform tr;

		protected Mover mover;

		protected CharacterInput characterInput;

		protected CeilingDetector ceilingDetector;

		private bool jumpInputIsLocked;

		private bool jumpKeyWasPressed;

		private bool jumpKeyWasLetGo;

		private bool jumpKeyIsPressed;

		public float movementSpeed = 7f;

		public float airControlRate = 2f;

		public float jumpSpeed = 10f;

		public float jumpDuration = 0.2f;

		private float currentJumpStartTime;

		public float airFriction = 0.5f;

		public float groundFriction = 100f;

		protected Vector3 momentum = Vector3.zero;

		private Vector3 savedVelocity = Vector3.zero;

		private Vector3 savedMovementVelocity = Vector3.zero;

		public float gravity = 30f;

		[Tooltip("How fast the character will slide down steep slopes.")]
		public float slideGravity = 5f;

		public float slopeLimit = 80f;

		[Tooltip("Whether to calculate and apply momentum relative to the controller's transform.")]
		public bool useLocalMomentum;

		[SerializeField]
		private float coyoteTime;

		private float lastGroundedTime;

		private ControllerState currentControllerState = ControllerState.Falling;

		[Tooltip("Optional camera transform used for calculating movement direction. If assigned, character movement will take camera view into account.")]
		public Transform cameraTransform;

		private void Awake()
		{
			mover = GetComponent<Mover>();
			tr = base.transform;
			characterInput = GetComponent<CharacterInput>();
			ceilingDetector = GetComponent<CeilingDetector>();
			if (characterInput == null)
			{
				Debug.LogWarning("No character input script has been attached to this gameobject", base.gameObject);
			}
			Setup();
		}

		protected virtual void Setup()
		{
		}

		public void SetCoyoteTime(float seconds)
		{
			coyoteTime = Mathf.Max(0f, seconds);
		}

		protected virtual void Update()
		{
			HandleJumpKeyInput();
		}

		protected void HandleJumpKeyInput()
		{
			bool flag = IsJumpKeyPressed();
			if (!jumpKeyIsPressed && flag)
			{
				jumpKeyWasPressed = true;
			}
			if (jumpKeyIsPressed && !flag)
			{
				jumpKeyWasLetGo = true;
				jumpInputIsLocked = false;
			}
			jumpKeyIsPressed = flag;
		}

		private void FixedUpdate()
		{
			ControllerUpdate();
		}

		private void ControllerUpdate()
		{
			mover.CheckForGround();
			currentControllerState = DetermineControllerState();
			HandleMomentum();
			HandleJumping();
			Vector3 velocity = Vector3.zero;
			if (currentControllerState == ControllerState.Grounded)
			{
				velocity = CalculateMovementVelocity();
			}
			Vector3 vector = momentum;
			if (useLocalMomentum)
			{
				vector = tr.localToWorldMatrix * momentum;
			}
			velocity += vector;
			mover.SetExtendSensorRange(IsGrounded());
			mover.SetVelocity(velocity);
			savedVelocity = velocity;
			savedMovementVelocity = CalculateMovementVelocity();
			jumpKeyWasLetGo = false;
			jumpKeyWasPressed = false;
			if (ceilingDetector != null)
			{
				ceilingDetector.ResetFlags();
			}
		}

		protected virtual Vector3 CalculateMovementDirection()
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

		protected virtual Vector3 CalculateMovementVelocity()
		{
			return CalculateMovementDirection() * movementSpeed;
		}

		protected virtual bool IsJumpKeyPressed()
		{
			if (characterInput == null)
			{
				return false;
			}
			return characterInput.IsJumpKeyPressed();
		}

		private ControllerState DetermineControllerState()
		{
			bool flag = IsRisingOrFalling() && VectorMath.GetDotProduct(GetMomentum(), tr.up) > 0f;
			bool flag2 = mover.IsGrounded() && IsGroundTooSteep();
			if (currentControllerState == ControllerState.Grounded)
			{
				if (flag)
				{
					OnGroundContactLost();
					return ControllerState.Rising;
				}
				if (!mover.IsGrounded())
				{
					if (coyoteTime > 0f && Time.time - lastGroundedTime < coyoteTime)
					{
						return ControllerState.Grounded;
					}
					OnGroundContactLost();
					return ControllerState.Falling;
				}
				if (flag2)
				{
					OnGroundContactLost();
					return ControllerState.Sliding;
				}
				lastGroundedTime = Time.time;
				return ControllerState.Grounded;
			}
			if (currentControllerState == ControllerState.Falling)
			{
				if (flag)
				{
					return ControllerState.Rising;
				}
				if (mover.IsGrounded() && !flag2)
				{
					OnGroundContactRegained();
					return ControllerState.Grounded;
				}
				if (flag2)
				{
					return ControllerState.Sliding;
				}
				return ControllerState.Falling;
			}
			if (currentControllerState == ControllerState.Sliding)
			{
				if (flag)
				{
					OnGroundContactLost();
					return ControllerState.Rising;
				}
				if (!mover.IsGrounded())
				{
					OnGroundContactLost();
					return ControllerState.Falling;
				}
				if (mover.IsGrounded() && !flag2)
				{
					OnGroundContactRegained();
					return ControllerState.Grounded;
				}
				return ControllerState.Sliding;
			}
			if (currentControllerState == ControllerState.Rising)
			{
				if (!flag)
				{
					if (mover.IsGrounded() && !flag2)
					{
						OnGroundContactRegained();
						return ControllerState.Grounded;
					}
					if (flag2)
					{
						return ControllerState.Sliding;
					}
					if (!mover.IsGrounded())
					{
						return ControllerState.Falling;
					}
				}
				if (ceilingDetector != null && ceilingDetector.HitCeiling())
				{
					OnCeilingContact();
					return ControllerState.Falling;
				}
				return ControllerState.Rising;
			}
			if (currentControllerState == ControllerState.Jumping)
			{
				if (Time.time - currentJumpStartTime > jumpDuration)
				{
					return ControllerState.Rising;
				}
				if (jumpKeyWasLetGo)
				{
					return ControllerState.Rising;
				}
				if (ceilingDetector != null && ceilingDetector.HitCeiling())
				{
					OnCeilingContact();
					return ControllerState.Falling;
				}
				return ControllerState.Jumping;
			}
			return ControllerState.Falling;
		}

		private void HandleJumping()
		{
			if (currentControllerState == ControllerState.Grounded && (jumpKeyIsPressed || jumpKeyWasPressed) && !jumpInputIsLocked)
			{
				OnGroundContactLost();
				OnJumpStart();
				currentControllerState = ControllerState.Jumping;
			}
		}

		private void HandleMomentum()
		{
			if (useLocalMomentum)
			{
				momentum = tr.localToWorldMatrix * momentum;
			}
			Vector3 vector = Vector3.zero;
			Vector3 vector2 = Vector3.zero;
			if (momentum != Vector3.zero)
			{
				vector = VectorMath.ExtractDotVector(momentum, tr.up);
				vector2 = momentum - vector;
			}
			vector -= tr.up * gravity * Time.deltaTime;
			if (currentControllerState == ControllerState.Grounded && VectorMath.GetDotProduct(vector, tr.up) < 0f)
			{
				vector = Vector3.zero;
			}
			if (!IsGrounded())
			{
				Vector3 vector3 = CalculateMovementVelocity();
				if (vector2.magnitude > movementSpeed)
				{
					if (VectorMath.GetDotProduct(vector3, vector2.normalized) > 0f)
					{
						vector3 = VectorMath.RemoveDotVector(vector3, vector2.normalized);
					}
					float num = 0.25f;
					vector2 += vector3 * Time.deltaTime * airControlRate * num;
				}
				else
				{
					vector2 += vector3 * Time.deltaTime * airControlRate;
					vector2 = Vector3.ClampMagnitude(vector2, movementSpeed);
				}
			}
			if (currentControllerState == ControllerState.Sliding)
			{
				Vector3 normalized = Vector3.ProjectOnPlane(mover.GetGroundNormal(), tr.up).normalized;
				Vector3 vector4 = CalculateMovementVelocity();
				vector4 = VectorMath.RemoveDotVector(vector4, normalized);
				vector2 += vector4 * Time.fixedDeltaTime;
			}
			vector2 = ((currentControllerState != ControllerState.Grounded) ? VectorMath.IncrementVectorTowardTargetVector(vector2, airFriction, Time.deltaTime, Vector3.zero) : VectorMath.IncrementVectorTowardTargetVector(vector2, groundFriction, Time.deltaTime, Vector3.zero));
			momentum = vector2 + vector;
			if (currentControllerState == ControllerState.Sliding)
			{
				momentum = Vector3.ProjectOnPlane(momentum, mover.GetGroundNormal());
				if (VectorMath.GetDotProduct(momentum, tr.up) > 0f)
				{
					momentum = VectorMath.RemoveDotVector(momentum, tr.up);
				}
				Vector3 normalized2 = Vector3.ProjectOnPlane(-tr.up, mover.GetGroundNormal()).normalized;
				momentum += normalized2 * slideGravity * Time.deltaTime;
			}
			if (currentControllerState == ControllerState.Jumping)
			{
				momentum = VectorMath.RemoveDotVector(momentum, tr.up);
				momentum += tr.up * jumpSpeed;
			}
			if (useLocalMomentum)
			{
				momentum = tr.worldToLocalMatrix * momentum;
			}
		}

		private void OnJumpStart()
		{
			if (useLocalMomentum)
			{
				momentum = tr.localToWorldMatrix * momentum;
			}
			momentum += tr.up * jumpSpeed;
			currentJumpStartTime = Time.time;
			jumpInputIsLocked = true;
			if (OnJump != null)
			{
				OnJump(momentum);
			}
			if (useLocalMomentum)
			{
				momentum = tr.worldToLocalMatrix * momentum;
			}
		}

		private void OnGroundContactLost()
		{
			if (useLocalMomentum)
			{
				momentum = tr.localToWorldMatrix * momentum;
			}
			Vector3 vector = GetMovementVelocity();
			if (vector.sqrMagnitude >= 0f && momentum.sqrMagnitude > 0f)
			{
				Vector3 vector2 = Vector3.Project(momentum, vector.normalized);
				float dotProduct = VectorMath.GetDotProduct(vector2.normalized, vector.normalized);
				if (vector2.sqrMagnitude >= vector.sqrMagnitude && dotProduct > 0f)
				{
					vector = Vector3.zero;
				}
				else if (dotProduct > 0f)
				{
					vector -= vector2;
				}
			}
			momentum += vector;
			if (useLocalMomentum)
			{
				momentum = tr.worldToLocalMatrix * momentum;
			}
		}

		private void OnGroundContactRegained()
		{
			if (OnLand != null)
			{
				Vector3 vector = momentum;
				if (useLocalMomentum)
				{
					vector = tr.localToWorldMatrix * vector;
				}
				OnLand(vector);
			}
		}

		private void OnCeilingContact()
		{
			if (useLocalMomentum)
			{
				momentum = tr.localToWorldMatrix * momentum;
			}
			momentum = VectorMath.RemoveDotVector(momentum, tr.up);
			if (useLocalMomentum)
			{
				momentum = tr.worldToLocalMatrix * momentum;
			}
		}

		private bool IsRisingOrFalling()
		{
			Vector3 vector = VectorMath.ExtractDotVector(GetMomentum(), tr.up);
			float num = 0.001f;
			return vector.magnitude > num;
		}

		private bool IsGroundTooSteep()
		{
			if (!mover.IsGrounded())
			{
				return true;
			}
			return Vector3.Angle(mover.GetGroundNormal(), tr.up) > slopeLimit;
		}

		public override Vector3 GetVelocity()
		{
			return savedVelocity;
		}

		public override Vector3 GetMovementVelocity()
		{
			return savedMovementVelocity;
		}

		public Vector3 GetMomentum()
		{
			Vector3 result = momentum;
			if (useLocalMomentum)
			{
				result = tr.localToWorldMatrix * momentum;
			}
			return result;
		}

		public override bool IsGrounded()
		{
			if (currentControllerState != ControllerState.Grounded)
			{
				return currentControllerState == ControllerState.Sliding;
			}
			return true;
		}

		public bool IsSliding()
		{
			return currentControllerState == ControllerState.Sliding;
		}

		public void AddMomentum(Vector3 _momentum)
		{
			if (useLocalMomentum)
			{
				momentum = tr.localToWorldMatrix * momentum;
			}
			momentum += _momentum;
			if (useLocalMomentum)
			{
				momentum = tr.worldToLocalMatrix * momentum;
			}
		}

		public void SetMomentum(Vector3 _newMomentum)
		{
			if (useLocalMomentum)
			{
				momentum = tr.worldToLocalMatrix * _newMomentum;
			}
			else
			{
				momentum = _newMomentum;
			}
		}
	}
}
