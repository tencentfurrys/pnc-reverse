using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace PixelCrushers.GridController;

public class AudioPool : MonoBehaviour
{
	[SerializeField]
	private int poolSize = 16;

	[SerializeField]
	private AudioMixerGroup sfxMixerGroup;

	private readonly Queue<AudioSource> pool = new Queue<AudioSource>();

	private Transform poolParent;

	public static AudioPool Instance { get; private set; }

	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Object.Destroy(gameObject);
			return;
		}
		Instance = this;
		Object.DontDestroyOnLoad(gameObject);
		CreatePoolParent();
	}

	private void OnDestroy()
	{
		if (Instance == this)
		{
			Instance = null;
		}
	}

	private void Start()
	{
		StartCoroutine(Prewarm(poolSize));
	}

	private void CreatePoolParent()
	{
		GameObject gameObject = new GameObject("AudioPool");
		gameObject.transform.SetParent(transform);
		poolParent = gameObject.transform;
	}

	private AudioSource CreatePooledAudioSource()
	{
		GameObject gameObject = new GameObject("PooledAudio");
		gameObject.transform.SetParent(poolParent);
		gameObject.SetActive(value: false);
		AudioSource audioSource = gameObject.AddComponent<AudioSource>();
		audioSource.playOnAwake = false;
		audioSource.rolloffMode = AudioRolloffMode.Linear;
		audioSource.minDistance = 1f;
		audioSource.maxDistance = 20f;
		pool.Enqueue(audioSource);
		return audioSource;
	}

	public void PlayAtPoint(AudioClip clip, Vector3 position, float volume = 1f, float spatialBlend = 1f)
	{
		if (!(clip == null))
		{
			AudioSource fromPool = GetFromPool();
			if (!(fromPool == null))
			{
				fromPool.transform.position = position;
				fromPool.clip = clip;
				fromPool.volume = volume;
				fromPool.spatialBlend = spatialBlend;
				fromPool.outputAudioMixerGroup = sfxMixerGroup;
				fromPool.gameObject.SetActive(value: true);
				fromPool.Play();
				StartCoroutine(ReturnAfterClip(fromPool, clip.length));
			}
		}
	}

	private AudioSource GetFromPool()
	{
		while (pool.Count > 0)
		{
			AudioSource audioSource = pool.Dequeue();
			if (audioSource != null)
			{
				return audioSource;
			}
		}
		return CreatePooledAudioSource();
	}

	private IEnumerator ReturnAfterClip(AudioSource source, float clipLength)
	{
		yield return new WaitForSeconds(clipLength + 0.1f);
		if (!(source == null))
		{
			source.Stop();
			source.clip = null;
			source.gameObject.SetActive(value: false);
			source.transform.SetParent(poolParent);
			pool.Enqueue(source);
		}
	}

	public IEnumerator Prewarm(int count)
	{
		for (int i = 0; i < count; i++)
		{
			CreatePooledAudioSource();
			if (i % 5 == 4)
			{
				yield return null;
			}
		}
	}
}
