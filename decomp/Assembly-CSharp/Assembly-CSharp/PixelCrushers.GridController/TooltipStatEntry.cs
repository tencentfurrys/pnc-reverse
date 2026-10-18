using System;

namespace PixelCrushers.GridController;

[Serializable]
public struct TooltipStatEntry(TooltipStatType type, string val)
{
	public TooltipStatType statType = type;

	public string value = val;
}
