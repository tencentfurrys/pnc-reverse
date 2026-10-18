using UnityEngine;

namespace PixelCrushers.GridController;

public struct TrinketDamageContext
{
	public GameObject target;

	public int damage;

	public WeaponData weapon;

	public HandSlot hand;

	public bool isProjectile;

	public bool isCritical;
}
