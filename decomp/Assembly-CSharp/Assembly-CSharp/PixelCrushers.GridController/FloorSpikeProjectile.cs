using UnityEngine;

namespace PixelCrushers.GridController;

public class FloorSpikeProjectile : MonoBehaviour
{
	[Header("Configuration")]
	[SerializeField]
	private ProjectileData projectileData;

	[SerializeField]
	private GameObject owner;

	[SerializeField]
	private GameObject floorSpikePrefab;

	[SerializeField]
	private float spikeSpawnInterval = 0.5f;

	[SerializeField]
	private LayerMask wallLayerMask = 1;

	[SerializeField]
	private LayerMask floorLayerMask = 1;

	[SerializeField]
	private float floorCheckDistance = 10f;

	[SerializeField]
	private float maxLifetime = 5f;

	[Header("Components")]
	[SerializeField]
	private Rigidbody rb;

	[SerializeField]
	private Collider col;

	private Vector3 velocity;

	private float lifetimeRemaining;

	private float nextSpikeSpawnTime;

	private bool hasHitWall;

	private void Awake()
	{
		if (rb == null)
		{
			rb = GetComponent<Rigidbody>();
		}
		if (col == null)
		{
			col = GetComponent<Collider>();
		}
		if (col != null)
		{
			col.isTrigger = true;
		}
		MakeInvisible();
		Object.Destroy(gameObject, maxLifetime);
	}

	private void MakeInvisible()
	{
		Renderer component = GetComponent<Renderer>();
		if (component != null)
		{
			component.enabled = false;
		}
		Renderer[] componentsInChildren = GetComponentsInChildren<Renderer>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].enabled = false;
		}
	}

	public void Initialize(ProjectileData data, Vector3 direction, GameObject shooter)
	{
		projectileData = data;
		owner = shooter;
		velocity = direction.normalized * data.speed;
		velocity.y = 0f;
		lifetimeRemaining = data.maxLifetime;
		nextSpikeSpawnTime = Time.time + spikeSpawnInterval;
		if (rb != null)
		{
			rb.useGravity = false;
			rb.linearVelocity = velocity;
		}
		transform.rotation = Quaternion.LookRotation(velocity);
	}

	private void Update()
	{
		if (hasHitWall || projectileData == null)
		{
			return;
		}
		lifetimeRemaining -= Time.deltaTime;
		if (lifetimeRemaining <= 0f)
		{
			DestroyProjectile();
			return;
		}
		if (rb == null)
		{
			transform.position += velocity * Time.deltaTime;
		}
		if (Time.time >= nextSpikeSpawnTime)
		{
			SpawnFloorSpike();
			nextSpikeSpawnTime = Time.time + spikeSpawnInterval;
		}
	}

	private void SpawnFloorSpike()
	{
		if (!(floorSpikePrefab == null) && Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out var hitInfo, floorCheckDistance, floorLayerMask))
		{
			Vector3 point = hitInfo.point;
			Quaternion identity = Quaternion.identity;
			FloorSpike component = Object.Instantiate(floorSpikePrefab, point, identity).GetComponent<FloorSpike>();
			if (component != null)
			{
				component.Initialize(owner);
			}
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (!hasHitWall && (!(owner != null) || !(other.transform == owner.transform)) && (wallLayerMask.value & (1 << other.gameObject.layer)) != 0)
		{
			hasHitWall = true;
			DestroyProjectile();
		}
	}

	private void OnCollisionEnter(Collision collision)
	{
		if (!hasHitWall && !(collision.transform == owner?.transform) && (wallLayerMask.value & (1 << collision.gameObject.layer)) != 0)
		{
			hasHitWall = true;
			DestroyProjectile();
		}
	}

	public void DestroyProjectile()
	{
		if (!hasHitWall)
		{
			hasHitWall = true;
			Object.Destroy(gameObject);
		}
	}

	public ProjectileData GetProjectileData()
	{
		return projectileData;
	}

	public GameObject GetOwner()
	{
		return owner;
	}

	public bool HasHitWall()
	{
		return hasHitWall;
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.green;
		Gizmos.DrawRay(transform.position, velocity.normalized * 2f);
		Gizmos.color = Color.red;
		Gizmos.DrawRay(transform.position + Vector3.up * 0.5f, Vector3.down * floorCheckDistance);
	}
}
