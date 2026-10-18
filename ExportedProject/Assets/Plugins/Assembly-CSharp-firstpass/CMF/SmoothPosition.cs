using UnityEngine;

namespace CMF
{
	public class SmoothPosition : MonoBehaviour
	{
		public enum UpdateType
		{
			Update = 0,
			LateUpdate = 1
		}

		public enum SmoothType
		{
			Lerp = 0,
			SmoothDamp = 1
		}

		public Transform target;

		private Transform tr;

		private Vector3 currentPosition;

		public float lerpSpeed = 20f;

		public float smoothDampTime = 0.02f;

		public bool extrapolatePosition;

		public UpdateType updateType;

		public SmoothType smoothType;

		private Vector3 localPositionOffset;

		private Vector3 refVelocity;

		private void Awake()
		{
			if (target == null)
			{
				target = base.transform.parent;
			}
			tr = base.transform;
			currentPosition = base.transform.position;
			localPositionOffset = tr.localPosition;
		}

		private void OnEnable()
		{
			ResetCurrentPosition();
		}

		private void Update()
		{
			if (updateType != UpdateType.LateUpdate)
			{
				SmoothUpdate();
			}
		}

		private void LateUpdate()
		{
			if (updateType != UpdateType.Update)
			{
				SmoothUpdate();
			}
		}

		private void SmoothUpdate()
		{
			currentPosition = Smooth(currentPosition, target.position, lerpSpeed);
			tr.position = currentPosition;
		}

		private Vector3 Smooth(Vector3 _start, Vector3 _target, float _smoothTime)
		{
			Vector3 vector = tr.localToWorldMatrix * localPositionOffset;
			if (extrapolatePosition)
			{
				Vector3 vector2 = _target - (_start - vector);
				_target += vector2;
			}
			_target += vector;
			return smoothType switch
			{
				SmoothType.Lerp => Vector3.Lerp(_start, _target, Time.deltaTime * _smoothTime), 
				SmoothType.SmoothDamp => Vector3.SmoothDamp(_start, _target, ref refVelocity, smoothDampTime), 
				_ => Vector3.zero, 
			};
		}

		public void ResetCurrentPosition()
		{
			Vector3 vector = tr.localToWorldMatrix * localPositionOffset;
			currentPosition = target.position + vector;
		}
	}
}
