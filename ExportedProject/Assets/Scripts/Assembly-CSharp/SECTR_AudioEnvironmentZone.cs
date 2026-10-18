using UnityEngine;

[RequireComponent(typeof(AudioReverbZone))]
[AddComponentMenu("Procedural Worlds/SECTR/Audio/SECTR Audio Environment Zone")]
public class SECTR_AudioEnvironmentZone : SECTR_AudioEnvironment
{
	private AudioReverbZone cachedZone;

	private void OnEnable()
	{
		cachedZone = GetComponent<AudioReverbZone>();
	}

	private void OnDisable()
	{
		cachedZone = null;
		Deactivate();
	}

	private void Update()
	{
		if (!SECTR_AudioSystem.Initialized)
		{
			return;
		}
		bool flag = Vector3.SqrMagnitude(SECTR_AudioSystem.Listener.position - base.transform.position) <= cachedZone.maxDistance * cachedZone.maxDistance;
		if (flag != base.Active)
		{
			if (flag)
			{
				Activate();
			}
			else
			{
				Deactivate();
			}
		}
	}
}
