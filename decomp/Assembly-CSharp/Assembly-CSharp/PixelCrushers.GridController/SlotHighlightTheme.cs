using System;
using UnityEngine;

namespace PixelCrushers.GridController;

[Serializable]
public class SlotHighlightTheme
{
	[Header("Hover (mouse-over / gamepad-selected slot)")]
	[Tooltip("Optional art for the hover overlay. Leave empty to fall back to the slot's own background sprite tinted with Hover Fallback Color.")]
	public Sprite hoverSprite;

	[Tooltip("Tint applied to Hover Sprite. Only used when Hover Sprite is assigned.")]
	public Color hoverTint = Color.white;

	[Tooltip("Used when no Hover Sprite is assigned - mirrors how LockedOverlay tints the slot sprite (white at 0.36 alpha).")]
	public Color hoverFallbackColor = new Color(1f, 1f, 1f, 0.36f);

	[Header("Valid Target (equipment slots the focused/held item fits)")]
	[Tooltip("Optional art for the valid-target overlay. Leave empty to fall back to the slot's own background sprite tinted with Valid Target Fallback Color.")]
	public Sprite validTargetSprite;

	[Tooltip("Tint applied to Valid Target Sprite. Only used when Valid Target Sprite is assigned.")]
	public Color validTargetTint = Color.white;

	[Tooltip("Used when no Valid Target Sprite is assigned.")]
	public Color validTargetFallbackColor = new Color(0.45f, 1f, 0.55f, 0.4f);
}
