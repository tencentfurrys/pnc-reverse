using UnityEngine;

namespace PixelCrushers.GridController;

public abstract class TrinketEffect : ScriptableObject
{
	public virtual void OnEquip(TrinketManager manager)
	{
	}

	public virtual void OnUnequip(TrinketManager manager)
	{
	}

	public virtual void ModifyDamage(ref TrinketDamageContext ctx)
	{
	}

	public virtual void OnDealtDamage(in TrinketDamageContext ctx)
	{
	}

	public virtual void OnProjectileSpawned(Projectile projectile, in TrinketProjectileContext ctx)
	{
	}

	public virtual void OnMeleeAttack(in TrinketMeleeContext ctx)
	{
	}

	public virtual bool GrantsLifeStealOnKill()
	{
		return false;
	}

	public virtual float GetGrabDamageBonus()
	{
		return 0f;
	}

	public virtual bool TryConsumeMaxHeatGrabImmunity()
	{
		return false;
	}

	public virtual bool PeekGuaranteedCrit(out float extraCritMultiplier)
	{
		extraCritMultiplier = 1f;
		return false;
	}

	public virtual bool TryConsumeGuaranteedCrit(out float extraCritMultiplier)
	{
		extraCritMultiplier = 1f;
		return false;
	}

	public virtual bool TryConsumeBestChestRarity()
	{
		return false;
	}

	public virtual bool TryConsumeRevive(out float healthFraction)
	{
		healthFraction = 0f;
		return false;
	}

	public virtual bool GrantsChestRerollKeepBest()
	{
		return false;
	}

	public virtual float GetCritChanceBonus()
	{
		return 0f;
	}
}
