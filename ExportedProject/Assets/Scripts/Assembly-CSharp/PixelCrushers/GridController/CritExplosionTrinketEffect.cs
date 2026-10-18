using UnityEngine;

namespace PixelCrushers.GridController
{
	[CreateAssetMenu(fileName = "Crit Explosion Trinket", menuName = "Trinkets/Crit Explosion")]
	public class CritExplosionTrinketEffect : TrinketEffect
	{
		[Tooltip("Damage dealt to every valid target inside the explosion radius.")]
		public int explosionDamage = 15;

		[Tooltip("Radius of the explosion, in world units.")]
		public float explosionRadius = 3f;

		[Tooltip("Layers the explosion searches for targets on. Defaults to everything; non-damageable colliders and the player are filtered out.")]
		public LayerMask enemyLayers = -1;

		[Tooltip("Optional VFX prefab spawned at the critted enemy's position.")]
		public GameObject explosionEffect;

		[Tooltip("How long (seconds) before the spawned VFX is returned/destroyed.")]
		public float effectLifetime = 2f;

		private static readonly Collider[] overlapBuffer = new Collider[32];

		public override void OnDealtDamage(in TrinketDamageContext ctx)
		{
			if (ctx.isCritical && !(ctx.target == null))
			{
				Vector3 position = ctx.target.transform.position;
				SpawnExplosionEffect(position);
				DealExplosionDamage(position);
			}
		}

		private void SpawnExplosionEffect(Vector3 center)
		{
			if (!(explosionEffect == null))
			{
				if (VFXPoolManager.Instance != null)
				{
					GameObject instance = VFXPoolManager.Instance.Get(explosionEffect, center, Quaternion.identity);
					VFXPoolManager.Instance.ReturnAfterDelay(instance, effectLifetime);
				}
				else
				{
					Object.Destroy(Object.Instantiate(explosionEffect, center, Quaternion.identity), effectLifetime);
				}
			}
		}

		private void DealExplosionDamage(Vector3 center)
		{
			int num = Physics.OverlapSphereNonAlloc(center, explosionRadius, overlapBuffer, enemyLayers);
			for (int i = 0; i < num; i++)
			{
				Collider collider = overlapBuffer[i];
				if (collider == null || collider.CompareTag("Player"))
				{
					continue;
				}
				IDamageable component = collider.GetComponent<IDamageable>();
				if (component != null)
				{
					if (DamageNumberManager.Instance != null)
					{
						DamageNumberManager.Instance.SetNextCritical(isCritical: false);
					}
					component.TakeDamage(explosionDamage);
				}
			}
		}
	}
}
