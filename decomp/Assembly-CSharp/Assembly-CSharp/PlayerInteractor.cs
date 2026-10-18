using PixelCrushers.GridController;
using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
	[Header("Raycast Settings")]
	[SerializeField]
	private Camera playerCamera;

	[SerializeField]
	private float maxInteractionRange = 5f;

	[Tooltip("Include both Interactable and environment layers (floors, walls)")]
	[SerializeField]
	private LayerMask interactionMask;

	[SerializeField]
	private KeyCode interactKey = KeyCode.E;

	[Header("Debug")]
	[SerializeField]
	private bool showGizmos = true;

	private IPlayerInteractable currentTarget;

	private Ray lastRay;

	private bool lastHit;

	private float lastHitDistance;

	private PauseMenuManager pauseMenu;

	private InventoryUI inventoryUI;

	private void Start()
	{
		if (playerCamera == null)
		{
			playerCamera = Camera.main;
		}
		pauseMenu = Object.FindAnyObjectByType<PauseMenuManager>();
		inventoryUI = Object.FindAnyObjectByType<InventoryUI>();
	}

	private void Update()
	{
		if (IsUIBlocking())
		{
			ClearTarget();
			return;
		}
		UpdateRaycast();
		if (currentTarget != null && InputManager.Interact != null && InputManager.Interact.WasPressedThisFrame() && currentTarget.CanInteract)
		{
			currentTarget.OnInteracted();
		}
	}

	private void UpdateRaycast()
	{
		Ray ray = (lastRay = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f)));
		lastHit = false;
		lastHitDistance = maxInteractionRange;
		if (Physics.Raycast(ray, out var hitInfo, maxInteractionRange, interactionMask, QueryTriggerInteraction.Collide))
		{
			IPlayerInteractable playerInteractable = hitInfo.collider.GetComponent<IPlayerInteractable>();
			if (playerInteractable == null)
			{
				playerInteractable = hitInfo.collider.GetComponentInParent<IPlayerInteractable>();
			}
			if (playerInteractable != null && playerInteractable.CanInteract)
			{
				lastHit = true;
				lastHitDistance = hitInfo.distance;
				if (playerInteractable != currentTarget)
				{
					ClearTarget();
					currentTarget = playerInteractable;
					currentTarget.OnLookedAt(gameObject);
				}
				return;
			}
		}
		ClearTarget();
	}

	private void ClearTarget()
	{
		if (currentTarget != null)
		{
			currentTarget.OnLookedAway();
			currentTarget = null;
		}
	}

	private bool IsUIBlocking()
	{
		if (LevelEndScreen.IsActive)
		{
			return true;
		}
		if (pauseMenu != null && pauseMenu.IsPaused)
		{
			return true;
		}
		if (inventoryUI != null && inventoryUI.IsInventoryOpen)
		{
			return true;
		}
		if (NoteViewer.Instance != null && NoteViewer.Instance.IsOpen)
		{
			return true;
		}
		return false;
	}

	private void OnDrawGizmos()
	{
		if (showGizmos && !(playerCamera == null))
		{
			Vector3 origin = lastRay.origin;
			Vector3 direction = lastRay.direction;
			float num = (lastHit ? lastHitDistance : maxInteractionRange);
			Vector3 vector = origin + direction * num;
			Gizmos.color = (lastHit ? Color.green : Color.red);
			Gizmos.DrawLine(origin, vector);
			if (lastHit)
			{
				Gizmos.color = Color.green;
				Gizmos.DrawSphere(vector, 0.05f);
			}
		}
	}
}
