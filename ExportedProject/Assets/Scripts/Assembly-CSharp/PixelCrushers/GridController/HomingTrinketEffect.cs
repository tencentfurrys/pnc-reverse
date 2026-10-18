using UnityEngine;

namespace PixelCrushers.GridController
{
	[CreateAssetMenu(fileName = "Homing Trinket", menuName = "Trinkets/Homing")]
	public class HomingTrinketEffect : TrinketEffect
	{
		[Tooltip("How aggressively projectiles steer toward their target (higher = sharper turns).")]
		public float homingStrength = 5f;

		[Tooltip("How far a projectile will look for a homing target, in world units.")]
		public float homingRange = 15f;

		public override void OnProjectileSpawned(Projectile projectile, in TrinketProjectileContext ctx)
		{
			projectile.EnableHoming(homingStrength, homingRange);
		}
	}
}
