using UnityEngine;
using UnityEngine.Audio;

public class SettingsLoader : MonoBehaviour
{
	[SerializeField]
	private AudioMixer audioMixer;

	private void Start()
	{
		if (audioMixer != null)
		{
			OptionsMenu.ApplyAudioSettings(audioMixer);
		}
	}
}
