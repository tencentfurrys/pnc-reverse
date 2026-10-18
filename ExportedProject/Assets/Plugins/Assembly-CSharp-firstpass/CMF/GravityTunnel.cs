using System.Collections.Generic;
using UnityEngine;

namespace CMF
{
	public class GravityTunnel : MonoBehaviour
	{
		private List<Rigidbody> rigidbodies = new List<Rigidbody>();

		private void FixedUpdate()
		{
			for (int i = 0; i < rigidbodies.Count; i++)
			{
				Vector3 vector = Vector3.Project(rigidbodies[i].transform.position - base.transform.position, base.transform.position + base.transform.forward - base.transform.position) + base.transform.position;
				RotateRigidbody(rigidbodies[i].transform, (vector - rigidbodies[i].transform.position).normalized);
			}
		}

		private void OnTriggerEnter(Collider col)
		{
			Rigidbody component = col.GetComponent<Rigidbody>();
			if ((bool)component && !(col.GetComponent<Mover>() == null))
			{
				rigidbodies.Add(component);
			}
		}

		private void OnTriggerExit(Collider col)
		{
			Rigidbody component = col.GetComponent<Rigidbody>();
			if ((bool)component && !(col.GetComponent<Mover>() == null))
			{
				rigidbodies.Remove(component);
				RotateRigidbody(component.transform, Vector3.up);
				Vector3 eulerAngles = component.rotation.eulerAngles;
				eulerAngles.z = 0f;
				eulerAngles.x = 0f;
				component.MoveRotation(Quaternion.Euler(eulerAngles));
			}
		}

		private void RotateRigidbody(Transform _transform, Vector3 _targetDirection)
		{
			Rigidbody component = _transform.GetComponent<Rigidbody>();
			_targetDirection.Normalize();
			Quaternion quaternion = Quaternion.FromToRotation(_transform.up, _targetDirection);
			_ = _transform.rotation;
			Quaternion rotation = quaternion * _transform.rotation;
			component.MoveRotation(rotation);
		}

		private Quaternion GetCounterRotation(Quaternion _rotation)
		{
			_rotation.ToAngleAxis(out var angle, out var axis);
			Quaternion rotation = Quaternion.AngleAxis(Mathf.Sign(angle) * 180f, axis);
			return _rotation * Quaternion.Inverse(rotation);
		}
	}
}
