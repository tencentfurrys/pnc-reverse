using UnityEngine;
using UnityEngine.Events;

public class AudioTrigger : MonoBehaviour
{
	[Header("Audio Settings")]
	[SerializeField]
	private AudioClip audioClip;

	[SerializeField]
	private AudioSource audioSource;

	[Header("Trigger Settings")]
	[SerializeField]
	private BoxCollider triggerBox;

	[SerializeField]
	private string playerTag = "Player";

	[SerializeField]
	private bool playOnce;

	[SerializeField]
	private bool resetOnExit;

	[SerializeField]
	private float cooldownTime;

	[Header("Volume Settings")]
	[SerializeField]
	private float volume = 1f;

	[SerializeField]
	private bool randomizePitch;

	[SerializeField]
	private float minPitch = 0.8f;

	[SerializeField]
	private float maxPitch = 1.2f;

	[Header("Events")]
	public UnityEvent OnTriggerEntered;

	public UnityEvent OnTriggerExited;

	private bool hasTriggered;

	private float lastTriggerTime;

	private void Awake()
	{
		if (triggerBox != null)
		{
			triggerBox.isTrigger = true;
			TriggerHandler triggerHandler = triggerBox.GetComponent<TriggerHandler>();
			if (triggerHandler == null)
			{
				triggerHandler = triggerBox.gameObject.AddComponent<TriggerHandler>();
			}
			triggerHandler.Initialize(this);
		}
		if (audioSource == null)
		{
			audioSource = base.gameObject.AddComponent<AudioSource>();
			audioSource.playOnAwake = false;
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		HandleTriggerEnter(other);
	}

	private void OnTriggerExit(Collider other)
	{
		HandleTriggerExit(other);
	}

	public void HandleTriggerEnter(Collider other)
	{
		if (other.CompareTag(playerTag) && (!playOnce || !hasTriggered) && !(Time.time - lastTriggerTime < cooldownTime))
		{
			PlaySound();
			hasTriggered = true;
			lastTriggerTime = Time.time;
			OnTriggerEntered?.Invoke();
		}
	}

	public void HandleTriggerExit(Collider other)
	{
		if (other.CompareTag(playerTag))
		{
			if (resetOnExit)
			{
				hasTriggered = false;
			}
			OnTriggerExited?.Invoke();
		}
	}

	private void PlaySound()
	{
		if (!(audioClip == null) && !(audioSource == null))
		{
			audioSource.volume = volume;
			if (randomizePitch)
			{
				audioSource.pitch = Random.Range(minPitch, maxPitch);
			}
			else
			{
				audioSource.pitch = 1f;
			}
			audioSource.PlayOneShot(audioClip, volume);
		}
	}

	public void TriggerSound()
	{
		PlaySound();
	}

	public void ResetTrigger()
	{
		hasTriggered = false;
		lastTriggerTime = 0f;
	}

	private void OnDrawGizmos()
	{
		if (triggerBox != null)
		{
			Gizmos.color = (hasTriggered ? Color.red : Color.green);
			Gizmos.matrix = triggerBox.transform.localToWorldMatrix;
			Gizmos.DrawWireCube(triggerBox.center, triggerBox.size);
		}
	}
}
