using System;

namespace PixelCrushers.GridController
{
	[Serializable]
	public struct TooltipStatEntry
	{
		public TooltipStatType statType;

		public string value;

		public TooltipStatEntry(TooltipStatType type, string val)
		{
			statType = type;
			value = val;
		}
	}
}
