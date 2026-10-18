using System.Collections;
using UnityEngine;

public class GasTrap : MonoBehaviour
{
	[Header("Heat Settings")]
	[SerializeField]
	private float heatPerSecond = 5f;

	[Tooltip("How often to apply heat damage (smaller = smoother but more CPU intensive)")]
	[SerializeField]
	private float heatTickRate = 0.2f;

	[Header("Trap Timing")]
	[SerializeField]
	private float onDuration = 5f;

	[SerializeField]
	private float offDuration = 3f;

	[SerializeField]
	private bool startActive = true;

	[Header("Visual & Audio")]
	[SerializeField]
	private ParticleSystem gasParticles;

	[SerializeField]
	private AudioClip gasOnSound;

	[SerializeField]
	private AudioClip gasOffSound;

	[SerializeField]
	private AudioClip gasLoopSound;

	[Range(0f, 1f)]
	[SerializeField]
	private float soundVolume = 0.5f;

	[Header("References")]
	[SerializeField]
	private BoxCollider trapCollider;

	[Header("Debug")]
	private bool isActive;

	private bool playerInside;

	private PlayerStats playerStats;

	private AudioSource audioSource;

	private Coroutine cycleCoroutine;

	private Coroutine heatCoroutine;

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
		audioSource = GetComponent<AudioSource>();
		if (audioSource == null)
		{
			audioSource = base.gameObject.AddComponent<AudioSource>();
		}
		audioSource.spatialBlend = 1f;
		audioSource.volume = soundVolume;
		audioSource.loop = false;
		if (gasParticles == null)
		{
			gasParticles = GetComponentInChildren<ParticleSystem>();
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
		if (heatCoroutine != null)
		{
			StopCoroutine(heatCoroutine);
		}
	}

	private IEnumerator TrapCycle()
	{
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
		if (gasParticles != null)
		{
			gasParticles.Play();
		}
		if (gasOnSound != null && audioSource != null)
		{
			audioSource.PlayOneShot(gasOnSound, soundVolume);
		}
		if (gasLoopSound != null && audioSource != null)
		{
			audioSource.clip = gasLoopSound;
			audioSource.loop = true;
			audioSource.Play();
		}
		if (playerInside && playerStats != null)
		{
			StartApplyingHeat();
		}
	}

	private void DeactivateTrap()
	{
		isActive = false;
		if (gasParticles != null)
		{
			gasParticles.Stop();
		}
		if (audioSource != null && audioSource.isPlaying && audioSource.loop)
		{
			audioSource.Stop();
		}
		if (gasOffSound != null && audioSource != null)
		{
			audioSource.PlayOneShot(gasOffSound, soundVolume);
		}
		StopApplyingHeat();
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
				StartApplyingHeat();
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
				StartApplyingHeat();
			}
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (IsPlayer(other))
		{
			playerInside = false;
			StopApplyingHeat();
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

	private void StartApplyingHeat()
	{
		if (heatCoroutine == null)
		{
			heatCoroutine = StartCoroutine(ApplyHeatOverTime());
		}
	}

	private void StopApplyingHeat()
	{
		if (heatCoroutine != null)
		{
			StopCoroutine(heatCoroutine);
			heatCoroutine = null;
		}
	}

	private IEnumerator ApplyHeatOverTime()
	{
		while (playerInside && isActive && playerStats != null)
		{
			float amount = heatPerSecond * heatTickRate;
			if (playerStats.CanGenerateHeat())
			{
				playerStats.GenerateHeat(amount);
			}
			yield return new WaitForSeconds(heatTickRate);
		}
		heatCoroutine = null;
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
