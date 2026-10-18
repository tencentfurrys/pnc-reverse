using System;
using System.Collections;
using PixelCrushers.GridController;
using PixelCrushers.GridController.Glyphs;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CurrencyChest : MonoBehaviour, IPlayerInteractable
{
	[Header("Currency Settings")]
	[SerializeField]
	private int minCurrencyValue = 10;

	[SerializeField]
	private int maxCurrencyValue = 50;

	[SerializeField]
	private int currencyCount = 5;

	[Tooltip("Random variance in currency count (e.g., 2 means ±2 from currencyCount)")]
	[SerializeField]
	private int currencyCountVariance = 2;

	[Header("Currency Prefab")]
	[SerializeField]
	private GameObject currencyPrefab;

	[Header("Spawn Settings")]
	[SerializeField]
	private Vector3 spawnOffset = new Vector3(0f, 1f, 0f);

	[SerializeField]
	private float minLaunchAngle = 30f;

	[SerializeField]
	private float maxLaunchAngle = 60f;

	[SerializeField]
	private float minLaunchForce = 3f;

	[SerializeField]
	private float maxLaunchForce = 5f;

	[SerializeField]
	private float arcHeight = 2f;

	[SerializeField]
	private float spawnDelay = 0.05f;

	[Header("Interaction Settings")]
	[SerializeField]
	private float interactionRange = 3f;

	[SerializeField]
	private bool oneTimeUse = true;

	[Header("Visual Feedback")]
	[SerializeField]
	private GameObject openEffect;

	[SerializeField]
	private AudioClip openSound;

	[SerializeField]
	private Animator chestAnimator;

	[SerializeField]
	private string openAnimationTrigger = "Open";

	[SerializeField]
	private bool destroyAfterOpen;

	[SerializeField]
	private float destroyDelay = 2f;

	[Header("UI")]
	[SerializeField]
	private GameObject interactPrompt;

	[SerializeField]
	private string promptText = "Press E to Open";

	[Header("Debug")]
	[SerializeField]
	private bool showInteractionRange = true;

	private bool isBeingLookedAt;

	private GameObject player;

	private bool hasBeenOpened;

	private AudioSource audioSource;

	public bool CanInteract
	{
		get
		{
			if (hasBeenOpened)
			{
				return !oneTimeUse;
			}
			return true;
		}
	}

	public float InteractionRange => interactionRange;

	public bool IsOpened => hasBeenOpened;

	private void Awake()
	{
		audioSource = GetComponent<AudioSource>();
		if (interactPrompt != null)
		{
			interactPrompt.SetActive(value: false);
		}
	}

	private void Start()
	{
		UpdatePromptText();
	}

	private void Update()
	{
	}

	public void OnLookedAt(GameObject playerObj)
	{
		player = playerObj;
		isBeingLookedAt = true;
		if (interactPrompt != null)
		{
			interactPrompt.SetActive(value: true);
		}
	}

	public void OnLookedAway()
	{
		isBeingLookedAt = false;
		player = null;
		if (interactPrompt != null)
		{
			interactPrompt.SetActive(value: false);
		}
	}

	public void OnInteracted()
	{
		OpenChest();
	}

	private void OpenChest()
	{
		if (hasBeenOpened && oneTimeUse)
		{
			return;
		}
		hasBeenOpened = true;
		if (interactPrompt != null)
		{
			interactPrompt.SetActive(value: false);
		}
		if (chestAnimator != null && !string.IsNullOrEmpty(openAnimationTrigger))
		{
			chestAnimator.SetTrigger(openAnimationTrigger);
		}
		if (openSound != null)
		{
			if (audioSource != null)
			{
				audioSource.PlayOneShot(openSound);
			}
			else
			{
				AudioSource.PlayClipAtPoint(openSound, transform.position);
			}
		}
		if (openEffect != null)
		{
			UnityEngine.Object.Destroy(UnityEngine.Object.Instantiate(openEffect, transform.position + spawnOffset, Quaternion.identity), 3f);
		}
		StartCoroutine(SpawnCurrency());
		if (destroyAfterOpen)
		{
			UnityEngine.Object.Destroy(gameObject, destroyDelay);
		}
	}

	private IEnumerator SpawnCurrency()
	{
		int actualCount = currencyCount;
		if (currencyCountVariance > 0)
		{
			actualCount += UnityEngine.Random.Range(-currencyCountVariance, currencyCountVariance + 1);
			actualCount = Mathf.Max(1, actualCount);
		}
		Vector3 spawnPosition = transform.position + spawnOffset;
		for (int i = 0; i < actualCount; i++)
		{
			SpawnSingleCurrency(spawnPosition);
			if (spawnDelay > 0f)
			{
				yield return new WaitForSeconds(spawnDelay);
			}
		}
	}

	private void SpawnSingleCurrency(Vector3 basePosition)
	{
		int num = UnityEngine.Random.Range(minCurrencyValue, maxCurrencyValue + 1);
		GameObject gameObject;
		if (currencyPrefab != null)
		{
			gameObject = ((!(PickupPoolManager.Instance != null)) ? UnityEngine.Object.Instantiate(currencyPrefab, basePosition, Quaternion.identity) : PickupPoolManager.Instance.GetCurrencyPickup(basePosition, Quaternion.identity));
			CurrencyPickup component = gameObject.GetComponent<CurrencyPickup>();
			if (component != null)
			{
				component.SetCurrencyValue(num);
			}
		}
		else
		{
			gameObject = CurrencyPickup.CreateCurrencyPickup(basePosition, num);
		}
		Rigidbody component2 = gameObject.GetComponent<Rigidbody>();
		if (component2 != null)
		{
			float f = UnityEngine.Random.Range(0f, 360f) * (MathF.PI / 180f);
			float f2 = UnityEngine.Random.Range(minLaunchAngle, maxLaunchAngle) * (MathF.PI / 180f);
			float num2 = UnityEngine.Random.Range(minLaunchForce, maxLaunchForce);
			Vector3 vector = new Vector3(Mathf.Cos(f), 0f, Mathf.Sin(f)) * num2 * Mathf.Cos(f2);
			float num3 = num2 * Mathf.Sin(f2) + arcHeight;
			Vector3 linearVelocity = vector + Vector3.up * num3;
			component2.linearVelocity = linearVelocity;
			Vector3 torque = new Vector3(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f)) * 3f;
			component2.AddTorque(torque, ForceMode.Impulse);
		}
	}

	private void UpdatePromptText()
	{
		if (interactPrompt == null)
		{
			return;
		}
		InputPromptLabel componentInChildren = interactPrompt.GetComponentInChildren<InputPromptLabel>(includeInactive: true);
		if (componentInChildren != null)
		{
			componentInChildren.Refresh();
			return;
		}
		TextMeshProUGUI textMeshProUGUI = interactPrompt.GetComponent<TextMeshProUGUI>();
		if (textMeshProUGUI == null)
		{
			textMeshProUGUI = interactPrompt.GetComponentInChildren<TextMeshProUGUI>();
		}
		if (textMeshProUGUI != null)
		{
			textMeshProUGUI.text = promptText;
			return;
		}
		Text text = interactPrompt.GetComponent<Text>();
		if (text == null)
		{
			text = interactPrompt.GetComponentInChildren<Text>();
		}
		if (text != null)
		{
			text.text = promptText;
		}
	}

	public void Open()
	{
		OpenChest();
	}

	public void Reset()
	{
		if (!oneTimeUse)
		{
			hasBeenOpened = false;
		}
	}

	public void SetCurrencyRange(int min, int max)
	{
		minCurrencyValue = min;
		maxCurrencyValue = max;
	}

	public void SetCurrencyCount(int count, int variance = 0)
	{
		currencyCount = count;
		currencyCountVariance = variance;
	}

	private void OnDrawGizmosSelected()
	{
		if (showInteractionRange)
		{
			Collider component = GetComponent<Collider>();
			if (component != null && component is SphereCollider sphereCollider)
			{
				Gizmos.color = Color.cyan;
				Gizmos.DrawWireSphere(transform.position, sphereCollider.radius);
			}
			else
			{
				Gizmos.color = Color.cyan;
				Gizmos.DrawWireSphere(transform.position, interactionRange);
			}
			Vector3 vector = transform.position + spawnOffset;
			Gizmos.color = Color.green;
			Gizmos.DrawWireSphere(vector, 0.2f);
			Gizmos.DrawLine(transform.position, vector);
			Gizmos.color = Color.yellow;
			int num = 8;
			for (int i = 0; i < num; i++)
			{
				float f = 360f / (float)num * (float)i * (MathF.PI / 180f);
				Vector3 vector2 = new Vector3(Mathf.Cos(f), 0f, Mathf.Sin(f));
				float num2 = (minLaunchForce + maxLaunchForce) / 2f;
				float f2 = (minLaunchAngle + maxLaunchAngle) / 2f * (MathF.PI / 180f);
				Vector3 to = vector + vector2 * num2 * Mathf.Cos(f2) * 0.5f;
				Gizmos.DrawLine(vector, to);
			}
		}
	}

	private void OnValidate()
	{
		if (minCurrencyValue < 0)
		{
			minCurrencyValue = 0;
		}
		if (maxCurrencyValue < minCurrencyValue)
		{
			maxCurrencyValue = minCurrencyValue;
		}
		if (currencyCount < 0)
		{
			currencyCount = 1;
		}
		if (currencyCountVariance < 0)
		{
			currencyCountVariance = 0;
		}
	}

	[ContextMenu("Open Chest (Play Mode Only)")]
	private void TestOpenChest()
	{
		if (Application.isPlaying)
		{
			OpenChest();
		}
	}
}
