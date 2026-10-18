using UnityEngine;

namespace PixelCrushers.GridController;

[CreateAssetMenu(fileName = "Melee Projectile Trinket", menuName = "Trinkets/Melee Projectiles")]
public class MeleeProjectileTrinketEffect : TrinketEffect
{
	[Tooltip("Projectile fired on each melee swing.")]
	public ProjectileData projectileData;

	[Tooltip("How many projectiles to spawn per swing.")]
	public int projectileCount = 1;

	[Tooltip("Total spread angle (degrees) across the spawned projectiles.")]
	public float spreadAngle = 15f;

	[Tooltip("Damage of each spawned projectile. -1 uses the ProjectileData's own damage.")]
	public int projectileDamage = -1;

	[Tooltip("Minimum seconds between projectile bursts. 0 = fire on every melee swing.")]
	public float cooldownSeconds;

	private float lastFireTime;

	public override void OnEquip(TrinketManager manager)
	{
		lastFireTime = float.NegativeInfinity;
	}

	public override void OnMeleeAttack(in TrinketMeleeContext ctx)
	{
		if (projectileData == null || ctx.launcher == null || Time.time - lastFireTime < cooldownSeconds)
		{
			return;
		}
		lastFireTime = Time.time;
		Vector3 vector = ((ctx.firePoint != null) ? ctx.firePoint.forward : Vector3.forward);
		for (int i = 0; i < projectileCount; i++)
		{
			Vector3 direction = vector;
			if (projectileCount > 1 && spreadAngle > 0f)
			{
				float num = spreadAngle / (float)(projectileCount - 1);
				direction = Quaternion.AngleAxis((0f - spreadAngle) * 0.5f + num * (float)i, Vector3.up) * vector;
			}
			ctx.launcher.LaunchProjectile(projectileData, ctx.hand, direction, projectileDamage, ctx.firePoint);
		}
	}
}
