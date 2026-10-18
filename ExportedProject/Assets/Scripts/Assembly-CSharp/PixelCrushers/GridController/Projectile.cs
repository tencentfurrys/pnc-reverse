using System;
using System.Collections.Generic;
using UnityEngine;

namespace PixelCrushers.GridController
{
	public class Projectile : MonoBehaviour
	{
		[Header("Configuration")]
		[SerializeField]
		private ProjectileData projectileData;

		[SerializeField]
		private int damage;

		[SerializeField]
		private GameObject owner;

		[Header("Components")]
		[SerializeField]
		private Rigidbody rb;

		[SerializeField]
		private Collider col;

		[SerializeField]
		private TrailRenderer trail;

		[SerializeField]
		private AudioSource audioSource;

		private bool isCritical;

		private Vector3 startPosition;

		private Vector3 velocity;

		private float lifetimeRemaining;

		private int bouncesRemaining;

		private int pierceTargetsRemaining;

		private bool hasImpacted;

		private Collider homingTarget;

		private List<Collider> hitTargets = new List<Collider>();

		private HashSet<Collider> impactDamagedTargets = new HashSet<Collider>();

		private bool forceHoming;

		private float homingStrengthOverride;

		private float homingRangeOverride;

		private bool forceExplodeOnImpact;

		private int explosionDamageOverride;

		private float explosionRadiusOverride;

		private GameObject overrideImpactEffect;

		private bool pendingGuaranteedCrit;

		private TrinketManager ownerTrinketManager;

		private bool isHeatDrain;

		private PlayerStats heatDrainStats;

		private bool hasDrainedHeat;

		private static readonly Collider[] homingBuffer = new Collider[16];

		private static readonly Collider[] impactBuffer = new Collider[32];

		public Action<Projectile, ProjectileHit> OnProjectileHit;

		public Action<Projectile> OnProjectileDestroyed;

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
			if (trail == null)
			{
				trail = GetComponent<TrailRenderer>();
			}
			if (audioSource == null)
			{
				audioSource = GetComponent<AudioSource>();
			}
			if (col != null)
			{
				col.isTrigger = true;
			}
		}

		public void Initialize(ProjectileData data, Vector3 direction, GameObject shooter, int projectileDamage = -1, bool isCritical = false)
		{
			projectileData = data;
			owner = shooter;
			damage = ((projectileDamage >= 0) ? projectileDamage : data.damage);
			this.isCritical = isCritical;
			forceHoming = false;
			homingStrengthOverride = 0f;
			homingRangeOverride = 0f;
			forceExplodeOnImpact = false;
			explosionDamageOverride = 0;
			explosionRadiusOverride = 0f;
			overrideImpactEffect = null;
			pendingGuaranteedCrit = false;
			isHeatDrain = false;
			heatDrainStats = null;
			hasDrainedHeat = false;
			ownerTrinketManager = ((shooter != null) ? shooter.GetComponentInParent<TrinketManager>() : null);
			base.gameObject.layer = LayerMask.NameToLayer("Projectile");
			startPosition = base.transform.position;
			velocity = direction.normalized * data.speed;
			lifetimeRemaining = data.maxLifetime;
			bouncesRemaining = data.maxBounces;
			pierceTargetsRemaining = data.maxPierceTargets;
			if (rb != null)
			{
				rb.useGravity = false;
				rb.linearVelocity = velocity;
				if (data.useGravity)
				{
					rb.linearDamping = 0f;
				}
			}
			if (data.trailEffect != null && trail == null)
			{
				GameObject gameObject = ((VFXPoolManager.Instance != null) ? VFXPoolManager.Instance.Get(data.trailEffect, base.transform) : UnityEngine.Object.Instantiate(data.trailEffect, base.transform));
				trail = gameObject.GetComponent<TrailRenderer>();
			}
			if (data.launchSound != null && audioSource != null)
			{
				audioSource.PlayOneShot(data.launchSound);
			}
			if (data.faceMovementDirection)
			{
				base.transform.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(data.rotationOffset);
			}
		}

		private void Update()
		{
			if (hasImpacted || projectileData == null)
			{
				return;
			}
			lifetimeRemaining -= Time.deltaTime;
			if (lifetimeRemaining <= 0f)
			{
				DestroyProjectile();
				return;
			}
			if ((base.transform.position - startPosition).sqrMagnitude >= projectileData.maxRange * projectileData.maxRange)
			{
				DestroyProjectile();
				return;
			}
			if (projectileData.isHoming || forceHoming)
			{
				UpdateHoming();
			}
			if (rb == null)
			{
				UpdateManualMovement();
			}
			if (projectileData.faceMovementDirection && velocity != Vector3.zero)
			{
				base.transform.rotation = Quaternion.LookRotation(velocity.normalized) * Quaternion.Euler(projectileData.rotationOffset);
			}
			if (projectileData.scalesOverTime)
			{
				float time = 1f - lifetimeRemaining / projectileData.maxLifetime;
				float t = projectileData.scaleCurve.Evaluate(time);
				base.transform.localScale = Vector3.Lerp(projectileData.startScale, projectileData.endScale, t);
			}
		}

		private void FixedUpdate()
		{
			if (rb != null && projectileData != null && projectileData.useGravity)
			{
				rb.AddForce(Physics.gravity * projectileData.gravityMultiplier, ForceMode.Acceleration);
			}
		}

		public void EnableHoming(float strength, float range)
		{
			forceHoming = true;
			homingStrengthOverride = strength;
			homingRangeOverride = range;
		}

		public void EnableExplodeOnImpact(int explosionDamage, float explosionRadius, GameObject impactEffectOverride)
		{
			forceExplodeOnImpact = true;
			explosionDamageOverride = explosionDamage;
			explosionRadiusOverride = explosionRadius;
			if (impactEffectOverride != null)
			{
				overrideImpactEffect = impactEffectOverride;
			}
		}

		public void MarkPendingGuaranteedCrit()
		{
			pendingGuaranteedCrit = true;
		}

		public void MarkHeatDrain(PlayerStats stats)
		{
			isHeatDrain = true;
			heatDrainStats = stats;
		}

		private void UpdateHoming()
		{
			if (homingTarget == null)
			{
				FindHomingTarget();
			}
			if (homingTarget != null)
			{
				float num = (forceHoming ? homingStrengthOverride : projectileData.homingStrength);
				Vector3 normalized = (homingTarget.bounds.center - base.transform.position).normalized;
				velocity = Vector3.Slerp(velocity.normalized, normalized, num * Time.deltaTime) * projectileData.speed;
				if (rb != null)
				{
					rb.linearVelocity = velocity;
				}
			}
		}

		private void FindHomingTarget()
		{
			float radius = (forceHoming ? homingRangeOverride : projectileData.homingRange);
			int num = Physics.OverlapSphereNonAlloc(base.transform.position, radius, homingBuffer, projectileData.homingTargetMask);
			float num2 = float.MaxValue;
			Collider collider = null;
			for (int i = 0; i < num; i++)
			{
				Collider collider2 = homingBuffer[i];
				if (!(collider2.transform == owner?.transform) && !hitTargets.Contains(collider2))
				{
					float sqrMagnitude = (base.transform.position - collider2.bounds.center).sqrMagnitude;
					if (sqrMagnitude < num2)
					{
						num2 = sqrMagnitude;
						collider = collider2;
					}
				}
			}
			homingTarget = collider;
		}

		private void UpdateManualMovement()
		{
			if (projectileData.useGravity)
			{
				velocity += Physics.gravity * projectileData.gravityMultiplier * Time.deltaTime;
			}
			base.transform.position += velocity * Time.deltaTime;
		}

		private void OnTriggerEnter(Collider other)
		{
			if (!hasImpacted && !(other.transform == owner?.transform) && ((projectileData.impactLayerMask.value & (1 << other.gameObject.layer)) != 0 || other.gameObject.layer == 18) && !hitTargets.Contains(other))
			{
				ProjectileHit hit = new ProjectileHit
				{
					collider = other,
					point = base.transform.position,
					normal = -velocity.normalized
				};
				HandleImpact(hit);
			}
		}

		private void OnCollisionEnter(Collision collision)
		{
			if (!hasImpacted && !(collision.transform == owner?.transform))
			{
				if (projectileData.bounces && bouncesRemaining > 0)
				{
					HandleBounce(collision);
					return;
				}
				ProjectileHit hit = new ProjectileHit
				{
					collider = collision.collider,
					point = collision.contacts[0].point,
					normal = collision.contacts[0].normal
				};
				HandleImpact(hit);
			}
		}

		private void HandleBounce(Collision collision)
		{
			bouncesRemaining--;
			Vector3 vector = Vector3.Reflect(velocity, collision.contacts[0].normal);
			velocity = vector * projectileData.bounceSpeedMultiplier;
			if (rb != null)
			{
				rb.linearVelocity = velocity;
			}
			if (projectileData.faceMovementDirection)
			{
				base.transform.rotation = Quaternion.LookRotation(velocity.normalized) * Quaternion.Euler(projectileData.rotationOffset);
			}
		}

		private void HandleImpact(ProjectileHit hit)
		{
			hitTargets.Add(hit.collider);
			if (projectileData.impactRadius > 0f)
			{
				DealAreaDamage(hit.point);
			}
			else
			{
				DealSingleDamage(hit.collider);
			}
			if (!isHeatDrain && (projectileData.explodeOnImpact || forceExplodeOnImpact))
			{
				DealExplosionDamage(hit.point);
			}
			PlayImpactEffects(hit);
			OnProjectileHit?.Invoke(this, hit);
			if (projectileData.piercing && pierceTargetsRemaining > 0)
			{
				pierceTargetsRemaining--;
				if (pierceTargetsRemaining > 0)
				{
					return;
				}
			}
			DestroyProjectile();
		}

		private void DealSingleDamage(Collider target)
		{
			if (target.CompareTag("Player"))
			{
				GrabScreen component = target.GetComponent<GrabScreen>();
				if (component != null && component.IsGrabbed)
				{
					return;
				}
			}
			IDamageable component2 = target.GetComponent<IDamageable>();
			if (component2 != null)
			{
				impactDamagedTargets.Add(target);
				ApplyDamage(component2, target.gameObject);
			}
		}

		private void ApplyDamage(IDamageable damageable, GameObject targetObj)
		{
			if (isHeatDrain)
			{
				ApplyHeatDrain(targetObj);
				BreakIfBreakable(targetObj);
				return;
			}
			int num = damage;
			TrinketDamageContext ctx = default;
			bool flag = ownerTrinketManager != null;
			if (pendingGuaranteedCrit && TrinketManager.IsTrinketTarget(targetObj))
			{
				pendingGuaranteedCrit = false;
				if (flag)
				{
					ownerTrinketManager.TryConsumeGuaranteedCrit(out var _);
				}
			}
			if (flag)
			{
				ctx = new TrinketDamageContext
				{
					target = targetObj,
					damage = damage,
					weapon = null,
					hand = HandSlot.RightHand,
					isProjectile = true,
					isCritical = isCritical
				};
				ownerTrinketManager.ModifyDamage(ref ctx);
				num = ctx.damage;
			}
			if (DamageNumberManager.Instance != null)
			{
				DamageNumberManager.Instance.SetNextCritical(isCritical);
			}
			damageable.TakeDamage(num);
			if (flag)
			{
				ctx.damage = num;
				ownerTrinketManager.OnDealtDamage(in ctx);
			}
		}

		private void ApplyHeatDrain(GameObject targetObj)
		{
			if (!hasDrainedHeat)
			{
				hasDrainedHeat = true;
				if (heatDrainStats != null)
				{
					heatDrainStats.CoolHeat(damage);
					heatDrainStats.ClearOverheatIfBelowMax();
				}
				if (DamageNumberManager.Instance != null)
				{
					DamageNumberManager.Instance.SpawnHeatDrainNumber(targetObj.transform.position, damage, isCritical);
				}
			}
		}

		private void BreakIfBreakable(GameObject targetObj)
		{
			BreakableObject component = targetObj.GetComponent<BreakableObject>();
			if (component != null && !component.IsBroken)
			{
				component.TakeDamage(damage);
			}
		}

		private void DealAreaDamage(Vector3 center)
		{
			int num = Physics.OverlapSphereNonAlloc(center, projectileData.impactRadius, impactBuffer, (int)projectileData.impactLayerMask | 0x40000);
			for (int i = 0; i < num; i++)
			{
				Collider collider = impactBuffer[i];
				if (collider.transform == owner?.transform)
				{
					continue;
				}
				if (collider.CompareTag("Player"))
				{
					GrabScreen component = collider.GetComponent<GrabScreen>();
					if (component != null && component.IsGrabbed)
					{
						continue;
					}
				}
				IDamageable component2 = collider.GetComponent<IDamageable>();
				if (component2 != null)
				{
					impactDamagedTargets.Add(collider);
					ApplyDamage(component2, collider.gameObject);
				}
			}
		}

		private void DealExplosionDamage(Vector3 center)
		{
			float radius = (forceExplodeOnImpact ? explosionRadiusOverride : projectileData.explosionRadius);
			int num = (forceExplodeOnImpact ? explosionDamageOverride : projectileData.explosionDamage);
			int num2 = Physics.OverlapSphereNonAlloc(center, radius, impactBuffer, (int)projectileData.impactLayerMask | 0x40000);
			for (int i = 0; i < num2; i++)
			{
				Collider collider = impactBuffer[i];
				if (collider.transform == owner?.transform || impactDamagedTargets.Contains(collider))
				{
					continue;
				}
				if (collider.CompareTag("Player"))
				{
					GrabScreen component = collider.GetComponent<GrabScreen>();
					if (component != null && component.IsGrabbed)
					{
						continue;
					}
				}
				IDamageable component2 = collider.GetComponent<IDamageable>();
				if (component2 != null)
				{
					if (DamageNumberManager.Instance != null)
					{
						DamageNumberManager.Instance.SetNextCritical(isCritical);
					}
					component2.TakeDamage(num);
				}
			}
		}

		private void PlayImpactEffects(ProjectileHit hit)
		{
			GameObject gameObject = ((overrideImpactEffect != null) ? overrideImpactEffect : projectileData.impactEffect);
			if (gameObject != null)
			{
				GameObject gameObject2 = ((VFXPoolManager.Instance != null) ? VFXPoolManager.Instance.Get(gameObject, hit.point, Quaternion.LookRotation(hit.normal)) : UnityEngine.Object.Instantiate(gameObject, hit.point, Quaternion.LookRotation(hit.normal)));
				if (VFXPoolManager.Instance != null)
				{
					VFXPoolManager.Instance.ReturnAfterDelay(gameObject2, 5f);
				}
				else
				{
					UnityEngine.Object.Destroy(gameObject2, 5f);
				}
			}
			if (projectileData.impactSound != null)
			{
				if (AudioPool.Instance != null)
				{
					AudioPool.Instance.PlayAtPoint(projectileData.impactSound, hit.point);
					return;
				}
				GameObject obj = new GameObject("ImpactAudio");
				obj.transform.position = hit.point;
				obj.AddComponent<ImpactAudioPlayer>().Play(projectileData.impactSound);
			}
		}

		public void DestroyProjectile()
		{
			if (!hasImpacted)
			{
				hasImpacted = true;
				OnProjectileDestroyed?.Invoke(this);
				if (trail != null)
				{
					trail.autodestruct = true;
					trail.transform.SetParent(null);
				}
				UnityEngine.Object.Destroy(base.gameObject);
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

		public int GetDamage()
		{
			return damage;
		}

		public Vector3 GetVelocity()
		{
			return velocity;
		}

		public float GetRemainingLifetime()
		{
			return lifetimeRemaining;
		}

		public bool HasImpacted()
		{
			return hasImpacted;
		}

		private void OnDrawGizmosSelected()
		{
			if (!(projectileData == null))
			{
				if (projectileData.impactRadius > 0f)
				{
					Gizmos.color = Color.yellow;
					Gizmos.DrawWireSphere(base.transform.position, projectileData.impactRadius);
				}
				if (projectileData.explodeOnImpact)
				{
					Gizmos.color = Color.red;
					Gizmos.DrawWireSphere(base.transform.position, projectileData.explosionRadius);
				}
				if (projectileData.isHoming)
				{
					Gizmos.color = Color.cyan;
					Gizmos.DrawWireSphere(base.transform.position, projectileData.homingRange);
				}
				Gizmos.color = Color.green;
				Gizmos.DrawRay(base.transform.position, velocity.normalized * 2f);
			}
		}
	}
}
