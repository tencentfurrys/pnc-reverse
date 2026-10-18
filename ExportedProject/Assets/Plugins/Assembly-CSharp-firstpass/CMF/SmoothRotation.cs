using UnityEngine;

namespace CMF
{
	public class SmoothRotation : MonoBehaviour
	{
		public enum UpdateType
		{
			Update = 0,
			LateUpdate = 1
		}

		public Transform target;

		private Transform tr;

		private Quaternion currentRotation;

		public float smoothSpeed = 20f;

		public bool extrapolateRotation;

		public UpdateType updateType;

		private void Awake()
		{
			if (target == null)
			{
				target = base.transform.parent;
			}
			tr = base.transform;
			currentRotation = base.transform.rotation;
		}

		private void OnEnable()
		{
			ResetCurrentRotation();
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
			currentRotation = Smooth(currentRotation, target.rotation, smoothSpeed);
			tr.rotation = currentRotation;
		}

		private Quaternion Smooth(Quaternion _currentRotation, Quaternion _targetRotation, float _smoothSpeed)
		{
			if (extrapolateRotation && Quaternion.Angle(_currentRotation, _targetRotation) < 90f)
			{
				Quaternion quaternion = _targetRotation * Quaternion.Inverse(_currentRotation);
				_targetRotation *= quaternion;
			}
			return Quaternion.Slerp(_currentRotation, _targetRotation, Time.deltaTime * _smoothSpeed);
		}

		public void ResetCurrentRotation()
		{
			currentRotation = target.rotation;
		}
	}
}
