using UnityEngine;

namespace PixelCrushers.GridController;

[CreateAssetMenu(fileName = "Lucky Charm Trinket", menuName = "Trinkets/Guaranteed Crit & Best Loot")]
public class GuaranteedCritChestTrinketEffect : TrinketEffect
{
	[Tooltip("Extra damage multiplier applied to the guaranteed critical hit, on top of the weapon's crit multiplier.")]
	public float critDamageMultiplier = 2f;

	[Header("Consume Sound")]
	[Tooltip("Played once when the charge is spent, by either trigger.")]
	public AudioClip consumeSound;

	[Range(0f, 1f)]
	public float consumeSoundVolume = 1f;

	private bool chargeUsed;

	private AudioSource audioSource;

	public override void OnEquip(TrinketManager manager)
	{
		audioSource = ((manager != null) ? manager.ResolveTrinketAudioSource() : null);
	}

	public override void OnUnequip(TrinketManager manager)
	{
		audioSource = null;
	}

	public override bool PeekGuaranteedCrit(out float extraCritMultiplier)
	{
		if (chargeUsed)
		{
			extraCritMultiplier = 1f;
			return false;
		}
		extraCritMultiplier = critDamageMultiplier;
		return true;
	}

	public override bool TryConsumeGuaranteedCrit(out float extraCritMultiplier)
	{
		if (chargeUsed)
		{
			extraCritMultiplier = 1f;
			return false;
		}
		extraCritMultiplier = critDamageMultiplier;
		chargeUsed = true;
		PlayConsumeSound();
		return true;
	}

	public override bool TryConsumeBestChestRarity()
	{
		if (chargeUsed)
		{
			return false;
		}
		chargeUsed = true;
		PlayConsumeSound();
		return true;
	}

	private void PlayConsumeSound()
	{
		if (!(consumeSound == null))
		{
			AudioSource audioSource = ResolveAudio();
			if (!(audioSource == null))
			{
				audioSource.pitch = 1f;
				audioSource.PlayOneShot(consumeSound, consumeSoundVolume);
			}
		}
	}

	private AudioSource ResolveAudio()
	{
		if (audioSource == null && TrinketManager.Instance != null)
		{
			audioSource = TrinketManager.Instance.ResolveTrinketAudioSource();
		}
		return audioSource;
	}
}
