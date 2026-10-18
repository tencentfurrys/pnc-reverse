using UnityEngine;

namespace FronkonGames.Retro.NTSC;

public sealed class Swing : MonoBehaviour
{
	[SerializeField]
	private Transform lookAt;

	[SerializeField]
	private Vector3 lookAtOffset = new Vector3(0f, 0.5f, 0f);

	[SerializeField]
	private Vector3 swingStrength = new Vector3(0.5f, 0.5f, 0.5f);

	[SerializeField]
	private Vector3 swingVelocity = new Vector3(0.25f, 0.5f, 0.25f);

	[SerializeField]
	private float smoothStart = 10f;

	private Vector3 originalPosition;

	private float progress;

	public void Reset()
	{
		originalPosition = transform.position;
		progress = 0f;
	}

	private void OnEnable()
	{
		Reset();
	}

	private void Update()
	{
		Vector3 vector = new Vector3(Mathf.Sin(Time.time * swingVelocity.x) * swingStrength.x, Mathf.Cos(Time.time * swingVelocity.y) * swingStrength.y, Mathf.Sin(Time.time * swingVelocity.z) * swingStrength.z);
		transform.position = originalPosition + vector * progress;
		if (lookAt != null && lookAtOffset.sqrMagnitude > 0f)
		{
			transform.LookAt(lookAt.position + lookAtOffset);
		}
		if (progress < 1f)
		{
			progress = Mathf.Clamp01(progress + Time.deltaTime * smoothStart);
		}
	}
}
