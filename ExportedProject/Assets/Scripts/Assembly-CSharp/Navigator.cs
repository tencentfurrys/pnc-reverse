using Pathfinding;
using Pathfinding.ECS;
using UnityEngine;

[RequireComponent(typeof(FollowerEntity))]
public class Navigator : MonoBehaviour
{
	[Header("Navigation Settings")]
	[Tooltip("Name of the target object to navigate to")]
	[SerializeField]
	private string targetObjectName = "Exit Destination";

	[Header("Component References")]
	[Tooltip("Reference to the FollowerEntity component (required)")]
	[SerializeField]
	private FollowerEntity followerEntity;

	[Tooltip("Auto-find FollowerEntity component if not assigned")]
	[SerializeField]
	private bool autoFindFollowerEntity = true;

	[Header("Lifetime Settings")]
	[Tooltip("Time in seconds before the navigator disappears")]
	[SerializeField]
	private float lifetime = 3f;

	[Header("Visual Settings")]
	[Tooltip("Optional: Line renderer to show path")]
	[SerializeField]
	private LineRenderer pathLineRenderer;

	[Tooltip("Optional: Trail renderer to show movement trail")]
	[SerializeField]
	private TrailRenderer trailRenderer;

	[Tooltip("Detach trail renderer on destroy so it fades naturally")]
	[SerializeField]
	private bool detachTrailOnDestroy = true;

	[Tooltip("Show debug information")]
	[SerializeField]
	private bool showDebug = true;

	private Transform targetDestination;

	private bool destinationFound;

	private float debugUpdateInterval = 1f;

	private float lastDebugTime;

	private void Start()
	{
		InitializeComponents();
		FindExitDestination();
		SetupPathfinding();
		DisableColliders();
		Object.Destroy(base.gameObject, lifetime);
	}

	private void DisableColliders()
	{
		Collider[] componentsInChildren = GetComponentsInChildren<Collider>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].enabled = false;
		}
	}

	private void InitializeComponents()
	{
		if (autoFindFollowerEntity && (Object)(object)followerEntity == null)
		{
			followerEntity = GetComponent<FollowerEntity>();
		}
		if ((Object)(object)followerEntity == null)
		{
			base.enabled = false;
		}
		else if (trailRenderer == null)
		{
			trailRenderer = GetComponent<TrailRenderer>();
		}
	}

	private void SetupPathfinding()
	{
		if (!((Object)(object)followerEntity == null))
		{
			followerEntity.simulateMovement = false;
			Pathfinding.ECS.AutoRepathPolicy autoRepath = followerEntity.autoRepath;
			autoRepath.mode = Pathfinding.AutoRepathPolicy.Mode.EveryNSeconds;
			followerEntity.autoRepath = autoRepath;
			if (destinationFound && targetDestination != null)
			{
				followerEntity.destination = targetDestination.position;
				followerEntity.simulateMovement = true;
				autoRepath = followerEntity.autoRepath;
				autoRepath.mode = Pathfinding.AutoRepathPolicy.Mode.EveryNSeconds;
				followerEntity.autoRepath = autoRepath;
			}
		}
	}

	private void FindExitDestination()
	{
		GameObject gameObject = GameObject.Find(targetObjectName);
		if (gameObject != null)
		{
			targetDestination = gameObject.transform;
			destinationFound = true;
		}
		else
		{
			destinationFound = false;
		}
	}

	private void Update()
	{
		if (destinationFound && (Object)(object)followerEntity != null && targetDestination != null)
		{
			if (!followerEntity.simulateMovement)
			{
				followerEntity.simulateMovement = true;
			}
			followerEntity.destination = targetDestination.position;
		}
	}

	public void SetDestination(Transform newDestination)
	{
		targetDestination = newDestination;
		destinationFound = true;
		if ((Object)(object)followerEntity != null)
		{
			followerEntity.destination = targetDestination.position;
			followerEntity.simulateMovement = true;
			Pathfinding.ECS.AutoRepathPolicy autoRepath = followerEntity.autoRepath;
			autoRepath.mode = Pathfinding.AutoRepathPolicy.Mode.EveryNSeconds;
			followerEntity.autoRepath = autoRepath;
		}
	}

	public void SetDestinationByName(string objectName)
	{
		targetObjectName = objectName;
		FindExitDestination();
		if (destinationFound && (Object)(object)followerEntity != null)
		{
			SetupPathfinding();
		}
	}

	private void OnDestroy()
	{
		if (detachTrailOnDestroy && trailRenderer != null)
		{
			trailRenderer.transform.SetParent(null);
			trailRenderer.emitting = false;
			float time = trailRenderer.time;
			Object.Destroy(trailRenderer.gameObject, time + 0.5f);
		}
	}

	private void OnDrawGizmos()
	{
		if (destinationFound && targetDestination != null)
		{
			Gizmos.color = Color.green;
			Gizmos.DrawLine(base.transform.position, targetDestination.position);
			Gizmos.DrawWireSphere(targetDestination.position, 0.5f);
		}
	}
}
