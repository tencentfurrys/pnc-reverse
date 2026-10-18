using PixelCrushers.GridController;
using UnityEngine;

public class GrabProjectile : MonoBehaviour
{
	private Vector3 direction;

	private float speed;

	private float lifetime;

	private float spawnTime;

	private bool hasHit;

	private ProjectileEnemyAI sourceEnemy;

	[Header("Effects")]
	public GameObject hitEffect;

	public GameObject trailEffect;

	private Rigidbody rb;

	public void Initialize(Vector3 fireDirection, float projectileSpeed, float projectileLifetime, ProjectileEnemyAI enemy)
	{
		direction = fireDirection.normalized;
		speed = projectileSpeed;
		lifetime = projectileLifetime;
		sourceEnemy = enemy;
		spawnTime = Time.time;
		rb = GetComponent<Rigidbody>();
		if (rb == null)
		{
			rb = gameObject.AddComponent<Rigidbody>();
		}
		rb.useGravity = false;
		rb.isKinematic = false;
		rb.linearVelocity = direction * speed;
		if (GetComponent<Collider>() == null)
		{
			SphereCollider sphereCollider = gameObject.AddComponent<SphereCollider>();
			sphereCollider.radius = 0.2f;
			sphereCollider.isTrigger = true;
		}
		gameObject.layer = LayerMask.NameToLayer("EnemyProjectile");
	}

	private void Update()
	{
		if (Time.time >= spawnTime + lifetime)
		{
			DestroyProjectile(showHitEffect: false);
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (hasHit)
		{
			return;
		}
		if (other.CompareTag("Player"))
		{
			hasHit = true;
			GrabPlayer(other.gameObject);
			DestroyProjectile(showHitEffect: true);
		}
		else if (!other.CompareTag("Enemy") && !other.isTrigger)
		{
			hasHit = true;
			IDamageable damageable = other.GetComponent<IDamageable>();
			if (damageable == null)
			{
				damageable = other.GetComponentInParent<IDamageable>();
			}
			damageable?.TakeDamage(1);
			DestroyProjectile(showHitEffect: true);
		}
	}

	private void GrabPlayer(GameObject player)
	{
		if (!(sourceEnemy == null))
		{
			sourceEnemy.OnGrabProjectileHitPlayer();
		}
	}

	private void DestroyProjectile(bool showHitEffect)
	{
		if (showHitEffect && hitEffect != null)
		{
			Object.Instantiate(hitEffect, transform.position, Quaternion.identity);
		}
		Object.Destroy(gameObject);
	}

	private void OnDrawGizmos()
	{
		if (Application.isPlaying)
		{
			Gizmos.color = Color.magenta;
			Gizmos.DrawWireSphere(transform.position, 0.2f);
			Gizmos.DrawLine(transform.position, transform.position + direction * 2f);
		}
	}
}
