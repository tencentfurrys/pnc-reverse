using UnityEngine;

[ExecuteInEditMode]
[AddComponentMenu("Procedural Worlds/SECTR/Audio/SECTR Region Source")]
public class SECTR_RegionSource : SECTR_PointSource
{
	[SECTR_ToolTip("Determine the closest point by raycast instead of bounding box. More accurate but more expensive.")]
	public bool Raycast;

	private void Update()
	{
		if ((bool)instance)
		{
			Vector3 position = SECTR_AudioSystem.Listener.position;
			Vector3 position2 = base.transform.position;
			Collider component = GetComponent<Collider>();
			if (Raycast && (bool)component)
			{
				Vector3 direction = base.transform.position - position;
				float magnitude = direction.magnitude;
				direction /= magnitude;
				position2 = ((!component.Raycast(new Ray(position, direction), out var hitInfo, magnitude)) ? position : hitInfo.point);
			}
			else if ((bool)component)
			{
				position2 = ((!component.bounds.Contains(position)) ? component.ClosestPointOnBounds(position) : position);
			}
			instance.Position = position2;
		}
	}
}
