using UnityEngine;

[ExecuteInEditMode]
[AddComponentMenu("Procedural Worlds/SECTR/Audio/SECTR Music Trigger")]
public class SECTR_MusicTrigger : MonoBehaviour
{
	private GameObject activator;

	[SECTR_ToolTip("The Cue to play as music. If null, this trigger will stop the current music.", null, false)]
	public SECTR_AudioCue Cue;

	[SECTR_ToolTip("Should music be forced to loop when playing.")]
	public bool Loop = true;

	[SECTR_ToolTip("Should the music stop when leaving the trigger.")]
	public bool StopOnExit;

	private void OnEnable()
	{
		if ((bool)activator)
		{
			_Play();
		}
	}

	private void OnDisable()
	{
		if (StopOnExit)
		{
			_Stop(stopImmediately: false);
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (activator == null)
		{
			if (Cue != null)
			{
				_Play();
			}
			else
			{
				_Stop(stopImmediately: false);
			}
			activator = other.gameObject;
		}
	}

	private void OnTriggerEnter2D(Collider2D other)
	{
		if (activator == null)
		{
			if (Cue != null)
			{
				_Play();
			}
			else
			{
				_Stop(stopImmediately: false);
			}
			activator = other.gameObject;
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (StopOnExit && other == activator)
		{
			_Stop(stopImmediately: false);
			activator = null;
		}
	}

	private void OnTriggerExit2D(Collider2D other)
	{
		if (StopOnExit && other.gameObject == activator)
		{
			_Stop(stopImmediately: false);
			activator = null;
		}
	}

	private void _Play()
	{
		if (Cue != null)
		{
			SECTR_AudioSystem.PlayMusic(Cue);
		}
	}

	private void _Stop(bool stopImmediately)
	{
		SECTR_AudioSystem.StopMusic(stopImmediately);
	}
}
