using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
[ExecuteInEditMode]
[AddComponentMenu("")]
public class SECTR_ComputeRMS : MonoBehaviour
{
	private struct BakeInfo
	{
		public SECTR_AudioCue cue;

		public SECTR_AudioCue.ClipData clipData;

		public BakeInfo(SECTR_AudioCue cue, SECTR_AudioCue.ClipData clipData)
		{
			this.cue = cue;
			this.clipData = clipData;
		}
	}

	private List<BakeInfo> hdrBakeList;

	private List<SECTR_ComputeRMS> activeBakeList = new List<SECTR_ComputeRMS>();

	private int hdrBakeIndex;

	private SECTR_AudioCue cue;

	private SECTR_AudioCue.ClipData clipData;

	private List<float> samples = new List<float>();

	private int numChannels;

	public float Progress
	{
		get
		{
			if (hdrBakeList != null)
			{
				int count = hdrBakeList.Count;
				int count2 = activeBakeList.Count;
				float a = (float)(hdrBakeIndex - count2) / (float)count;
				float b = (float)hdrBakeIndex / (float)count;
				float num = 1f;
				for (int i = 0; i < count2; i++)
				{
					SECTR_ComputeRMS sECTR_ComputeRMS = activeBakeList[i];
					if ((bool)sECTR_ComputeRMS)
					{
						num = Mathf.Min(num, sECTR_ComputeRMS.Progress);
					}
				}
				return Mathf.Lerp(a, b, num);
			}
			AudioSource component = GetComponent<AudioSource>();
			if ((bool)component)
			{
				return component.time / component.clip.length;
			}
			return 1f;
		}
	}

	private void OnEnable()
	{
	}

	private void OnDisable()
	{
	}

	private void Update()
	{
		bool flag = false;
		if (hdrBakeList != null)
		{
			int count = hdrBakeList.Count;
			flag = count == 0;
			if (!flag)
			{
				if (activeBakeList.Count == 0)
				{
					if (hdrBakeIndex == count)
					{
						flag = true;
					}
					else
					{
						int num = Mathf.Min(hdrBakeIndex + 4, count);
						for (int i = hdrBakeIndex; i < num; i++)
						{
							BakeInfo bakeInfo = hdrBakeList[i];
							GameObject obj = new GameObject("Bake " + bakeInfo.cue.name + bakeInfo.clipData.Clip.name);
							obj.transform.parent = base.transform;
							obj.transform.localPosition = Vector3.zero;
							obj.hideFlags = HideFlags.HideAndDontSave;
							SECTR_ComputeRMS sECTR_ComputeRMS = obj.AddComponent<SECTR_ComputeRMS>();
							sECTR_ComputeRMS._StartCompute(bakeInfo.cue, bakeInfo.clipData);
							activeBakeList.Add(sECTR_ComputeRMS);
						}
						hdrBakeIndex = num;
					}
				}
				else
				{
					bool flag2 = true;
					int count2 = activeBakeList.Count;
					for (int j = 0; j < count2; j++)
					{
						if (activeBakeList[j] != null)
						{
							flag2 = false;
							break;
						}
					}
					if (flag2)
					{
						activeBakeList.Clear();
					}
				}
			}
		}
		else
		{
			int count3 = samples.Count;
			flag = clipData == null;
			if (!flag && count3 > 0)
			{
				int num2 = AudioSettings.outputSampleRate * numChannels;
				int num3 = (int)(clipData.Clip.length * (float)num2);
				int num4 = num2 / 10;
				AudioSource component = GetComponent<AudioSource>();
				if ((!component.isPlaying && count3 >= num3 - num4) || (component.isPlaying && count3 >= num3))
				{
					int num5 = Mathf.CeilToInt((float)count3 / (float)num2) + 1;
					float[] array = new float[num5];
					int num6 = 0;
					for (int k = 1; k < num5; k++)
					{
						float num7 = 0f;
						int num8 = 0;
						for (int l = 0; l < num2; l++)
						{
							if (num6 >= count3)
							{
								break;
							}
							num8++;
							float num9 = samples[num6++];
							num7 += num9 * num9;
						}
						num7 = Mathf.Sqrt(num7 / (float)num8);
						if (Mathf.Abs(num7) < 0.001f && k == num5 - 1 && num5 > 2)
						{
							array[k] = array[k - 1];
						}
						else
						{
							array[k] = Mathf.Clamp(20f * Mathf.Log10(num7), -160f, 160f);
						}
					}
					if (cue.SourceCue.Loops)
					{
						array[0] = array[^1];
					}
					else
					{
						array[0] = array[1];
					}
					bool flag3 = false;
					for (int m = 0; m < num5; m++)
					{
						if (array[m] > -160f)
						{
							flag3 = false;
							break;
						}
					}
					flag = true;
				}
				else if (!component.isPlaying)
				{
					flag = true;
				}
			}
		}
		if (flag)
		{
			Object.Destroy(base.gameObject);
		}
	}

	private void OnAudioFilterRead(float[] samples, int numChannels)
	{
		this.numChannels = numChannels;
		this.samples.AddRange(samples);
	}

	public void _StartCompute(SECTR_AudioCue cue, SECTR_AudioCue.ClipData clipData)
	{
		this.cue = cue;
		this.clipData = clipData;
		AudioSource component = GetComponent<AudioSource>();
		component.clip = clipData.Clip;
		component.dopplerLevel = 0f;
		component.ignoreListenerPause = true;
		component.ignoreListenerVolume = true;
		component.bypassListenerEffects = true;
		component.bypassReverbZones = true;
		component.maxDistance = float.MaxValue;
		component.minDistance = float.MaxValue;
		component.rolloffMode = AudioRolloffMode.Linear;
		component.playOnAwake = false;
		component.loop = false;
		component.volume = 1f;
		samples.Clear();
		GetComponent<AudioSource>().Play();
	}
}
