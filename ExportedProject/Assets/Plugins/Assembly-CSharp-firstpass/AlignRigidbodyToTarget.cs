using UnityEngine;

public class AlignRigidbodyToTarget : MonoBehaviour
{
	public Transform target;

	private Transform tr;

	private Rigidbody r;

	private void Start()
	{
		tr = base.transform;
		r = GetComponent<Rigidbody>();
		if (target == null)
		{
			Debug.LogWarning("No target has been assigned.", this);
			base.enabled = false;
		}
	}

	private void FixedUpdate()
	{
		Vector3 forward = tr.forward;
		Vector3 normalized = (tr.position - target.position).normalized;
		Quaternion rotation = Quaternion.LookRotation(Quaternion.FromToRotation(tr.up, normalized) * forward, normalized);
		r.MoveRotation(rotation);
	}
}
