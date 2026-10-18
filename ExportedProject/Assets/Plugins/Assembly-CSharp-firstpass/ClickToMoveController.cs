using CMF;
using UnityEngine;

public class ClickToMoveController : Controller
{
	public enum MouseDetectionType
	{
		AbstractPlane = 0,
		Raycast = 1
	}

	public float movementSpeed = 10f;

	public float gravity = 30f;

	private float currentVerticalSpeed;

	private bool isGrounded;

	private Vector3 currentTargetPosition;

	private float reachTargetThreshold = 0.001f;

	public bool holdMouseButtonToMove;

	public MouseDetectionType mouseDetectionType;

	public LayerMask raycastLayerMask = -1;

	public float timeOutTime = 1f;

	private float currentTimeOutTime = 1f;

	public float timeOutDistanceThreshold = 0.05f;

	private Vector3 lastPosition;

	public Camera playerCamera;

	private bool hasTarget;

	private Vector3 lastVelocity = Vector3.zero;

	private Vector3 lastMovementVelocity = Vector3.zero;

	private Plane groundPlane;

	private Mover mover;

	private Transform tr;

	private void Start()
	{
		mover = GetComponent<Mover>();
		tr = base.transform;
		if (playerCamera == null)
		{
			Debug.LogWarning("No camera has been assigned to this controller!", this);
		}
		lastPosition = tr.position;
		currentTargetPosition = base.transform.position;
		groundPlane = new Plane(tr.up, tr.position);
	}

	private void Update()
	{
		HandleMouseInput();
	}

	private void FixedUpdate()
	{
		mover.CheckForGround();
		isGrounded = mover.IsGrounded();
		HandleTimeOut();
		Vector3 zero = Vector3.zero;
		zero = (lastMovementVelocity = CalculateMovementVelocity());
		HandleGravity();
		zero += tr.up * currentVerticalSpeed;
		mover.SetExtendSensorRange(isGrounded);
		mover.SetVelocity(zero);
		lastVelocity = zero;
	}

	private Vector3 CalculateMovementVelocity()
	{
		if (!hasTarget)
		{
			return Vector3.zero;
		}
		Vector3 vector = currentTargetPosition - tr.position;
		vector = VectorMath.RemoveDotVector(vector, tr.up);
		float magnitude = vector.magnitude;
		if (magnitude <= reachTargetThreshold)
		{
			hasTarget = false;
			return Vector3.zero;
		}
		Vector3 result = vector.normalized * movementSpeed;
		if (movementSpeed * Time.fixedDeltaTime > magnitude)
		{
			result = vector.normalized * magnitude;
			hasTarget = false;
		}
		return result;
	}

	private void HandleGravity()
	{
		if (!isGrounded)
		{
			currentVerticalSpeed -= gravity * Time.deltaTime;
			return;
		}
		if (currentVerticalSpeed < 0f && OnLand != null)
		{
			OnLand(tr.up * currentVerticalSpeed);
		}
		currentVerticalSpeed = 0f;
	}

	private void HandleMouseInput()
	{
		if (playerCamera == null || ((holdMouseButtonToMove || !WasMouseButtonJustPressed()) && (!holdMouseButtonToMove || !IsMouseButtonPressed())))
		{
			return;
		}
		Ray ray = playerCamera.ScreenPointToRay(GetMousePosition());
		if (mouseDetectionType == MouseDetectionType.AbstractPlane)
		{
			groundPlane.SetNormalAndPosition(tr.up, tr.position);
			float enter = 0f;
			if (groundPlane.Raycast(ray, out enter))
			{
				currentTargetPosition = ray.GetPoint(enter);
				hasTarget = true;
			}
			else
			{
				hasTarget = false;
			}
		}
		else if (mouseDetectionType == MouseDetectionType.Raycast)
		{
			if (Physics.Raycast(ray, out var hitInfo, 100f, raycastLayerMask, QueryTriggerInteraction.Ignore))
			{
				currentTargetPosition = hitInfo.point;
				hasTarget = true;
			}
			else
			{
				hasTarget = false;
			}
		}
	}

	private void HandleTimeOut()
	{
		if (!hasTarget)
		{
			currentTimeOutTime = 0f;
			return;
		}
		if (Vector3.Distance(tr.position, lastPosition) > timeOutDistanceThreshold)
		{
			currentTimeOutTime = 0f;
			lastPosition = tr.position;
			return;
		}
		currentTimeOutTime += Time.deltaTime;
		if (currentTimeOutTime >= timeOutTime)
		{
			hasTarget = false;
		}
	}

	protected Vector2 GetMousePosition()
	{
		return Input.mousePosition;
	}

	protected bool IsMouseButtonPressed()
	{
		return Input.GetMouseButton(0);
	}

	protected bool WasMouseButtonJustPressed()
	{
		return Input.GetMouseButtonDown(0);
	}

	public override bool IsGrounded()
	{
		return isGrounded;
	}

	public override Vector3 GetMovementVelocity()
	{
		return lastMovementVelocity;
	}

	public override Vector3 GetVelocity()
	{
		return lastVelocity;
	}
}
