using System;
using UnityEngine;

namespace PixelCrushers.GridController
{
	[Serializable]
	public class ChestSpawnPoint
	{
		public Transform position;

		[Tooltip("Override chest prefab for this position. Null = use ChestManager default.")]
		public GameObject chestPrefabOverride;

		[Tooltip("Override item pool for this position. Null = use ChestManager default.")]
		public ChestItemPool itemPoolOverride;
	}
}
