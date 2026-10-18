using System.Collections;
using UnityEngine;

public class SteamTrap : MonoBehaviour
{
	[Header("Steam Settings")]
	[SerializeField]
	private float damagePerSecond = 10f;

	[Tooltip("How often to apply health damage (smaller = smoother but more CPU intensive)")]
	[SerializeField]
	private float damageTickRate = 0.2f;

	[Header("Trap Timing")]
	[SerializeField]
	private float onDuration = 5f;

	[SerializeField]
	private float offDuration = 3f;

	[SerializeField]
	private bool startActive = true;

	[Tooltip("Keep the steam on permanently and ignore the on/off cycle timing.")]
	[SerializeField]
	private bool alwaysOn;

	[Header("Visual & Audio")]
	[SerializeField]
	private ParticleSystem steamParticles;

	[SerializeField]
	private AudioClip steamOnSound;

	[SerializeField]
	private AudioClip steamOffSound;

	[SerializeField]
	private AudioClip steamLoopSound;

	[Header("References")]
	[SerializeField]
	private BoxCollider trapCollider;

	[Tooltip("Assign the mixer-routed AudioSource (e.g. the 'steam audio' child). Auto-added if left empty.")]
	[SerializeField]
	private AudioSource audioSource;

	[Header("Debug")]
	private bool isActive;

	private bool playerInside;

	private PlayerStats playerStats;

	private Coroutine cycleCoroutine;

	private Coroutine damageCoroutine;

	private void Awake()
	{
		if (trapCollider == null)
		{
			trapCollider = GetComponent<BoxCollider>();
		}
		if (trapCollider != null)
		{
			trapCollider.isTrigger = true;
		}
		if (GetComponent<Rigidbody>() == null)
		{
			Rigidbody rigidbody = base.gameObject.AddComponent<Rigidbody>();
			rigidbody.isKinematic = true;
			rigidbody.useGravity = false;
		}
		if (audioSource == null)
		{
			audioSource = GetComponent<AudioSource>();
		}
		if (audioSource == null)
		{
			audioSource = base.gameObject.AddComponent<AudioSource>();
		}
		audioSource.spatialBlend = 1f;
		audioSource.loop = false;
		if (steamParticles == null)
		{
			steamParticles = GetComponentInChildren<ParticleSystem>();
		}
	}

	private void Start()
	{
		cycleCoroutine = StartCoroutine(TrapCycle());
	}

	private void OnDestroy()
	{
		if (cycleCoroutine != null)
		{
			StopCoroutine(cycleCoroutine);
		}
		if (damageCoroutine != null)
		{
			StopCoroutine(damageCoroutine);
		}
	}

	private IEnumerator TrapCycle()
	{
		if (alwaysOn)
		{
			ActivateTrap();
			yield break;
		}
		if (startActive)
		{
			ActivateTrap();
			yield return new WaitForSeconds(onDuration);
		}
		else
		{
			DeactivateTrap();
			yield return new WaitForSeconds(offDuration);
		}
		while (true)
		{
			if (isActive)
			{
				DeactivateTrap();
				yield return new WaitForSeconds(offDuration);
			}
			else
			{
				ActivateTrap();
				yield return new WaitForSeconds(onDuration);
			}
		}
	}

	private void ActivateTrap()
	{
		isActive = true;
		if (audioSource != null)
		{
			audioSource.mute = false;
		}
		if (steamParticles != null)
		{
			steamParticles.Play();
		}
		if (steamOnSound != null && audioSource != null)
		{
			audioSource.PlayOneShot(steamOnSound);
		}
		if (steamLoopSound != null && audioSource != null)
		{
			audioSource.clip = steamLoopSound;
			audioSource.loop = true;
			audioSource.Play();
		}
		if (playerInside && playerStats != null)
		{
			StartApplyingDamage();
		}
	}

	private void DeactivateTrap()
	{
		isActive = false;
		if (steamParticles != null)
		{
			steamParticles.Stop();
		}
		if (audioSource != null && audioSource.isPlaying && audioSource.loop)
		{
			audioSource.Stop();
		}
		if (audioSource != null)
		{
			if (steamOffSound != null)
			{
				audioSource.PlayOneShot(steamOffSound);
			}
			else
			{
				audioSource.mute = true;
			}
		}
		StopApplyingDamage();
	}

	private void OnTriggerEnter(Collider other)
	{
		if (!IsPlayer(other))
		{
			return;
		}
		playerStats = other.GetComponent<PlayerStats>();
		if (!(playerStats == null))
		{
			playerInside = true;
			if (isActive)
			{
				StartApplyingDamage();
			}
		}
	}

	private void OnTriggerStay(Collider other)
	{
		if (!IsPlayer(other) || playerInside)
		{
			return;
		}
		playerStats = other.GetComponent<PlayerStats>();
		if (playerStats != null)
		{
			playerInside = true;
			if (isActive)
			{
				StartApplyingDamage();
			}
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (IsPlayer(other))
		{
			playerInside = false;
			StopApplyingDamage();
		}
	}

	private bool IsPlayer(Collider other)
	{
		if (!other.CompareTag("Player") && !(other.GetComponent<FirstPersonController>() != null) && !(other.GetComponent<CharacterController>() != null))
		{
			return other.GetComponent<PlayerStats>() != null;
		}
		return true;
	}

	private void StartApplyingDamage()
	{
		if (damageCoroutine == null)
		{
			damageCoroutine = StartCoroutine(ApplyDamageOverTime());
		}
	}

	private void StopApplyingDamage()
	{
		if (damageCoroutine != null)
		{
			StopCoroutine(damageCoroutine);
			damageCoroutine = null;
		}
	}

	private IEnumerator ApplyDamageOverTime()
	{
		float damageBuffer = 0f;
		while (playerInside && isActive && playerStats != null)
		{
			damageBuffer += damagePerSecond * damageTickRate;
			int num = Mathf.FloorToInt(damageBuffer);
			if (num > 0)
			{
				playerStats.TakeRawDamage(num);
				damageBuffer -= (float)num;
			}
			yield return new WaitForSeconds(damageTickRate);
		}
		damageCoroutine = null;
	}

	public void SetActive(bool active)
	{
		if (active)
		{
			ActivateTrap();
		}
		else
		{
			DeactivateTrap();
		}
	}

	public void StopCycle()
	{
		if (cycleCoroutine != null)
		{
			StopCoroutine(cycleCoroutine);
			cycleCoroutine = null;
		}
	}

	public void StartCycle()
	{
		if (cycleCoroutine == null)
		{
			cycleCoroutine = StartCoroutine(TrapCycle());
		}
	}
}
