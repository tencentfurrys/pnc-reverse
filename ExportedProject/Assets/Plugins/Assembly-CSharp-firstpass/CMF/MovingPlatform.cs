using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CMF
{
	public class MovingPlatform : MonoBehaviour
	{
		public float movementSpeed = 10f;

		public bool reverseDirection;

		public float waitTime = 1f;

		private bool isWaiting;

		private Rigidbody r;

		private TriggerArea triggerArea;

		public List<Transform> waypoints = new List<Transform>();

		private int currentWaypointIndex;

		private Transform currentWaypoint;

		private void Start()
		{
			r = GetComponent<Rigidbody>();
			triggerArea = GetComponentInChildren<TriggerArea>();
			r.freezeRotation = true;
			r.useGravity = false;
			r.isKinematic = true;
			if (waypoints.Count <= 0)
			{
				Debug.LogWarning("No waypoints have been assigned to 'MovingPlatform'!");
			}
			else
			{
				currentWaypoint = waypoints[currentWaypointIndex];
			}
			StartCoroutine(WaitRoutine());
			StartCoroutine(LateFixedUpdate());
		}

		private IEnumerator LateFixedUpdate()
		{
			WaitForFixedUpdate _instruction = new WaitForFixedUpdate();
			while (true)
			{
				yield return _instruction;
				MovePlatform();
			}
		}

		private void MovePlatform()
		{
			if (waypoints.Count <= 0 || isWaiting)
			{
				return;
			}
			Vector3 vector = currentWaypoint.position - base.transform.position;
			Vector3 normalized = vector.normalized;
			normalized *= movementSpeed * Time.deltaTime;
			if (normalized.magnitude >= vector.magnitude || normalized.magnitude == 0f)
			{
				r.transform.position = currentWaypoint.position;
				UpdateWaypoint();
			}
			else
			{
				r.transform.position += normalized;
			}
			if (!(triggerArea == null))
			{
				for (int i = 0; i < triggerArea.rigidbodiesInTriggerArea.Count; i++)
				{
					triggerArea.rigidbodiesInTriggerArea[i].MovePosition(triggerArea.rigidbodiesInTriggerArea[i].position + normalized);
				}
			}
		}

		private void UpdateWaypoint()
		{
			if (reverseDirection)
			{
				currentWaypointIndex--;
			}
			else
			{
				currentWaypointIndex++;
			}
			if (currentWaypointIndex >= waypoints.Count)
			{
				currentWaypointIndex = 0;
			}
			if (currentWaypointIndex < 0)
			{
				currentWaypointIndex = waypoints.Count - 1;
			}
			currentWaypoint = waypoints[currentWaypointIndex];
			isWaiting = true;
		}

		private IEnumerator WaitRoutine()
		{
			WaitForSeconds _waitInstruction = new WaitForSeconds(waitTime);
			while (true)
			{
				if (isWaiting)
				{
					yield return _waitInstruction;
					isWaiting = false;
				}
				yield return null;
			}
		}
	}
}
