using System.Collections.Generic;
using UnityEngine;

namespace PixelCrushers.GridController
{
	[CreateAssetMenu(fileName = "Escalating Damage Trinket", menuName = "Trinkets/Escalating Damage")]
	public class EscalatingDamageTrinketEffect : TrinketEffect
	{
		private struct StackState
		{
			public int stacks;

			public float lastHitTime;
		}

		[Tooltip("Extra flat damage added per accumulated stack.")]
		public int bonusPerStack = 2;

		[Tooltip("Maximum number of stacks per target.")]
		public int maxStacks = 10;

		[Tooltip("If this many seconds pass without hitting a target, its stacks reset.")]
		public float resetSeconds = 3f;

		[Header("Stack Sound")]
		[Tooltip("Played on every hit. Pitch rises with the stack count.")]
		public AudioClip stackSound;

		[Range(0f, 1f)]
		public float stackSoundVolume = 1f;

		[Tooltip("Pitch of the first hit in a chain.")]
		public float basePitch = 1f;

		[Tooltip("Semitones added per stack. 12 = one octave per stack; 1-2 reads as a scale climbing.")]
		public float semitonesPerStack = 2f;

		[Tooltip("Hard ceiling so long chains don't turn into a squeak.")]
		public float maxPitch = 3f;

		[Tooltip("Minimum seconds between stack sounds. Stops multi-pellet weapons firing several in one frame.")]
		public float minSoundInterval = 0.05f;

		private readonly Dictionary<GameObject, StackState> stacksByTarget = new Dictionary<GameObject, StackState>();

		private AudioSource audioSource;

		private float lastSoundTime;

		public override void OnEquip(TrinketManager manager)
		{
			audioSource = ((manager != null) ? manager.ResolveTrinketAudioSource() : null);
			lastSoundTime = 0f;
		}

		public override void OnUnequip(TrinketManager manager)
		{
			stacksByTarget.Clear();
			audioSource = null;
		}

		public override void ModifyDamage(ref TrinketDamageContext ctx)
		{
			if (!(ctx.target == null) && stacksByTarget.TryGetValue(ctx.target, out var value) && Time.time - value.lastHitTime <= resetSeconds)
			{
				ctx.damage += bonusPerStack * value.stacks;
			}
		}

		public override void OnDealtDamage(in TrinketDamageContext ctx)
		{
			if (!(ctx.target == null))
			{
				if (stacksByTarget.TryGetValue(ctx.target, out var value) && Time.time - value.lastHitTime > resetSeconds)
				{
					value.stacks = 0;
				}
				value.stacks = Mathf.Min(value.stacks + 1, maxStacks);
				value.lastHitTime = Time.time;
				stacksByTarget[ctx.target] = value;
				PlayStackSound(value.stacks);
				PruneStaleTargets();
			}
		}

		private void PlayStackSound(int stacks)
		{
			if (!(stackSound == null) && !(Time.time - lastSoundTime < minSoundInterval))
			{
				AudioSource audioSource = ResolveAudio();
				if (!(audioSource == null))
				{
					float num = semitonesPerStack * (float)(stacks - 1);
					audioSource.pitch = Mathf.Min(basePitch * Mathf.Pow(2f, num / 12f), maxPitch);
					audioSource.PlayOneShot(stackSound, stackSoundVolume);
					lastSoundTime = Time.time;
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

		private void PruneStaleTargets()
		{
			if (stacksByTarget.Count < 16)
			{
				return;
			}
			List<GameObject> list = new List<GameObject>();
			foreach (KeyValuePair<GameObject, StackState> item in stacksByTarget)
			{
				if (item.Key == null || Time.time - item.Value.lastHitTime > resetSeconds)
				{
					list.Add(item.Key);
				}
			}
			foreach (GameObject item2 in list)
			{
				stacksByTarget.Remove(item2);
			}
		}
	}
}
