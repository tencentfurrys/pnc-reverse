using System;
using UnityEngine;

namespace PixelCrushers.GridController;

[Serializable]
public class WeaponSlot
{
	public WeaponData weaponData;

	public Animator handAnimator;

	public bool isAttacking;

	public bool isSpecialAttacking;

	public float lastAttackTime;

	public bool animationStarted;

	public bool hitEventTriggered;

	[NonSerialized]
	public int nextStageIndex;

	public bool CanAttack
	{
		get
		{
			if (!isAttacking)
			{
				return Time.time - lastAttackTime >= GetAttackCooldown();
			}
			return false;
		}
	}

	private float GetAttackCooldown()
	{
		if (!(weaponData != null))
		{
			return 1f;
		}
		return weaponData.GetAttackCooldown();
	}

	public void EquipWeaponAnimator(WeaponData weapon)
	{
		if (handAnimator != null && weapon != null && weapon.weaponAnimatorController != null)
		{
			handAnimator.runtimeAnimatorController = weapon.weaponAnimatorController;
			handAnimator.SetBool("IsEquipped", value: true);
		}
	}

	public void UnequipWeaponAnimator()
	{
		if (handAnimator != null)
		{
			handAnimator.runtimeAnimatorController = null;
			handAnimator.SetBool("IsEquipped", value: false);
		}
	}

	public void StartAttack()
	{
		isAttacking = true;
		isSpecialAttacking = false;
		animationStarted = false;
		hitEventTriggered = false;
		lastAttackTime = Time.time;
		PlayAttackAnimation();
	}

	public void StartSpecialAttack()
	{
		isAttacking = true;
		isSpecialAttacking = true;
		animationStarted = false;
		hitEventTriggered = false;
		lastAttackTime = Time.time;
		PlaySpecialAttackAnimation();
	}

	public void PlayAttackAnimation()
	{
		if (handAnimator != null && handAnimator.runtimeAnimatorController != null)
		{
			handAnimator.SetTrigger("Attack");
		}
	}

	public void PlaySpecialAttackAnimation()
	{
		if (handAnimator != null && handAnimator.runtimeAnimatorController != null)
		{
			handAnimator.SetTrigger("SpecialAttack");
		}
	}

	public void OnAnimationStart()
	{
		animationStarted = true;
		if (handAnimator != null && handAnimator.runtimeAnimatorController != null)
		{
			handAnimator.SetBool("IsAttacking", value: true);
		}
	}

	public void OnAnimationHit()
	{
		hitEventTriggered = true;
	}

	public void OnAnimationEnd()
	{
		EndAttack();
	}

	public void EndAttack()
	{
		isAttacking = false;
		isSpecialAttacking = false;
		animationStarted = false;
		hitEventTriggered = false;
		SetIdleState();
	}

	public void PlayBlockAnimation(bool blocking)
	{
		if (handAnimator != null && handAnimator.runtimeAnimatorController != null)
		{
			handAnimator.SetBool("IsBlocking", blocking);
		}
	}

	public void SetIdleState()
	{
		if (handAnimator != null && handAnimator.runtimeAnimatorController != null)
		{
			handAnimator.SetBool("IsAttacking", value: false);
			handAnimator.SetBool("IsBlocking", value: false);
		}
	}

	public void ForceStopAttack()
	{
		isAttacking = false;
		isSpecialAttacking = false;
		animationStarted = false;
		hitEventTriggered = false;
		nextStageIndex = 0;
		if (handAnimator != null)
		{
			handAnimator.ResetTrigger("Attack");
			handAnimator.ResetTrigger("SpecialAttack");
			handAnimator.SetBool("IsAttacking", value: false);
		}
		SetIdleState();
	}

	public bool IsReadyForHitDetection()
	{
		if (isAttacking && animationStarted)
		{
			return hitEventTriggered;
		}
		return false;
	}
}
