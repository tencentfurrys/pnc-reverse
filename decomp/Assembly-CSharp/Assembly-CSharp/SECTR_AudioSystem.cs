using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

[RequireComponent(typeof(AudioListener))]
[RequireComponent(typeof(SECTR_Member))]
[ExecuteInEditMode]
[AddComponentMenu("Procedural Worlds/SECTR/Audio/SECTR Audio System")]
public class SECTR_AudioSystem : MonoBehaviour
{
	public class Instance : SECTR_IAudioInstance
	{
		[Flags]
		private enum Flags
		{
			Loops = 1,
			FadingIn = 2,
			FadingOut = 4,
			Muted = 8,
			Local = 0x10,
			ThreeD = 0x20,
			Paused = 0x40,
			HDR = 0x80,
			Occludable = 0x100,
			Occluded = 0x200,
			ForcedInfinite = 0x400,
			Delayed = 0x800
		}

		public double KeepAliveTimeStamp;

		private int generation;

		private AudioSource source;

		private AudioLowPassFilter lowpass;

		private SECTR_AudioCue audioCue;

		private Transform parent;

		private Vector3 localPosition = Vector3.zero;

		private Flags flags;

		private float nextTestTime;

		private float fadeStarTime;

		private float basePitch = 1f;

		private float baseVolumeLoudness = 1f;

		private float userVolume = 1f;

		private float userPitch = 1f;

		private float occlusionAlpha = 1f;

		private AnimationCurve hdrCurve;

		private Dictionary<SECTR_CueParam, float> paramTable = new Dictionary<SECTR_CueParam, float>();

		private List<float> volumeParamValues = new List<float>();

		private List<float> pitchParamValues = new List<float>();

		private Dictionary<SECTR_CueParam.AttributeData, float> attributeParamValues = new Dictionary<SECTR_CueParam.AttributeData, float>();

		private Dictionary<SECTR_CueParam.AttributeData, float> attributeParamBaseValues = new Dictionary<SECTR_CueParam.AttributeData, float>();

		public int Generation => generation;

		public bool Active
		{
			get
			{
				if (Loops || Delayed || ((bool)source && (source.isPlaying || Paused)))
				{
					return !FadingOut;
				}
				return false;
			}
		}

		public Vector3 Position
		{
			get
			{
				Vector3 vector = localPosition;
				if ((bool)parent)
				{
					if (ThreeD && Local)
					{
						vector += parent.transform.position;
					}
					else
					{
						vector = parent.localToWorldMatrix.MultiplyPoint3x4(vector);
					}
				}
				return vector;
			}
			set
			{
				if ((bool)parent)
				{
					if (ThreeD && Local)
					{
						localPosition = value - parent.transform.position;
					}
					else
					{
						localPosition = parent.worldToLocalMatrix.MultiplyPoint3x4(value);
					}
				}
				else
				{
					localPosition = value;
				}
				if ((bool)source)
				{
					source.transform.position = value;
				}
			}
		}

		public Vector3 LocalPosition
		{
			get
			{
				return localPosition;
			}
			set
			{
				localPosition = value;
				if ((bool)source)
				{
					source.transform.position = Position;
				}
			}
		}

		public Transform Parent => parent;

		public float Volume
		{
			get
			{
				return userVolume;
			}
			set
			{
				if (userVolume != value)
				{
					userVolume = Mathf.Clamp01(value);
					Update(0f, volumeOnly: true);
				}
			}
		}

		public float Pitch
		{
			get
			{
				return userPitch;
			}
			set
			{
				if (userPitch != value)
				{
					userPitch = Mathf.Clamp(value, -3f, 3f);
					Update(0f, volumeOnly: true);
				}
			}
		}

		public bool Mute
		{
			get
			{
				return Muted;
			}
			set
			{
				if (Muted != value)
				{
					_SetFlag(Flags.Muted, value);
					if ((bool)source)
					{
						source.mute = value;
					}
				}
			}
		}

		public bool Pause
		{
			get
			{
				return Paused;
			}
			set
			{
				if (_GetFlag(Flags.Paused) == value)
				{
					return;
				}
				_SetFlag(Flags.Paused, value);
				if ((bool)source)
				{
					if (value)
					{
						source.Pause();
					}
					else
					{
						source.Play();
					}
				}
			}
		}

		public float TimeSeconds
		{
			get
			{
				if (!(source != null))
				{
					return 0f;
				}
				return source.time;
			}
			set
			{
				if ((bool)source)
				{
					source.time = value;
				}
			}
		}

		public int TimeSamples
		{
			get
			{
				if (!(source != null))
				{
					return 0;
				}
				return source.timeSamples;
			}
			set
			{
				if ((bool)source)
				{
					source.timeSamples = value;
				}
			}
		}

		public bool Loops => (flags & Flags.Loops) != 0;

		public bool Local => (flags & Flags.Local) != 0;

		public bool ThreeD => (flags & Flags.ThreeD) != 0;

		public bool FadingIn => (flags & Flags.FadingIn) != 0;

		public bool FadingOut => (flags & Flags.FadingOut) != 0;

		public bool Muted => (flags & Flags.Muted) != 0;

		public bool Paused
		{
			get
			{
				if ((flags & Flags.Paused) == 0)
				{
					return AudioListener.pause;
				}
				return true;
			}
		}

		public bool HDR => (flags & Flags.HDR) != 0;

		public bool Occludable => (flags & Flags.Occludable) != 0;

		public bool Occluded => (flags & Flags.Occluded) != 0;

		public bool ForcedInfinite => (flags & Flags.ForcedInfinite) != 0;

		public bool Delayed => (flags & Flags.Delayed) != 0;

		public SECTR_AudioBus Bus
		{
			get
			{
				if (!(audioCue != null))
				{
					return null;
				}
				return audioCue.Bus;
			}
		}

		public SECTR_AudioCue Cue => audioCue;

		public void ForceInfinite()
		{
			_SetFlag(Flags.ForcedInfinite, on: true);
			_SetFlag(Flags.Local, on: true);
			_SetFlag(Flags.ThreeD, on: true);
			occlusionAlpha = 1f;
			if ((bool)source)
			{
				source.rolloffMode = AudioRolloffMode.Linear;
				source.maxDistance = 1000000f;
				source.minDistance = source.maxDistance - 0.001f;
				source.dopplerLevel = 0f;
			}
			Update(0f, volumeOnly: true);
		}

		public void ForceOcclusion(bool occluded)
		{
			if ((bool)audioCue && audioCue.SourceCue.Spatialization == SECTR_AudioCue.Spatializations.Occludable3D)
			{
				_SetFlag(Flags.Occluded, occluded);
			}
		}

		public void SetParameter(string param, float value)
		{
			if (!(audioCue != null))
			{
				return;
			}
			SECTR_AudioCue sourceCue = audioCue.SourceCue;
			int count = sourceCue.ControlParams.Count;
			volumeParamValues.Clear();
			pitchParamValues.Clear();
			for (int i = 0; i < count; i++)
			{
				SECTR_CueParam sECTR_CueParam = sourceCue.ControlParams[i];
				if (sECTR_CueParam.name == param)
				{
					paramTable[sECTR_CueParam] = sECTR_CueParam.curve.Evaluate(value);
				}
				if (paramTable.TryGetValue(sECTR_CueParam, out var value2))
				{
					switch (sECTR_CueParam.affects)
					{
					case SECTR_CueParam.TargetType.Volume:
						volumeParamValues.Add(value2);
						break;
					case SECTR_CueParam.TargetType.Pitch:
						pitchParamValues.Add(value2);
						break;
					case SECTR_CueParam.TargetType.Attribute:
						attributeParamValues[sECTR_CueParam.attributeData] = value2;
						break;
					}
				}
			}
		}

		public AudioSource GetInternalAudioSource()
		{
			return source;
		}

		public void Init(SECTR_AudioCue audioCue, Transform parent, Vector3 localPosition, bool loops)
		{
			if (!(this.audioCue == null))
			{
				return;
			}
			generation++;
			this.audioCue = audioCue;
			SECTR_AudioCue sourceCue = audioCue.SourceCue;
			flags = (Flags)0;
			_SetFlag(Flags.Loops, loops);
			_SetFlag(Flags.Local, sourceCue.IsLocal);
			_SetFlag(Flags.ThreeD, sourceCue.Is3D);
			_SetFlag(Flags.HDR, sourceCue.HDR);
			_SetFlag(Flags.Occludable, audioSystem.OcclusionFlags != 0 && sourceCue.Spatialization == SECTR_AudioCue.Spatializations.Occludable3D);
			userVolume = 1f;
			userPitch = 1f;
			if (Local)
			{
				this.parent = Listener;
			}
			else
			{
				this.parent = parent;
			}
			this.localPosition = localPosition;
			int count = sourceCue.ControlParams.Count;
			if (count > 0)
			{
				for (int i = 0; i < count; i++)
				{
					SECTR_CueParam sECTR_CueParam = sourceCue.ControlParams[i];
					SetParameter(sECTR_CueParam.name, sECTR_CueParam.defaultValue);
				}
			}
			_AddProximityInstance(sourceCue);
			_ScheduleNextTest();
		}

		public void Clone(Instance instance, Vector3 newPosition)
		{
			if (instance == null || !instance.Active)
			{
				return;
			}
			generation++;
			audioCue = instance.audioCue;
			flags = instance.flags;
			fadeStarTime = instance.fadeStarTime;
			basePitch = instance.basePitch;
			baseVolumeLoudness = instance.baseVolumeLoudness;
			userVolume = instance.userVolume;
			userPitch = instance.userPitch;
			occlusionAlpha = instance.occlusionAlpha;
			hdrCurve = instance.hdrCurve;
			parent = instance.parent;
			Position = newPosition;
			_AddProximityInstance(audioCue.SourceCue);
			_ScheduleNextTest();
			if (_AcquireSource())
			{
				Update(0f, volumeOnly: true);
				if ((bool)source)
				{
					_SetFlag(Flags.Paused, on: false);
					source.clip = instance.source.clip;
					source.timeSamples = instance.source.timeSamples;
					source.Play();
				}
			}
		}

		public void Uninit()
		{
			KeepAliveTimeStamp = 0.0;
			if (audioCue != null)
			{
				if (audioCue.SourceCue.ProximityLimit > 0 && proximityTable.TryGetValue(audioCue, out var value))
				{
					value.Remove(this);
				}
				_ReleaseSource();
				audioCue = null;
				parent = null;
				flags = (Flags)0;
				if (paramTable.Count > 0)
				{
					paramTable.Clear();
					volumeParamValues.Clear();
					pitchParamValues.Clear();
					attributeParamBaseValues.Clear();
					attributeParamValues.Clear();
				}
			}
		}

		public void Play()
		{
			SECTR_AudioCue.ClipData nextClip = audioCue.GetNextClip();
			if (nextClip == null || !(nextClip.Clip != null) || !_AcquireSource())
			{
				return;
			}
			if (nextClip.Clip.loadState == AudioDataLoadState.Unloaded)
			{
				nextClip.Clip.LoadAudioData();
			}
			SECTR_AudioCue sourceCue = audioCue.SourceCue;
			if (sourceCue.FadeInTime > 0f)
			{
				fadeStarTime = currentTime;
				_SetFlag(Flags.FadingIn, on: true);
				_SetFlag(Flags.FadingOut, on: false);
			}
			if (Occludable && !ForcedInfinite)
			{
				_SetFlag(Flags.Occluded, IsOccluded(Position, audioSystem.OcclusionFlags));
				occlusionAlpha = (Occluded ? 1f : 0f);
			}
			if (HDR)
			{
				baseVolumeLoudness = UnityEngine.Random.Range(sourceCue.Loudness.x, sourceCue.Loudness.y);
			}
			else
			{
				baseVolumeLoudness = UnityEngine.Random.Range(sourceCue.Volume.x, sourceCue.Volume.y);
			}
			baseVolumeLoudness *= nextClip.Volume;
			if (HDR)
			{
				if (nextClip.HDRCurve != null && nextClip.HDRCurve.length > 0)
				{
					hdrCurve = nextClip.HDRCurve;
				}
				else
				{
					Debug.LogWarning("Playing " + audioCue.name + " without HDR keys. Bake HDR keys for higher quality audio.");
				}
			}
			Update(0f, volumeOnly: true);
			if ((bool)source)
			{
				_SetFlag(Flags.Paused, on: false);
				source.clip = nextClip.Clip;
				if (sourceCue.Delay.y > 0f)
				{
					source.Stop();
					_SetFlag(Flags.Delayed, on: true);
					nextTestTime = currentTime + UnityEngine.Random.Range(sourceCue.Delay.x, sourceCue.Delay.y);
				}
				else
				{
					source.Play();
				}
			}
		}

		public void Stop(bool stopImmediately)
		{
			_SetFlag(Flags.Loops, on: false);
			_SetFlag(Flags.Delayed, on: false);
			_Stop(stopImmediately);
		}

		public void Update(float deltaTime, bool volumeOnly)
		{
			if (Delayed)
			{
				if (!(currentTime >= nextTestTime))
				{
					return;
				}
				source.Play();
				_SetFlag(Flags.Delayed, on: false);
				_ScheduleNextTest();
			}
			SECTR_AudioCue sourceCue = audioCue.SourceCue;
			Vector3 position;
			if (ThreeD)
			{
				position = Position;
				if ((bool)source)
				{
					source.transform.position = position;
				}
			}
			else
			{
				position = Listener.position;
			}
			int count = sourceCue.ControlParams.Count;
			float num = 1f;
			if (FadingIn)
			{
				num = Mathf.Clamp01((currentTime - fadeStarTime) / sourceCue.FadeInTime);
				if (num >= 1f)
				{
					_SetFlag(Flags.FadingIn, on: false);
				}
			}
			else if (FadingOut)
			{
				float num2 = currentTime - fadeStarTime;
				num = Mathf.Clamp01(1f - num2 / sourceCue.FadeOutTime);
				if (num <= 0f)
				{
					_SetFlag(Flags.FadingOut, on: false);
					_Stop(stopImmediately: true);
				}
			}
			Vector3 position2 = Listener.transform.position;
			float num3 = Vector3.Magnitude(position - position2);
			if ((bool)source && ((source.isPlaying || Paused) | volumeOnly) && !Muted)
			{
				float num4 = (audioCue.Bus ? audioCue.Bus.EffectiveVolume : audioSystem.MasterBus.EffectiveVolume);
				float num5 = (audioCue.Bus ? audioCue.Bus.EffectivePitch : audioSystem.MasterBus.Pitch);
				float num6 = 1f;
				float num7 = 1f;
				if (count > 0)
				{
					SetParameter("distance", num3);
					SetParameter("time", source.time);
					int count2 = volumeParamValues.Count;
					for (int i = 0; i < count2; i++)
					{
						num6 *= volumeParamValues[i];
					}
					int count3 = pitchParamValues.Count;
					for (int j = 0; j < count3; j++)
					{
						num7 *= pitchParamValues[j];
					}
				}
				float num8 = 1f;
				if (HDR)
				{
					float num9 = 1f;
					if (!Local && num3 > sourceCue.MinDistance)
					{
						float maxDistance = sourceCue.MaxDistance;
						float minDistance = sourceCue.MinDistance;
						switch (sourceCue.Falloff)
						{
						case SECTR_AudioCue.FalloffTypes.Linear:
							num9 = 1f - Mathf.Clamp01((num3 - minDistance) / (maxDistance - minDistance));
							break;
						case SECTR_AudioCue.FalloffTypes.Logarithmic:
							num9 = Mathf.Clamp01(1f / Mathf.Max(num3 - minDistance - 1f, 0.001f));
							if (num3 > maxDistance)
							{
								num9 *= 1f - Mathf.Clamp01((num3 - maxDistance) / AudioSystem.CullingBuffer);
							}
							break;
						}
					}
					float num10 = baseVolumeLoudness;
					if (hdrCurve != null)
					{
						float num11 = hdrCurve.Evaluate(source.time);
						num10 += num11;
					}
					num10 += 20f * Mathf.Log10(Mathf.Max(userVolume * num9, 0.001f));
					if (num10 < windowHDRMin && (volumeOnly || (baseVolumeLoudness - windowHDRMin) / audioSystem.HDRDecay > source.time - source.clip.length))
					{
						_Stop(stopImmediately: false);
						return;
					}
					num10 += 20f * Mathf.Log10(Mathf.Max(num * num6, 0.001f));
					currentLoudness += Mathf.Pow(10f, num10 * 0.1f);
					num8 = Mathf.Clamp01(Mathf.Pow(10f, (num10 - windowHDRMax) * 0.05f));
				}
				else
				{
					num8 = baseVolumeLoudness * num * userVolume * num6;
				}
				if (Occludable)
				{
					float num12 = 1f;
					occlusionAlpha += deltaTime * (Occluded ? num12 : (0f - num12));
					occlusionAlpha = Mathf.Clamp01(occlusionAlpha);
					float t = occlusionAlpha * sourceCue.OcclusionScale;
					num8 *= Mathf.Lerp(1f, audioSystem.OcclusionVolume, t);
					if ((bool)lowpass)
					{
						lowpass.enabled = occlusionAlpha > 0f;
						if (lowpass.enabled)
						{
							lowpass.cutoffFrequency = Mathf.Lerp(22000f, audioSystem.OcclusionCutoff, t);
							lowpass.lowpassResonanceQ = Mathf.Lerp(1f, audioSystem.OcclusionResonanceQ, t);
						}
					}
				}
				source.volume = Mathf.Clamp01(num8 * num4);
				source.pitch = Mathf.Clamp(userPitch * basePitch * num7 * num5, 0f, 2f);
			}
			if (volumeOnly)
			{
				return;
			}
			if ((bool)source && (source.isPlaying || Paused) && !Local && audioSystem.BlendNearbySounds)
			{
				float num13 = 0f;
				if (num3 <= audioSystem.NearBlendRange.x)
				{
					num13 = 0f;
				}
				else
				{
					num13 = ((!(num3 <= audioSystem.NearBlendRange.y)) ? 1f : (Mathf.Clamp01(num3 - audioSystem.NearBlendRange.x) / (audioSystem.NearBlendRange.y - audioSystem.NearBlendRange.x)));
				}
				source.spatialBlend = num13;
			}
			if (Loops && !Paused)
			{
				bool flag = source != null && source.isPlaying;
				bool flag2 = !flag && (!HDR || baseVolumeLoudness >= windowHDRMin);
				if (Local)
				{
					if ((!flag & flag2) && _CheckInstances(audioCue, flag))
					{
						Play();
					}
				}
				else if (currentTime >= nextTestTime)
				{
					bool flag3 = _CheckProximity(audioCue, parent, localPosition, this);
					if (((flag3 && !flag) & flag2) && _CheckInstances(audioCue, flag))
					{
						Play();
					}
					else if (!flag3 & flag)
					{
						_Stop(stopImmediately: true);
					}
					else if (Occludable && !ForcedInfinite)
					{
						_SetFlag(Flags.Occluded, IsOccluded(position, audioSystem.OcclusionFlags));
					}
					_ScheduleNextTest();
				}
			}
			if (!source || count <= 0)
			{
				return;
			}
			Dictionary<SECTR_CueParam.AttributeData, float>.Enumerator enumerator = attributeParamBaseValues.GetEnumerator();
			while (enumerator.MoveNext())
			{
				SECTR_CueParam.AttributeData key = enumerator.Current.Key;
				Component component = ((key.ComponentType != null) ? source.GetComponent(key.ComponentType) : null);
				if (!component)
				{
					continue;
				}
				if (key.fieldAttribute)
				{
					FieldInfo field = key.ComponentType.GetField(key.attributeName);
					if (field != null)
					{
						field.SetValue(component, enumerator.Current.Value);
					}
				}
				else
				{
					PropertyInfo property = key.ComponentType.GetProperty(key.attributeName);
					if (property != null)
					{
						property.SetValue(component, enumerator.Current.Value, null);
					}
				}
			}
			enumerator = attributeParamValues.GetEnumerator();
			while (enumerator.MoveNext())
			{
				SECTR_CueParam.AttributeData key2 = enumerator.Current.Key;
				Component component2 = ((key2.ComponentType != null) ? source.GetComponent(key2.ComponentType) : null);
				if (!component2)
				{
					continue;
				}
				if (key2.fieldAttribute)
				{
					FieldInfo field2 = key2.ComponentType.GetField(key2.attributeName);
					if (field2 != null)
					{
						float num14 = (float)field2.GetValue(component2);
						field2.SetValue(component2, num14 * enumerator.Current.Value);
					}
				}
				else
				{
					PropertyInfo property2 = key2.ComponentType.GetProperty(key2.attributeName);
					if (property2 != null)
					{
						float num15 = (float)property2.GetValue(component2, null);
						property2.SetValue(component2, num15 * enumerator.Current.Value, null);
					}
				}
			}
		}

		private void _SetFlag(Flags flag, bool on)
		{
			if (on)
			{
				flags |= flag;
			}
			else
			{
				flags &= ~flag;
			}
		}

		private bool _GetFlag(Flags flag)
		{
			return (flags & flag) != 0;
		}

		private bool _AcquireSource()
		{
			if (!source)
			{
				SECTR_AudioCue sourceCue = audioCue.SourceCue;
				bool flag = Occludable && !sourceCue.BypassEffects && SECTR_Modules.HasPro();
				if (sourceCue.Prefab != null)
				{
					Stack<AudioSource> value = null;
					if (!prefabSourcePool.TryGetValue(sourceCue.Prefab, out value))
					{
						value = new Stack<AudioSource>(8);
						prefabSourcePool[sourceCue.Prefab] = value;
					}
					if (value.Count > 0)
					{
						source = value.Pop();
						if (flag)
						{
							lowpass = source.GetComponent<AudioLowPassFilter>();
							if (lowpass == null)
							{
								lowpass = source.gameObject.AddComponent<AudioLowPassFilter>();
							}
						}
					}
					else
					{
						GameObject gameObject = UnityEngine.Object.Instantiate(sourceCue.Prefab);
						source = gameObject.GetComponent<AudioSource>();
						if (source == null)
						{
							source = gameObject.AddComponent<AudioSource>();
						}
						if (flag)
						{
							lowpass = source.GetComponent<AudioLowPassFilter>();
							if (lowpass == null)
							{
								lowpass = source.gameObject.AddComponent<AudioLowPassFilter>();
							}
						}
						gameObject.hideFlags = HideFlags.HideAndDontSave;
						gameObject.transform.parent = sourcePoolParent;
					}
				}
				else
				{
					flag = flag && lowpassSourcePool.Count > 0;
					source = (flag ? lowpassSourcePool.Pop() : simpleSourcePool.Pop());
				}
				if ((bool)source)
				{
					if (flag)
					{
						lowpass = source.GetComponent<AudioLowPassFilter>();
						lowpass.enabled = false;
					}
					source.volume = 1f;
					source.pitch = 1f;
					source.time = 0f;
					source.timeSamples = 0;
					source.priority = sourceCue.Priority;
					source.bypassEffects = sourceCue.BypassEffects;
					source.bypassReverbZones = sourceCue.BypassEffects;
					source.loop = sourceCue.Loops;
					source.spread = sourceCue.Spread;
					source.mute = Muted;
					basePitch = UnityEngine.Random.Range(sourceCue.Pitch.x, sourceCue.Pitch.y);
					if (sourceCue.MaxInstances > 0)
					{
						if (maxInstancesTable.TryGetValue(audioCue, out var value2))
						{
							value2 = (maxInstancesTable[audioCue] = value2 + 1);
						}
						else
						{
							maxInstancesTable.Add(audioCue, 1);
						}
					}
					source.panStereo = 0f;
					source.spatialBlend = 1f;
					if (Local)
					{
						if (ThreeD)
						{
							source.rolloffMode = AudioRolloffMode.Linear;
							source.maxDistance = 1000000f;
							source.minDistance = source.maxDistance - 0.001f;
						}
						else
						{
							source.panStereo = sourceCue.Pan2D;
							source.spatialBlend = 0f;
						}
						source.dopplerLevel = 0f;
						if ((currentAmbience != null && currentAmbience.BackgroundLoop == audioCue) || (currentMusic != null && currentMusic == audioCue))
						{
							source.priority = 0;
						}
					}
					else
					{
						if (HDR)
						{
							source.rolloffMode = AudioRolloffMode.Linear;
							source.minDistance = 1000000f;
							source.maxDistance = source.minDistance + 0.001f;
						}
						else
						{
							SECTR_AudioCue.FalloffTypes falloff = sourceCue.Falloff;
							if (falloff != SECTR_AudioCue.FalloffTypes.Linear && falloff == SECTR_AudioCue.FalloffTypes.Logarithmic)
							{
								source.rolloffMode = AudioRolloffMode.Logarithmic;
							}
							else
							{
								source.rolloffMode = AudioRolloffMode.Linear;
							}
							source.minDistance = sourceCue.MinDistance;
							source.maxDistance = Mathf.Max(sourceCue.MaxDistance, sourceCue.MinDistance + 0.001f);
						}
						source.dopplerLevel = sourceCue.DopplerLevel;
						source.velocityUpdateMode = AudioVelocityUpdateMode.Dynamic;
					}
					source.transform.position = Position;
					source.gameObject.SetActive(value: true);
					int count = sourceCue.ControlParams.Count;
					for (int i = 0; i < count; i++)
					{
						SECTR_CueParam sECTR_CueParam = sourceCue.ControlParams[i];
						if (sECTR_CueParam.affects != SECTR_CueParam.TargetType.Attribute)
						{
							continue;
						}
						SECTR_CueParam.AttributeData attributeData = sECTR_CueParam.attributeData;
						Component component = ((attributeData.ComponentType != null) ? source.GetComponent(attributeData.ComponentType) : null);
						if (!component)
						{
							continue;
						}
						if (sECTR_CueParam.attributeData.fieldAttribute)
						{
							FieldInfo field = attributeData.ComponentType.GetField(sECTR_CueParam.attributeData.attributeName);
							if (field != null)
							{
								attributeParamBaseValues[sECTR_CueParam.attributeData] = (float)field.GetValue(component);
							}
						}
						else
						{
							PropertyInfo property = attributeData.ComponentType.GetProperty(sECTR_CueParam.attributeData.attributeName);
							if (property != null)
							{
								attributeParamBaseValues[sECTR_CueParam.attributeData] = (float)property.GetValue(component, null);
							}
						}
					}
				}
			}
			return source != null;
		}

		private void _ReleaseSource()
		{
			if (!(source != null))
			{
				return;
			}
			SECTR_AudioCue sourceCue = audioCue.SourceCue;
			if (sourceCue.MaxInstances > 0 && maxInstancesTable.TryGetValue(audioCue, out var value))
			{
				value--;
				if (value <= 0)
				{
					maxInstancesTable.Remove(audioCue);
				}
				else
				{
					maxInstancesTable[audioCue] = value;
				}
			}
			source.Stop();
			source.clip = null;
			source.gameObject.SetActive(value: false);
			if ((bool)sourceCue.Prefab)
			{
				prefabSourcePool[sourceCue.Prefab].Push(source);
			}
			else if ((bool)lowpass)
			{
				lowpass.enabled = false;
				lowpassSourcePool.Push(source);
			}
			else
			{
				simpleSourcePool.Push(source);
			}
			source = null;
			lowpass = null;
			hdrCurve = null;
		}

		private void _AddProximityInstance(SECTR_AudioCue srcCue)
		{
			int proximityLimit = srcCue.ProximityLimit;
			if (proximityLimit > 0)
			{
				if (!proximityTable.TryGetValue(audioCue, out var value))
				{
					value = new List<Instance>(proximityLimit * 2);
					proximityTable[audioCue] = value;
				}
				value.Add(this);
			}
		}

		private void _ScheduleNextTest()
		{
			if (nextTestTime < currentTime)
			{
				nextTestTime = currentTime + UnityEngine.Random.Range(audioSystem.RetestInterval.x, audioSystem.RetestInterval.y);
			}
		}

		private void _Stop(bool stopImmediately)
		{
			if (!stopImmediately && (bool)source && source.isPlaying && (bool)audioCue && audioCue.SourceCue.FadeOutTime > 0f)
			{
				if (FadingIn)
				{
					float num = 1f - Mathf.Clamp01((currentTime - fadeStarTime) / audioCue.SourceCue.FadeInTime);
					fadeStarTime = currentTime - num * audioCue.SourceCue.FadeOutTime;
				}
				else
				{
					fadeStarTime = currentTime;
				}
				_SetFlag(Flags.FadingOut, on: true);
				_SetFlag(Flags.FadingIn, on: false);
				return;
			}
			if (KeepAliveTimeStamp == 0.0)
			{
				KeepAliveTimeStamp = GetUnixTimeStamp();
			}
			if (GetUnixTimeStamp() >= KeepAliveTimeStamp + (double)audioCue.KeepAliveTime)
			{
				_SetFlag(Flags.FadingOut, on: false);
				_ReleaseSource();
			}
			else
			{
				_SetFlag(Flags.FadingOut, on: true);
			}
		}
	}

	[Flags]
	public enum OcclusionModes
	{
		Graph = 1,
		Raycast = 2,
		Distance = 4
	}

	private static SECTR_AudioSystem audioSystem;

	private static Stack<Instance> instancePool;

	private static Stack<AudioSource> simpleSourcePool;

	private static Stack<AudioSource> lowpassSourcePool;

	private static Dictionary<GameObject, Stack<AudioSource>> prefabSourcePool;

	private static Transform sourcePoolParent;

	private static List<Instance> activeInstances;

	private static Dictionary<SECTR_AudioCue, int> maxInstancesTable;

	private static Dictionary<SECTR_AudioCue, List<Instance>> proximityTable;

	private static float currentTime;

	private static List<SECTR_AudioAmbience> ambienceStack;

	private static SECTR_AudioAmbience currentAmbience;

	private static SECTR_AudioCueInstance ambienceLoop;

	private static SECTR_AudioCueInstance ambienceOneShot;

	private static float nextAmbienceOneShotTime;

	private static SECTR_AudioCue currentMusic;

	private static SECTR_AudioCueInstance musicLoop;

	private static float windowHDRMax;

	private static float windowHDRMin;

	private static float currentLoudness;

	private static List<SECTR_Graph.Node> occlusionPath;

	private static SECTR_Member cachedMember;

	private const float EPSILON = 0.001f;

	[SECTR_ToolTip("The maximum number of instances that can be active at once. Inaudible sounds do not count against this limit.")]
	public int MaxInstances = 128;

	[SECTR_ToolTip("The number of instances to allocate with lowpass effects (for occlusion and the like).")]
	public int LowpassInstances = 32;

	[SECTR_ToolTip("The Bus at the top of the mixing heirarchy. Required to play sounds.", null, false)]
	public SECTR_AudioBus MasterBus;

	[SECTR_ToolTip("The baseline settings for any environmental audio. Will be audible when no other ambiences are active.")]
	public SECTR_AudioAmbience DefaultAmbience = new SECTR_AudioAmbience();

	[SECTR_ToolTip("Minimum Loudness for the HDR mixer. Current Loudness will never drop below this.", 0f, 200f)]
	public float HDRBaseLoudness = 50f;

	[SECTR_ToolTip("The maximum difference between the loudest sound and the softest sound before sounds are simply culled out.", 0f, 200f)]
	public float HDRWindowSize = 50f;

	[SECTR_ToolTip("Speed at which HDR window decays after a loud sound is played.", 0f, 100f)]
	public float HDRDecay = 1f;

	[SECTR_ToolTip("Should sounds close to the listener be blended into 2D (to avoid harsh stereo switching).")]
	public bool BlendNearbySounds = true;

	[SECTR_ToolTip("Objects close to the listener will be blended into 2D, as a kind of fake HRTF. This determines the start and end of that blend.", "BlendNearbySounds")]
	public Vector2 NearBlendRange = new Vector2(0.25f, 0.75f);

	[SECTR_ToolTip("Determines what kind of logic to use for computing sound occlusion.", null, typeof(OcclusionModes))]
	public OcclusionModes OcclusionFlags;

	[SECTR_ToolTip("The distance beyond which sounds will be considered occluded, if Distance occlusion is enabled.", "OcclusionFlags")]
	public float OcclusionDistance = 100f;

	[SECTR_ToolTip("The layers to test against when raycasting for occlusion.", "OcclusionFlags")]
	public LayerMask RaycastLayers = -5;

	[SECTR_ToolTip("The amount by which to decrease the volume of occluded sounds.", "OcclusionFlags", 0f, 1f)]
	public float OcclusionVolume = 0.5f;

	[SECTR_ToolTip("The frequency cutoff of the lowpass filter for occluded sounds.", "OcclusionFlags", 10f, 22000f)]
	public float OcclusionCutoff = 2200f;

	[SECTR_ToolTip("The resonance Q of the lowpass filter for occluded sounds.", "OcclusionFlags", 1f, 10f)]
	public float OcclusionResonanceQ = 1f;

	[SECTR_ToolTip("The amount of time between tests to see if looping sounds should start or stop running.")]
	public Vector2 RetestInterval = new Vector2(0.5f, 1f);

	[SECTR_ToolTip("The amount of buffer to give before culling distant sounds.")]
	public float CullingBuffer = 10f;

	[SECTR_ToolTip("Enable or disable of the in-game audio HUD.", true)]
	public bool ShowAudioHUD;

	[SECTR_ToolTip("Material to use to render HUD lines.", true)]
	public Material HUDLineMaterial;

	[SECTR_ToolTip("In the editor only, puts the listener at the AudioSystem, not at the Scene Camera.", true)]
	public bool Debugging;

	public static bool Initialized => audioSystem != null;

	public static SECTR_Member Member => cachedMember;

	public static SECTR_AudioSystem AudioSystem => audioSystem;

	public static Transform Listener => audioSystem.transform;

	public static List<Instance> ActiveInstances => activeInstances;

	public static SECTR_AudioCueInstance Play(SECTR_AudioCue audioCue, Vector3 position, bool loop)
	{
		return Play(audioCue, null, position, loop);
	}

	public static SECTR_AudioCueInstance Play(SECTR_AudioCue audioCue, Transform parent, Vector3 localPosition, bool loop)
	{
		if (!Initialized)
		{
			Debug.LogWarning("Cannot play sounds before SECTR_AudioSystem is initialized.");
			return default;
		}
		if (audioSystem.MasterBus == null)
		{
			Debug.LogWarning("SECTR_AudioSystem needs a Master Bus before you can play sounds.");
			return default;
		}
		if (activeInstances.Count >= audioSystem.MaxInstances || instancePool.Count == 0)
		{
			Debug.LogWarning("Global max audio instances exceeded.");
			return default;
		}
		if (audioCue == null || !_CheckInstances(audioCue, isPlaying: false))
		{
			return default;
		}
		if (audioCue.AudioClips.Count == 0)
		{
			Debug.LogWarning("Cannot play a clipless Audio Cues.");
			return default;
		}
		SECTR_AudioCue sourceCue = audioCue.SourceCue;
		if (UnityEngine.Random.value <= sourceCue.PlayProbability)
		{
			bool flag = sourceCue.IsLocal || _CheckProximity(audioCue, parent, localPosition, null);
			loop |= sourceCue.Loops;
			if (flag | loop)
			{
				Instance instance = instancePool.Pop();
				activeInstances.Add(instance);
				instance.Init(audioCue, parent, localPosition, loop);
				if (flag)
				{
					instance.Play();
				}
				return new SECTR_AudioCueInstance(instance, instance.Generation);
			}
		}
		return default;
	}

	public static SECTR_AudioCueInstance Clone(SECTR_AudioCueInstance instance, Vector3 newPosition)
	{
		if ((bool)instance && instancePool.Count > 0)
		{
			Instance instance2 = instancePool.Pop();
			activeInstances.Add(instance2);
			instance2.Clone((Instance)instance.GetInternalInstance(), newPosition);
			return new SECTR_AudioCueInstance(instance2, instance2.Generation);
		}
		return default;
	}

	public static void PlayMusic(SECTR_AudioCue musicCue)
	{
		if (!Initialized)
		{
			Debug.LogWarning("Cannot play music before Audio System is initialized.");
		}
		else if (musicCue != null)
		{
			if (musicCue.Is3D)
			{
				Debug.LogWarning("Music Cue " + musicCue.name + "is 3D but music should be Simple 2D.");
			}
			musicLoop.Stop(stopImmediately: false);
			currentMusic = musicCue;
			musicLoop = Play(currentMusic, Listener, Vector3.zero, loop: true);
		}
	}

	public static void StopMusic(bool stopImmediate)
	{
		if (Initialized)
		{
			musicLoop.Stop(stopImmediate);
			currentMusic = null;
		}
	}

	public static void PushAmbience(SECTR_AudioAmbience ambience)
	{
		if (!Initialized)
		{
			Debug.LogWarning("Cannot activate an ambience before audio system is initialzied.");
		}
		else if (ambience != null)
		{
			ambienceStack.Add(ambience);
		}
	}

	public static void RemoveAmbience(SECTR_AudioAmbience ambience)
	{
		if (Initialized && ambience != null)
		{
			ambienceStack.Remove(ambience);
		}
	}

	public static void SetBusVolume(string busName, float volume)
	{
		if (!Initialized)
		{
			Debug.LogWarning("Cannot activate an ambience before audio system is initialzied.");
		}
		else if (!string.IsNullOrEmpty(busName))
		{
			SetBusVolume(_FindBus(audioSystem.MasterBus, busName), volume);
		}
	}

	public static void SetBusVolume(SECTR_AudioBus bus, float volume)
	{
		if (!Initialized)
		{
			Debug.LogWarning("Cannot set bus volume before Audio System is initialzied.");
		}
		else if ((bool)bus)
		{
			bus.UserVolume = volume;
		}
	}

	public static void MuteBus(string busName, bool mute)
	{
		if (!Initialized)
		{
			Debug.LogWarning("Cannot mute bus before Audio System is initialzied.");
		}
		else if (!string.IsNullOrEmpty(busName))
		{
			MuteBus(_FindBus(audioSystem.MasterBus, busName), mute);
		}
	}

	public static void MuteBus(SECTR_AudioBus bus, bool mute)
	{
		if (!Initialized)
		{
			Debug.LogWarning("Cannot mute bus before Audio System is initialzied.");
		}
		else if ((bool)bus)
		{
			bus.Muted = mute;
		}
	}

	public static void PauseBus(SECTR_AudioBus bus, bool paused)
	{
		if (!Initialized)
		{
			Debug.LogWarning("Cannot pause bus before Audio System is initialzied.");
		}
		else
		{
			if (!bus)
			{
				return;
			}
			int count = activeInstances.Count;
			for (int i = 0; i < count; i++)
			{
				Instance instance = activeInstances[i];
				if (bus.IsAncestorOf(instance.Bus))
				{
					instance.Pause = paused;
				}
			}
		}
	}

	public static bool IsOccluded(Vector3 worldSpacePosition, OcclusionModes occlusionFlags)
	{
		bool flag = false;
		Vector3 position = Listener.position;
		Vector3 direction = position - worldSpacePosition;
		float sqrMagnitude = direction.sqrMagnitude;
		if (!flag && (occlusionFlags & OcclusionModes.Distance) != 0)
		{
			flag = sqrMagnitude >= audioSystem.OcclusionDistance * audioSystem.OcclusionDistance;
		}
		if (!flag && (occlusionFlags & OcclusionModes.Raycast) != 0)
		{
			float maxDistance = Mathf.Sqrt(sqrMagnitude);
			flag = Physics.Raycast(worldSpacePosition, direction, out var hitInfo, maxDistance, audioSystem.RaycastLayers) && hitInfo.transform != Listener;
		}
		if (!flag && (occlusionFlags & OcclusionModes.Graph) != 0)
		{
			SECTR_Graph.FindShortestPath(ref occlusionPath, worldSpacePosition, position, (SECTR_Portal.PortalFlags)0);
			int count = occlusionPath.Count;
			for (int i = 0; i < count; i++)
			{
				if (flag)
				{
					break;
				}
				SECTR_Graph.Node node = occlusionPath[i];
				if ((bool)node.Portal && (node.Portal.Flags & SECTR_Portal.PortalFlags.Closed) != 0)
				{
					flag = true;
				}
			}
		}
		return flag;
	}

	private void OnEnable()
	{
		if ((bool)audioSystem && audioSystem != this)
		{
			UnityEngine.Object.Destroy(this);
		}
		else if (audioSystem == null)
		{
			audioSystem = this;
			instancePool = new Stack<Instance>(MaxInstances);
			for (int i = 0; i < MaxInstances; i++)
			{
				instancePool.Push(new Instance());
			}
			int num = (SECTR_Modules.HasPro() ? Mathf.Max(0, MaxInstances - LowpassInstances) : MaxInstances);
			int num2 = MaxInstances - num;
			simpleSourcePool = new Stack<AudioSource>(num);
			lowpassSourcePool = (SECTR_Modules.HasPro() ? new Stack<AudioSource>(num2) : null);
			prefabSourcePool = new Dictionary<GameObject, Stack<AudioSource>>(32);
			HideFlags hideFlags = HideFlags.HideAndDontSave;
			sourcePoolParent = new GameObject("SourcePool")
			{
				hideFlags = hideFlags
			}.transform;
			for (int j = 0; j < num; j++)
			{
				GameObject gameObject = new GameObject("SimpleInstance" + j);
				gameObject.hideFlags = hideFlags;
				gameObject.transform.parent = sourcePoolParent.transform;
				AudioSource audioSource = gameObject.AddComponent<AudioSource>();
				audioSource.playOnAwake = false;
				gameObject.SetActive(value: false);
				simpleSourcePool.Push(audioSource);
			}
			for (int k = 0; k < num2; k++)
			{
				GameObject gameObject2 = new GameObject("LowpassInstance" + k);
				gameObject2.hideFlags = hideFlags;
				gameObject2.transform.parent = sourcePoolParent.transform;
				AudioSource audioSource2 = gameObject2.AddComponent<AudioSource>();
				audioSource2.playOnAwake = false;
				gameObject2.AddComponent<AudioLowPassFilter>().enabled = false;
				gameObject2.SetActive(value: false);
				lowpassSourcePool.Push(audioSource2);
			}
			ambienceStack = new List<SECTR_AudioAmbience>(32);
			activeInstances = new List<Instance>(MaxInstances);
			maxInstancesTable = new Dictionary<SECTR_AudioCue, int>(MaxInstances / 8);
			proximityTable = new Dictionary<SECTR_AudioCue, List<Instance>>(MaxInstances / 8);
			_UpdateTime();
			cachedMember = GetComponent<SECTR_Member>();
			windowHDRMax = HDRBaseLoudness;
			windowHDRMin = windowHDRMax - HDRWindowSize;
			occlusionPath = new List<SECTR_Graph.Node>(32);
			if (MasterBus != null)
			{
				MasterBus.ResetUserVolume();
				_UpdateBusPitchVolume(MasterBus, 1f, 1f);
			}
			else
			{
				Debug.LogWarning("SECTR AudioSystem has no MasterBus. Game sounds will not play.");
			}
		}
	}

	private void OnDisable()
	{
		if (audioSystem == this)
		{
			int count = activeInstances.Count;
			for (int i = 0; i < count; i++)
			{
				activeInstances[i].Stop(stopImmediately: true);
			}
			if ((bool)sourcePoolParent)
			{
				UnityEngine.Object.Destroy(sourcePoolParent.gameObject);
				sourcePoolParent = null;
			}
			audioSystem = null;
			activeInstances = null;
			maxInstancesTable = null;
			proximityTable = null;
			instancePool = null;
			simpleSourcePool = null;
			lowpassSourcePool = null;
			prefabSourcePool = null;
			currentTime = 0f;
			ambienceStack = null;
			currentAmbience = null;
			nextAmbienceOneShotTime = 0f;
			currentMusic = null;
			cachedMember = null;
			occlusionPath = null;
		}
	}

	private void LateUpdate()
	{
		if (!(audioSystem == this) || AudioListener.pause || !MasterBus)
		{
			return;
		}
		float num = _UpdateTime();
		_UpdateBusPitchVolume(MasterBus, 1f, 1f);
		_UpdateAmbience();
		windowHDRMax = Mathf.Max(HDRBaseLoudness, windowHDRMax - HDRDecay * num);
		windowHDRMin = windowHDRMax - HDRWindowSize;
		currentLoudness = 0f;
		int num2 = activeInstances.Count;
		int num3 = 0;
		while (num3 < num2)
		{
			Instance instance = activeInstances[num3];
			instance.Update(num, volumeOnly: false);
			if (!instance.Active && !instance.FadingOut)
			{
				if (instance.KeepAliveTimeStamp == 0.0)
				{
					instance.KeepAliveTimeStamp = GetUnixTimeStamp();
				}
				if (GetUnixTimeStamp() >= instance.KeepAliveTimeStamp + (double)instance.Cue.KeepAliveTime)
				{
					instance.Uninit();
					activeInstances.RemoveAt(num3);
					instancePool.Push(instance);
					num2--;
				}
				else
				{
					num3++;
				}
			}
			else
			{
				num3++;
			}
		}
		currentLoudness = 10f * Mathf.Log10(currentLoudness);
		windowHDRMax = Mathf.Max(currentLoudness, windowHDRMax);
	}

	private static bool _CheckInstances(SECTR_AudioCue audioCue, bool isPlaying)
	{
		int num = audioCue.SourceCue.MaxInstances;
		if (isPlaying)
		{
			num++;
		}
		if (num > 0 && maxInstancesTable.TryGetValue(audioCue, out var value) && value >= num)
		{
			return false;
		}
		return true;
	}

	private static bool _CheckProximity(SECTR_AudioCue audioCue, Transform parent, Vector3 position, Instance testInstance)
	{
		if ((bool)parent)
		{
			position = parent.localToWorldMatrix.MultiplyPoint3x4(position);
		}
		SECTR_AudioCue sourceCue = audioCue.SourceCue;
		float num = sourceCue.MaxDistance + audioSystem.CullingBuffer;
		if (Vector3.SqrMagnitude(position - Listener.position) <= num * num)
		{
			int proximityLimit = sourceCue.ProximityLimit;
			if (proximityLimit > 0 && proximityTable.TryGetValue(audioCue, out var value))
			{
				int count = value.Count;
				if (count > proximityLimit)
				{
					float num2 = sourceCue.MaxDistance + sourceCue.MaxDistance;
					int num3 = 0;
					for (int i = 0; i < count; i++)
					{
						Instance instance = value[i];
						if (instance != testInstance && Vector3.SqrMagnitude(position - instance.Position) < num2 && ++num3 >= proximityLimit)
						{
							return false;
						}
					}
				}
			}
			return true;
		}
		return false;
	}

	private static double GetUnixTimeStamp()
	{
		return DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
	}

	private static float _UpdateTime()
	{
		float num = (float)AudioSettings.dspTime;
		float result = num - currentTime;
		currentTime = num;
		return result;
	}

	private static void _UpdateBusPitchVolume(SECTR_AudioBus bus, float effectiveVolume, float effectivePitch)
	{
		if ((bool)bus)
		{
			bus.EffectiveVolume = effectiveVolume;
			bus.EffectivePitch = effectivePitch;
			int count = bus.Children.Count;
			for (int i = 0; i < count; i++)
			{
				_UpdateBusPitchVolume(bus.Children[i], bus.EffectiveVolume, bus.EffectivePitch);
			}
		}
	}

	private static void _UpdateAmbience()
	{
		SECTR_AudioAmbience sECTR_AudioAmbience = ((ambienceStack.Count > 0) ? ambienceStack[ambienceStack.Count - 1] : audioSystem.DefaultAmbience);
		if (sECTR_AudioAmbience != currentAmbience)
		{
			ambienceLoop.Stop(stopImmediately: false);
			ambienceOneShot.Stop(stopImmediately: false);
			currentAmbience = sECTR_AudioAmbience;
			if (currentAmbience != null)
			{
				if (currentAmbience.OneShots.Count > 0)
				{
					nextAmbienceOneShotTime = currentTime + UnityEngine.Random.Range(currentAmbience.OneShotInterval.x, currentAmbience.OneShotInterval.y);
				}
				if ((bool)currentAmbience.BackgroundLoop)
				{
					if (currentAmbience.BackgroundLoop.Spatialization == SECTR_AudioCue.Spatializations.Infinite3D)
					{
						ambienceLoop = Play(currentAmbience.BackgroundLoop, Listener, UnityEngine.Random.onUnitSphere, loop: true);
					}
					else
					{
						ambienceLoop = Play(currentAmbience.BackgroundLoop, Listener, Vector3.zero, loop: true);
					}
				}
			}
		}
		if (currentAmbience == null)
		{
			return;
		}
		int count = currentAmbience.OneShots.Count;
		if (count > 0 && currentTime >= nextAmbienceOneShotTime)
		{
			SECTR_AudioCue sECTR_AudioCue = null;
			if (currentAmbience.UseOneShotCuesProbability)
			{
				if (currentAmbience.TotalProbability == 0f)
				{
					for (int i = 0; i < count; i++)
					{
						SECTR_AudioCue sECTR_AudioCue2 = currentAmbience.OneShots[i];
						if ((bool)sECTR_AudioCue2)
						{
							currentAmbience.TotalProbability += sECTR_AudioCue2.PlayProbability;
						}
					}
				}
				float num = UnityEngine.Random.value * currentAmbience.TotalProbability;
				float num2 = 0f;
				for (int j = 0; j < count; j++)
				{
					SECTR_AudioCue sECTR_AudioCue3 = currentAmbience.OneShots[j];
					if ((bool)sECTR_AudioCue3)
					{
						num2 += sECTR_AudioCue3.PlayProbability;
						if (num2 > num)
						{
							sECTR_AudioCue = sECTR_AudioCue3;
							break;
						}
					}
				}
			}
			else
			{
				sECTR_AudioCue = currentAmbience.OneShots[UnityEngine.Random.Range(0, count)];
			}
			if (sECTR_AudioCue != null)
			{
				if (sECTR_AudioCue.SourceCue.Loops)
				{
					Debug.LogWarning("Cannot play ambient one shot " + sECTR_AudioCue.name + ". It is set to loop.");
				}
				else
				{
					if (!sECTR_AudioCue.IsLocal)
					{
						Debug.LogWarning("Ambient one shot " + sECTR_AudioCue.name + "should be 2D or Infinite 3D.");
					}
					ambienceOneShot = Play(sECTR_AudioCue, Listener, UnityEngine.Random.onUnitSphere, loop: false);
				}
			}
			nextAmbienceOneShotTime = currentTime + UnityEngine.Random.Range(currentAmbience.OneShotInterval.x, currentAmbience.OneShotInterval.y);
		}
		if ((bool)ambienceLoop)
		{
			ambienceLoop.Volume = currentAmbience.Volume;
		}
		if ((bool)ambienceOneShot)
		{
			ambienceOneShot.Volume = currentAmbience.Volume;
		}
	}

	private static SECTR_AudioBus _FindBus(SECTR_AudioBus bus, string busName)
	{
		if ((bool)bus)
		{
			if (bus.name == busName)
			{
				return bus;
			}
			int count = bus.Children.Count;
			for (int i = 0; i < count; i++)
			{
				SECTR_AudioBus sECTR_AudioBus = _FindBus(bus.Children[i], busName);
				if ((bool)sECTR_AudioBus)
				{
					return sECTR_AudioBus;
				}
			}
		}
		return null;
	}
}
