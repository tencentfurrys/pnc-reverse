using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class DamageNumberManager : MonoBehaviour
{
	private struct BufferedDamage
	{
		public int totalDamage;

		public bool isCritical;

		public Vector3 position;
	}

	[Header("Prefab")]
	[SerializeField]
	private GameObject damageNumberPrefab;

	[Header("Pool Settings")]
	[SerializeField]
	private int poolSize = 20;

	[Header("Spawn Settings")]
	[SerializeField]
	private Vector3 spawnOffset = new Vector3(0f, 1.5f, 0f);

	[SerializeField]
	private float horizontalRandomOffset = 0.5f;

	[Header("Colors")]
	[SerializeField]
	private Color normalDamageColor = Color.white;

	[SerializeField]
	private Color criticalDamageColor = Color.red;

	[SerializeField]
	private Color healColor = Color.green;

	[SerializeField]
	private Color heatDrainColor = new Color(0.35f, 0.85f, 1f);

	[Header("Sounds")]
	[SerializeField]
	private AudioSource audioSource;

	[SerializeField]
	private AudioClip critSound;

	[SerializeField]
	private AudioClip zeroDamageSound;

	[SerializeField]
	private AudioMixerGroup sfxMixerGroup;

	private Queue<DamageNumber> pool = new Queue<DamageNumber>();

	private Dictionary<Vector3, BufferedDamage> damageBuffer = new Dictionary<Vector3, BufferedDamage>();

	private readonly List<BufferedDamage> damageListCache = new List<BufferedDamage>();

	private bool nextCritical;

	private bool isPrewarmed;

	public static DamageNumberManager Instance { get; private set; }

	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Object.Destroy(gameObject);
			return;
		}
		Instance = this;
		if (audioSource == null)
		{
			audioSource = GetComponent<AudioSource>();
		}
		if (audioSource == null)
		{
			audioSource = gameObject.AddComponent<AudioSource>();
		}
		if (audioSource != null && sfxMixerGroup != null)
		{
			audioSource.outputAudioMixerGroup = sfxMixerGroup;
		}
	}

	private void Start()
	{
		if (!isPrewarmed)
		{
			StartCoroutine(PrewarmPool());
		}
	}

	public IEnumerator PrewarmPool()
	{
		if (isPrewarmed || damageNumberPrefab == null)
		{
			yield break;
		}
		isPrewarmed = true;
		for (int i = 0; i < poolSize; i++)
		{
			CreatePooledObject();
			if (i % 3 == 2)
			{
				yield return null;
			}
		}
	}

	private DamageNumber CreatePooledObject()
	{
		GameObject gameObject = Object.Instantiate(damageNumberPrefab, transform);
		gameObject.SetActive(value: false);
		DamageNumber damageNumber = gameObject.GetComponent<DamageNumber>();
		if (damageNumber == null)
		{
			damageNumber = gameObject.AddComponent<DamageNumber>();
		}
		pool.Enqueue(damageNumber);
		return damageNumber;
	}

	public void SetNextCritical(bool isCritical)
	{
		nextCritical = isCritical;
	}

	public void SpawnDamageNumber(Vector3 position, int damage, bool isCritical = false)
	{
		if (!(damageNumberPrefab == null))
		{
			isCritical = isCritical || nextCritical;
			nextCritical = false;
			if (damageBuffer.TryGetValue(position, out var value))
			{
				value.totalDamage += damage;
				value.isCritical |= isCritical;
				damageBuffer[position] = value;
			}
			else
			{
				damageBuffer[position] = new BufferedDamage
				{
					totalDamage = damage,
					isCritical = isCritical,
					position = position
				};
			}
		}
	}

	private void LateUpdate()
	{
		if (damageBuffer.Count != 0)
		{
			bool playedCritSound = false;
			damageListCache.Clear();
			damageListCache.AddRange(damageBuffer.Values);
			damageBuffer.Clear();
			for (int i = 0; i < damageListCache.Count; i++)
			{
				BufferedDamage bufferedDamage = damageListCache[i];
				SpawnDamageNumberImmediate(bufferedDamage.position, bufferedDamage.totalDamage, bufferedDamage.isCritical, ref playedCritSound);
			}
		}
	}

	private void SpawnDamageNumberImmediate(Vector3 position, int damage, bool isCritical, ref bool playedCritSound)
	{
		DamageNumber fromPool = GetFromPool();
		if (!(fromPool == null))
		{
			Vector3 vector = new Vector3(Random.Range(0f - horizontalRandomOffset, horizontalRandomOffset), 0f, Random.Range(0f - horizontalRandomOffset, horizontalRandomOffset));
			Vector3 position2 = position + spawnOffset + vector;
			fromPool.transform.position = position2;
			Color color = (isCritical ? criticalDamageColor : normalDamageColor);
			if (isCritical && !playedCritSound && audioSource != null && critSound != null)
			{
				audioSource.PlayOneShot(critSound);
				playedCritSound = true;
			}
			if (damage == 0 && !playedCritSound && audioSource != null && zeroDamageSound != null)
			{
				audioSource.PlayOneShot(zeroDamageSound);
			}
			fromPool.gameObject.SetActive(value: true);
			fromPool.Initialize(damage, color, isCritical);
		}
	}

	private DamageNumber GetFromPool()
	{
		if (pool.Count > 0)
		{
			return pool.Dequeue();
		}
		return CreatePooledObject();
	}

	public void SpawnHealNumber(Vector3 position, int amount)
	{
		if (!(damageNumberPrefab == null) && amount > 0)
		{
			StartCoroutine(SpawnHealNumberDelayed(position, amount));
		}
	}

	private IEnumerator SpawnHealNumberDelayed(Vector3 position, int amount)
	{
		yield return new WaitForSeconds(0.3f);
		DamageNumber fromPool = GetFromPool();
		if (!(fromPool == null))
		{
			Vector3 vector = new Vector3(Random.Range(0f - horizontalRandomOffset, horizontalRandomOffset), 0f, Random.Range(0f - horizontalRandomOffset, horizontalRandomOffset));
			fromPool.transform.position = position + spawnOffset + vector;
			fromPool.gameObject.SetActive(value: true);
			fromPool.Initialize($"+{amount}", healColor);
		}
	}

	public void SpawnHeatDrainNumber(Vector3 position, int amount, bool isCritical = false)
	{
		if (!(damageNumberPrefab == null) && amount > 0)
		{
			DamageNumber fromPool = GetFromPool();
			if (!(fromPool == null))
			{
				Vector3 vector = new Vector3(Random.Range(0f - horizontalRandomOffset, horizontalRandomOffset), 0f, Random.Range(0f - horizontalRandomOffset, horizontalRandomOffset));
				fromPool.transform.position = position + spawnOffset + vector;
				fromPool.gameObject.SetActive(value: true);
				fromPool.Initialize($"-{amount}", heatDrainColor, isCritical ? 1.5f : 1f);
			}
		}
	}

	public void ReturnToPool(DamageNumber damageNumber)
	{
		if (!(damageNumber == null))
		{
			damageNumber.ResetState();
			damageNumber.gameObject.SetActive(value: false);
			pool.Enqueue(damageNumber);
		}
	}
}
