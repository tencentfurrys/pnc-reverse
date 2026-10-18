using System.Collections.Generic;
using UnityEngine;

public class SECTR_AudioBus : ScriptableObject
{
	[SerializeField]
	[HideInInspector]
	private SECTR_AudioBus parent;

	private List<SECTR_AudioBus> children = new List<SECTR_AudioBus>();

	private float userVolume = 1f;

	private float userPitch = 1f;

	private float effectiveVolume = 1f;

	private float effectivePitch = 1f;

	private bool muted;

	[SECTR_ToolTip("The volume of this bus, between 0 and 1.", 0f, 1f)]
	public float Volume = 1f;

	[SECTR_ToolTip("The pitch of this bus, between 0 and 2.", 0f, 2f)]
	public float Pitch = 1f;

	public float UserVolume
	{
		get
		{
			return userVolume;
		}
		set
		{
			userVolume = value;
		}
	}

	public float UserPitch
	{
		get
		{
			return userPitch;
		}
		set
		{
			userPitch = value;
		}
	}

	public bool Muted
	{
		get
		{
			return muted;
		}
		set
		{
			muted = value;
		}
	}

	public float EffectiveVolume
	{
		get
		{
			return effectiveVolume;
		}
		set
		{
			effectiveVolume = (muted ? 0f : Mathf.Clamp01(Volume * userVolume * value));
		}
	}

	public float EffectivePitch
	{
		get
		{
			return effectivePitch;
		}
		set
		{
			effectivePitch = Mathf.Clamp(Pitch * userPitch * value, 0f, 2f);
		}
	}

	public SECTR_AudioBus Parent
	{
		get
		{
			return parent;
		}
		set
		{
			if (value != parent && value != this)
			{
				if ((bool)parent)
				{
					parent._RemoveChild(this);
				}
				parent = value;
				if ((bool)parent)
				{
					parent._AddChild(this);
				}
			}
		}
	}

	public List<SECTR_AudioBus> Children => children;

	public bool IsAncestorOf(SECTR_AudioBus bus)
	{
		SECTR_AudioBus sECTR_AudioBus = bus;
		while (sECTR_AudioBus != null)
		{
			if (sECTR_AudioBus == this)
			{
				return true;
			}
			sECTR_AudioBus = sECTR_AudioBus.Parent;
		}
		return false;
	}

	public bool IsDecendentOf(SECTR_AudioBus bus)
	{
		SECTR_AudioBus sECTR_AudioBus = Parent;
		while (sECTR_AudioBus != null)
		{
			if (sECTR_AudioBus == bus)
			{
				return true;
			}
			sECTR_AudioBus = sECTR_AudioBus.Parent;
		}
		return false;
	}

	public void ResetUserVolume()
	{
		userVolume = 1f;
		int count = children.Count;
		for (int i = 0; i < count; i++)
		{
			SECTR_AudioBus sECTR_AudioBus = children[i];
			if ((bool)sECTR_AudioBus)
			{
				sECTR_AudioBus.ResetUserVolume();
			}
		}
	}

	private void OnEnable()
	{
		if ((bool)parent)
		{
			parent._AddChild(this);
		}
	}

	private void OnDisable()
	{
		if ((bool)parent)
		{
			parent._RemoveChild(this);
		}
	}

	private void _AddChild(SECTR_AudioBus child)
	{
		if (child != null && !children.Contains(child))
		{
			children.Add(child);
		}
	}

	private void _RemoveChild(SECTR_AudioBus child)
	{
		children.Remove(child);
	}
}
