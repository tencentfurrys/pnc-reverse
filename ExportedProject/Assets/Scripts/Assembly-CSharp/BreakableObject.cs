using System.Collections;
using PixelCrushers.GridController;
using UnityEngine;

public class BreakableObject : MonoBehaviour, IDamageable
{
	[Header("Break Effects")]
	[SerializeField]
	private GameObject breakEffect;

	[SerializeField]
	private AudioClip breakSound;

	[SerializeField]
	private float effectDuration = 2f;

	[Header("Audio Settings")]
	[SerializeField]
	private bool randomizePitch = true;

	[SerializeField]
	private float minPitch = 0.8f;

	[SerializeField]
	private float maxPitch = 1.2f;

	[Header("Break Settings")]
	[SerializeField]
	private bool breakOnAnyDamage = true;

	[SerializeField]
	private int breakThreshold = 1;

	[SerializeField]
	private bool spawnDebris;

	[Header("Debris Settings")]
	[SerializeField]
	private GameObject[] debrisPrefabs;

	[SerializeField]
	private int minDebrisCount = 2;

	[SerializeField]
	private int maxDebrisCount = 5;

	[SerializeField]
	private float debrisForce = 5f;

	[SerializeField]
	private float debrisLifetime = 10f;

	[Header("Optional Components")]
	[SerializeField]
	private Collider objectCollider;

	[SerializeField]
	private Renderer objectRenderer;

	[SerializeField]
	private AudioSource audioSource;

	[Header("Item Drop Settings")]
	[SerializeField]
	private ChestItemPool itemPool;

	[SerializeField]
	private GameObject itemPickupPrefab;

	[SerializeField]
	[Range(0f, 1f)]
	private float dropChance;

	[Header("Debug")]
	[SerializeField]
	private bool showDebugMessages = true;

	private bool isBroken;

	public bool IsBroken => isBroken;

	private void Awake()
	{
		if (objectCollider == null)
		{
			objectCollider = GetComponent<Collider>();
		}
		if (objectRenderer == null)
		{
			objectRenderer = GetComponent<Renderer>();
		}
		if (audioSource == null)
		{
			audioSource = GetComponent<AudioSource>();
			if (audioSource == null)
			{
				audioSource = base.gameObject.AddComponent<AudioSource>();
			}
		}
	}

	private void Start()
	{
		if (objectCollider == null)
		{
			objectCollider = base.gameObject.AddComponent<BoxCollider>();
		}
	}

	public void TakeDamage(int damage)
	{
		if (!isBroken && (breakOnAnyDamage || damage >= breakThreshold))
		{
			BreakObject();
		}
	}

	public void BreakObject()
	{
		if (!isBroken)
		{
			isBroken = true;
			PlayBreakSound();
			SpawnBreakEffect();
			if (spawnDebris)
			{
				StartCoroutine(SpawnDebrisCoroutine());
			}
			TryDropItem();
			HideObject();
			float t = Mathf.Max(GetAudioDestroyDelay(), spawnDebris ? 0.15f : 0f);
			Object.Destroy(base.gameObject, t);
		}
	}

	public void ForceBreak()
	{
		BreakObject();
	}

	private void PlayBreakSound()
	{
		if (audioSource != null && breakSound != null)
		{
			if (randomizePitch)
			{
				float pitch = Random.Range(minPitch, maxPitch);
				audioSource.pitch = pitch;
			}
			else
			{
				audioSource.pitch = 1f;
			}
			audioSource.PlayOneShot(breakSound);
		}
	}

	private float GetAudioDestroyDelay()
	{
		if (audioSource != null && breakSound != null)
		{
			float num = breakSound.length / audioSource.pitch;
			float num2 = 0.1f;
			return num + num2;
		}
		return 0.5f;
	}

	private void SpawnBreakEffect()
	{
		if (breakEffect != null)
		{
			Vector3 position = base.transform.position;
			if (objectRenderer != null)
			{
				position = objectRenderer.bounds.center;
			}
			Object.Destroy(Object.Instantiate(breakEffect, position, Quaternion.identity), effectDuration);
		}
	}

	private IEnumerator SpawnDebrisCoroutine()
	{
		if (debrisPrefabs == null || debrisPrefabs.Length == 0)
		{
			yield break;
		}
		int debrisCount = Random.Range(minDebrisCount, maxDebrisCount + 1);
		Vector3 centerPosition = base.transform.position;
		if (objectRenderer != null)
		{
			centerPosition = objectRenderer.bounds.center;
		}
		for (int i = 0; i < debrisCount; i++)
		{
			GameObject gameObject = debrisPrefabs[Random.Range(0, debrisPrefabs.Length)];
			if (!(gameObject == null))
			{
				Vector3 position = centerPosition + Random.insideUnitSphere * 0.5f;
				Quaternion rotation = Random.rotation;
				GameObject gameObject2 = Object.Instantiate(gameObject, position, rotation);
				Rigidbody rigidbody = gameObject2.GetComponent<Rigidbody>();
				if (rigidbody == null)
				{
					rigidbody = gameObject2.AddComponent<Rigidbody>();
				}
				rigidbody.AddForce((Random.insideUnitSphere + Vector3.up * 0.5f).normalized * debrisForce, ForceMode.Impulse);
				rigidbody.AddTorque(Random.insideUnitSphere * debrisForce, ForceMode.Impulse);
				Object.Destroy(gameObject2, debrisLifetime);
				yield return null;
			}
		}
	}

	private void TryDropItem()
	{
		if (itemPool == null || itemPickupPrefab == null || Random.value > dropChance)
		{
			return;
		}
		InventoryItem randomItem = itemPool.GetRandomItem();
		if (!(randomItem == null))
		{
			Vector3 position = base.transform.position + Vector3.up * 0.5f;
			GameObject gameObject = ((!(PickupPoolManager.Instance != null)) ? Object.Instantiate(itemPickupPrefab, position, Quaternion.identity) : PickupPoolManager.Instance.GetItemPickup(position, Quaternion.identity));
			InventoryItemPickup component = gameObject.GetComponent<InventoryItemPickup>();
			if (component != null)
			{
				component.SetItemData(randomItem);
			}
			Rigidbody component2 = gameObject.GetComponent<Rigidbody>();
			if (component2 != null)
			{
				component2.isKinematic = false;
				component2.useGravity = true;
				Vector3 force = Vector3.up * 2f + Random.insideUnitSphere * 0.5f;
				component2.AddForce(force, ForceMode.Impulse);
			}
		}
	}

	private void HideObject()
	{
		if (objectCollider != null)
		{
			objectCollider.enabled = false;
		}
		Renderer[] componentsInChildren = GetComponentsInChildren<Renderer>(includeInactive: true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			Object.Destroy(componentsInChildren[i]);
		}
	}

	public void ResetObject()
	{
		isBroken = false;
		if (objectCollider != null)
		{
			objectCollider.enabled = true;
		}
		if (objectRenderer != null)
		{
			objectRenderer.enabled = true;
		}
		Renderer[] componentsInChildren = GetComponentsInChildren<Renderer>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].enabled = true;
		}
	}

	public void SetBreakThreshold(int threshold)
	{
		breakThreshold = threshold;
		breakOnAnyDamage = threshold <= 1;
	}

	public void SetBreakEffect(GameObject effect, float duration = 2f)
	{
		breakEffect = effect;
		effectDuration = duration;
	}

	public void SetBreakSound(AudioClip sound)
	{
		breakSound = sound;
	}

	public void SetPitchRandomization(bool enabled, float minPitch = 0.8f, float maxPitch = 1.2f)
	{
		randomizePitch = enabled;
		this.minPitch = minPitch;
		this.maxPitch = maxPitch;
	}

	public void PlayBreakSoundWithPitch(float pitch)
	{
		if (audioSource != null && breakSound != null)
		{
			audioSource.pitch = pitch;
			audioSource.PlayOneShot(breakSound);
		}
	}

	[ContextMenu("Test Break")]
	public void TestBreak()
	{
		TakeDamage(1);
	}

	[ContextMenu("Force Break")]
	public void TestForceBreak()
	{
		ForceBreak();
	}

	private void OnDrawGizmosSelected()
	{
		if (spawnDebris)
		{
			Gizmos.color = Color.yellow;
			Gizmos.DrawWireSphere(base.transform.position, 0.5f);
		}
		if (objectRenderer != null)
		{
			Gizmos.color = (isBroken ? Color.red : Color.green);
			Gizmos.DrawWireCube(objectRenderer.bounds.center, objectRenderer.bounds.size);
		}
	}
}
