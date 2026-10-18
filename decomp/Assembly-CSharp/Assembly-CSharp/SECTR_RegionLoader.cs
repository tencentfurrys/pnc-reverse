using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("Procedural Worlds/SECTR/Stream/SECTR Region Loader")]
public class SECTR_RegionLoader : SECTR_Loader
{
	private List<SECTR_Sector> sectors = new List<SECTR_Sector>(16);

	private List<SECTR_Sector> loadSectors = new List<SECTR_Sector>(16);

	private List<SECTR_Sector> unloadSectors = new List<SECTR_Sector>(16);

	private bool updated;

	[SECTR_ToolTip("The dimensions of the volume in which terrain chunks should be loaded.")]
	public Vector3 LoadSize = new Vector3(20f, 10f, 20f);

	[SECTR_ToolTip("The distance from the load size that you need to move for a Sector to unload (as a percentage).", 0f, 1f)]
	public float UnloadBuffer = 0.1f;

	[SECTR_ToolTip("If set, will only load Sectors in matching layers.")]
	public LayerMask LayersToLoad = -1;

	public override bool Loaded
	{
		get
		{
			int count = sectors.Count;
			bool flag = updated || count == 0;
			for (int i = 0; (i < count) & flag; i++)
			{
				SECTR_Sector sECTR_Sector = sectors[i];
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
		return "Region Loader";
	}

	private void Start()
	{
		LockSelf(lockSelf: true);
	}

	private void OnDisable()
	{
		int count = sectors.Count;
		for (int i = 0; i < count; i++)
		{
			SECTR_Sector sECTR_Sector = sectors[i];
			if ((bool)sECTR_Sector)
			{
				SECTR_Chunk component = sECTR_Sector.GetComponent<SECTR_Chunk>();
				if ((bool)component)
				{
					component.RemoveReference();
				}
			}
		}
		sectors.Clear();
		updated = false;
	}

	private void Update()
	{
		Vector3 position = transform.position;
		Bounds bounds = new Bounds(position, LoadSize);
		Bounds bounds2 = new Bounds(position, LoadSize * (1f + UnloadBuffer));
		SECTR_Sector.GetContaining(ref loadSectors, bounds);
		SECTR_Sector.GetContaining(ref unloadSectors, bounds2);
		int num = 0;
		int num2 = sectors.Count;
		while (num < num2)
		{
			SECTR_Sector sECTR_Sector = sectors[num];
			if (loadSectors.Contains(sECTR_Sector))
			{
				loadSectors.Remove(sECTR_Sector);
				num++;
			}
			else if (!unloadSectors.Contains(sECTR_Sector))
			{
				SECTR_Chunk sECTR_Chunk = ((sECTR_Sector != null) ? sECTR_Sector.GetComponent<SECTR_Chunk>() : null);
				if ((bool)sECTR_Chunk)
				{
					sECTR_Chunk.RemoveReference();
				}
				sectors.RemoveAt(num);
				num2--;
			}
			else
			{
				num++;
			}
		}
		num2 = loadSectors.Count;
		int value = LayersToLoad.value;
		if (num2 > 0)
		{
			for (num = 0; num < num2; num++)
			{
				SECTR_Sector sECTR_Sector2 = loadSectors[num];
				if ((bool)sECTR_Sector2 && sECTR_Sector2.Frozen && (value & (1 << sECTR_Sector2.gameObject.layer)) != 0)
				{
					SECTR_Chunk component = sECTR_Sector2.GetComponent<SECTR_Chunk>();
					if ((bool)component)
					{
						component.AddReference();
					}
					sectors.Add(sECTR_Sector2);
				}
			}
		}
		if (locked && Loaded)
		{
			LockSelf(lockSelf: false);
		}
		updated = true;
	}
}
