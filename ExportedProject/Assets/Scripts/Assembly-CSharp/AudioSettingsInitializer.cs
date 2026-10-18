using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;

public class AudioSettingsInitializer : MonoBehaviour
{
	[SerializeField]
	private AudioMixer audioMixer;

	[SerializeField]
	private Volume globalVolume;

	private void Awake()
	{
		OptionsMenu.ApplyAudioSettings(audioMixer);
		OptionsMenu.ApplyBrightnessSettings(globalVolume);
	}
}
