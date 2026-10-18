using UnityEngine;

namespace PixelCrushers.GridController;

[CreateAssetMenu(fileName = "New Trinket", menuName = "Combat/Trinket Data")]
public class TrinketData : ScriptableObject
{
	[Header("Basic Info")]
	public string trinketName = "Trinket";

	[TextArea(2, 4)]
	public string description = "A mysterious trinket.";

	public Sprite trinketIcon;

	[Header("Effect")]
	[Tooltip("The behavior this trinket grants. Assign a TrinketEffect asset.")]
	public TrinketEffect effect;

	public TrinketEffect Effect => effect;
}
