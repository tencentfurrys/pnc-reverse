using UnityEngine;

[ExecuteInEditMode]
[AddComponentMenu("Procedural Worlds/SECTR/Audio/SECTR Point Source")]
public class SECTR_PointSource : SECTR_AudioSource
{
	protected SECTR_AudioCueInstance instance;

	public override bool IsPlaying => instance;

	public override void Play()
	{
		if (Loop && IsPlaying)
		{
			instance.Stop(stopImmediately: false);
		}
		if (Cue != null)
		{
			if (Cue.Spatialization == SECTR_AudioCue.Spatializations.Infinite3D)
			{
				instance = SECTR_AudioSystem.Play(Cue, SECTR_AudioSystem.Listener, Random.onUnitSphere, Loop);
			}
			else
			{
				instance = SECTR_AudioSystem.Play(Cue, base.transform, Vector3.zero, Loop);
			}
			if ((bool)instance)
			{
				instance.Volume = volume;
				instance.Pitch = pitch;
			}
		}
	}

	public override void Stop(bool stopImmediately)
	{
		instance.Stop(stopImmediately);
	}

	protected override void OnVolumePitchChanged()
	{
		if ((bool)instance)
		{
			instance.Volume = volume;
			instance.Pitch = pitch;
		}
	}
}
