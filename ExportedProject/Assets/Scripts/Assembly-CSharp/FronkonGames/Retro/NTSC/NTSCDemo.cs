using System;
using UnityEngine;

namespace FronkonGames.Retro.NTSC
{
	public class NTSCDemo : MonoBehaviour
	{
		private NTSC.Settings settings;

		private GUIStyle styleTitle;

		private GUIStyle styleLabel;

		private GUIStyle styleButton;

		private void ResetEffect()
		{
			settings.ResetDefaultValues();
		}

		private void Awake()
		{
			if (!NTSC.IsInRenderFeatures())
			{
				Debug.LogWarning("Effect 'NTSC' not found. You must add it as a Render Feature.");
			}
			base.enabled = NTSC.IsInRenderFeatures();
		}

		private void Start()
		{
			settings = NTSC.Instance.settings;
			ResetEffect();
		}

		private void OnGUI()
		{
			styleTitle = new GUIStyle(GUI.skin.label)
			{
				alignment = TextAnchor.LowerCenter,
				fontSize = 32,
				fontStyle = FontStyle.Bold
			};
			styleLabel = new GUIStyle(GUI.skin.label)
			{
				alignment = TextAnchor.UpperLeft,
				fontSize = 24
			};
			styleButton = new GUIStyle(GUI.skin.button)
			{
				fontSize = 24
			};
			GUILayout.BeginHorizontal();
			GUILayout.BeginVertical("box", GUILayout.Width(600f), GUILayout.Height(Screen.height));
			GUILayout.Space(10f);
			GUILayout.Label("NTSC".ToUpper(), styleTitle);
			settings.intensity = SliderField("Intensity", settings.intensity);
			GUILayout.Space(10f);
			settings.ntscWindowRadius = SliderField("Window Radius", settings.ntscWindowRadius, 1, 40);
			settings.amCarrierSignalWavelength = SliderField("AM Carrier Signal", settings.amCarrierSignalWavelength, 0.5f, 10f);
			settings.yLowPassWavelength = SliderField("Y Low Pass", settings.yLowPassWavelength, 0.5f, 10f);
			settings.iLowPassWavelength = SliderField("I Low Pass", settings.iLowPassWavelength, 1f, 20f);
			settings.qLowPassWavelength = SliderField("Q Low Pass", settings.qLowPassWavelength, 1f, 20f);
			settings.colorburstWavelengthEncoder = SliderField("Colorburst Encoder", settings.colorburstWavelengthEncoder, 1f, 10f);
			settings.ntscScale = SliderField("NTSC Scale", settings.ntscScale, 0.1f, 5f);
			settings.ntscPhaseAlternation = SliderField("Phase Alternation", settings.ntscPhaseAlternation, 0f, MathF.PI);
			settings.ntscNoiseStrength = SliderField("Noise Strength", settings.ntscNoiseStrength);
			settings.ntscWindowBias = SliderField("Window Bias", settings.ntscWindowBias, -1f);
			settings.amDemodulateWavelength = SliderField("AM Demodulate", settings.amDemodulateWavelength, 0.5f, 10f);
			settings.amDecodeHighPassWavelength = SliderField("AM High Pass", settings.amDecodeHighPassWavelength, 0.5f, 10f);
			settings.colorburstWavelengthDecoder = SliderField("Colorburst Decoder", settings.colorburstWavelengthDecoder, 1f, 10f);
			settings.decodeLowPassWavelength = SliderField("Decode Low Pass", settings.decodeLowPassWavelength, 1f, 10f);
			GUILayout.FlexibleSpace();
			if (GUILayout.Button("RESET", styleButton))
			{
				ResetEffect();
			}
			GUILayout.Space(4f);
			if (GUILayout.Button("ONLINE DOCUMENTATION", styleButton))
			{
				Application.OpenURL("https://fronkongames.github.io/store/retro/");
			}
			GUILayout.Space(4f);
			if (GUILayout.Button("❤\ufe0f LEAVE A REVIEW ❤\ufe0f", styleButton))
			{
				Application.OpenURL("https://assetstore.unity.com/packages/slug/321946");
			}
			GUILayout.Space(20f);
			GUILayout.EndVertical();
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
		}

		private void OnDestroy()
		{
			settings?.ResetDefaultValues();
		}

		private bool ToggleField(string label, bool value)
		{
			GUILayout.BeginHorizontal();
			GUILayout.Label(label, styleLabel);
			value = GUILayout.Toggle(value, string.Empty);
			GUILayout.EndHorizontal();
			return value;
		}

		private float SliderField(string label, float value, float min = 0f, float max = 1f)
		{
			GUILayout.BeginHorizontal();
			GUILayout.Label(label, styleLabel);
			value = GUILayout.HorizontalSlider(value, min, max);
			GUILayout.EndHorizontal();
			return value;
		}

		private int SliderField(string label, int value, int min, int max = 1)
		{
			GUILayout.BeginHorizontal();
			GUILayout.Label(label, styleLabel);
			value = (int)GUILayout.HorizontalSlider(value, min, max);
			GUILayout.EndHorizontal();
			return value;
		}

		private Color ColorField(string label, Color value, bool alpha = true)
		{
			GUILayout.BeginHorizontal();
			GUILayout.Label(label, styleLabel);
			float a = value.a;
			Color.RGBToHSV(value, out var H, out var S, out var V);
			H = GUILayout.HorizontalSlider(H, 0f, 1f);
			value = Color.HSVToRGB(H, S, V);
			if (!alpha)
			{
				value.a = a;
			}
			GUILayout.EndHorizontal();
			return value;
		}

		private Vector2 Vector2Field(string label, Vector2 value, float min, float max)
		{
			return Vector2Field(label, value, "X", "Y", min, max);
		}

		private Vector2 Vector2Field(string label, Vector2 value, string x = "X", string y = "Y", float min = 0f, float max = 1f)
		{
			GUILayout.Label(label, styleLabel);
			value.x = SliderField("   " + x, value.x, min, max);
			value.y = SliderField("   " + y, value.y, min, max);
			return value;
		}

		private Vector3 Vector3Field(string label, Vector3 value, float min, float max)
		{
			return Vector3Field(label, value, "X", "Y", "Z", min, max);
		}

		private Vector3 Vector3Field(string label, Vector3 value, string x = "X", string y = "Y", string z = "Z", float min = 0f, float max = 1f)
		{
			GUILayout.Label(label, styleLabel);
			value.x = SliderField("   " + x, value.x, min, max);
			value.y = SliderField("   " + y, value.y, min, max);
			value.z = SliderField("   " + z, value.z, min, max);
			return value;
		}

		private Vector4 Vector4Field(string label, Vector4 value, float min, float max)
		{
			return Vector4Field(label, value, "X", "Y", "Z", "W", min, max);
		}

		private Vector4 Vector4Field(string label, Vector4 value, string x = "X", string y = "Y", string z = "Z", string w = "W", float min = 0f, float max = 1f)
		{
			GUILayout.Label(label, styleLabel);
			value.x = SliderField("   " + x, value.x, min, max);
			value.y = SliderField("   " + y, value.y, min, max);
			value.z = SliderField("   " + z, value.z, min, max);
			value.w = SliderField("   " + w, value.w, min, max);
			return value;
		}

		private T EnumField<T>(string label, T value) where T : Enum
		{
			string[] names = Enum.GetNames(typeof(T));
			Array values = Enum.GetValues(typeof(T));
			int num = Array.IndexOf(values, value);
			GUILayout.BeginHorizontal();
			GUILayout.Label(label, styleLabel);
			if (GUILayout.Button("<", styleButton))
			{
				num = ((num > 0) ? (num - 1) : (values.Length - 1));
			}
			GUILayout.Label(names[num], styleLabel);
			if (GUILayout.Button(">", styleButton))
			{
				num = ((num < values.Length - 1) ? (num + 1) : 0);
			}
			GUILayout.EndHorizontal();
			return (T)(object)num;
		}
	}
}
