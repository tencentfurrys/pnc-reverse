using UnityEngine;

[AddComponentMenu("Procedural Worlds/SECTR/Audio/SECTR Audio Environment Trigger")]
public class SECTR_AudioEnvironmentTrigger : SECTR_AudioEnvironment
{
	private GameObject activator;

	private void OnEnable()
	{
		if ((bool)activator)
		{
			Activate();
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (activator == null)
		{
			Activate();
			activator = other.gameObject;
		}
	}

	private void OnTriggerEnter2D(Collider2D other)
	{
		if (activator == null)
		{
			Activate();
			activator = other.gameObject;
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (activator == other.gameObject)
		{
			Deactivate();
			activator = null;
		}
	}

	private void OnTriggerExit2D(Collider2D other)
	{
		if (activator == other.gameObject)
		{
			Deactivate();
			activator = null;
		}
	}
}
