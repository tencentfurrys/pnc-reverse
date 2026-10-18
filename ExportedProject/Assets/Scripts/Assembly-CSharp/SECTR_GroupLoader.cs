using System.Collections.Generic;

public class SECTR_GroupLoader : SECTR_Loader
{
	[SECTR_ToolTip("The Sectors to load and unload together.")]
	public List<SECTR_Sector> Sectors = new List<SECTR_Sector>();

	public override bool Loaded
	{
		get
		{
			int count = Sectors.Count;
			bool result = count > 0;
			for (int i = 0; i < count; i++)
			{
				SECTR_Sector sECTR_Sector = Sectors[i];
				if ((bool)sECTR_Sector && sECTR_Sector.Frozen)
				{
					SECTR_Chunk component = sECTR_Sector.GetComponent<SECTR_Chunk>();
					if ((bool)component && !component.IsLoaded())
					{
						result = false;
						break;
					}
				}
			}
			return result;
		}
	}

	public override string ToString()
	{
		return "Group Loader";
	}

	private void OnEnable()
	{
		int count = Sectors.Count;
		for (int i = 0; i < count; i++)
		{
			SECTR_Sector sECTR_Sector = Sectors[i];
			if ((bool)sECTR_Sector)
			{
				SECTR_Chunk component = sECTR_Sector.GetComponent<SECTR_Chunk>();
				if ((bool)component)
				{
					component.ReferenceChange += ChunkChanged;
				}
			}
		}
	}

	private void OnDisable()
	{
		int count = Sectors.Count;
		for (int i = 0; i < count; i++)
		{
			SECTR_Sector sECTR_Sector = Sectors[i];
			if ((bool)sECTR_Sector)
			{
				SECTR_Chunk component = sECTR_Sector.GetComponent<SECTR_Chunk>();
				if ((bool)component)
				{
					component.ReferenceChange -= ChunkChanged;
				}
			}
		}
	}

	private void ChunkChanged(SECTR_Chunk source, SECTR_Chunk.LoadState loadState)
	{
		int count = Sectors.Count;
		for (int i = 0; i < count; i++)
		{
			SECTR_Sector sECTR_Sector = Sectors[i];
			if (!sECTR_Sector)
			{
				continue;
			}
			SECTR_Chunk component = sECTR_Sector.GetComponent<SECTR_Chunk>();
			if ((bool)component && component != source)
			{
				component.ReferenceChange -= ChunkChanged;
				switch (loadState)
				{
				case SECTR_Chunk.LoadState.Loading:
					component.AddReference();
					break;
				case SECTR_Chunk.LoadState.Unloading:
					component.RemoveReference();
					break;
				}
				component.ReferenceChange += ChunkChanged;
			}
		}
	}
}
