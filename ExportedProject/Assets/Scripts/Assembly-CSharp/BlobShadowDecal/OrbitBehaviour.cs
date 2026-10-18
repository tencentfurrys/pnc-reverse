using UnityEngine;

namespace BlobShadowDecal
{
	public class OrbitBehaviour : MonoBehaviour
	{
		[SerializeField]
		private float xDistance;

		[SerializeField]
		private float yDistance;

		[SerializeField]
		private float zDistance;

		[SerializeField]
		private float speed = 1f;

		[SerializeField]
		private float amplitude;

		[SerializeField]
		private float frequency;

		[SerializeField]
		private Vector3 offset;

		[SerializeField]
		private bool cosX;

		[SerializeField]
		private bool cosY;

		[SerializeField]
		private bool cosZ;

		private float timer;

		private Vector3 startPosition;

		private void Start()
		{
			startPosition = base.transform.position;
		}

		private void Update()
		{
			Orbit();
		}

		private void Orbit()
		{
			float x = ((!cosX) ? (xDistance * Mathf.Sin(Time.time * speed)) : (xDistance * Mathf.Cos(Time.time * speed)));
			float y = ((!cosY) ? (yDistance * Mathf.Sin(Time.time * speed)) : (yDistance * Mathf.Cos(Time.time * speed)));
			float z = ((!cosZ) ? (zDistance * Mathf.Sin(Time.time * speed)) : (zDistance * Mathf.Cos(Time.time * speed)));
			base.transform.position = startPosition + offset + new Vector3(x, y, z);
		}
	}
}
