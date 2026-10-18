using System;
using UnityEngine;

namespace CMF
{
	public class AudioControl : MonoBehaviour
	{
		private Controller controller;

		private Animator animator;

		private Mover mover;

		private Transform tr;

		public AudioSource audioSource;

		public bool useAnimationBasedFootsteps = true;

		public float landVelocityThreshold = 5f;

		public float footstepDistance = 0.2f;

		private float currentFootstepDistance;

		private float currentFootStepValue;

		[Range(0f, 1f)]
		public float audioClipVolume = 0.1f;

		public float relativeRandomizedVolumeRange = 0.2f;

		public AudioClip[] footStepClips;

		public AudioClip jumpClip;

		public AudioClip landClip;

		private void Start()
		{
			controller = GetComponent<Controller>();
			animator = GetComponentInChildren<Animator>();
			mover = GetComponent<Mover>();
			tr = base.transform;
			Controller obj = controller;
			obj.OnLand = (Controller.VectorEvent)Delegate.Combine(obj.OnLand, new Controller.VectorEvent(OnLand));
			Controller obj2 = controller;
			obj2.OnJump = (Controller.VectorEvent)Delegate.Combine(obj2.OnJump, new Controller.VectorEvent(OnJump));
			if (!animator)
			{
				useAnimationBasedFootsteps = false;
			}
		}

		private void Update()
		{
			FootStepUpdate(VectorMath.RemoveDotVector(controller.GetVelocity(), tr.up).magnitude);
		}

		private void FootStepUpdate(float _movementSpeed)
		{
			float num = 0.05f;
			if (useAnimationBasedFootsteps)
			{
				float num2 = animator.GetFloat("FootStep");
				if (((currentFootStepValue <= 0f && num2 > 0f) || (currentFootStepValue >= 0f && num2 < 0f)) && mover.IsGrounded() && _movementSpeed > num)
				{
					PlayFootstepSound(_movementSpeed);
				}
				currentFootStepValue = num2;
				return;
			}
			currentFootstepDistance += Time.deltaTime * _movementSpeed;
			if (currentFootstepDistance > footstepDistance)
			{
				if (mover.IsGrounded() && _movementSpeed > num)
				{
					PlayFootstepSound(_movementSpeed);
				}
				currentFootstepDistance = 0f;
			}
		}

		private void PlayFootstepSound(float _movementSpeed)
		{
			int num = UnityEngine.Random.Range(0, footStepClips.Length);
			audioSource.PlayOneShot(footStepClips[num], audioClipVolume + audioClipVolume * UnityEngine.Random.Range(0f - relativeRandomizedVolumeRange, relativeRandomizedVolumeRange));
		}

		private void OnLand(Vector3 _v)
		{
			if (!(VectorMath.GetDotProduct(_v, tr.up) > 0f - landVelocityThreshold))
			{
				audioSource.PlayOneShot(landClip, audioClipVolume);
			}
		}

		private void OnJump(Vector3 _v)
		{
			audioSource.PlayOneShot(jumpClip, audioClipVolume);
		}
	}
}
