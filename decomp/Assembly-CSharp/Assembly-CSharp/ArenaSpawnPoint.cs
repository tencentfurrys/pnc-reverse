using UnityEngine;

public class ArenaSpawnPoint : MonoBehaviour
{
	[Tooltip("Unique ID within this arena, e.g. 'Pillar_North'. Spawn groups reference this.")]
	public string id = "Point";

	[Tooltip("Draw this point's ID in the Scene view.")]
	[SerializeField]
	private bool showGizmo = true;

	public bool HasId => !string.IsNullOrWhiteSpace(id);

	private void OnDrawGizmos()
	{
		if (showGizmo)
		{
			Gizmos.color = (HasId ? Color.cyan : Color.red);
			Gizmos.DrawWireSphere(transform.position, 0.5f);
			Gizmos.DrawRay(transform.position, Vector3.up * 2f);
		}
	}
}
