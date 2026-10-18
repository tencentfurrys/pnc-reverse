using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace PixelCrushers.GridController
{
	public class ProjectileWeaponLauncher : MonoBehaviour
	{
		[Header("Launch Points")]
		[SerializeField]
		private Transform leftHandLaunchPoint;

		[SerializeField]
		private Transform rightHandLaunchPoint;

		[Header("Pooling (Optional)")]
		[SerializeField]
		private bool useObjectPooling = true;

		[SerializeField]
		private int poolSize = 20;

		[Header("Heat Integration")]
		[SerializeField]
		private bool debugHeatInfo = true;

		private Queue<GameObject> projectilePool = new Queue<GameObject>();

		private List<GameObject> activeProjectiles = new List<GameObject>();

		private DualWieldingSystem dualWieldingSystem;

		private PlayerStats playerStats;

		private TrinketManager trinketManager;

		private void Awake()
		{
			dualWieldingSystem = GetComponent<DualWieldingSystem>();
			playerStats = GetComponent<PlayerStats>();
			trinketManager = GetComponentInParent<TrinketManager>();
			if (leftHandLaunchPoint == null)
			{
				leftHandLaunchPoint = base.transform;
			}
			if (rightHandLaunchPoint == null)
			{
				rightHandLaunchPoint = base.transform;
			}
		}

		private void Start()
		{
			if (useObjectPooling)
			{
				InitializeProjectilePool();
			}
		}

		private void InitializeProjectilePool()
		{
		}

		public Projectile LaunchProjectile(ProjectileData projectileData, HandSlot handSlot, Vector3 direction, int customDamage = -1, Transform weaponFirePoint = null, bool isCritical = false, WeaponData weapon = null, bool isSpecial = false)
		{
			if (projectileData == null || projectileData.projectilePrefab == null)
			{
				return null;
			}
			Transform transform = ((!(weaponFirePoint != null)) ? ((handSlot == HandSlot.LeftHand) ? leftHandLaunchPoint : rightHandLaunchPoint) : weaponFirePoint);
			Vector3 position = transform.position;
			GameObject gameObject = CreateProjectile(projectileData.projectilePrefab, position);
			if (gameObject == null)
			{
				return null;
			}
			FloorSpikeProjectile component = gameObject.GetComponent<FloorSpikeProjectile>();
			Projectile projectile = null;
			if (component != null)
			{
				component.Initialize(projectileData, direction, base.gameObject);
			}
			else
			{
				projectile = gameObject.GetComponent<Projectile>();
				if (projectile == null)
				{
					projectile = gameObject.AddComponent<Projectile>();
				}
				int projectileDamage = ((customDamage >= 0) ? customDamage : projectileData.damage);
				projectile.Initialize(projectileData, direction, base.gameObject, projectileDamage, isCritical);
			}
			activeProjectiles.Add(gameObject);
			if (projectile != null)
			{
				Projectile projectile2 = projectile;
				projectile2.OnProjectileDestroyed = (Action<Projectile>)Delegate.Combine(projectile2.OnProjectileDestroyed, new Action<Projectile>(OnProjectileDestroyed));
				if (trinketManager != null)
				{
					TrinketProjectileContext ctx = new TrinketProjectileContext
					{
						weapon = weapon,
						hand = handSlot,
						isSpecial = isSpecial
					};
					trinketManager.OnProjectileSpawned(projectile, in ctx);
				}
			}
			return projectile;
		}

		private GameObject CreateProjectile(GameObject prefab, Vector3 position)
		{
			if (useObjectPooling && projectilePool.Count > 0)
			{
				GameObject gameObject = null;
				while (projectilePool.Count > 0)
				{
					gameObject = projectilePool.Dequeue();
					if (gameObject != null)
					{
						gameObject.transform.position = position;
						gameObject.transform.rotation = Quaternion.identity;
						gameObject.SetActive(value: true);
						return gameObject;
					}
				}
			}
			return UnityEngine.Object.Instantiate(prefab, position, Quaternion.identity);
		}

		private void OnProjectileDestroyed(Projectile projectile)
		{
			if (!(projectile == null) && !(projectile.gameObject == null))
			{
				if (activeProjectiles.Contains(projectile.gameObject))
				{
					activeProjectiles.Remove(projectile.gameObject);
				}
				if (useObjectPooling && projectile.gameObject != null)
				{
					ReturnToPool(projectile.gameObject);
				}
			}
		}

		private void ReturnToPool(GameObject projectileObj)
		{
			if (!(projectileObj == null))
			{
				Projectile component = projectileObj.GetComponent<Projectile>();
				if (component != null)
				{
					component.OnProjectileDestroyed = (Action<Projectile>)Delegate.Remove(component.OnProjectileDestroyed, new Action<Projectile>(OnProjectileDestroyed));
				}
				projectileObj.transform.position = Vector3.zero;
				projectileObj.transform.rotation = Quaternion.identity;
				projectileObj.SetActive(value: false);
				projectilePool.Enqueue(projectileObj);
			}
		}

		private int GetWeaponDamage(HandSlot handSlot)
		{
			if (dualWieldingSystem == null)
			{
				return 10;
			}
			WeaponData weaponInHand = dualWieldingSystem.GetWeaponInHand(handSlot);
			if (!(weaponInHand != null))
			{
				return 10;
			}
			return weaponInHand.damage;
		}

		public void SetLaunchPoint(HandSlot handSlot, Transform launchPoint)
		{
			if (handSlot == HandSlot.LeftHand)
			{
				leftHandLaunchPoint = launchPoint;
			}
			else
			{
				rightHandLaunchPoint = launchPoint;
			}
		}

		public Transform GetLaunchPoint(HandSlot handSlot)
		{
			if (handSlot != HandSlot.LeftHand)
			{
				return rightHandLaunchPoint;
			}
			return leftHandLaunchPoint;
		}

		public int GetActiveProjectileCount()
		{
			activeProjectiles.RemoveAll((GameObject obj) => obj == null);
			return activeProjectiles.Count;
		}

		public void DestroyAllProjectiles()
		{
			for (int num = activeProjectiles.Count - 1; num >= 0; num--)
			{
				if (activeProjectiles[num] != null)
				{
					Projectile component = activeProjectiles[num].GetComponent<Projectile>();
					if (component != null)
					{
						component.DestroyProjectile();
					}
					else
					{
						UnityEngine.Object.Destroy(activeProjectiles[num]);
					}
				}
			}
			activeProjectiles.Clear();
		}

		public string GetHeatDebugInfo()
		{
			if (playerStats == null)
			{
				return "No PlayerStats found";
			}
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("=== Heat System Status ===");
			stringBuilder.AppendLine($"Current Heat: {playerStats.CurrentHeat:F1}/{playerStats.MaxHeat:F1}");
			stringBuilder.AppendLine($"Heat Percentage: {playerStats.HeatPercentage:P1}");
			stringBuilder.AppendLine($"Is Overheating: {playerStats.IsOverheating}");
			stringBuilder.AppendLine($"Is Critical: {playerStats.IsCriticallyHot}");
			stringBuilder.AppendLine($"Can Perform Actions: {playerStats.CanPerformActions}");
			stringBuilder.AppendLine($"Active Projectiles: {GetActiveProjectileCount()}");
			if (dualWieldingSystem != null)
			{
				WeaponData weaponInHand = dualWieldingSystem.GetWeaponInHand(HandSlot.LeftHand);
				WeaponData weaponInHand2 = dualWieldingSystem.GetWeaponInHand(HandSlot.RightHand);
				if (weaponInHand != null && weaponInHand.IsProjectileWeapon)
				{
					stringBuilder.AppendLine($"Left Weapon Heat Cost: {weaponInHand.GetHeatCost():F1}");
				}
				if (weaponInHand2 != null && weaponInHand2.IsProjectileWeapon)
				{
					stringBuilder.AppendLine($"Right Weapon Heat Cost: {weaponInHand2.GetHeatCost():F1}");
				}
			}
			return stringBuilder.ToString();
		}

		public bool CanLaunchProjectile(HandSlot handSlot)
		{
			if (playerStats == null)
			{
				return true;
			}
			if (!playerStats.CanPerformActions)
			{
				return false;
			}
			if (dualWieldingSystem == null)
			{
				return true;
			}
			WeaponData weaponInHand = dualWieldingSystem.GetWeaponInHand(handSlot);
			if (weaponInHand == null || !weaponInHand.IsProjectileWeapon)
			{
				return true;
			}
			float heatCost = weaponInHand.GetHeatCost();
			return playerStats.CanGenerateHeat(heatCost);
		}

		public float GetNextLaunchHeatCost(HandSlot handSlot)
		{
			if (dualWieldingSystem == null)
			{
				return 0f;
			}
			WeaponData weaponInHand = dualWieldingSystem.GetWeaponInHand(handSlot);
			if (weaponInHand == null || !weaponInHand.IsProjectileWeapon)
			{
				return 0f;
			}
			return weaponInHand.GetHeatCost();
		}

		public void SetDebugHeatInfo(bool enabled)
		{
			debugHeatInfo = enabled;
		}

		private void OnDestroy()
		{
			while (projectilePool.Count > 0)
			{
				GameObject gameObject = projectilePool.Dequeue();
				if (gameObject != null)
				{
					UnityEngine.Object.Destroy(gameObject);
				}
			}
			DestroyAllProjectiles();
		}

		[ContextMenu("Destroy All Projectiles")]
		public void DebugDestroyAllProjectiles()
		{
			DestroyAllProjectiles();
		}
	}
}
