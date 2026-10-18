using System.Collections.Generic;
using UnityEngine;

namespace CMF
{
	public class TriggerArea : MonoBehaviour
	{
		public List<Rigidbody> rigidbodiesInTriggerArea = new List<Rigidbody>();

		private void OnTriggerEnter(Collider col)
		{
			if (col.attachedRigidbody != null && col.GetComponent<Mover>() != null)
			{
				rigidbodiesInTriggerArea.Add(col.attachedRigidbody);
			}
		}

		private void OnTriggerExit(Collider col)
		{
			if (col.attachedRigidbody != null && col.GetComponent<Mover>() != null)
			{
				rigidbodiesInTriggerArea.Remove(col.attachedRigidbody);
			}
		}
	}
}
