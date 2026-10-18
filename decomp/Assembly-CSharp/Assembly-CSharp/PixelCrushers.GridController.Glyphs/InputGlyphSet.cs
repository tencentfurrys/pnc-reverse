using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem.Layouts;

namespace PixelCrushers.GridController.Glyphs;

[CreateAssetMenu(fileName = "InputGlyphSet", menuName = "Input/Glyph Set")]
public class InputGlyphSet : ScriptableObject
{
	[Serializable]
	public struct Entry
	{
		[Tooltip("Input System control — pick from the dropdown (buttons, sticks, d-pad, triggers, keys, mouse).")]
		[InputControl]
		public string controlPath;

		[Tooltip("Name of the sprite inside the TMP Sprite Asset above.")]
		public string glyphName;
	}

	[Tooltip("TMP Sprite Asset holding this family's glyphs.")]
	[SerializeField]
	private TMP_SpriteAsset spriteAsset;

	[Tooltip("Control path → glyph sprite name.")]
	[SerializeField]
	private List<Entry> entries = new List<Entry>();

	private Dictionary<string, string> lookup;

	public TMP_SpriteAsset SpriteAsset => spriteAsset;

	private void OnEnable()
	{
		lookup = null;
	}

	private void BuildLookup()
	{
		lookup = new Dictionary<string, string>(entries.Count, StringComparer.OrdinalIgnoreCase);
		for (int i = 0; i < entries.Count; i++)
		{
			Entry entry = entries[i];
			if (!string.IsNullOrEmpty(entry.controlPath) && !string.IsNullOrEmpty(entry.glyphName))
			{
				lookup[Normalize(entry.controlPath)] = entry.glyphName;
			}
		}
	}

	public bool TryGetGlyphName(string controlPath, out string glyphName)
	{
		glyphName = null;
		if (string.IsNullOrEmpty(controlPath))
		{
			return false;
		}
		if (lookup == null)
		{
			BuildLookup();
		}
		return lookup.TryGetValue(Normalize(controlPath), out glyphName);
	}

	private static string Normalize(string path)
	{
		if (string.IsNullOrEmpty(path))
		{
			return path;
		}
		path = path.Trim();
		int num = path.IndexOf('>');
		if (path.Length > 0 && path[0] == '<' && num >= 0 && num + 1 < path.Length && path[num + 1] == '/')
		{
			path = path.Substring(num + 2);
		}
		if (path.Length > 0 && path[0] == '/')
		{
			path = path.Substring(1);
		}
		return path;
	}
}
