using System.Collections;
using UnityEngine;

public class LingeringHeatCloud : MonoBehaviour
{
	[Header("Heat Settings")]
	[Tooltip("Heat applied per second while the player stands inside the cloud.")]
	[SerializeField]
	private float heatPerSecond = 5f;

	[Tooltip("How often heat is applied (smaller = smoother but more CPU). Keep this below the player's Heat Cooldown Delay, or heat bleeds off between ticks and the cloud barely registers.")]
	[SerializeField]
	private float heatTickRate = 0.2f;

	[Header("Lifetime")]
	[Tooltip("How long the cloud lasts before it removes itself.")]
	[SerializeField]
	private float lifetime = 6f;

	[Tooltip("How long before the end to stop emitting, so the cloud thins out instead of vanishing.")]
	[SerializeField]
	private float fadeOutTime = 1f;

	[Header("Visual & Audio")]
	[SerializeField]
	private ParticleSystem cloudParticles;

	[SerializeField]
	private AudioClip loopSound;

	[Range(0f, 1f)]
	[SerializeField]
	private float soundVolume = 0.5f;

	private bool playerInside;

	private PlayerStats playerStats;

	private AudioSource audioSource;

	private Coroutine heatCoroutine;

	private void Awake()
	{
		Collider component = GetComponent<Collider>();
		if (component != null)
		{
			component.isTrigger = true;
		}
		if (GetComponent<Rigidbody>() == null)
		{
			Rigidbody rigidbody = base.gameObject.AddComponent<Rigidbody>();
			rigidbody.isKinematic = true;
			rigidbody.useGravity = false;
		}
		if (cloudParticles == null)
		{
			cloudParticles = GetComponentInChildren<ParticleSystem>();
		}
		audioSource = GetComponent<AudioSource>();
		if (audioSource == null)
		{
			audioSource = base.gameObject.AddComponent<AudioSource>();
		}
		audioSource.spatialBlend = 1f;
		audioSource.playOnAwake = false;
		SyncParticlesToCollider();
	}

	private void SyncParticlesToCollider()
	{
		if (cloudParticles == null)
		{
			return;
		}
		SphereCollider component = GetComponent<SphereCollider>();
		if (component == null)
		{
			return;
		}
		float num = component.radius * MaxAxis(base.transform.lossyScale);
		float num2 = MaxAxis(cloudParticles.transform.lossyScale);
		if (!(num2 <= 0.0001f))
		{
			ParticleSystem.ShapeModule shape = cloudParticles.shape;
			float num3 = MaxAxis(shape.scale);
			if (!(num3 <= 0.0001f))
			{
				shape.radius = num / (num2 * num3);
			}
		}
	}

	private static float MaxAxis(Vector3 v)
	{
		return Mathf.Max(Mathf.Abs(v.x), Mathf.Max(Mathf.Abs(v.y), Mathf.Abs(v.z)));
	}

	private void OnValidate()
	{
		if (cloudParticles == null)
		{
			cloudParticles = GetComponentInChildren<ParticleSystem>();
		}
		SyncParticlesToCollider();
	}

	private void Start()
	{
		if (cloudParticles != null)
		{
			cloudParticles.Play();
		}
		if (loopSound != null)
		{
			audioSource.clip = loopSound;
			audioSource.loop = true;
			audioSource.volume = soundVolume;
			audioSource.Play();
		}
		StartCoroutine(ExpireAfterLifetime());
	}

	private IEnumerator ExpireAfterLifetime()
	{
		float emitDuration = Mathf.Max(0f, lifetime - fadeOutTime);
		yield return new WaitForSeconds(emitDuration);
		if (cloudParticles != null)
		{
			cloudParticles.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
		}
		yield return new WaitForSeconds(lifetime - emitDuration);
		Object.Destroy(base.gameObject);
	}

	private void OnTriggerEnter(Collider other)
	{
		if (IsPlayer(other))
		{
			playerStats = other.GetComponent<PlayerStats>();
			if (!(playerStats == null))
			{
				playerInside = true;
				StartApplyingHeat();
			}
		}
	}

	private void OnTriggerStay(Collider other)
	{
		if (IsPlayer(other) && !playerInside)
		{
			playerStats = other.GetComponent<PlayerStats>();
			if (!(playerStats == null))
			{
				playerInside = true;
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
		while (playerInside && playerStats != null)
		{
			float amount = heatPerSecond * heatTickRate;
			if (playerStats.CanGenerateHeat(amount))
			{
				playerStats.GenerateHeat(amount);
			}
			yield return new WaitForSeconds(heatTickRate);
		}
		heatCoroutine = null;
	}

	private void OnDestroy()
	{
		StopApplyingHeat();
	}
}
