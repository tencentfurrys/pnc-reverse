using UnityEngine;

namespace PixelCrushers.GridController;

public struct SpawnAllocation
{
	public Transform position;

	public bool isMimic;

	public ChestItemPool itemPool;

	public GameObject chestPrefabOverride;
}
