using System;
using System.Collections.Generic;
using DunGen;
using DunGen.Tags;
using DunGen.TilePlacement;
using UnityEngine;

public class AllTagsTileConnectionRule : MonoBehaviour
{
	[Serializable]
	public class TagPairGroup
	{
		[Tooltip("Optional label for clarity in the Inspector (e.g. \"Biome\", \"Shape\").")]
		public string name;

		[Tooltip("Matching ANY one of these pairs satisfies this group (OR).")]
		public List<TagPair> anyOf = new List<TagPair>();
	}

	[Header("Requirement groups (match ANY within a group, ALL groups must pass)")]
	[Tooltip("A connection is allowed only if it matches at least one pair in EVERY group. Leave empty to disable this rule.")]
	[SerializeField]
	private List<TagPairGroup> requiredGroups = new List<TagPairGroup>();

	[Tooltip("If true, a pair matches whether the tags appear as A->B or B->A between the two tiles.")]
	[SerializeField]
	private bool twoWay = true;

	[Tooltip("Higher priority rules are evaluated first. Only relevant if other custom rules exist.")]
	[SerializeField]
	private int priority;

	private TileConnectionRule rule;

	private void OnEnable()
	{
		rule = new TileConnectionRule(Evaluate, priority);
		DoorwayPairFinder.CustomConnectionRules.Add(rule);
		int num = 0;
		int num2 = 0;
		foreach (TagPairGroup requiredGroup in requiredGroups)
		{
			if (requiredGroup?.anyOf != null && requiredGroup.anyOf.Count != 0)
			{
				num++;
				num2 += requiredGroup.anyOf.Count;
			}
		}
		Debug.Log($"AllTagsTileConnectionRule registered: {num} active groups, {num2} pairs, twoWay={twoWay}, totalRules={DoorwayPairFinder.CustomConnectionRules.Count}");
	}

	private void OnDisable()
	{
		if (rule != null)
		{
			DoorwayPairFinder.CustomConnectionRules.Remove(rule);
			rule = null;
		}
	}

	private TileConnectionRule.ConnectionResult Evaluate(ProposedConnection connection)
	{
		Tile tile = connection.PreviousTile?.PrefabTile;
		Tile tile2 = connection.NextTile?.PrefabTile;
		if (tile == null || tile2 == null || requiredGroups.Count == 0)
		{
			return TileConnectionRule.ConnectionResult.Passthrough;
		}
		foreach (TagPairGroup requiredGroup in requiredGroups)
		{
			if (requiredGroup?.anyOf == null || requiredGroup.anyOf.Count == 0)
			{
				continue;
			}
			bool flag = false;
			foreach (TagPair item in requiredGroup.anyOf)
			{
				if (item != null && ((tile.Tags.HasTag(item.TagA) && tile2.Tags.HasTag(item.TagB)) || (twoWay && tile.Tags.HasTag(item.TagB) && tile2.Tags.HasTag(item.TagA))))
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				return TileConnectionRule.ConnectionResult.Deny;
			}
		}
		return TileConnectionRule.ConnectionResult.Passthrough;
	}
}
