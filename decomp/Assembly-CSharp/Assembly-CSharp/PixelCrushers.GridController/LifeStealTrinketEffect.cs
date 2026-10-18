using UnityEngine;

namespace PixelCrushers.GridController;

[CreateAssetMenu(fileName = "Life Steal Trinket", menuName = "Trinkets/Life Steal")]
public class LifeStealTrinketEffect : TrinketEffect
{
	public override bool GrantsLifeStealOnKill()
	{
		return true;
	}
}
