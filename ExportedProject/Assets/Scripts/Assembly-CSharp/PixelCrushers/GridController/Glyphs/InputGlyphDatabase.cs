using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace PixelCrushers.GridController.Glyphs
{
	[CreateAssetMenu(fileName = "InputGlyphDatabase", menuName = "Input/Glyph Database")]
	public class InputGlyphDatabase : ScriptableObject
	{
		[SerializeField]
		private InputGlyphSet keyboardMouse;

		[SerializeField]
		private InputGlyphSet xbox;

		[SerializeField]
		private InputGlyphSet playStation;

		[Tooltip("Optional glyph shown when a control path has no mapping in a set (e.g. a generic key icon). Sprite name lives in each family's sprite asset.")]
		[SerializeField]
		private string missingGlyphName = "unknown";

		private readonly HashSet<string> warned = new HashSet<string>();

		public InputGlyphSet GetSet(DeviceFamily family)
		{
			return family switch
			{
				DeviceFamily.Xbox => xbox, 
				DeviceFamily.PlayStation => playStation, 
				_ => keyboardMouse, 
			};
		}

		public TMP_SpriteAsset GetSpriteAsset(DeviceFamily family)
		{
			InputGlyphSet set = GetSet(family);
			if (!(set != null))
			{
				return null;
			}
			return set.SpriteAsset;
		}

		public bool TryGetMarkup(DeviceFamily family, string controlPath, out string markup, out TMP_SpriteAsset asset)
		{
			markup = null;
			asset = null;
			InputGlyphSet set = GetSet(family);
			if (set == null || set.SpriteAsset == null)
			{
				WarnOnce($"{family}", $"[InputGlyphs] No glyph set (or sprite asset) assigned for {family} on '{base.name}'. Falling back to text.");
				return false;
			}
			asset = set.SpriteAsset;
			string text = set.SpriteAsset.name;
			if (set.TryGetGlyphName(controlPath, out var glyphName))
			{
				markup = "<sprite=\"" + text + "\" name=\"" + glyphName + "\">";
				return true;
			}
			if (!string.IsNullOrEmpty(missingGlyphName))
			{
				markup = "<sprite=\"" + text + "\" name=\"" + missingGlyphName + "\">";
				return true;
			}
			WarnOnce($"{family}:{controlPath}", $"[InputGlyphs] No glyph mapped for '{controlPath}' in the {family} set, and no placeholder set. Falling back to text.");
			return false;
		}

		private void WarnOnce(string key, string message)
		{
			if (warned.Add(key))
			{
				Debug.LogWarning(message);
			}
		}
	}
}
