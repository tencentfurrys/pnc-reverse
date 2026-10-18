using PixelCrushers.GridController;
using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
	private Vector3 direction;

	private float speed;

	private int damage;

	private float lifetime;

	private float spawnTime;

	private bool hasHit;

	[Header("Effects")]
	public GameObject hitEffect;

	public GameObject trailEffect;

	[Header("Physics")]
	[Tooltip("If true, the projectile is pulled down by gravity and arcs as it travels.")]
	public bool useGravity;

	[Tooltip("Scales gravity strength. 1 = normal Physics.gravity.")]
	public float gravityMultiplier = 1f;

	[Tooltip("If true, the projectile rotates to face its travel direction each frame so the sprite tilts along the arc.")]
	public bool faceMovementDirection;

	[Header("Homing")]
	[Tooltip("If true, the projectile steers toward the player after firing instead of flying straight.")]
	public bool isHoming;

	[Tooltip("How hard it turns. Higher = tighter tracking. Roughly the fraction of the remaining angle corrected per second.")]
	public float homingStrength = 5f;

	[Tooltip("The projectile only steers while the player is within this distance. Outside it, the orb flies straight.")]
	public float homingRange = 15f;

	[Tooltip("Seconds of steering after firing, then the heading locks and the orb flies straight. This is the dodge window — the player can bait the turn and sidestep. Also stops a fast orb from orbiting the player until its lifetime expires.")]
	public float homingDuration = 1.5f;

	private Rigidbody rb;

	private Transform homingTarget;

	private Collider homingTargetCollider;

	public void Initialize(Vector3 fireDirection, float projectileSpeed, int projectileDamage, float projectileLifetime)
	{
		direction = fireDirection.normalized;
		speed = projectileSpeed;
		damage = projectileDamage;
		lifetime = projectileLifetime;
		spawnTime = Time.time;
		rb = GetComponent<Rigidbody>();
		if (rb == null)
		{
			rb = base.gameObject.AddComponent<Rigidbody>();
		}
		rb.useGravity = false;
		rb.isKinematic = false;
		rb.linearVelocity = direction * speed;
		if (faceMovementDirection)
		{
			transform.rotation = Quaternion.LookRotation(direction);
		}
		if (GetComponent<Collider>() == null)
		{
			SphereCollider sphereCollider = base.gameObject.AddComponent<SphereCollider>();
			sphereCollider.radius = 0.2f;
			sphereCollider.isTrigger = true;
		}
		base.gameObject.layer = LayerMask.NameToLayer("EnemyProjectile");
		if (isHoming)
		{
			GameObject gameObject = GameObject.FindGameObjectWithTag("Player");
			if (gameObject != null)
			{
				homingTarget = gameObject.transform;
				homingTargetCollider = gameObject.GetComponent<Collider>();
			}
		}
	}

	private void Update()
	{
		if (Time.time >= spawnTime + lifetime)
		{
			DestroyProjectile(showHitEffect: false);
		}
	}

	private void FixedUpdate()
	{
		if (!(rb == null))
		{
			bool flag = isHoming && Time.time < spawnTime + homingDuration && UpdateHoming();
			if (useGravity && !flag)
			{
				rb.AddForce(Physics.gravity * gravityMultiplier, ForceMode.Acceleration);
			}
			if (faceMovementDirection && rb.linearVelocity.sqrMagnitude > 0.0001f)
			{
				transform.rotation = Quaternion.LookRotation(rb.linearVelocity.normalized);
			}
		}
	}

	private bool UpdateHoming()
	{
		if (homingTarget == null)
		{
			return false;
		}
		Vector3 vector = ((homingTargetCollider != null) ? homingTargetCollider.bounds.center : (homingTarget.position + Vector3.up)) - transform.position;
		if (vector.sqrMagnitude > homingRange * homingRange)
		{
			return false;
		}
		if (vector.sqrMagnitude < 0.0001f)
		{
			return false;
		}
		Vector3 vector2 = Vector3.Slerp((rb.linearVelocity.sqrMagnitude > 0.0001f) ? rb.linearVelocity.normalized : direction, vector.normalized, homingStrength * Time.fixedDeltaTime);
		rb.linearVelocity = vector2 * speed;
		direction = vector2;
		return true;
	}

	public static void FireSpread(GameObject prefab, Vector3 origin, Vector3 baseDirection, float speed, int damage, float lifetime, int count, float spreadAngle)
	{
		if (prefab == null)
		{
			return;
		}
		count = Mathf.Max(1, count);
		for (int i = 0; i < count; i++)
		{
			Vector3 vector = ((count > 1 && spreadAngle > 0f) ? ApplySpread(baseDirection, spreadAngle, i, count) : baseDirection);
			GameObject gameObject = Object.Instantiate(prefab, origin, Quaternion.LookRotation(vector));
			EnemyProjectile enemyProjectile = gameObject.GetComponent<EnemyProjectile>();
			if (enemyProjectile == null)
			{
				enemyProjectile = gameObject.AddComponent<EnemyProjectile>();
			}
			enemyProjectile.Initialize(vector, speed, damage, lifetime);
		}
	}

	private static Vector3 ApplySpread(Vector3 baseDirection, float spreadAngle, int index, int total)
	{
		if (total <= 1)
		{
			return baseDirection;
		}
		float num = spreadAngle / (float)(total - 1);
		Quaternion quaternion = Quaternion.AngleAxis((0f - spreadAngle) * 0.5f + num * (float)index + Random.Range((0f - num) * 0.3f, num * 0.3f), Vector3.up);
		return Quaternion.AngleAxis(Random.Range((0f - spreadAngle) * 0.2f, spreadAngle * 0.2f), Vector3.right) * quaternion * baseDirection;
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
			DealDamageToPlayer(other.gameObject);
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
			damageable?.TakeDamage(damage);
			DestroyProjectile(showHitEffect: true);
		}
	}

	private void DealDamageToPlayer(GameObject player)
	{
		GrabScreen component = player.GetComponent<GrabScreen>();
		if (!(component != null) || !component.IsGrabbed)
		{
			DualWieldingSystem component2 = player.GetComponent<DualWieldingSystem>();
			if (component2 != null)
			{
				component2.TakeDamage(damage);
			}
			else
			{
				player.GetComponent<IDamageable>()?.TakeDamage(damage);
			}
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
			Gizmos.color = Color.red;
			Gizmos.DrawWireSphere(transform.position, 0.2f);
			Gizmos.DrawLine(transform.position, transform.position + direction * 2f);
		}
	}
}
