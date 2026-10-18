using UnityEngine;

namespace PixelCrushers.GridController
{
	[CreateAssetMenu(fileName = "Overheat Damage Trinket", menuName = "Trinkets/Overheat Damage")]
	public class OverheatDamageTrinketEffect : TrinketEffect
	{
		[Tooltip("Damage multiplier applied while the player is overheated and cooling down (2 = double damage).")]
		[Min(1f)]
		public float overheatMultiplier = 2f;

		private PlayerStats playerStats;

		public override void OnEquip(TrinketManager manager)
		{
			playerStats = ((manager != null) ? manager.ResolvePlayerStats() : null);
		}

		public override void OnUnequip(TrinketManager manager)
		{
			playerStats = null;
		}

		public override void ModifyDamage(ref TrinketDamageContext ctx)
		{
			PlayerStats playerStats = ResolveStats();
			if (!(playerStats == null) && playerStats.IsAttackLocked())
			{
				ctx.damage = Mathf.RoundToInt((float)ctx.damage * overheatMultiplier);
			}
		}

		private PlayerStats ResolveStats()
		{
			if (playerStats == null && TrinketManager.Instance != null)
			{
				playerStats = TrinketManager.Instance.ResolvePlayerStats();
			}
			return playerStats;
		}
	}
}
