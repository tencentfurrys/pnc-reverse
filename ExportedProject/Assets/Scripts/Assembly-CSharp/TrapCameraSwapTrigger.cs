using System.Collections;
using UnityEngine;

public class TrapCameraSwapTrigger : CameraSwapTrigger
{
	[Header("Trap Settings")]
	[SerializeField]
	private RuntimeAnimatorController trapAnimatorController;

	[SerializeField]
	private string trapAnimationTriggerName = "PlayTrapAnimation";

	[SerializeField]
	private AudioClip[] trapAnimationSounds;

	private int useCount;

	public override bool CanInteract
	{
		get
		{
			if (useCount < 2 && !isSwapping)
			{
				return !isInCinematicView;
			}
			return false;
		}
	}

	public override void OnInteracted()
	{
		if (useCount < 2 && !isSwapping)
		{
			if (useCount == 0)
			{
				useCount++;
				ActivateTrigger();
			}
			else if (useCount == 1 && !isInCinematicView)
			{
				useCount++;
				ActivateTrapSequence();
			}
		}
	}

	private void ActivateTrapSequence()
	{
		HidePrompt();
		StartCoroutine(TrapSequenceCoroutine());
	}

	protected override void TriggerAnimation()
	{
		if (useCount >= 2 && animatorToTrigger != null && trapAnimatorController != null)
		{
			if (activateAnimatorObject)
			{
				animatorToTrigger.gameObject.SetActive(value: true);
			}
			animatorToTrigger.runtimeAnimatorController = trapAnimatorController;
			animationSounds = trapAnimationSounds;
			animatorToTrigger.SetTrigger(trapAnimationTriggerName);
		}
		else
		{
			base.TriggerAnimation();
		}
	}

	private IEnumerator TrapSequenceCoroutine()
	{
		yield return StartCoroutine(PerformCameraSwap());
		if (trapAnimationSounds != null && trapAnimationSounds.Length != 0)
		{
			PlayAudioClip(trapAnimationSounds[0]);
		}
		if (playerStats != null)
		{
			playerStats.TakeDamage(999999);
		}
	}
}
