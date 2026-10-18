using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

namespace PixelCrushers.GridController.Glyphs
{
	public static class InputGlyphs
	{
		public const string KbmGroup = "Keyboard&Mouse";

		public const string GamepadGroup = "Gamepad";

		private const string DatabaseResourcePath = "InputGlyphs/InputGlyphDatabase";

		private static InputGlyphDatabase database;

		private static bool initialized;

		public static DeviceFamily CurrentGamepadFamily
		{
			get
			{
				Gamepad current = Gamepad.current;
				if (current != null)
				{
					string text = (current.name + " " + current.layout).ToLowerInvariant();
					if (text.Contains("dualshock") || text.Contains("dualsense") || text.Contains("sony") || text.Contains("playstation") || text.Contains("ps4") || text.Contains("ps5"))
					{
						return DeviceFamily.PlayStation;
					}
				}
				return DeviceFamily.Xbox;
			}
		}

		public static DeviceFamily ActiveFamily
		{
			get
			{
				if (!InputDeviceTracker.UsingGamepad)
				{
					return DeviceFamily.KeyboardMouse;
				}
				return CurrentGamepadFamily;
			}
		}

		public static event Action OnActiveDeviceChanged;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void Init()
		{
			EnsureInitialized();
		}

		private static void EnsureInitialized()
		{
			if (!initialized)
			{
				initialized = true;
				database = Resources.Load<InputGlyphDatabase>("InputGlyphs/InputGlyphDatabase");
				if (database == null)
				{
					Debug.LogWarning("[InputGlyphs] No InputGlyphDatabase found at Resources/InputGlyphs/InputGlyphDatabase. Glyphs will fall back to text.");
				}
				InputDeviceTracker.OnDeviceChanged += HandleDeviceChanged;
			}
		}

		private static void HandleDeviceChanged()
		{
			OnActiveDeviceChanged?.Invoke();
		}

		public static TMP_SpriteAsset GetSpriteAsset(DeviceFamily family)
		{
			EnsureInitialized();
			if (!(database != null))
			{
				return null;
			}
			return database.GetSpriteAsset(family);
		}

		private static string FamilyGroup(DeviceFamily family)
		{
			if (family != DeviceFamily.KeyboardMouse)
			{
				return "Gamepad";
			}
			return "Keyboard&Mouse";
		}

		public static string GetBindingMarkup(InputAction action, int bindingIndex, DeviceFamily family, out TMP_SpriteAsset asset)
		{
			asset = null;
			if (action == null)
			{
				return string.Empty;
			}
			EnsureInitialized();
			string bindingDisplayString = action.GetBindingDisplayString(bindingIndex, out var _, out var controlPath, InputBinding.DisplayStringOptions.DontUseShortDisplayNames);
			if (family == DeviceFamily.KeyboardMouse)
			{
				return bindingDisplayString;
			}
			if (database != null && database.TryGetMarkup(family, controlPath, out var markup, out asset))
			{
				return markup;
			}
			return bindingDisplayString;
		}

		public static string GetActionMarkup(InputAction action, out TMP_SpriteAsset asset)
		{
			return GetActionMarkup(action, ActiveFamily, out asset);
		}

		public static string GetActionMarkup(InputAction action, DeviceFamily family, out TMP_SpriteAsset asset)
		{
			asset = null;
			if (action == null)
			{
				return string.Empty;
			}
			int num = FindRepresentativeBinding(action, FamilyGroup(family));
			if (num < 0)
			{
				return string.Empty;
			}
			return GetBindingMarkup(action, num, family, out asset);
		}

		private static int FindRepresentativeBinding(InputAction action, string group)
		{
			ReadOnlyArray<InputBinding> bindings = action.bindings;
			for (int i = 0; i < bindings.Count; i++)
			{
				InputBinding binding = bindings[i];
				if (binding.isComposite)
				{
					for (int j = i + 1; j < bindings.Count && bindings[j].isPartOfComposite; j++)
					{
						if (BindingInGroup(bindings[j], group))
						{
							return j;
						}
					}
				}
				else if (!binding.isPartOfComposite && BindingInGroup(binding, group))
				{
					return i;
				}
			}
			return -1;
		}

		private static bool BindingInGroup(InputBinding binding, string group)
		{
			if (string.IsNullOrEmpty(binding.groups))
			{
				return false;
			}
			string[] array = binding.groups.Split(';');
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i] == group)
				{
					return true;
				}
			}
			return false;
		}
	}
}
