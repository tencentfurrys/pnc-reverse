using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("Procedural Worlds/SECTR/Stream/SECTR Loading Door")]
public class SECTR_LoadingDoor : SECTR_Door
{
	private enum FadeMode
	{
		None = 0,
		FadeIn = 1,
		FadeOut = 2,
		Hold = 3
	}

	private class LoadRequest
	{
		public SECTR_Chunk chunkToLoad;

		public SECTR_Chunk chunkToUnload;

		public SECTR_Chunk loadedChunk;

		public bool enteredFront;

		public bool enteredBack;

		public FadeMode fadeMode;

		public float fadeAmount;

		public float holdStart;
	}

	private Texture2D fadeTexture;

	private Dictionary<Collider, LoadRequest> loadRequests = new Dictionary<Collider, LoadRequest>(4);

	[SECTR_ToolTip("Specifies which layers are allow to cause loads (vs simply opening the door).")]
	public LayerMask LoadLayers = 16777215;

	[SECTR_ToolTip("Should screen fade to black before loading.")]
	public bool FadeBeforeLoad;

	[SECTR_ToolTip("How long to fade out before loading. Also, how long to fade back in.", "FadeBeforeLoad")]
	public float FadeTime = 1f;

	[SECTR_ToolTip("How long to stay faded out. Helps cover pops right at the moment of loading.", "FadeBeforeLoad")]
	public float HoldTime = 0.1f;

	[SECTR_ToolTip("The color to fade the screen to on load.", "FadeBeforeLoad")]
	public Color FadeColor = Color.black;

	protected override void OnEnable()
	{
		base.OnEnable();
		if (FadeBeforeLoad)
		{
			fadeTexture = new Texture2D(1, 1);
			fadeTexture.SetPixel(0, 0, FadeColor);
			fadeTexture.Apply();
		}
	}

	protected override void OnTriggerEnter(Collider other)
	{
		base.OnTriggerEnter(other);
		if (!Portal || ((int)LoadLayers & (1 << other.gameObject.layer)) == 0)
		{
			return;
		}
		SECTR_Chunk sECTR_Chunk = _GetOppositeChunk(other.transform.position);
		if (!sECTR_Chunk)
		{
			return;
		}
		SECTR_Chunk sECTR_Chunk2 = null;
		if (loadRequests.TryGetValue(other, out var value))
		{
			if ((bool)value.chunkToUnload)
			{
				sECTR_Chunk2 = value.chunkToUnload;
				value.chunkToUnload = null;
			}
		}
		else
		{
			value = new LoadRequest();
		}
		if (FadeBeforeLoad && !sECTR_Chunk.IsLoaded())
		{
			value.fadeMode = FadeMode.FadeOut;
		}
		value.enteredFront = sECTR_Chunk.Sector == Portal.BackSector;
		value.enteredBack = sECTR_Chunk.Sector == Portal.FrontSector;
		if (FadeBeforeLoad)
		{
			value.chunkToLoad = sECTR_Chunk;
		}
		else
		{
			sECTR_Chunk.AddReference();
			value.loadedChunk = sECTR_Chunk;
		}
		loadRequests[other] = value;
		if ((bool)sECTR_Chunk2)
		{
			sECTR_Chunk2.RemoveReference();
		}
	}

	protected override void OnTriggerExit(Collider other)
	{
		base.OnTriggerExit(other);
		if (!Portal || ((int)LoadLayers & (1 << other.gameObject.layer)) == 0)
		{
			return;
		}
		SECTR_Chunk sECTR_Chunk = _GetOppositeChunk(other.transform.position);
		if (!sECTR_Chunk)
		{
			return;
		}
		LoadRequest loadRequest = loadRequests[other];
		if (FadeBeforeLoad && loadRequest.fadeMode == FadeMode.FadeOut)
		{
			loadRequest.fadeMode = FadeMode.FadeIn;
		}
		bool flag = sECTR_Chunk.Sector == Portal.FrontSector;
		bool flag2 = sECTR_Chunk.Sector == Portal.BackSector;
		if ((bool)loadRequest.loadedChunk && ((loadRequest.enteredFront & flag2) || (loadRequest.enteredBack & flag)))
		{
			loadRequest.chunkToUnload = loadRequest.loadedChunk;
		}
		else if ((loadRequest.enteredFront & flag) || (loadRequest.enteredBack & flag2))
		{
			loadRequest.chunkToUnload = sECTR_Chunk;
		}
		else
		{
			loadRequest.chunkToUnload = loadRequest.loadedChunk;
		}
		if (loadRequests.Count > 1 || IsClosed())
		{
			if ((bool)loadRequest.chunkToUnload)
			{
				loadRequest.chunkToUnload.RemoveReference();
			}
			loadRequests.Remove(other);
		}
	}

	private void OnGUI()
	{
		if (!FadeBeforeLoad)
		{
			return;
		}
		float num = Time.deltaTime / FadeTime;
		float num2 = 0f;
		foreach (LoadRequest value in loadRequests.Values)
		{
			switch (value.fadeMode)
			{
			case FadeMode.FadeOut:
				value.fadeAmount += num;
				if (value.fadeAmount >= 1f)
				{
					if ((bool)value.chunkToLoad)
					{
						value.chunkToLoad.AddReference();
						value.loadedChunk = value.chunkToLoad;
						value.chunkToLoad = null;
					}
					value.fadeMode = FadeMode.Hold;
					value.holdStart = Time.time;
				}
				break;
			case FadeMode.FadeIn:
				value.fadeAmount -= num;
				if (value.fadeAmount <= 0f)
				{
					value.fadeMode = FadeMode.None;
				}
				break;
			case FadeMode.Hold:
				if (!CanOpen())
				{
					value.holdStart = Time.time;
				}
				else if (Time.time >= value.holdStart + HoldTime)
				{
					value.fadeMode = FadeMode.FadeIn;
				}
				break;
			}
			value.fadeAmount = Mathf.Clamp01(value.fadeAmount);
			num2 = Mathf.Max(num2, value.fadeAmount);
		}
		if (num2 > 0f)
		{
			GUI.color = new Color(1f, 1f, 1f, num2);
			GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), fadeTexture);
		}
	}

	protected override bool CanOpen()
	{
		if ((bool)Portal)
		{
			if (!_IsSectorLoaded(Portal.FrontSector))
			{
				return false;
			}
			if (!_IsSectorLoaded(Portal.BackSector))
			{
				return false;
			}
		}
		return true;
	}

	private void OnClose()
	{
		if (loadRequests.Count == 1)
		{
			Dictionary<Collider, LoadRequest>.Enumerator enumerator = loadRequests.GetEnumerator();
			enumerator.MoveNext();
			LoadRequest value = enumerator.Current.Value;
			if ((bool)value.chunkToUnload)
			{
				value.chunkToUnload.RemoveReference();
				loadRequests.Clear();
			}
		}
	}

	private bool _IsSectorLoaded(SECTR_Sector sector)
	{
		if ((bool)sector && sector.Frozen)
		{
			SECTR_Chunk component = sector.GetComponent<SECTR_Chunk>();
			if ((bool)component && !component.IsLoaded())
			{
				return false;
			}
		}
		return true;
	}

	private SECTR_Chunk _GetOppositeChunk(Vector3 position)
	{
		if ((bool)Portal)
		{
			SECTR_Sector sECTR_Sector = (SECTR_Geometry.IsPointInFrontOfPlane(position, Portal.Center, Portal.Normal) ? Portal.BackSector : Portal.FrontSector);
			if ((bool)sECTR_Sector)
			{
				return sECTR_Sector.GetComponent<SECTR_Chunk>();
			}
		}
		return null;
	}
}
