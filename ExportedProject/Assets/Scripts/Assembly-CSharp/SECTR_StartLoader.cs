using System;
using UnityEngine;

[RequireComponent(typeof(SECTR_Member))]
[AddComponentMenu("Procedural Worlds/SECTR/Stream/SECTR Start Loader")]
public class SECTR_StartLoader : SECTR_Loader
{
	private Texture2D fadeTexture;

	private float fadeAmount = 1f;

	private SECTR_Member cachedMember;

	[SECTR_ToolTip("Set to true if the scene should start at black and fade in when loaded.")]
	public bool FadeIn;

	[SECTR_ToolTip("Amount of time to fade in.", "FadeIn")]
	public float FadeTime = 2f;

	[SECTR_ToolTip("The color to fade the screen to on load.", "FadeIn")]
	public Color FadeColor = Color.black;

	[NonSerialized]
	public bool Paused;

	public override bool Loaded
	{
		get
		{
			bool result = true;
			int num = (cachedMember ? cachedMember.Sectors.Count : 0);
			for (int i = 0; i < num; i++)
			{
				SECTR_Sector sECTR_Sector = cachedMember.Sectors[i];
				if (sECTR_Sector.Frozen)
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
		return "Start Loader";
	}

	private void OnEnable()
	{
		cachedMember = GetComponent<SECTR_Member>();
		if (FadeIn)
		{
			fadeTexture = new Texture2D(1, 1);
			fadeTexture.SetPixel(0, 0, FadeColor);
			fadeTexture.Apply();
		}
	}

	private void OnDisable()
	{
		cachedMember = null;
		fadeTexture = null;
	}

	private void Start()
	{
		cachedMember.ForceUpdate(updateChildren: true);
		int count = cachedMember.Sectors.Count;
		for (int i = 0; i < count; i++)
		{
			SECTR_Chunk component = cachedMember.Sectors[i].GetComponent<SECTR_Chunk>();
			if ((bool)component)
			{
				component.AddReference();
			}
		}
		LockSelf(lockSelf: true);
	}

	private void Update()
	{
		if (Loaded)
		{
			if (locked)
			{
				LockSelf(lockSelf: false);
			}
			if (!FadeIn)
			{
				UnityEngine.Object.Destroy(this);
			}
		}
	}

	private void OnGUI()
	{
		if (FadeIn && base.enabled)
		{
			if (Loaded && !Paused)
			{
				float num = Time.deltaTime / FadeTime;
				fadeAmount -= num;
				fadeAmount = Mathf.Clamp01(fadeAmount);
			}
			GUI.color = new Color(1f, 1f, 1f, fadeAmount);
			GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), fadeTexture);
			if (fadeAmount == 0f)
			{
				UnityEngine.Object.Destroy(this);
			}
		}
	}
}
