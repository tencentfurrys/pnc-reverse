using System;
using UnityEngine.Events;

namespace PixelCrushers.GridController
{
	[Serializable]
	public class CombatEvents
	{
		public UnityEvent<WeaponData, HandSlot> OnWeaponEquipped;

		public UnityEvent<WeaponData, HandSlot> OnWeaponUnequipped;

		public UnityEvent<WeaponData, HandSlot, int> OnAttackPerformed;

		public UnityEvent<WeaponData, HandSlot> OnBlockPerformed;

		public UnityEvent<int> OnDamageTaken;

		public UnityEvent OnPlayerDeath;
	}
}
