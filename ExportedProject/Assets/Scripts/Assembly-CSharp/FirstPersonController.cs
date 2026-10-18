using System.Text;
using CMF;
using UnityEngine;

public class FirstPersonController : AdvancedWalkerController
{
	[Header("Component References")]
	[SerializeField]
	private PlayerStats playerStats;

	[Header("Dash FOV Settings")]
	public float dashFOVIncrease = 20f;

	public float fovTransitionSpeed = 8f;

	[Header("Dash Particle Effects")]
	public ParticleSystem dashParticles;

	[Header("Dash Sound")]
	[SerializeField]
	private AudioSource audioSource;

	[SerializeField]
	private AudioClip dashSound;

	[Header("Crouch Settings")]
	public float crouchHeight = 1f;

	public float standingHeight = 2f;

	public float crouchTransitionSpeed = 10f;

	public Vector3 crouchColliderOffset = new Vector3(0f, 0.5f, 0f);

	public Vector3 standingColliderOffset = new Vector3(0f, 0.5f, 0f);

	[Header("Crouch Check")]
	public Transform crouchCheck;

	public float crouchCheckDistance = 0.4f;

	public LayerMask obstacleMask = 1;

	[Header("Ground Check")]
	public Transform groundCheck;

	public LayerMask groundMask = 1;

	public float groundCheckDistance = 0.3f;

	[Header("Debug")]
	[SerializeField]
	private bool showDebugInfo;

	public bool invincible;

	private Camera cameraComponent;

	private CameraController cameraController;

	private bool isCrouching;

	private bool isDashing;

	private float dashTimer;

	private int currentDashCharges;

	private float dashChargeRechargeTimer;

	private int rechargingCharges;

	private Vector3 dashDirection;

	private float originalFOV;

	private float targetFOV;

	private PlayerStats.MovementSpeeds currentSpeeds;

	public bool IsCrouching => isCrouching;

	public bool IsDashing => isDashing;

	public bool CanCurrentlyDash
	{
		get
		{
			if (playerStats != null && playerStats.CanDash && currentDashCharges > 0 && IsGrounded())
			{
				return !isCrouching;
			}
			return false;
		}
	}

	public float CurrentSpeed
	{
		get
		{
			if (!isCrouching)
			{
				return currentSpeeds.walkSpeed;
			}
			return currentSpeeds.crouchSpeed;
		}
	}

	public float SpeedMultiplier => currentSpeeds.speedMultiplier;

	protected override void Setup()
	{
		if (playerStats == null)
		{
			playerStats = GetComponent<PlayerStats>();
		}
		if (cameraTransform != null)
		{
			cameraController = cameraTransform.GetComponent<CameraController>();
			cameraComponent = cameraTransform.GetComponent<Camera>();
			if (cameraComponent == null)
			{
				cameraComponent = cameraTransform.GetComponentInChildren<Camera>();
			}
			if (cameraComponent != null)
			{
				originalFOV = cameraComponent.fieldOfView;
				targetFOV = originalFOV;
			}
		}
		Cursor.lockState = CursorLockMode.Locked;
		if (audioSource == null)
		{
			audioSource = GetComponent<AudioSource>();
		}
		if (audioSource == null)
		{
			audioSource = base.gameObject.AddComponent<AudioSource>();
		}
		if (crouchCheck == null)
		{
			GameObject gameObject = new GameObject("CrouchCheck");
			gameObject.transform.SetParent(base.transform);
			gameObject.transform.localPosition = new Vector3(0f, standingHeight, 0f);
			crouchCheck = gameObject.transform;
		}
		if (playerStats != null)
		{
			playerStats.OnMovementSpeedChanged.AddListener(OnMovementSpeedChanged);
			UpdateMovementSpeeds();
			currentDashCharges = currentSpeeds.maxDashCharges;
			rechargingCharges = 0;
			dashChargeRechargeTimer = 0f;
		}
	}

	private void OnDestroy()
	{
		if (playerStats != null)
		{
			playerStats.OnMovementSpeedChanged.RemoveListener(OnMovementSpeedChanged);
		}
	}

	protected override void Update()
	{
		if (playerStats != null && !playerStats.IsAlive)
		{
			if (cameraController != null && cameraController.enabled)
			{
				cameraController.enabled = false;
			}
		}
		else
		{
			base.Update();
			HandleCrouch();
			HandleDash();
			HandleFOVTransition();
		}
	}

	protected override Vector3 CalculateMovementVelocity()
	{
		if (playerStats != null && !playerStats.IsAlive)
		{
			return Vector3.zero;
		}
		if (isDashing)
		{
			return dashDirection * currentSpeeds.dashForce;
		}
		Vector3 vector = CalculateMovementDirection();
		float num = (isCrouching ? currentSpeeds.crouchSpeed : currentSpeeds.walkSpeed);
		return vector * num;
	}

	protected override bool IsJumpKeyPressed()
	{
		return false;
	}

	private void HandleCrouch()
	{
		if (InputManager.Crouch != null && InputManager.Crouch.WasPressedThisFrame())
		{
			if (isCrouching)
			{
				if (CanStandUp())
				{
					StandUp();
				}
			}
			else
			{
				CrouchDown();
			}
		}
		Vector3 localPosition = cameraTransform.localPosition;
		float b = (isCrouching ? (crouchHeight * 0.8f) : (standingHeight * 0.8f));
		localPosition.y = Mathf.Lerp(localPosition.y, b, Time.deltaTime * crouchTransitionSpeed);
		cameraTransform.localPosition = localPosition;
		UpdateCrouchCheckPosition();
	}

	private bool CanStandUp()
	{
		return !Physics.CheckSphere(crouchCheck.position, crouchCheckDistance, obstacleMask);
	}

	private void CrouchDown()
	{
		isCrouching = true;
		if (mover != null)
		{
			mover.SetColliderHeight(crouchHeight);
			mover.SetColliderOffset(crouchColliderOffset);
		}
	}

	private void StandUp()
	{
		isCrouching = false;
		if (mover != null)
		{
			mover.SetColliderHeight(standingHeight);
			mover.SetColliderOffset(standingColliderOffset);
		}
	}

	private void UpdateCrouchCheckPosition()
	{
		if (crouchCheck != null)
		{
			float y = (isCrouching ? (standingHeight * 0.9f) : (standingHeight * 0.5f));
			crouchCheck.localPosition = new Vector3(0f, y, 0f);
		}
	}

	private bool IsOnGround()
	{
		return Physics.CheckSphere((groundCheck != null) ? groundCheck.position : base.transform.position, groundCheckDistance, groundMask);
	}

	private void HandleDash()
	{
		if (rechargingCharges > 0)
		{
			dashChargeRechargeTimer -= Time.deltaTime;
			if (dashChargeRechargeTimer <= 0f)
			{
				currentDashCharges++;
				rechargingCharges--;
				if (rechargingCharges > 0)
				{
					dashChargeRechargeTimer = currentSpeeds.dashCooldown;
				}
			}
		}
		int num;
		if (!(playerStats == null))
		{
			num = (playerStats.CanDash ? 1 : 0);
			if (num == 0 && isDashing)
			{
				EndDash();
			}
		}
		else
		{
			num = 1;
		}
		if (num != 0 && InputManager.Dash != null && InputManager.Dash.WasPressedThisFrame() && currentDashCharges > 0 && IsOnGround() && !isCrouching)
		{
			Vector2 obj = ((InputManager.Move != null) ? InputManager.Move.ReadValue<Vector2>() : Vector2.zero);
			float x = obj.x;
			float y = obj.y;
			if (x != 0f || y != 0f)
			{
				StartDash(((cameraTransform != null) ? (Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized * x + Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized * y) : (base.transform.right * x + base.transform.forward * y)).normalized);
			}
		}
		if (isDashing)
		{
			if (Physics.Raycast(base.transform.position + Vector3.up * 0.5f, dashDirection, 1f, obstacleMask))
			{
				EndDash();
			}
			dashTimer -= Time.deltaTime;
			if (dashTimer <= 0f)
			{
				EndDash();
			}
		}
	}

	private void HandleFOVTransition()
	{
		if (cameraComponent != null)
		{
			cameraComponent.fieldOfView = Mathf.Lerp(cameraComponent.fieldOfView, targetFOV, fovTransitionSpeed * Time.deltaTime);
		}
	}

	private void StartDash(Vector3 direction)
	{
		isDashing = true;
		dashTimer = currentSpeeds.dashDuration;
		currentDashCharges--;
		rechargingCharges++;
		if (rechargingCharges == 1)
		{
			dashChargeRechargeTimer = currentSpeeds.dashCooldown;
		}
		dashDirection = direction;
		targetFOV = originalFOV + dashFOVIncrease;
		if (dashParticles != null)
		{
			dashParticles.Play();
		}
		if (audioSource != null && dashSound != null)
		{
			audioSource.PlayOneShot(dashSound);
		}
	}

	private void EndDash()
	{
		isDashing = false;
		targetFOV = originalFOV;
	}

	private void OnMovementSpeedChanged(float newMultiplier)
	{
		UpdateMovementSpeeds();
	}

	private void UpdateMovementSpeeds()
	{
		if (playerStats != null)
		{
			int maxDashCharges = currentSpeeds.maxDashCharges;
			currentSpeeds = playerStats.GetCurrentMovementSpeeds();
			int num = currentSpeeds.maxDashCharges - maxDashCharges;
			if (num > 0)
			{
				currentDashCharges += num;
			}
			else if (num < 0)
			{
				currentDashCharges = Mathf.Min(currentDashCharges, currentSpeeds.maxDashCharges);
				rechargingCharges = Mathf.Min(rechargingCharges, currentSpeeds.maxDashCharges - currentDashCharges);
			}
		}
	}

	public void ForceUpdateMovementSpeeds()
	{
		UpdateMovementSpeeds();
	}

	public void ForceCrouch()
	{
		if (!isCrouching)
		{
			CrouchDown();
		}
	}

	public void ForceStand()
	{
		if (isCrouching && CanStandUp())
		{
			StandUp();
		}
	}

	public bool CheckCanStandUp()
	{
		return CanStandUp();
	}

	public void ApplyExternalForce(Vector3 force)
	{
		AddMomentum(force);
	}

	public void SetPlayerStats(PlayerStats stats)
	{
		if (playerStats != null)
		{
			playerStats.OnMovementSpeedChanged.RemoveListener(OnMovementSpeedChanged);
		}
		playerStats = stats;
		if (playerStats != null)
		{
			playerStats.OnMovementSpeedChanged.AddListener(OnMovementSpeedChanged);
			UpdateMovementSpeeds();
		}
	}

	public string GetDebugInfo()
	{
		if (playerStats == null)
		{
			return "PlayerStats not found!";
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("=== FirstPersonController Debug ===");
		stringBuilder.AppendLine($"Walk Speed: {currentSpeeds.walkSpeed:F2}");
		stringBuilder.AppendLine($"Crouch Speed: {currentSpeeds.crouchSpeed:F2}");
		stringBuilder.AppendLine($"Dash Force: {currentSpeeds.dashForce:F2}");
		stringBuilder.AppendLine($"Speed Multiplier: {currentSpeeds.speedMultiplier:P0}");
		stringBuilder.AppendLine($"Is Grounded: {IsGrounded()}");
		stringBuilder.AppendLine($"Is Crouching: {isCrouching}");
		stringBuilder.AppendLine($"Is Dashing: {isDashing}");
		stringBuilder.AppendLine($"Can Dash: {playerStats.CanDash}");
		if (playerStats.IsAttackLocked())
		{
			stringBuilder.AppendLine("STATUS: OVERHEATED!");
		}
		return stringBuilder.ToString();
	}

	private void OnDrawGizmosSelected()
	{
		if (crouchCheck != null)
		{
			Gizmos.color = ((!Application.isPlaying || CanStandUp()) ? Color.blue : Color.magenta);
			Gizmos.DrawWireSphere(crouchCheck.position, crouchCheckDistance);
		}
	}

	private void OnGUI()
	{
		if (showDebugInfo && !(playerStats == null))
		{
			GUILayout.BeginArea(new Rect(10f, 500f, 300f, 200f));
			GUILayout.BeginVertical("box");
			GUILayout.Label("Movement Debug", GUI.skin.label);
			GUILayout.Label($"Current Speed: {CurrentSpeed:F2}");
			GUILayout.Label($"Speed Multiplier: {SpeedMultiplier:P0}");
			if (playerStats.IsAttackLocked())
			{
				GUI.color = Color.red;
				GUILayout.Label("OVERHEATED - MOVEMENT SLOWED!");
				GUILayout.Label("DASH DISABLED UNTIL COOLED!");
				GUI.color = Color.white;
			}
			if (playerStats.CanDash)
			{
				GUILayout.Label($"Dash Charges: {currentDashCharges}/{currentSpeeds.maxDashCharges}");
			}
			else
			{
				GUI.color = Color.red;
				GUILayout.Label("Dash: DISABLED (Overheated)");
				GUI.color = Color.white;
			}
			GUILayout.EndVertical();
			GUILayout.EndArea();
		}
	}
}
