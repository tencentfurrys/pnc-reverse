using UnityEngine;

namespace CMF
{
	public class FlipAtRightAngle : MonoBehaviour
	{
		private AudioSource audioSource;

		private Transform tr;

		private void Start()
		{
			tr = base.transform;
			audioSource = GetComponent<AudioSource>();
		}

		private void OnTriggerEnter(Collider col)
		{
			if (!(col.GetComponent<Controller>() == null))
			{
				SwitchDirection(tr.forward, col.GetComponent<Controller>());
			}
		}

		private void SwitchDirection(Vector3 _newUpDirection, Controller _controller)
		{
			float num = 0.001f;
			if (!(Vector3.Angle(_newUpDirection, _controller.transform.up) < num))
			{
				audioSource.Play();
				Transform transform = _controller.transform;
				Quaternion quaternion = Quaternion.FromToRotation(transform.up, _newUpDirection);
				transform.rotation = quaternion * transform.rotation;
			}
		}
	}
}
