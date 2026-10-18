using PixelCrushers.GridController;
using UnityEngine;

[CreateAssetMenu(fileName = "New Completion Unlock", menuName = "Unlocks/Completion Unlock")]
public class CompletionUnlock : ScriptableObject
{
	[Header("Requirement")]
	[Tooltip("Class required to earn this unlock. Leave empty for any class.")]
	public PlayerClass requiredClass;

	[Header("Rewards")]
	[Tooltip("Items unlocked on completion")]
	public InventoryItem[] itemsToUnlock;

	[Tooltip("Classes unlocked on completion")]
	public PlayerClass[] classesToUnlock;
}
