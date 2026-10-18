using UnityEngine;

public class HealingCameraSwapTrigger : CameraSwapTrigger
{
	[Header("Second Animation (50/50)")]
	[SerializeField]
	private string secondAnimationTriggerName = "PlayAnimationB";

	[SerializeField]
	private AudioClip[] secondAnimationSounds;

	[SerializeField]
	private RuntimeAnimatorController secondAnimatorController;

	private string originalAnimationTriggerName;

	private AudioClip[] originalAnimationSounds;

	private RuntimeAnimatorController originalAnimatorController;

	private bool originalsStored;

	protected override void ActivateTrigger()
	{
		if (!originalsStored)
		{
			originalAnimationTriggerName = animationTriggerName;
			originalAnimationSounds = animationSounds;
			originalAnimatorController = ((animatorToTrigger != null) ? animatorToTrigger.runtimeAnimatorController : null);
			originalsStored = true;
		}
		if (Random.value < 0.5f)
		{
			animationTriggerName = secondAnimationTriggerName;
			if (secondAnimationSounds != null && secondAnimationSounds.Length != 0)
			{
				animationSounds = secondAnimationSounds;
			}
			if (secondAnimatorController != null && animatorToTrigger != null)
			{
				animatorToTrigger.runtimeAnimatorController = secondAnimatorController;
			}
		}
		else
		{
			animationTriggerName = originalAnimationTriggerName;
			animationSounds = originalAnimationSounds;
			if (originalAnimatorController != null && animatorToTrigger != null)
			{
				animatorToTrigger.runtimeAnimatorController = originalAnimatorController;
			}
		}
		enableRewards = true;
		rewardType = RewardType.HealPlayer;
		healToFullHealth = true;
		base.ActivateTrigger();
	}
}
