using UnityEngine;

namespace BlobShadowDecal;

public class SineBehaviour : MonoBehaviour
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

	private float timer;

	private Vector3 startPosition;

	private void Start()
	{
		startPosition = transform.position;
	}

	private void Update()
	{
		Sine();
	}

	private void Sine()
	{
		timer = Time.time;
		transform.position = startPosition + offset + new Vector3(xDistance, yDistance, zDistance) * Mathf.Sin(Time.time * speed);
	}
}
