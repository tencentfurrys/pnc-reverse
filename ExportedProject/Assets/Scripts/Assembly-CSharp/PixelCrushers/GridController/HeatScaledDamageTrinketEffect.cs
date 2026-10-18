using UnityEngine;

namespace PixelCrushers.GridController
{
	[CreateAssetMenu(fileName = "Heat Scaled Damage Trinket", menuName = "Trinkets/Heat Scaled Damage")]
	public class HeatScaledDamageTrinketEffect : TrinketEffect
	{
		[Tooltip("Damage multiplier at full heat (2 = double damage at max heat). Scales linearly from 1x at zero heat.")]
		[Min(1f)]
		public float maxMultiplier = 2f;

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
			if (!(playerStats == null))
			{
				float t = Mathf.Clamp01(playerStats.HeatPercentage);
				float num = Mathf.Lerp(1f, maxMultiplier, t);
				ctx.damage = Mathf.RoundToInt((float)ctx.damage * num);
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
