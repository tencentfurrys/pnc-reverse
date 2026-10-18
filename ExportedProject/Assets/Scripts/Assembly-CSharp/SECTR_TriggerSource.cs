using UnityEngine;

[ExecuteInEditMode]
[AddComponentMenu("Procedural Worlds/SECTR/Audio/SECTR Trigger Source")]
public class SECTR_TriggerSource : SECTR_PointSource
{
	private GameObject activator;

	public SECTR_TriggerSource()
	{
		Loop = false;
		PlayOnStart = false;
	}

	private void OnEnable()
	{
		if (!IsPlaying && (bool)activator)
		{
			Play();
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (activator == null)
		{
			Play();
			activator = other.gameObject;
		}
	}

	private void OnTriggerEnter2D(Collider2D other)
	{
		if (activator == null)
		{
			Play();
			activator = other.gameObject;
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (activator == other.gameObject)
		{
			Stop(stopImmediately: false);
			activator = null;
		}
	}

	private void OnTriggerExit2D(Collider2D other)
	{
		if (activator == other.gameObject)
		{
			Stop(stopImmediately: false);
			activator = null;
		}
	}
}
