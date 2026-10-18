using System;
using UnityEngine;

namespace PixelCrushers.GridController
{
	[CreateAssetMenu(fileName = "TooltipIcons", menuName = "UI/Tooltip Icons")]
	public class TooltipIcons : ScriptableObject
	{
		[Serializable]
		public struct StatIconEntry
		{
			public TooltipStatType statType;

			public Sprite icon;

			[Tooltip("Description shown when hovering this stat on weapons/armor")]
			public string description;

			[Tooltip("Description shown when hovering this stat on consumables (falls back to description if empty)")]
			public string consumableDescription;
		}

		[Serializable]
		public struct WeaponTypeIconEntry
		{
			public WeaponType weaponType;

			public Sprite icon;
		}

		[SerializeField]
		private StatIconEntry[] statIcons;

		[Header("Hand Type Icons")]
		[SerializeField]
		private Sprite oneHandedIcon;

		[SerializeField]
		private Sprite twoHandedIcon;

		[Header("Weapon Type Icons")]
		[SerializeField]
		private WeaponTypeIconEntry[] weaponTypeIcons;

		public Sprite GetIcon(TooltipStatType statType)
		{
			if (statIcons == null)
			{
				return null;
			}
			for (int i = 0; i < statIcons.Length; i++)
			{
				if (statIcons[i].statType == statType)
				{
					return statIcons[i].icon;
				}
			}
			return null;
		}

		public Sprite GetHandTypeIcon(bool twoHanded)
		{
			if (!twoHanded)
			{
				return oneHandedIcon;
			}
			return twoHandedIcon;
		}

		public Sprite GetWeaponTypeIcon(WeaponType weaponType)
		{
			if (weaponTypeIcons == null)
			{
				return null;
			}
			for (int i = 0; i < weaponTypeIcons.Length; i++)
			{
				if (weaponTypeIcons[i].weaponType == weaponType)
				{
					return weaponTypeIcons[i].icon;
				}
			}
			return null;
		}

		public string GetDescription(TooltipStatType statType, bool isConsumable = false)
		{
			if (statIcons == null)
			{
				return "";
			}
			for (int i = 0; i < statIcons.Length; i++)
			{
				if (statIcons[i].statType == statType)
				{
					if (isConsumable && !string.IsNullOrEmpty(statIcons[i].consumableDescription))
					{
						return statIcons[i].consumableDescription;
					}
					return statIcons[i].description;
				}
			}
			return "";
		}
	}
}
