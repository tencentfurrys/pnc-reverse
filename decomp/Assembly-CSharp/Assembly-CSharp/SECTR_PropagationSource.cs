using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(SECTR_Member))]
[AddComponentMenu("Procedural Worlds/SECTR/Audio/SECTR Propagation Source")]
public class SECTR_PropagationSource : SECTR_AudioSource
{
	private class PathSound
	{
		public SECTR_AudioCueInstance instance;

		public SECTR_Portal firstPortal;

		public SECTR_Portal secondPortal;

		public float firstDistance;

		public float secondDistance;

		public float distance;

		public Vector3 position;

		public Vector3 lastListenerPosition;

		public float weight = 1f;

		public bool occluded;
	}

	private SECTR_Member cachedMember;

	private List<SECTR_Graph.Node> path = new List<SECTR_Graph.Node>(32);

	private List<PathSound> activeSounds = new List<PathSound>(4);

	private float directDistanceToListener;

	private bool playing;

	private bool played;

	[SECTR_ToolTip("When the listener gets within this distance of a portal, the sound direction will start to blend towards the next portal or source position.", 0f, -1f)]
	public float InterpDistance = 2f;

	public override bool IsPlaying
	{
		get
		{
			if (!playing)
			{
				return activeSounds.Count > 0;
			}
			return true;
		}
	}

	public override void Play()
	{
		playing = true;
		played = false;
	}

	public override void Stop(bool stopImmediately)
	{
		int count = activeSounds.Count;
		for (int i = 0; i < count; i++)
		{
			activeSounds[i]?.instance.Stop(stopImmediately);
		}
		activeSounds.Clear();
		playing = false;
		played = false;
	}

	private void OnEnable()
	{
		cachedMember = GetComponent<SECTR_Member>();
	}

	protected override void OnDisable()
	{
		base.OnDisable();
		cachedMember = null;
	}

	private void Update()
	{
		if (!playing || !(Cue != null) || cachedMember.Sectors.Count <= 0 || !SECTR_AudioSystem.Initialized)
		{
			return;
		}
		Vector3 position = SECTR_AudioSystem.Listener.position;
		Vector3 position2 = transform.position;
		directDistanceToListener = Vector3.Distance(position2, position);
		bool flag = Cue.SourceCue.Spatialization == SECTR_AudioCue.Spatializations.Occludable3D;
		int num = activeSounds.Count;
		if (played && !Loop && !Cue.SourceCue.Loops && num == 0)
		{
			Stop(stopImmediately: false);
		}
		else if (directDistanceToListener <= Cue.SourceCue.MaxDistance)
		{
			PathSound pathSound = null;
			SECTR_Graph.FindShortestPath(ref path, position, transform.position, (SECTR_Portal.PortalFlags)0);
			int count = path.Count;
			if (count > 0)
			{
				SECTR_Portal portal = path[0].Portal;
				SECTR_Portal sECTR_Portal = ((count > 1) ? path[1].Portal : null);
				bool flag2 = false;
				for (int i = 0; i < num; i++)
				{
					PathSound pathSound2 = activeSounds[i];
					if (portal == pathSound2.firstPortal || portal == pathSound2.secondPortal || sECTR_Portal == pathSound2.firstPortal)
					{
						pathSound = pathSound2;
						break;
					}
				}
				if (pathSound == null)
				{
					pathSound = new PathSound();
					flag2 = true;
				}
				pathSound.firstPortal = portal;
				pathSound.secondPortal = sECTR_Portal;
				pathSound.occluded = false;
				pathSound.firstDistance = 0f;
				pathSound.secondDistance = 0f;
				pathSound.distance = 0f;
				SECTR_AudioSystem.OcclusionModes occlusionModes = (flag ? SECTR_AudioSystem.AudioSystem.OcclusionFlags : ((SECTR_AudioSystem.OcclusionModes)0));
				bool flag3 = (occlusionModes & SECTR_AudioSystem.OcclusionModes.Graph) != 0;
				if (count == 1 && path[0].Portal == null)
				{
					pathSound.firstDistance = directDistanceToListener;
					pathSound.secondDistance = directDistanceToListener;
				}
				else
				{
					for (int j = 0; j < count; j++)
					{
						SECTR_Portal portal2 = path[j].Portal;
						SECTR_Portal sECTR_Portal2 = ((j < count - 1) ? path[j + 1].Portal : null);
						Vector3 center = portal2.Center;
						switch (j)
						{
						case 0:
							pathSound.firstDistance += Vector3.Distance(center, position);
							break;
						case 1:
							if ((bool)portal2)
							{
								pathSound.secondDistance += Vector3.Distance(center, position);
							}
							break;
						}
						float num2 = 0f;
						num2 = ((!portal2 || !sECTR_Portal2) ? Vector3.Distance(center, position2) : Vector3.Distance(center, sECTR_Portal2.Center));
						pathSound.firstDistance += num2;
						if (j >= 1)
						{
							pathSound.secondDistance += num2;
						}
						if (((bool)portal2 & flag3) && !pathSound.occluded && (portal2.Flags & SECTR_Portal.PortalFlags.Closed) != 0)
						{
							pathSound.occluded = true;
						}
					}
				}
				occlusionModes &= ~SECTR_AudioSystem.OcclusionModes.Graph;
				if (!pathSound.occluded && occlusionModes != 0)
				{
					pathSound.occluded = SECTR_AudioSystem.IsOccluded(position2, occlusionModes);
				}
				_ComputeSoundSpatialization(position, directDistanceToListener, pathSound);
				if (!pathSound.instance)
				{
					if (activeSounds.Count > 0)
					{
						pathSound.instance = SECTR_AudioSystem.Clone(activeSounds[0].instance, pathSound.position);
					}
					else
					{
						pathSound.instance = SECTR_AudioSystem.Play(Cue, pathSound.position, Loop);
					}
					if ((bool)pathSound.instance)
					{
						pathSound.instance.ForceInfinite();
						if (flag2)
						{
							activeSounds.Add(pathSound);
							num++;
						}
					}
					else
					{
						if (!flag2)
						{
							activeSounds.Remove(pathSound);
							num--;
						}
						pathSound = null;
					}
				}
				else
				{
					pathSound.instance.Position = pathSound.position;
				}
				if (pathSound != null)
				{
					pathSound.lastListenerPosition = position;
					played = true;
				}
			}
			int num3 = 0;
			float num4 = 1f;
			for (num3 = 0; num3 < num; num3++)
			{
				PathSound pathSound3 = activeSounds[num3];
				if (pathSound3 != pathSound)
				{
					_ComputeSoundSpatialization(position, directDistanceToListener, pathSound3);
					pathSound3.weight = (pathSound3.instance ? (1f - Mathf.Clamp01(Vector3.Distance(pathSound3.lastListenerPosition, position) * 0.5f)) : 0f);
					num4 -= pathSound3.weight;
				}
			}
			if (pathSound != null)
			{
				pathSound.weight = Mathf.Max(0f, num4);
			}
			num3 = 0;
			float minDistance = Cue.SourceCue.MinDistance;
			float maxDistance = Cue.SourceCue.MaxDistance;
			float num5 = 1f / (maxDistance - minDistance);
			while (num3 < num)
			{
				PathSound pathSound4 = activeSounds[num3];
				if ((bool)pathSound4.instance)
				{
					pathSound4.instance.Position = pathSound4.position;
					float num6 = 1f;
					switch (Cue.SourceCue.Falloff)
					{
					case SECTR_AudioCue.FalloffTypes.Linear:
						num6 = 1f - Mathf.Clamp01((pathSound4.distance - minDistance) * num5);
						break;
					case SECTR_AudioCue.FalloffTypes.Logarithmic:
						num6 = Mathf.Clamp01(1f / Mathf.Max(pathSound4.distance - minDistance - 1f, 0.001f));
						break;
					}
					pathSound4.instance.Volume = num6 * pathSound4.weight * volume;
					pathSound4.instance.Pitch = pitch;
					if (flag)
					{
						pathSound4.instance.ForceOcclusion(pathSound4.occluded);
					}
				}
				if (pathSound4.weight <= 0f || !pathSound4.instance)
				{
					pathSound4.instance.Stop(stopImmediately: true);
					activeSounds.RemoveAt(num3);
					num--;
				}
				else
				{
					num3++;
				}
			}
		}
		else
		{
			for (int k = 0; k < num; k++)
			{
				activeSounds[k]?.instance.Stop(stopImmediately: false);
			}
			activeSounds.Clear();
		}
	}

	protected override void OnVolumePitchChanged()
	{
	}

	private void _ComputeSoundSpatialization(Vector3 listenerPosition, float distanceToListener, PathSound pathSound)
	{
		if (pathSound.firstPortal != null)
		{
			Vector3 center = pathSound.firstPortal.Center;
			float num = pathSound.firstPortal.BoundingBox.SqrDistance(listenerPosition);
			Vector3 position;
			float distance;
			if (num >= InterpDistance * InterpDistance)
			{
				position = center;
				distance = pathSound.firstDistance;
			}
			else
			{
				float t = Mathf.Clamp01(Mathf.Sqrt(num) / InterpDistance);
				position = Vector3.Lerp(pathSound.secondPortal ? pathSound.secondPortal.Center : transform.position, center, t);
				distance = Mathf.Lerp(pathSound.secondPortal ? pathSound.secondDistance : distanceToListener, pathSound.firstDistance, t);
			}
			pathSound.position = position;
			pathSound.distance = distance;
		}
		else
		{
			pathSound.position = transform.position;
			pathSound.distance = distanceToListener;
		}
	}
}
