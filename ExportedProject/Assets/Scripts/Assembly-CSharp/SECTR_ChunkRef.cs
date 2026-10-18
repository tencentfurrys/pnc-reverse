using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("")]
public class SECTR_ChunkRef : MonoBehaviour
{
	private static List<SECTR_ChunkRef> allChunkRefs = new List<SECTR_ChunkRef>();

	public Transform RealSector;

	public bool Recentered;

	public static SECTR_ChunkRef FindChunkRef(string chunkName)
	{
		int count = allChunkRefs.Count;
		for (int i = 0; i < count; i++)
		{
			SECTR_ChunkRef sECTR_ChunkRef = allChunkRefs[i];
			if (sECTR_ChunkRef.name == chunkName)
			{
				return sECTR_ChunkRef;
			}
		}
		return null;
	}

	private void OnEnable()
	{
		allChunkRefs.Add(this);
	}

	private void OnDisable()
	{
		allChunkRefs.Remove(this);
	}
}
