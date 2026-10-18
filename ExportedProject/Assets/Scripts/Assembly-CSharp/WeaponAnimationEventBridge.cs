using System.Reflection;
using PixelCrushers.GridController;
using UnityEngine;

public class WeaponAnimationEventBridge : MonoBehaviour
{
	[Header("References")]
	[SerializeField]
	private DualWieldingSystem dualWieldingSystem;

	[SerializeField]
	private HandSlot handSlot = HandSlot.RightHand;

	[Header("Audio Override")]
	[SerializeField]
	private AudioSource audioSource;

	[Header("Debug")]
	[SerializeField]
	private bool debugAnimationEvents = true;

	[SerializeField]
	private bool showEventTimestamps = true;

	private float lastEventTime;

	private string lastEventName;

	private void Awake()
	{
		if (dualWieldingSystem == null)
		{
			dualWieldingSystem = DualWieldingSystem.Instance;
		}
		if (audioSource == null)
		{
			audioSource = GetComponent<AudioSource>();
		}
	}

	public void OnAttackStart()
	{
		LogEvent("OnAttackStart");
		if (dualWieldingSystem != null)
		{
			dualWieldingSystem.OnAttackStart(handSlot);
		}
	}

	public void OnAttackHit()
	{
		LogEvent("OnAttackHit");
		if (dualWieldingSystem != null)
		{
			dualWieldingSystem.OnAttackHit(handSlot);
		}
	}

	public void OnAttackEnd()
	{
		LogEvent("OnAttackEnd");
		if (dualWieldingSystem != null)
		{
			dualWieldingSystem.OnAttackEnd(handSlot);
		}
	}

	public void PlaySwingSound()
	{
		LogEvent("PlaySwingSound");
		if (!(dualWieldingSystem != null))
		{
			return;
		}
		WeaponData weaponInHand = dualWieldingSystem.GetWeaponInHand(handSlot);
		if (weaponInHand != null && weaponInHand.swingSound != null)
		{
			if (audioSource != null)
			{
				audioSource.PlayOneShot(weaponInHand.swingSound);
			}
			else
			{
				PlayWeaponSoundFallback(weaponInHand.swingSound);
			}
		}
	}

	public void PlayCustomSound(AudioClip clip)
	{
		LogEvent("PlayCustomSound(" + (clip?.name ?? "null") + ")");
		if (clip != null)
		{
			if (audioSource != null)
			{
				audioSource.PlayOneShot(clip);
			}
			else
			{
				PlayWeaponSoundFallback(clip);
			}
		}
	}

	public void TriggerEffect(string effectName)
	{
		LogEvent("TriggerEffect(" + effectName + ")");
		switch (effectName.ToLower())
		{
		case "hit":
		case "damage":
		case "impact":
			OnAttackHit();
			break;
		case "swing":
		case "woosh":
			PlaySwingSound();
			break;
		case "start":
		case "begin":
			OnAttackStart();
			break;
		case "end":
		case "finish":
			OnAttackEnd();
			break;
		case "midpoint":
		case "middle":
			OnAttackMidpoint();
			break;
		}
	}

	public void OnAttackMidpoint()
	{
		LogEvent("OnAttackMidpoint");
	}

	public void SpawnEffect(GameObject effectPrefab)
	{
		LogEvent("SpawnEffect(" + (effectPrefab?.name ?? "null") + ")");
		if (effectPrefab != null)
		{
			Object.Instantiate(effectPrefab, base.transform.position, base.transform.rotation);
		}
	}

	public void SetWeaponActive(bool active)
	{
		LogEvent($"SetWeaponActive({active})");
		Renderer componentInChildren = GetComponentInChildren<Renderer>();
		if (componentInChildren != null)
		{
			componentInChildren.gameObject.SetActive(active);
		}
	}

	public void TriggerComboNext()
	{
		LogEvent("TriggerComboNext");
	}

	public void OnChargeComplete()
	{
		LogEvent("OnChargeComplete");
	}

	public void OnLeftHandAttackStart()
	{
		LogEvent("OnLeftHandAttackStart");
		if (dualWieldingSystem != null)
		{
			dualWieldingSystem.OnAttackStart(HandSlot.LeftHand);
		}
	}

	public void OnLeftHandAttackHit()
	{
		LogEvent("OnLeftHandAttackHit");
		if (dualWieldingSystem != null)
		{
			dualWieldingSystem.OnAttackHit(HandSlot.LeftHand);
		}
	}

	public void OnLeftHandAttackEnd()
	{
		LogEvent("OnLeftHandAttackEnd");
		if (dualWieldingSystem != null)
		{
			dualWieldingSystem.OnAttackEnd(HandSlot.LeftHand);
		}
	}

	public void OnRightHandAttackStart()
	{
		LogEvent("OnRightHandAttackStart");
		if (dualWieldingSystem != null)
		{
			dualWieldingSystem.OnAttackStart(HandSlot.RightHand);
		}
	}

	public void OnRightHandAttackHit()
	{
		LogEvent("OnRightHandAttackHit");
		if (dualWieldingSystem != null)
		{
			dualWieldingSystem.OnAttackHit(HandSlot.RightHand);
		}
	}

	public void OnRightHandAttackEnd()
	{
		LogEvent("OnRightHandAttackEnd");
		if (dualWieldingSystem != null)
		{
			dualWieldingSystem.OnAttackEnd(HandSlot.RightHand);
		}
	}

	public void OnBothHandsAttackStart()
	{
		LogEvent("OnBothHandsAttackStart");
		if (dualWieldingSystem != null)
		{
			dualWieldingSystem.OnAttackStart(HandSlot.BothHands);
		}
	}

	public void OnBothHandsAttackHit()
	{
		LogEvent("OnBothHandsAttackHit");
		if (dualWieldingSystem != null)
		{
			dualWieldingSystem.OnAttackHit(HandSlot.BothHands);
		}
	}

	public void OnAttackHitStage(int stageIndex)
	{
		LogEvent($"OnAttackHitStage({stageIndex})");
		if (dualWieldingSystem != null)
		{
			dualWieldingSystem.OnAttackHitStage(handSlot, stageIndex);
		}
	}

	public void OnBothHandsAttackEnd()
	{
		LogEvent("OnBothHandsAttackEnd");
		if (dualWieldingSystem != null)
		{
			dualWieldingSystem.OnAttackEnd(HandSlot.BothHands);
		}
	}

	public void OnBothHandsSpecialAttackStart()
	{
		LogEvent("OnBothHandsSpecialAttackStart");
		if (dualWieldingSystem != null)
		{
			dualWieldingSystem.OnAttackStart(HandSlot.BothHands);
		}
	}

	public void OnBothHandsSpecialAttackHit()
	{
		LogEvent("OnBothHandsSpecialAttackHit");
		if (dualWieldingSystem != null)
		{
			dualWieldingSystem.OnSpecialAttackHit(HandSlot.BothHands);
		}
	}

	public void OnBothHandsSpecialAttackEnd()
	{
		LogEvent("OnBothHandsSpecialAttackEnd");
		if (dualWieldingSystem != null)
		{
			dualWieldingSystem.OnAttackEnd(HandSlot.BothHands);
		}
	}

	public void OnBothHandsSpecialHit()
	{
		LogEvent("OnBothHandsSpecialHit");
		if (dualWieldingSystem != null)
		{
			dualWieldingSystem.OnSpecialAttackHit(HandSlot.BothHands);
		}
	}

	public void OnSpecialAttackHit()
	{
		LogEvent("OnSpecialAttackHit");
		if (dualWieldingSystem != null)
		{
			dualWieldingSystem.OnSpecialAttackHit(handSlot);
		}
	}

	public void TriggerAttackDamage()
	{
		OnAttackHit();
	}

	public void AttackHit()
	{
		OnAttackHit();
	}

	public void AttackStart()
	{
		OnAttackStart();
	}

	public void AttackEnd()
	{
		OnAttackEnd();
	}

	public void BothHandsAttackStart()
	{
		OnBothHandsAttackStart();
	}

	public void BothHandsAttackHit()
	{
		OnBothHandsAttackHit();
	}

	public void BothHandsAttackEnd()
	{
		OnBothHandsAttackEnd();
	}

	public void BothHandsSpecialStart()
	{
		OnBothHandsSpecialAttackStart();
	}

	public void BothHandsSpecialHit()
	{
		OnBothHandsSpecialAttackHit();
	}

	public void BothHandsSpecialEnd()
	{
		OnBothHandsSpecialAttackEnd();
	}

	public void SpecialHit()
	{
		OnSpecialAttackHit();
	}

	public void BothHandsSpecialAttackHit()
	{
		OnBothHandsSpecialAttackHit();
	}

	private void PlayWeaponSoundFallback(AudioClip clip)
	{
		MethodInfo method = typeof(DualWieldingSystem).GetMethod("PlayWeaponSound", BindingFlags.Instance | BindingFlags.NonPublic);
		if (method != null)
		{
			method.Invoke(dualWieldingSystem, new object[1] { clip });
		}
	}

	public WeaponData GetCurrentWeapon()
	{
		if (dualWieldingSystem != null)
		{
			return dualWieldingSystem.GetWeaponInHand(handSlot);
		}
		return null;
	}

	public bool IsAttacking()
	{
		if (dualWieldingSystem == null)
		{
			return false;
		}
		switch (handSlot)
		{
		case HandSlot.LeftHand:
			return dualWieldingSystem.IsLeftHandAttacking;
		case HandSlot.RightHand:
			return dualWieldingSystem.IsRightHandAttacking;
		case HandSlot.BothHands:
			if (dualWieldingSystem.IsUsingBothHandsWeapon())
			{
				if (!dualWieldingSystem.IsLeftHandAttacking)
				{
					return dualWieldingSystem.IsRightHandAttacking;
				}
				return true;
			}
			return false;
		default:
			return false;
		}
	}

	public bool CanAttack()
	{
		if (dualWieldingSystem == null)
		{
			return false;
		}
		return dualWieldingSystem.CanHandAttack(handSlot);
	}

	public float GetAttackCooldown()
	{
		if (dualWieldingSystem == null)
		{
			return 0f;
		}
		return dualWieldingSystem.GetAttackCooldown(handSlot);
	}

	private void LogEvent(string eventName)
	{
		if (debugAnimationEvents)
		{
			lastEventTime = Time.time;
			lastEventName = eventName;
		}
	}

	public void SetHandSlot(HandSlot slot)
	{
		handSlot = slot;
	}

	public void SetDualWieldingSystem(DualWieldingSystem system)
	{
		dualWieldingSystem = system;
	}

	public void SetDebugMode(bool enabled)
	{
		debugAnimationEvents = enabled;
	}

	public void SetTimestampLogging(bool enabled)
	{
		showEventTimestamps = enabled;
	}

	private void OnValidate()
	{
		if (dualWieldingSystem == null)
		{
			dualWieldingSystem = DualWieldingSystem.Instance;
		}
	}

	private void OnGUI()
	{
		if (debugAnimationEvents)
		{
			GUILayout.BeginArea(new Rect(Screen.width - 300, 10f, 290f, 200f));
			GUILayout.BeginVertical("box");
			GUILayout.Label($"Animation Bridge - {handSlot} Hand", GUI.skin.label);
			GUILayout.Label("Current Weapon: " + (GetCurrentWeapon()?.weaponName ?? "None"));
			GUILayout.Label($"Is Attacking: {IsAttacking()}");
			GUILayout.Label($"Can Attack: {CanAttack()}");
			GUILayout.Label($"Attack Cooldown: {GetAttackCooldown():F2}s");
			if (lastEventTime > 0f)
			{
				GUILayout.Label("Last Event: " + lastEventName);
				GUILayout.Label($"Time Since: {Time.time - lastEventTime:F2}s");
			}
			GUILayout.EndVertical();
			GUILayout.EndArea();
		}
	}
}
