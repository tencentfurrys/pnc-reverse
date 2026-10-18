using UnityEngine;

namespace PixelCrushers.GridController;

[CreateAssetMenu(fileName = "Explosive Projectile Trinket", menuName = "Trinkets/Explosive Projectiles")]
public class ExplosiveProjectileTrinketEffect : TrinketEffect
{
	[Tooltip("Damage dealt to everything inside the explosion radius.")]
	public int explosionDamage = 20;

	[Tooltip("Radius of the explosion, in world units.")]
	public float explosionRadius = 3f;

	[Tooltip("Explosion VFX that replaces the projectile's normal impact effect.")]
	public GameObject explosionEffect;

	public override void OnProjectileSpawned(Projectile projectile, in TrinketProjectileContext ctx)
	{
		projectile.EnableExplodeOnImpact(explosionDamage, explosionRadius, explosionEffect);
	}
}
