using System;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("")]
public class SECTR_LightmapRef : MonoBehaviour
{
	[Serializable]
	public class RefData
	{
		public Texture2D FarLightmap;

		public Texture2D NearLightmap;

		public int index = -1;
	}

	[Serializable]
	public class RenderData
	{
		public Renderer renderer;

		public int rendererLightmapIndex = -1;

		public Vector4 rendererLightmapScaleOffset = Vector4.zero;

		public Terrain terrain;

		public int terrainLightmapIndex = -1;
	}

	[SerializeField]
	[HideInInspector]
	private List<RefData> lightmapRefs = new List<RefData>();

	[SerializeField]
	[HideInInspector]
	private List<RenderData> lightmapRenderRefs = new List<RenderData>();

	private static int[] globalLightmapRefCount;

	public List<RefData> LightmapRefs => lightmapRefs;

	public List<RenderData> LightmapRenderers => lightmapRenderRefs;

	public static void InitRefCounts()
	{
		int num = LightmapSettings.lightmaps.Length;
		if (globalLightmapRefCount == null || globalLightmapRefCount.Length != num)
		{
			globalLightmapRefCount = new int[num];
		}
		for (int i = 0; i < num; i++)
		{
			LightmapData lightmapData = LightmapSettings.lightmaps[i];
			globalLightmapRefCount[i] = (((bool)lightmapData.lightmapColor || (bool)lightmapData.lightmapDir) ? 1 : 0);
		}
	}

	private void Start()
	{
		if ((Application.isEditor && !Application.isPlaying) || globalLightmapRefCount == null)
		{
			return;
		}
		int count = lightmapRenderRefs.Count;
		for (int i = 0; i < count; i++)
		{
			RenderData renderData = lightmapRenderRefs[i];
			if ((bool)renderData.renderer)
			{
				renderData.renderer.lightmapIndex = renderData.rendererLightmapIndex;
				renderData.renderer.lightmapScaleOffset = renderData.rendererLightmapScaleOffset;
			}
			if ((bool)renderData.terrain)
			{
				renderData.terrain.lightmapIndex = renderData.terrainLightmapIndex;
			}
		}
	}

	private void OnDestroy()
	{
	}
}
