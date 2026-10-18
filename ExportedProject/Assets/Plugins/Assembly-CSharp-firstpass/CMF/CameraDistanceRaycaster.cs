using UnityEngine;

namespace CMF
{
	public class CameraDistanceRaycaster : MonoBehaviour
	{
		public enum CastType
		{
			Raycast = 0,
			Spherecast = 1
		}

		public Transform cameraTransform;

		public Transform cameraTargetTransform;

		private Transform tr;

		public CastType castType;

		public LayerMask layerMask = -1;

		private int ignoreRaycastLayer;

		public Collider[] ignoreList;

		private int[] ignoreListLayers;

		private float currentDistance;

		public float minimumDistanceFromObstacles = 0.1f;

		public float smoothingFactor = 25f;

		public float spherecastRadius = 0.2f;

		private void Awake()
		{
			tr = base.transform;
			ignoreListLayers = new int[ignoreList.Length];
			ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
			if ((int)layerMask == ((int)layerMask | (1 << ignoreRaycastLayer)))
			{
				layerMask = (int)layerMask ^ (1 << ignoreRaycastLayer);
			}
			if (cameraTransform == null)
			{
				Debug.LogWarning("No camera transform has been assigned.", this);
			}
			if (cameraTargetTransform == null)
			{
				Debug.LogWarning("No camera target transform has been assigned.", this);
			}
			if (cameraTransform == null || cameraTargetTransform == null)
			{
				base.enabled = false;
			}
			else
			{
				currentDistance = (cameraTargetTransform.position - tr.position).magnitude;
			}
		}

		private void LateUpdate()
		{
			if (ignoreListLayers.Length != ignoreList.Length)
			{
				ignoreListLayers = new int[ignoreList.Length];
			}
			for (int i = 0; i < ignoreList.Length; i++)
			{
				ignoreListLayers[i] = ignoreList[i].gameObject.layer;
				ignoreList[i].gameObject.layer = ignoreRaycastLayer;
			}
			float cameraDistance = GetCameraDistance();
			for (int j = 0; j < ignoreList.Length; j++)
			{
				ignoreList[j].gameObject.layer = ignoreListLayers[j];
			}
			currentDistance = Mathf.Lerp(currentDistance, cameraDistance, Time.deltaTime * smoothingFactor);
			cameraTransform.position = tr.position + (cameraTargetTransform.position - tr.position).normalized * currentDistance;
		}

		private float GetCameraDistance()
		{
			Vector3 direction = cameraTargetTransform.position - tr.position;
			RaycastHit hitInfo;
			if (castType == CastType.Raycast)
			{
				if (Physics.Raycast(new Ray(tr.position, direction), out hitInfo, direction.magnitude + minimumDistanceFromObstacles, layerMask, QueryTriggerInteraction.Ignore))
				{
					if (hitInfo.distance - minimumDistanceFromObstacles < 0f)
					{
						return hitInfo.distance;
					}
					return hitInfo.distance - minimumDistanceFromObstacles;
				}
			}
			else if (Physics.SphereCast(new Ray(tr.position, direction), spherecastRadius, out hitInfo, direction.magnitude, layerMask, QueryTriggerInteraction.Ignore))
			{
				return hitInfo.distance;
			}
			return direction.magnitude;
		}
	}
}
