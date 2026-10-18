using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace BlobShadowDecal
{
	public class ShadowDecal : MonoBehaviour
	{
		[Header("References")]
		[SerializeField]
		private Transform origin;

		private DecalProjector projector;

		[Header("Distance")]
		private float maxRaycastDistance = 999f;

		private float distanceCap = 3f;

		[SerializeField]
		private bool clampMaxDistance;

		[SerializeField]
		private float clampedMaxDistance = 0.01f;

		[Header("Curves")]
		[SerializeField]
		private AnimationCurve sizeCurve;

		[SerializeField]
		private AnimationCurve opacityCurve;

		[Header("Size")]
		[SerializeField]
		private float size = 1f;

		[Header("Decal")]
		private float calculatedSize;

		private float opacity;

		private float distancePercent;

		private float distance;

		private float higherDistance;

		[Header("Info")]
		[SerializeField]
		private LayerMask collisionMask;

		private float ClampedDistance => Mathf.Clamp(distance, 0f, distanceCap);

		private void Awake()
		{
			projector = GetComponent<DecalProjector>();
		}

		private void OnDrawGizmos()
		{
			Gizmos.color = Color.green;
			Gizmos.DrawLine(base.transform.position, base.transform.position + Vector3.down * distance);
			Gizmos.DrawWireSphere(base.transform.position + Vector3.down * distance, 0.3f);
			if (clampMaxDistance)
			{
				Gizmos.color = Color.red;
				Gizmos.DrawLine(base.transform.position, base.transform.position + Vector3.down * clampedMaxDistance);
				Gizmos.DrawWireSphere(base.transform.position + Vector3.down * clampedMaxDistance, 0.3f);
			}
		}

		private void Update()
		{
			if (Physics.Raycast(origin.position, Vector3.down, out var hitInfo, maxRaycastDistance, collisionMask))
			{
				CalculateDistancePercent();
				SetDistance(hitInfo);
				UpdateSize();
				UpdateOpacity();
				SetCapToHigherDistance();
			}
		}

		private void SetDistance(RaycastHit hit)
		{
			distance = hit.distance;
		}

		private void UpdateSize()
		{
			calculatedSize = sizeCurve.Evaluate(distancePercent) * size;
			UpdateProjectorSize();
		}

		private void UpdateProjectorSize()
		{
			projector.size = new Vector3(1f * calculatedSize, 1f * calculatedSize, distance + 0.3f);
			projector.pivot = new Vector3(projector.pivot.x, projector.pivot.y, distance / 2f + 0.15f);
		}

		private void UpdateOpacity()
		{
			opacity = opacityCurve.Evaluate(distancePercent);
			UpdateProjectorOpacity();
		}

		private void CalculateDistancePercent()
		{
			distancePercent = ClampedDistance / distanceCap;
		}

		private void UpdateProjectorOpacity()
		{
			projector.fadeFactor = opacity;
		}

		private void SetCapToHigherDistance()
		{
			higherDistance = ((distance > higherDistance) ? distance : higherDistance);
			distanceCap = (clampMaxDistance ? clampedMaxDistance : higherDistance);
		}
	}
}
