using UnityEngine;
using UnityEngine.Audio;

namespace PixelCrushers.GridController
{
	public class TrinketManager : MonoBehaviour
	{
		public const int SlotCount = 2;

		private readonly TrinketEffect[] active = new TrinketEffect[2];

		private GridInventorySystem inventorySystem;

		private PlayerStats playerStats;

		[Tooltip("Optional: the player's main light source. If left empty it is auto-found in the player hierarchy.")]
		[SerializeField]
		private Light playerLight;

		[Tooltip("Optional: mixer group for trinket SFX. Wire to the SFX group so the volume slider applies.")]
		[SerializeField]
		private AudioMixerGroup trinketMixerGroup;

		private AudioSource trinketAudio;

		public static TrinketManager Instance { get; private set; }

		public Light ResolvePlayerLight()
		{
			if (playerLight == null)
			{
				playerLight = base.transform.root.GetComponentInChildren<Light>(includeInactive: true);
			}
			return playerLight;
		}

		public AudioSource ResolveTrinketAudioSource()
		{
			if (trinketAudio != null)
			{
				return trinketAudio;
			}
			trinketAudio = base.gameObject.AddComponent<AudioSource>();
			trinketAudio.playOnAwake = false;
			trinketAudio.loop = false;
			trinketAudio.spatialBlend = 0f;
			if (trinketMixerGroup != null)
			{
				trinketAudio.outputAudioMixerGroup = trinketMixerGroup;
			}
			return trinketAudio;
		}

		private void Awake()
		{
			if (Instance != null && Instance != this)
			{
				Debug.LogWarning("[TrinketManager] Multiple instances found; keeping the first.", this);
			}
			else
			{
				Instance = this;
			}
		}

		private GridInventorySystem ResolveInventorySystem()
		{
			if (inventorySystem != null)
			{
				return inventorySystem;
			}
			inventorySystem = GetComponent<GridInventorySystem>() ?? GetComponentInParent<GridInventorySystem>() ?? GetComponentInChildren<GridInventorySystem>() ?? Object.FindAnyObjectByType<GridInventorySystem>();
			return inventorySystem;
		}

		public PlayerStats ResolvePlayerStats()
		{
			if (playerStats != null)
			{
				return playerStats;
			}
			playerStats = GetComponent<PlayerStats>() ?? GetComponentInParent<PlayerStats>() ?? GetComponentInChildren<PlayerStats>() ?? Object.FindAnyObjectByType<PlayerStats>();
			return playerStats;
		}

		private void OnDestroy()
		{
			if (Instance == this)
			{
				Instance = null;
			}
			for (int i = 0; i < active.Length; i++)
			{
				ClearSlot(i);
			}
		}

		public void EquipTrinket(TrinketData data, int slotIndex)
		{
			if (slotIndex >= 0 && slotIndex < 2)
			{
				ClearSlot(slotIndex);
				if (!(data == null) && !(data.effect == null))
				{
					TrinketEffect trinketEffect = Object.Instantiate(data.effect);
					trinketEffect.name = data.effect.name + " (Runtime)";
					active[slotIndex] = trinketEffect;
					trinketEffect.OnEquip(this);
				}
			}
		}

		public void UnequipTrinket(int slotIndex)
		{
			if (slotIndex >= 0 && slotIndex < 2)
			{
				ClearSlot(slotIndex);
			}
		}

		private void ClearSlot(int slotIndex)
		{
			TrinketEffect trinketEffect = active[slotIndex];
			if (!(trinketEffect == null))
			{
				trinketEffect.OnUnequip(this);
				Object.Destroy(trinketEffect);
				active[slotIndex] = null;
			}
		}

		public static bool IsTrinketTarget(GameObject target)
		{
			if (target == null)
			{
				return false;
			}
			if (target.GetComponent<BreakableObject>() != null)
			{
				return false;
			}
			if (target.GetComponent<PlayerStats>() != null)
			{
				return false;
			}
			return true;
		}

		public void ModifyDamage(ref TrinketDamageContext ctx)
		{
			if (!IsTrinketTarget(ctx.target))
			{
				return;
			}
			for (int i = 0; i < active.Length; i++)
			{
				if (active[i] != null)
				{
					active[i].ModifyDamage(ref ctx);
				}
			}
		}

		public void OnDealtDamage(in TrinketDamageContext ctx)
		{
			if (!IsTrinketTarget(ctx.target))
			{
				return;
			}
			for (int i = 0; i < active.Length; i++)
			{
				if (active[i] != null)
				{
					active[i].OnDealtDamage(in ctx);
				}
			}
		}

		public void OnProjectileSpawned(Projectile projectile, in TrinketProjectileContext ctx)
		{
			if (projectile == null)
			{
				return;
			}
			for (int i = 0; i < active.Length; i++)
			{
				if (active[i] != null)
				{
					active[i].OnProjectileSpawned(projectile, in ctx);
				}
			}
		}

		public void OnMeleeAttack(in TrinketMeleeContext ctx)
		{
			for (int i = 0; i < active.Length; i++)
			{
				if (active[i] != null)
				{
					active[i].OnMeleeAttack(in ctx);
				}
			}
		}

		public bool HasLifeStealOnKill()
		{
			for (int i = 0; i < active.Length; i++)
			{
				if (active[i] != null && active[i].GrantsLifeStealOnKill())
				{
					return true;
				}
			}
			return false;
		}

		public float GetGrabDamageBonus()
		{
			float num = 0f;
			for (int i = 0; i < active.Length; i++)
			{
				if (active[i] != null)
				{
					num += active[i].GetGrabDamageBonus();
				}
			}
			return num;
		}

		public bool TryConsumeMaxHeatGrabImmunity()
		{
			for (int i = 0; i < active.Length; i++)
			{
				if (active[i] != null && active[i].TryConsumeMaxHeatGrabImmunity())
				{
					ConsumeTrinketItem(i);
					return true;
				}
			}
			return false;
		}

		public bool PeekGuaranteedCrit(out float extraCritMultiplier)
		{
			for (int i = 0; i < active.Length; i++)
			{
				if (active[i] != null && active[i].PeekGuaranteedCrit(out extraCritMultiplier))
				{
					return true;
				}
			}
			extraCritMultiplier = 1f;
			return false;
		}

		public bool TryConsumeGuaranteedCrit(out float extraCritMultiplier)
		{
			for (int i = 0; i < active.Length; i++)
			{
				if (active[i] != null && active[i].TryConsumeGuaranteedCrit(out extraCritMultiplier))
				{
					ConsumeTrinketItem(i);
					return true;
				}
			}
			extraCritMultiplier = 1f;
			return false;
		}

		public bool TryConsumeBestChestRarity()
		{
			for (int i = 0; i < active.Length; i++)
			{
				if (active[i] != null && active[i].TryConsumeBestChestRarity())
				{
					ConsumeTrinketItem(i);
					return true;
				}
			}
			return false;
		}

		public bool TryConsumeRevive(out float healthFraction)
		{
			for (int i = 0; i < active.Length; i++)
			{
				if (active[i] != null && active[i].TryConsumeRevive(out healthFraction))
				{
					ConsumeTrinketItem(i);
					return true;
				}
			}
			healthFraction = 0f;
			return false;
		}

		public bool HasChestRerollKeepBest()
		{
			for (int i = 0; i < active.Length; i++)
			{
				if (active[i] != null && active[i].GrantsChestRerollKeepBest())
				{
					return true;
				}
			}
			return false;
		}

		public float GetCritChanceBonus()
		{
			float num = 0f;
			for (int i = 0; i < active.Length; i++)
			{
				if (active[i] != null)
				{
					num += active[i].GetCritChanceBonus();
				}
			}
			return num;
		}

		private void ConsumeTrinketItem(int slotIndex)
		{
			GridInventorySystem gridInventorySystem = ResolveInventorySystem();
			if (gridInventorySystem != null)
			{
				gridInventorySystem.ConsumeTrinket(slotIndex);
			}
			else
			{
				UnequipTrinket(slotIndex);
			}
		}
	}
}
