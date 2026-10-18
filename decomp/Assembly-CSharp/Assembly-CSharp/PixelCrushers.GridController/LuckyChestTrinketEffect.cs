using UnityEngine;

namespace PixelCrushers.GridController;

[CreateAssetMenu(fileName = "Lucky Chest Trinket", menuName = "Trinkets/Lucky Chest")]
public class LuckyChestTrinketEffect : TrinketEffect
{
	[Tooltip("Flat crit-chance bonus added to the player's attacks (0.05 = +5%).")]
	[Range(0f, 1f)]
	public float critChanceBonus = 0.05f;

	public override bool GrantsChestRerollKeepBest()
	{
		return true;
	}

	public override float GetCritChanceBonus()
	{
		return critChanceBonus;
	}
}
