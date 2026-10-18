using UnityEngine;

namespace CMF
{
	public class RotateObject : MonoBehaviour
	{
		private Transform tr;

		public float rotationSpeed = 20f;

		public Vector3 rotationAxis = new Vector3(0f, 1f, 0f);

		private void Start()
		{
			tr = base.transform;
		}

		private void Update()
		{
			tr.Rotate(rotationAxis * rotationSpeed * Time.deltaTime);
		}
	}
}
