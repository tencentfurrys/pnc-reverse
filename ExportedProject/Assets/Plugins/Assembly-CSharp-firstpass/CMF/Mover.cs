using UnityEngine;

namespace CMF
{
	public class Mover : MonoBehaviour
	{
		[Header("Mover Options :")]
		[Range(0f, 1f)]
		[SerializeField]
		private float stepHeightRatio = 0.25f;

		[Header("Collider Options :")]
		[SerializeField]
		private float colliderHeight = 2f;

		[SerializeField]
		private float colliderThickness = 1f;

		[SerializeField]
		private Vector3 colliderOffset = Vector3.zero;

		private BoxCollider boxCollider;

		private SphereCollider sphereCollider;

		private CapsuleCollider capsuleCollider;

		[Header("Sensor Options :")]
		[SerializeField]
		public Sensor.CastType sensorType;

		private float sensorRadiusModifier = 0.8f;

		private int currentLayer;

		[SerializeField]
		private bool isInDebugMode;

		[Header("Sensor Array Options :")]
		[SerializeField]
		[Range(1f, 5f)]
		private int sensorArrayRows = 1;

		[SerializeField]
		[Range(3f, 10f)]
		private int sensorArrayRayCount = 6;

		[SerializeField]
		private bool sensorArrayRowsAreOffset;

		[HideInInspector]
		public Vector3[] raycastArrayPreviewPositions;

		private bool isGrounded;

		private bool IsUsingExtendedSensorRange = true;

		private float baseSensorRange;

		private Vector3 currentGroundAdjustmentVelocity = Vector3.zero;

		private Collider col;

		private Rigidbody rig;

		private Transform tr;

		private Sensor sensor;

		private void Awake()
		{
			Setup();
			sensor = new Sensor(tr, col);
			RecalculateColliderDimensions();
			RecalibrateSensor();
		}

		private void Reset()
		{
			Setup();
		}

		private void OnValidate()
		{
			if (base.gameObject.activeInHierarchy)
			{
				RecalculateColliderDimensions();
			}
			if (sensorType == Sensor.CastType.RaycastArray)
			{
				raycastArrayPreviewPositions = Sensor.GetRaycastStartPositions(sensorArrayRows, sensorArrayRayCount, sensorArrayRowsAreOffset, 1f);
			}
		}

		private void Setup()
		{
			tr = base.transform;
			col = GetComponent<Collider>();
			if (col == null)
			{
				tr.gameObject.AddComponent<CapsuleCollider>();
				col = GetComponent<Collider>();
			}
			rig = GetComponent<Rigidbody>();
			if (rig == null)
			{
				tr.gameObject.AddComponent<Rigidbody>();
				rig = GetComponent<Rigidbody>();
			}
			boxCollider = GetComponent<BoxCollider>();
			sphereCollider = GetComponent<SphereCollider>();
			capsuleCollider = GetComponent<CapsuleCollider>();
			rig.freezeRotation = true;
			rig.useGravity = false;
		}

		private void LateUpdate()
		{
			if (isInDebugMode)
			{
				sensor.DrawDebug();
			}
		}

		public void RecalculateColliderDimensions()
		{
			if (col == null)
			{
				Setup();
				if (col == null)
				{
					Debug.LogWarning("There is no collider attached to " + base.gameObject.name + "!");
					return;
				}
			}
			if ((bool)boxCollider)
			{
				Vector3 zero = Vector3.zero;
				zero.x = colliderThickness;
				zero.z = colliderThickness;
				boxCollider.center = colliderOffset * colliderHeight;
				zero.y = colliderHeight * (1f - stepHeightRatio);
				boxCollider.size = zero;
				boxCollider.center += new Vector3(0f, stepHeightRatio * colliderHeight / 2f, 0f);
			}
			else if ((bool)sphereCollider)
			{
				sphereCollider.radius = colliderHeight / 2f;
				sphereCollider.center = colliderOffset * colliderHeight;
				sphereCollider.center += new Vector3(0f, stepHeightRatio * sphereCollider.radius, 0f);
				sphereCollider.radius *= 1f - stepHeightRatio;
			}
			else if ((bool)capsuleCollider)
			{
				capsuleCollider.height = colliderHeight;
				capsuleCollider.center = colliderOffset * colliderHeight;
				capsuleCollider.radius = colliderThickness / 2f;
				capsuleCollider.center += new Vector3(0f, stepHeightRatio * capsuleCollider.height / 2f, 0f);
				capsuleCollider.height *= 1f - stepHeightRatio;
				if (capsuleCollider.height / 2f < capsuleCollider.radius)
				{
					capsuleCollider.radius = capsuleCollider.height / 2f;
				}
			}
			if (sensor != null)
			{
				RecalibrateSensor();
			}
		}

		private void RecalibrateSensor()
		{
			sensor.SetCastOrigin(GetColliderCenter());
			sensor.SetCastDirection(Sensor.CastDirection.Down);
			RecalculateSensorLayerMask();
			sensor.castType = sensorType;
			float num = colliderThickness / 2f * sensorRadiusModifier;
			float num2 = 0.001f;
			if ((bool)boxCollider)
			{
				num = Mathf.Clamp(num, num2, boxCollider.size.y / 2f * (1f - num2));
			}
			else if ((bool)sphereCollider)
			{
				num = Mathf.Clamp(num, num2, sphereCollider.radius * (1f - num2));
			}
			else if ((bool)capsuleCollider)
			{
				num = Mathf.Clamp(num, num2, capsuleCollider.height / 2f * (1f - num2));
			}
			sensor.sphereCastRadius = num * tr.localScale.x;
			float num3 = 0f;
			num3 += colliderHeight * (1f - stepHeightRatio) * 0.5f;
			num3 += colliderHeight * stepHeightRatio;
			baseSensorRange = num3 * (1f + num2) * tr.localScale.x;
			sensor.castLength = num3 * tr.localScale.x;
			sensor.ArrayRows = sensorArrayRows;
			sensor.arrayRayCount = sensorArrayRayCount;
			sensor.offsetArrayRows = sensorArrayRowsAreOffset;
			sensor.isInDebugMode = isInDebugMode;
			sensor.calculateRealDistance = true;
			sensor.calculateRealSurfaceNormal = true;
			sensor.RecalibrateRaycastArrayPositions();
		}

		private void RecalculateSensorLayerMask()
		{
			int num = 0;
			int layer = base.gameObject.layer;
			for (int i = 0; i < 32; i++)
			{
				if (!Physics.GetIgnoreLayerCollision(layer, i))
				{
					num |= 1 << i;
				}
			}
			if (num == (num | (1 << LayerMask.NameToLayer("Ignore Raycast"))))
			{
				num ^= 1 << LayerMask.NameToLayer("Ignore Raycast");
			}
			sensor.layermask = num;
			currentLayer = layer;
		}

		private Vector3 GetColliderCenter()
		{
			if (col == null)
			{
				Setup();
			}
			return col.bounds.center;
		}

		private void Check()
		{
			currentGroundAdjustmentVelocity = Vector3.zero;
			if (IsUsingExtendedSensorRange)
			{
				sensor.castLength = baseSensorRange + colliderHeight * tr.localScale.x * stepHeightRatio;
			}
			else
			{
				sensor.castLength = baseSensorRange;
			}
			sensor.Cast();
			if (!sensor.HasDetectedHit())
			{
				isGrounded = false;
				return;
			}
			isGrounded = true;
			float distance = sensor.GetDistance();
			float num = colliderHeight * tr.localScale.x * (1f - stepHeightRatio) * 0.5f + colliderHeight * tr.localScale.x * stepHeightRatio - distance;
			currentGroundAdjustmentVelocity = tr.up * (num / Time.fixedDeltaTime);
		}

		public void CheckForGround()
		{
			if (currentLayer != base.gameObject.layer)
			{
				RecalculateSensorLayerMask();
			}
			Check();
		}

		public void SetVelocity(Vector3 _velocity)
		{
			rig.linearVelocity = _velocity + currentGroundAdjustmentVelocity;
		}

		public bool IsGrounded()
		{
			return isGrounded;
		}

		public void SetExtendSensorRange(bool _isExtended)
		{
			IsUsingExtendedSensorRange = _isExtended;
		}

		public void SetColliderHeight(float _newColliderHeight)
		{
			if (colliderHeight != _newColliderHeight)
			{
				colliderHeight = _newColliderHeight;
				RecalculateColliderDimensions();
			}
		}

		public void SetColliderOffset(Vector3 _newColliderOffset)
		{
			if (!(colliderOffset == _newColliderOffset))
			{
				colliderOffset = _newColliderOffset;
				RecalculateColliderDimensions();
			}
		}

		public void SetColliderThickness(float _newColliderThickness)
		{
			if (colliderThickness != _newColliderThickness)
			{
				if (_newColliderThickness < 0f)
				{
					_newColliderThickness = 0f;
				}
				colliderThickness = _newColliderThickness;
				RecalculateColliderDimensions();
			}
		}

		public void SetStepHeightRatio(float _newStepHeightRatio)
		{
			_newStepHeightRatio = Mathf.Clamp(_newStepHeightRatio, 0f, 1f);
			stepHeightRatio = _newStepHeightRatio;
			RecalculateColliderDimensions();
		}

		public Vector3 GetGroundNormal()
		{
			return sensor.GetNormal();
		}

		public Vector3 GetGroundPoint()
		{
			return sensor.GetPosition();
		}

		public Collider GetGroundCollider()
		{
			return sensor.GetCollider();
		}
	}
}
