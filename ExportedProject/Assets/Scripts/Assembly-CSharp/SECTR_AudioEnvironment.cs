using UnityEngine;

public abstract class SECTR_AudioEnvironment : MonoBehaviour
{
	private bool ambienceActive;

	[SECTR_ToolTip("The configuraiton of the ambient audio in this Reverb Zone.")]
	public SECTR_AudioAmbience Ambience = new SECTR_AudioAmbience();

	public bool Active => ambienceActive;

	private void OnDisable()
	{
		Deactivate();
	}

	protected void Activate()
	{
		if (!ambienceActive && base.enabled)
		{
			SECTR_AudioSystem.PushAmbience(Ambience);
			ambienceActive = true;
		}
	}

	protected void Deactivate()
	{
		if (ambienceActive)
		{
			SECTR_AudioSystem.RemoveAmbience(Ambience);
			ambienceActive = false;
		}
	}
}
