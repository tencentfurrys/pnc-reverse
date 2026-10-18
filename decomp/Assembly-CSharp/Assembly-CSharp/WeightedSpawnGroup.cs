using System;
using UnityEngine;

[Serializable]
public class WeightedSpawnGroup
{
	public ArenaSpawnGroup group;

	[Tooltip("Relative chance of this group being picked in this arena. Higher = more often.")]
	[Range(0.1f, 100f)]
	public float weight = 1f;

	[Tooltip("Uncheck to take this group out of rotation without removing it.")]
	public bool enabled = true;

	public bool IsValid
	{
		get
		{
			if (enabled && group != null)
			{
				return group.HasValidEntry;
			}
			return false;
		}
	}
}
