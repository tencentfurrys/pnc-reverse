using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class OptionsMenu : MonoBehaviour
{
	[Header("Controller Navigation")]
	[SerializeField]
	private GameObject firstSelected;

	[Header("Audio")]
	[SerializeField]
	private AudioMixer audioMixer;

	[SerializeField]
	private Slider masterVolumeSlider;

	[SerializeField]
	private Slider sfxVolumeSlider;

	[SerializeField]
	private Slider ambienceVolumeSlider;

	[SerializeField]
	private Slider musicVolumeSlider;

	[Header("Display")]
	[SerializeField]
	private TMP_Dropdown resolutionDropdown;

	[SerializeField]
	private TMP_Dropdown fullscreenDropdown;

	[SerializeField]
	private Toggle vSyncToggle;

	[SerializeField]
	private Button frameRateLeftButton;

	[SerializeField]
	private Button frameRateRightButton;

	[SerializeField]
	private TMP_Text frameRateLabel;

	[SerializeField]
	private Slider brightnessSlider;

	[SerializeField]
	private Volume globalVolume;

	private Resolution[] filteredResolutions;

	private int frameRateIndex;

	private static readonly int[] FrameRateOptions = new int[8] { -1, 30, 60, 75, 120, 144, 165, 240 };

	private static readonly string[] FrameRateLabels = new string[8] { "Unlimited", "30", "60", "75", "120", "144", "165", "240" };

	public GameObject FirstSelected => firstSelected;

	private void Start()
	{
		SetupAudioSliders();
		SetupBrightnessSlider();
		SetupFullscreenDropdown();
		SetupResolutionDropdown();
		SetupVSyncToggle();
		SetupFrameRateLimitArrows();
	}

	private void OnDestroy()
	{
		if (masterVolumeSlider != null)
		{
			masterVolumeSlider.onValueChanged.RemoveListener(SetMasterVolume);
		}
		if (sfxVolumeSlider != null)
		{
			sfxVolumeSlider.onValueChanged.RemoveListener(SetSFXVolume);
		}
		if (ambienceVolumeSlider != null)
		{
			ambienceVolumeSlider.onValueChanged.RemoveListener(SetAmbienceVolume);
		}
		if (musicVolumeSlider != null)
		{
			musicVolumeSlider.onValueChanged.RemoveListener(SetMusicVolume);
		}
		if (resolutionDropdown != null)
		{
			resolutionDropdown.onValueChanged.RemoveListener(SetResolution);
		}
		if (fullscreenDropdown != null)
		{
			fullscreenDropdown.onValueChanged.RemoveListener(SetFullscreen);
		}
		if (brightnessSlider != null)
		{
			brightnessSlider.onValueChanged.RemoveListener(SetBrightness);
		}
		if (vSyncToggle != null)
		{
			vSyncToggle.onValueChanged.RemoveListener(SetVSync);
		}
		if (frameRateLeftButton != null)
		{
			frameRateLeftButton.onClick.RemoveListener(FrameRatePrev);
		}
		if (frameRateRightButton != null)
		{
			frameRateRightButton.onClick.RemoveListener(FrameRateNext);
		}
	}

	private void SetupAudioSliders()
	{
		float value = PlayerPrefs.GetFloat("MasterVolume", 1f);
		float value2 = PlayerPrefs.GetFloat("SFXVolume", 1f);
		float value3 = PlayerPrefs.GetFloat("AmbienceVolume", 1f);
		float value4 = PlayerPrefs.GetFloat("MusicVolume", 1f);
		if (masterVolumeSlider != null)
		{
			masterVolumeSlider.minValue = 0.0001f;
			masterVolumeSlider.maxValue = 1f;
			masterVolumeSlider.value = value;
			masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
		}
		if (sfxVolumeSlider != null)
		{
			sfxVolumeSlider.minValue = 0.0001f;
			sfxVolumeSlider.maxValue = 1f;
			sfxVolumeSlider.value = value2;
			sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);
		}
		if (ambienceVolumeSlider != null)
		{
			ambienceVolumeSlider.minValue = 0.0001f;
			ambienceVolumeSlider.maxValue = 1f;
			ambienceVolumeSlider.value = value3;
			ambienceVolumeSlider.onValueChanged.AddListener(SetAmbienceVolume);
		}
		if (musicVolumeSlider != null)
		{
			musicVolumeSlider.minValue = 0.0001f;
			musicVolumeSlider.maxValue = 1f;
			musicVolumeSlider.value = value4;
			musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
		}
		ApplyAudioSettings(audioMixer);
	}

	private void SetMasterVolume(float value)
	{
		audioMixer.SetFloat("MasterVolume", LinearToDecibel(value));
		PlayerPrefs.SetFloat("MasterVolume", value);
	}

	private void SetSFXVolume(float value)
	{
		audioMixer.SetFloat("SFXVolume", LinearToDecibel(value));
		PlayerPrefs.SetFloat("SFXVolume", value);
	}

	private void SetAmbienceVolume(float value)
	{
		audioMixer.SetFloat("AmbienceVolume", LinearToDecibel(value));
		PlayerPrefs.SetFloat("AmbienceVolume", value);
	}

	private void SetMusicVolume(float value)
	{
		audioMixer.SetFloat("MusicVolume", LinearToDecibel(value));
		PlayerPrefs.SetFloat("MusicVolume", value);
	}

	private static float LinearToDecibel(float linear)
	{
		return Mathf.Log10(Mathf.Clamp(linear, 0.0001f, 1f)) * 20f;
	}

	public static void ApplyAudioSettings(AudioMixer mixer)
	{
		if (!(mixer == null))
		{
			float linear = PlayerPrefs.GetFloat("MasterVolume", 1f);
			float linear2 = PlayerPrefs.GetFloat("SFXVolume", 1f);
			float linear3 = PlayerPrefs.GetFloat("AmbienceVolume", 1f);
			float linear4 = PlayerPrefs.GetFloat("MusicVolume", 1f);
			mixer.SetFloat("MasterVolume", LinearToDecibel(linear));
			mixer.SetFloat("SFXVolume", LinearToDecibel(linear2));
			mixer.SetFloat("AmbienceVolume", LinearToDecibel(linear3));
			mixer.SetFloat("MusicVolume", LinearToDecibel(linear4));
		}
	}

	private void SetupBrightnessSlider()
	{
		float value = PlayerPrefs.GetFloat("Brightness", 1f);
		if (brightnessSlider != null)
		{
			brightnessSlider.minValue = 1f;
			brightnessSlider.maxValue = 1.5f;
			brightnessSlider.value = value;
			brightnessSlider.onValueChanged.AddListener(SetBrightness);
		}
		ApplyBrightness(value);
	}

	private void SetBrightness(float value)
	{
		PlayerPrefs.SetFloat("Brightness", value);
		ApplyBrightness(value);
	}

	private void ApplyBrightness(float value)
	{
		if (!(globalVolume == null) && globalVolume.profile.TryGet<LiftGammaGain>(out var component))
		{
			component.gamma.Override(new Vector4(1f, 1f, 1f, value - 1f));
		}
	}

	public static void ApplyBrightnessSettings(Volume volume)
	{
		if (!(volume == null))
		{
			float num = PlayerPrefs.GetFloat("Brightness", 1f);
			if (volume.profile.TryGet<LiftGammaGain>(out var component))
			{
				component.gamma.Override(new Vector4(1f, 1f, 1f, num - 1f));
			}
		}
	}

	private void SetupResolutionDropdown()
	{
		if (resolutionDropdown == null)
		{
			return;
		}
		Resolution[] resolutions = Screen.resolutions;
		Dictionary<string, Resolution> dictionary = new Dictionary<string, Resolution>();
		for (int i = 0; i < resolutions.Length; i++)
		{
			if (resolutions[i].width * 9 == resolutions[i].height * 16)
			{
				string key = resolutions[i].width + "x" + resolutions[i].height;
				if (!dictionary.ContainsKey(key) || resolutions[i].refreshRateRatio.value > dictionary[key].refreshRateRatio.value)
				{
					dictionary[key] = resolutions[i];
				}
			}
		}
		filteredResolutions = new Resolution[dictionary.Count];
		dictionary.Values.CopyTo(filteredResolutions, 0);
		Array.Sort(filteredResolutions, (Resolution a, Resolution b) => (b.width * b.height).CompareTo(a.width * a.height));
		resolutionDropdown.ClearOptions();
		List<string> list = new List<string>();
		int num = 0;
		int num2 = PlayerPrefs.GetInt("ResolutionIndex", -1);
		for (int num3 = 0; num3 < filteredResolutions.Length; num3++)
		{
			list.Add(filteredResolutions[num3].width + " x " + filteredResolutions[num3].height);
			if (filteredResolutions[num3].width == Screen.currentResolution.width && filteredResolutions[num3].height == Screen.currentResolution.height)
			{
				num = num3;
			}
		}
		resolutionDropdown.AddOptions(list);
		int value = ((num2 >= 0 && num2 < filteredResolutions.Length) ? num2 : num);
		resolutionDropdown.value = value;
		resolutionDropdown.RefreshShownValue();
		resolutionDropdown.onValueChanged.AddListener(SetResolution);
	}

	private void SetupFullscreenDropdown()
	{
		if (!(fullscreenDropdown == null))
		{
			fullscreenDropdown.ClearOptions();
			fullscreenDropdown.AddOptions(new List<string> { "Fullscreen", "Windowed" });
			int value = PlayerPrefs.GetInt("Fullscreen", (!Screen.fullScreen) ? 1 : 0);
			fullscreenDropdown.value = value;
			fullscreenDropdown.RefreshShownValue();
			fullscreenDropdown.onValueChanged.AddListener(SetFullscreen);
		}
	}

	private void SetResolution(int index)
	{
		if (filteredResolutions != null && index >= 0 && index < filteredResolutions.Length)
		{
			Resolution resolution = filteredResolutions[index];
			Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
			PlayerPrefs.SetInt("ResolutionIndex", index);
		}
	}

	private void SetFullscreen(int index)
	{
		Screen.fullScreen = index == 0;
		PlayerPrefs.SetInt("Fullscreen", index);
	}

	private void SetupVSyncToggle()
	{
		if (!(vSyncToggle == null))
		{
			int num = PlayerPrefs.GetInt("VSync", 1);
			vSyncToggle.isOn = num > 0;
			vSyncToggle.onValueChanged.AddListener(SetVSync);
		}
	}

	private void SetupFrameRateLimitArrows()
	{
		int value = PlayerPrefs.GetInt("FrameRateLimit", 60);
		frameRateIndex = Array.IndexOf(FrameRateOptions, value);
		if (frameRateIndex < 0)
		{
			frameRateIndex = Array.IndexOf(FrameRateOptions, 60);
		}
		UpdateFrameRateLabel();
		if (frameRateLeftButton != null)
		{
			frameRateLeftButton.onClick.AddListener(FrameRatePrev);
		}
		if (frameRateRightButton != null)
		{
			frameRateRightButton.onClick.AddListener(FrameRateNext);
		}
	}

	private void SetVSync(bool isOn)
	{
		PlayerPrefs.SetInt("VSync", isOn ? 1 : 0);
		ApplyFramerateSettings();
	}

	private void FrameRatePrev()
	{
		frameRateIndex = (frameRateIndex - 1 + FrameRateOptions.Length) % FrameRateOptions.Length;
		CommitFrameRate();
	}

	private void FrameRateNext()
	{
		frameRateIndex = (frameRateIndex + 1) % FrameRateOptions.Length;
		CommitFrameRate();
	}

	private void CommitFrameRate()
	{
		PlayerPrefs.SetInt("FrameRateLimit", FrameRateOptions[frameRateIndex]);
		UpdateFrameRateLabel();
		ApplyFramerateSettings();
	}

	private void UpdateFrameRateLabel()
	{
		if (frameRateLabel != null)
		{
			frameRateLabel.text = FrameRateLabels[frameRateIndex];
		}
	}

	public static void ApplyFramerateSettings()
	{
		int vSyncCount = PlayerPrefs.GetInt("VSync", 1);
		int targetFrameRate = PlayerPrefs.GetInt("FrameRateLimit", 60);
		QualitySettings.vSyncCount = vSyncCount;
		Application.targetFrameRate = targetFrameRate;
	}
}
