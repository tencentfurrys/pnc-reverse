using UnityEngine;

namespace CMF
{
	public class CameraController : MonoBehaviour
	{
		private float currentXAngle;

		private float currentYAngle;

		[Range(0f, 90f)]
		public float upperVerticalLimit = 60f;

		[Range(0f, 90f)]
		public float lowerVerticalLimit = 60f;

		private float oldHorizontalInput;

		private float oldVerticalInput;

		public float cameraSpeed = 250f;

		public bool smoothCameraRotation;

		[Range(1f, 50f)]
		public float cameraSmoothingFactor = 25f;

		private Vector3 facingDirection;

		private Vector3 upwardsDirection;

		protected Transform tr;

		protected Camera cam;

		protected CameraInput cameraInput;

		private void Awake()
		{
			tr = base.transform;
			cam = GetComponent<Camera>();
			cameraInput = GetComponent<CameraInput>();
			if (cameraInput == null)
			{
				Debug.LogWarning("No camera input script has been attached to this gameobject", base.gameObject);
			}
			if (cam == null)
			{
				cam = GetComponentInChildren<Camera>();
			}
			currentXAngle = tr.localRotation.eulerAngles.x;
			currentYAngle = tr.localRotation.eulerAngles.y;
			RotateCamera(0f, 0f);
			Setup();
		}

		protected virtual void Setup()
		{
		}

		private void Update()
		{
			HandleCameraRotation();
		}

		protected virtual void HandleCameraRotation()
		{
			if (!(cameraInput == null))
			{
				float horizontalCameraInput = cameraInput.GetHorizontalCameraInput();
				float verticalCameraInput = cameraInput.GetVerticalCameraInput();
				RotateCamera(horizontalCameraInput, verticalCameraInput);
			}
		}

		protected void RotateCamera(float _newHorizontalInput, float _newVerticalInput)
		{
			if (smoothCameraRotation)
			{
				oldHorizontalInput = Mathf.Lerp(oldHorizontalInput, _newHorizontalInput, Time.deltaTime * cameraSmoothingFactor);
				oldVerticalInput = Mathf.Lerp(oldVerticalInput, _newVerticalInput, Time.deltaTime * cameraSmoothingFactor);
			}
			else
			{
				oldHorizontalInput = _newHorizontalInput;
				oldVerticalInput = _newVerticalInput;
			}
			currentXAngle += oldVerticalInput * cameraSpeed * Time.deltaTime;
			currentYAngle += oldHorizontalInput * cameraSpeed * Time.deltaTime;
			currentXAngle = Mathf.Clamp(currentXAngle, 0f - upperVerticalLimit, lowerVerticalLimit);
			UpdateRotation();
		}

		protected void UpdateRotation()
		{
			tr.localRotation = Quaternion.Euler(new Vector3(0f, currentYAngle, 0f));
			facingDirection = tr.forward;
			upwardsDirection = tr.up;
			tr.localRotation = Quaternion.Euler(new Vector3(currentXAngle, currentYAngle, 0f));
		}

		public void SetFOV(float _fov)
		{
			if ((bool)cam)
			{
				cam.fieldOfView = _fov;
			}
		}

		public void SetRotationAngles(float _xAngle, float _yAngle)
		{
			currentXAngle = _xAngle;
			currentYAngle = _yAngle;
			UpdateRotation();
		}

		public void RotateTowardPosition(Vector3 _position, float _lookSpeed)
		{
			Vector3 direction = _position - tr.position;
			RotateTowardDirection(direction, _lookSpeed);
		}

		public void RotateTowardDirection(Vector3 _direction, float _lookSpeed)
		{
			_direction.Normalize();
			_direction = tr.parent.InverseTransformDirection(_direction);
			Vector3 aimingDirection = GetAimingDirection();
			aimingDirection = tr.parent.InverseTransformDirection(aimingDirection);
			float angle = VectorMath.GetAngle(new Vector3(0f, aimingDirection.y, 1f), new Vector3(0f, _direction.y, 1f), Vector3.right);
			aimingDirection.y = 0f;
			_direction.y = 0f;
			float angle2 = VectorMath.GetAngle(aimingDirection, _direction, Vector3.up);
			Vector2 vector = new Vector2(currentXAngle, currentYAngle);
			Vector2 vector2 = new Vector2(angle, angle2);
			float magnitude = vector2.magnitude;
			if (magnitude != 0f)
			{
				Vector2 vector3 = vector2 / magnitude;
				if (_lookSpeed * Time.deltaTime > magnitude)
				{
					vector += vector3 * magnitude;
				}
				else
				{
					vector += vector3 * _lookSpeed * Time.deltaTime;
				}
				currentYAngle = vector.y;
				currentXAngle = Mathf.Clamp(vector.x, 0f - upperVerticalLimit, lowerVerticalLimit);
				UpdateRotation();
			}
		}

		public float GetCurrentXAngle()
		{
			return currentXAngle;
		}

		public float GetCurrentYAngle()
		{
			return currentYAngle;
		}

		public Vector3 GetFacingDirection()
		{
			return facingDirection;
		}

		public Vector3 GetAimingDirection()
		{
			return tr.forward;
		}

		public Vector3 GetStrafeDirection()
		{
			return tr.right;
		}

		public Vector3 GetUpDirection()
		{
			return upwardsDirection;
		}
	}
}
