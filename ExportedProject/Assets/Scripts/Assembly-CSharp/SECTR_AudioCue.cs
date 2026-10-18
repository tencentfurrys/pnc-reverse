using System;
using System.Collections.Generic;
using UnityEngine;

public class SECTR_AudioCue : ScriptableObject
{
	public enum PlaybackModes
	{
		Random = 0,
		Shuffle = 1,
		Loop = 2,
		PingPong = 3
	}

	public enum FalloffTypes
	{
		Linear = 0,
		Logarithmic = 1
	}

	public enum Spatializations
	{
		Simple2D = 0,
		Infinite3D = 1,
		Local3D = 2,
		Occludable3D = 3
	}

	[Serializable]
	public class ClipData
	{
		[SerializeField]
		private AudioClip clip;

		[SerializeField]
		private bool playedInShuffle;

		[SerializeField]
		private float volume = 1f;

		[SerializeField]
		private SECTR_ULong bakeTimestamp = new SECTR_ULong(0uL);

		public AnimationCurve HDRCurve;

		public AudioClip Clip => clip;

		public float Volume
		{
			get
			{
				return volume;
			}
			set
			{
				volume = value;
			}
		}

		public bool PlayedInShuffle
		{
			get
			{
				return playedInShuffle;
			}
			set
			{
				playedInShuffle = value;
			}
		}

		public ClipData(AudioClip clip)
		{
			this.clip = clip;
			playedInShuffle = false;
			volume = 1f;
		}
	}

	[SerializeField]
	[HideInInspector]
	private SECTR_AudioCue template;

	[SerializeField]
	[HideInInspector]
	private SECTR_AudioBus bus;

	private int clipPlaybackIndex = -1;

	private bool needsShuffling = true;

	private bool pingPongIncrement = true;

	[SECTR_ToolTip("List of Audio Clips for this Cue to choose from.")]
	public List<ClipData> AudioClips = new List<ClipData>();

	[SECTR_ToolTip("The rules for selecting which audio clip to play next")]
	public PlaybackModes PlaybackMode;

	[SECTR_ToolTip("Determines if the sound should be mixed in HDR or LDR.")]
	public bool HDR;

	[SECTR_ToolTip("The loudness, in dB(SPL), of this HDR Cue.")]
	public Vector2 Loudness = new Vector2(50f, 50f);

	[SECTR_ToolTip("The volume of this Cue.")]
	public Vector2 Volume = Vector2.one;

	[SECTR_ToolTip("The pitch adjustment of this Cue.")]
	public Vector2 Pitch = Vector2.one;

	[SECTR_ToolTip("Set to true to auto-loop this Cue.")]
	public bool Loops;

	[SECTR_ToolTip("Cue priority, lower is more important.", 0f, 255f)]
	public int Priority = 128;

	[SECTR_ToolTip("Prevent this Cue from recieving Audio Effects.")]
	public bool BypassEffects;

	[SECTR_ToolTip("Maximum number of instances of this Cue that can be played at once.", 1f, -1f)]
	public int MaxInstances = 10;

	[SECTR_ToolTip("Number of seconds over which to fade in the Cue when played.", 0f, -1f)]
	public float FadeInTime;

	[SECTR_ToolTip("Number of seconds over which to fade out the Cue when stopped.", 0f, -1f)]
	public float FadeOutTime;

	[SECTR_ToolTip("Number of seconds to keep this Audio Cue alive after it has stopped playing. (Used to allow effects like reverb etc. to play out before destroying the Audio Source.)")]
	public int KeepAliveTime;

	[SECTR_ToolTip("Sets rules for how to spatialize this sound.")]
	public Spatializations Spatialization = Spatializations.Local3D;

	[SECTR_ToolTip("Expands or narrows the range of speakers out of which this Cue plays.", 0f, 360f)]
	public float Spread;

	[SECTR_ToolTip("Moves the sound around the speaker field.", -1f, 1f)]
	public float Pan2D;

	[SECTR_ToolTip("Attenuation style of this clip.")]
	public FalloffTypes Falloff;

	[SECTR_ToolTip("The range at which the sound is no longer audible.", 0f, -1f)]
	public float MaxDistance = 100f;

	[SECTR_ToolTip("The range within which the sound will be at peak volume/loudness.", 0f, -1f)]
	public float MinDistance = 10f;

	[SECTR_ToolTip("Scales the amount of doppler effect applied to this Cue.", 0f, 1f)]
	public float DopplerLevel;

	[SECTR_ToolTip("Prevents too many instances of a cue playing near one another.", 0f, -1f)]
	public int ProximityLimit;

	[SECTR_ToolTip("The size of the proximity limit check.", "ProximityLimit", 0f, -1f)]
	public float ProximityRange = 10f;

	[SECTR_ToolTip("Allows you to scale down the amount of occlusion applied to this Cue (when occluded).", 0f, 1f)]
	public float OcclusionScale = 1f;

	[SECTR_ToolTip("The chance that this cue will actually make a sound when played.", 0f, 1f)]
	public float PlayProbability = 1f;

	[SECTR_ToolTip("Random delay before start of playback.")]
	public Vector2 Delay = Vector2.zero;

	[SECTR_ToolTip("Special prefab to use when playing this Cue. Useful for adding effects to this source.", null, false)]
	public GameObject Prefab;

	[SECTR_ToolTip("Control parameters for this Cue")]
	public List<SECTR_CueParam> ControlParams = new List<SECTR_CueParam>();

	public SECTR_AudioCue Template
	{
		get
		{
			return template;
		}
		set
		{
			if (template != value && value != this)
			{
				template = value;
			}
		}
	}

	public SECTR_AudioBus Bus
	{
		get
		{
			return bus;
		}
		set
		{
			if (bus != value)
			{
				bus = value;
			}
		}
	}

	public SECTR_AudioCue SourceCue
	{
		get
		{
			if (!(template != null))
			{
				return this;
			}
			return template;
		}
	}

	public bool Is3D => Spatialization != Spatializations.Simple2D;

	public bool IsLocal
	{
		get
		{
			if (Spatialization != Spatializations.Simple2D)
			{
				return Spatialization == Spatializations.Infinite3D;
			}
			return true;
		}
	}

	public int ClipIndex => clipPlaybackIndex;

	public ClipData GetNextClip()
	{
		int count = AudioClips.Count;
		if (count == 1)
		{
			return AudioClips[0];
		}
		if (count > 0)
		{
			switch (PlaybackMode)
			{
			case PlaybackModes.Random:
				return AudioClips[UnityEngine.Random.Range(0, count)];
			case PlaybackModes.Loop:
				clipPlaybackIndex = ++clipPlaybackIndex % count;
				return AudioClips[clipPlaybackIndex];
			case PlaybackModes.Shuffle:
				clipPlaybackIndex++;
				if (clipPlaybackIndex >= count)
				{
					clipPlaybackIndex = 0;
					needsShuffling = true;
				}
				if (needsShuffling)
				{
					_ShuffleClips();
					needsShuffling = false;
				}
				return AudioClips[clipPlaybackIndex];
			case PlaybackModes.PingPong:
				if (pingPongIncrement)
				{
					clipPlaybackIndex++;
					pingPongIncrement = clipPlaybackIndex < AudioClips.Count - 1;
				}
				else
				{
					clipPlaybackIndex--;
					pingPongIncrement = clipPlaybackIndex <= 0;
				}
				return AudioClips[clipPlaybackIndex];
			}
		}
		return null;
	}

	public float MinClipLength()
	{
		float num = float.MaxValue;
		bool flag = false;
		int count = AudioClips.Count;
		for (int i = 0; i < count; i++)
		{
			AudioClip clip = AudioClips[i].Clip;
			if ((bool)clip)
			{
				num = Mathf.Min(num, clip.length);
				flag = true;
			}
		}
		if (!flag)
		{
			return 0f;
		}
		return num;
	}

	public float MaxClipLength()
	{
		float num = 0f;
		int count = AudioClips.Count;
		for (int i = 0; i < count; i++)
		{
			AudioClip clip = AudioClips[i].Clip;
			if ((bool)clip)
			{
				num = Mathf.Max(num, clip.length);
			}
		}
		return num;
	}

	public void ResetClipIndex()
	{
		needsShuffling = true;
		pingPongIncrement = true;
		clipPlaybackIndex = -1;
	}

	private void OnEnable()
	{
		ResetClipIndex();
	}

	private void OnDisable()
	{
	}

	private void _ShuffleClips()
	{
		System.Random random = new System.Random();
		int num = AudioClips.Count;
		while (num >= 1)
		{
			num--;
			int index = random.Next(num + 1);
			ClipData value = AudioClips[index];
			AudioClips[index] = AudioClips[num];
			AudioClips[num] = value;
		}
	}
}
