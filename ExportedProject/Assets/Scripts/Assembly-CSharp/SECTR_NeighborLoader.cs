using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SECTR_Member))]
[AddComponentMenu("Procedural Worlds/SECTR/Stream/SECTR Neighbor Loader")]
public class SECTR_NeighborLoader : SECTR_Loader
{
	private SECTR_Member cachedMember;

	private List<SECTR_Sector> currentSectors = new List<SECTR_Sector>(4);

	private List<SECTR_Graph.Node> neighbors = new List<SECTR_Graph.Node>(8);

	[SECTR_ToolTip("Determines how far out to load neighbor sectors from the current sector. Depth of 0 means only the current Sector.")]
	public int MaxDepth = 1;

	public override bool Loaded
	{
		get
		{
			int count = currentSectors.Count;
			bool flag = count > 0;
			for (int i = 0; (i < count) & flag; i++)
			{
				SECTR_Sector sECTR_Sector = currentSectors[i];
				if (sECTR_Sector.Frozen)
				{
					SECTR_Chunk component = sECTR_Sector.GetComponent<SECTR_Chunk>();
					if ((bool)component && !component.IsLoaded())
					{
						flag = false;
						break;
					}
				}
			}
			return flag;
		}
	}

	public override string ToString()
	{
		return "Neighbor Loader";
	}

	private void OnEnable()
	{
		cachedMember = GetComponent<SECTR_Member>();
		cachedMember.Changed += _MembershipChanged;
	}

	private void OnDisable()
	{
		if ((bool)cachedMember)
		{
			cachedMember.Changed -= _MembershipChanged;
			cachedMember = null;
		}
		if (currentSectors.Count > 0)
		{
			_MembershipChanged(currentSectors, null);
		}
	}

	private void Start()
	{
		LockSelf(lockSelf: true);
	}

	private void Update()
	{
		if (locked && Loaded)
		{
			LockSelf(lockSelf: false);
		}
	}

	private void _MembershipChanged(List<SECTR_Sector> left, List<SECTR_Sector> joined)
	{
		if (joined != null)
		{
			int count = joined.Count;
			for (int i = 0; i < count; i++)
			{
				SECTR_Sector sECTR_Sector = joined[i];
				if (!sECTR_Sector || currentSectors.Contains(sECTR_Sector))
				{
					continue;
				}
				SECTR_Graph.BreadthWalk(ref neighbors, sECTR_Sector, (SECTR_Portal.PortalFlags)0, MaxDepth);
				int count2 = neighbors.Count;
				for (int j = 0; j < count2; j++)
				{
					SECTR_Chunk component = neighbors[j].Sector.GetComponent<SECTR_Chunk>();
					if ((bool)component)
					{
						component.AddReference();
					}
				}
				currentSectors.Add(sECTR_Sector);
			}
		}
		if (left == null)
		{
			return;
		}
		int count3 = left.Count;
		for (int k = 0; k < count3; k++)
		{
			SECTR_Sector sECTR_Sector2 = left[k];
			if (!sECTR_Sector2 || !currentSectors.Contains(sECTR_Sector2))
			{
				continue;
			}
			SECTR_Graph.BreadthWalk(ref neighbors, sECTR_Sector2, (SECTR_Portal.PortalFlags)0, MaxDepth);
			int count4 = neighbors.Count;
			for (int l = 0; l < count4; l++)
			{
				SECTR_Chunk component2 = neighbors[l].Sector.GetComponent<SECTR_Chunk>();
				if ((bool)component2)
				{
					component2.RemoveReference();
				}
			}
			currentSectors.Remove(sECTR_Sector2);
		}
	}
}
