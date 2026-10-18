using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SecretDoorButton : MonoBehaviour
{
	[Header("Interaction Settings")]
	[SerializeField]
	private KeyCode activationKey = KeyCode.E;

	[SerializeField]
	private float interactionRange = 2f;

	[SerializeField]
	private LayerMask playerLayer = 1;

	[Header("Button Targets")]
	[SerializeField]
	private SecretDoorBlock[] connectedBlocks;

	[SerializeField]
	private bool activateAllBlocks = true;

	[Header("Button State")]
	[SerializeField]
	private bool oneTimeUse;

	[SerializeField]
	private bool toggleMode;

	[SerializeField]
	private float buttonPressDepth = 0.2f;

	[Header("Visual Feedback")]
	[SerializeField]
	private GameObject interactionPrompt;

	[SerializeField]
	private Material unpressedMaterial;

	[SerializeField]
	private Material pressedMaterial;

	[SerializeField]
	private Material highlightMaterial;

	[SerializeField]
	private bool animateButtonPress = true;

	[Header("Audio")]
	[SerializeField]
	private AudioSource audioSource;

	[SerializeField]
	private AudioClip buttonPressSound;

	[SerializeField]
	private AudioClip buttonReleaseSound;

	[SerializeField]
	private AudioClip errorSound;

	[Header("Effects")]
	[SerializeField]
	private GameObject pressEffect;

	[SerializeField]
	private bool createSparkEffect = true;

	[Header("Animator")]
	[Tooltip("Play Animator states for the press/release instead of the procedural button movement.")]
	[SerializeField]
	private bool useAnimator;

	[SerializeField]
	private Animator buttonAnimator;

	[SerializeField]
	private string pressStateName = "ButtonPress";

	[SerializeField]
	private string releaseStateName = "ButtonRelease";

	[Header("Debug")]
	[SerializeField]
	private bool debugMode = true;

	[SerializeField]
	private bool showGizmos = true;

	private bool isPressed;

	private bool hasBeenUsed;

	private bool playerInRange;

	private Vector3 originalPosition;

	private Vector3 pressedPosition;

	private Transform playerTransform;

	private Renderer buttonRenderer;

	private Material originalMaterial;

	public bool IsPressed => isPressed;

	public bool CanInteract
	{
		get
		{
			if (playerInRange)
			{
				if (oneTimeUse)
				{
					return !hasBeenUsed;
				}
				return true;
			}
			return false;
		}
	}

	private void Awake()
	{
		originalPosition = base.transform.localPosition;
		pressedPosition = originalPosition - base.transform.up * buttonPressDepth;
		buttonRenderer = GetComponent<Renderer>();
		if (buttonRenderer != null)
		{
			originalMaterial = ((unpressedMaterial != null) ? unpressedMaterial : buttonRenderer.material);
		}
		if (useAnimator && buttonAnimator == null)
		{
			buttonAnimator = GetComponent<Animator>();
		}
		if (audioSource == null)
		{
			audioSource = GetComponent<AudioSource>();
			if (audioSource == null)
			{
				audioSource = base.gameObject.AddComponent<AudioSource>();
			}
		}
		audioSource.playOnAwake = false;
		if (interactionPrompt != null)
		{
			interactionPrompt.SetActive(value: false);
		}
		ValidateConnectedBlocks();
		Awake_SetupTrigger();
	}

	private void Awake_SetupTrigger()
	{
		SphereCollider sphereCollider = base.gameObject.AddComponent<SphereCollider>();
		sphereCollider.isTrigger = true;
		sphereCollider.radius = interactionRange;
	}

	private void Update()
	{
		HandleInput();
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other.CompareTag("Player"))
		{
			playerInRange = true;
			playerTransform = other.transform;
			OnPlayerEnterRange();
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (other.CompareTag("Player"))
		{
			playerInRange = false;
			playerTransform = null;
			OnPlayerExitRange();
		}
	}

	private void HandleInput()
	{
		if (CanInteract && InputManager.Interact != null && InputManager.Interact.WasPressedThisFrame())
		{
			ActivateButton();
		}
	}

	private void OnPlayerEnterRange()
	{
		if (interactionPrompt != null)
		{
			interactionPrompt.SetActive(value: true);
			UpdatePromptText();
		}
		if (highlightMaterial != null && buttonRenderer != null && !isPressed)
		{
			buttonRenderer.material = highlightMaterial;
		}
	}

	private void OnPlayerExitRange()
	{
		if (interactionPrompt != null)
		{
			interactionPrompt.SetActive(value: false);
		}
		RestoreButtonMaterial();
	}

	private void UpdatePromptText()
	{
		if (!(interactionPrompt == null))
		{
			string text = "";
			text = ((oneTimeUse && hasBeenUsed) ? "Button already used" : (HasValidTargets() ? $"Press {activationKey} to activate" : "Button not connected"));
			Text componentInChildren = interactionPrompt.GetComponentInChildren<Text>();
			if (componentInChildren != null)
			{
				componentInChildren.text = text;
			}
			TextMeshProUGUI componentInChildren2 = interactionPrompt.GetComponentInChildren<TextMeshProUGUI>();
			if (componentInChildren2 != null)
			{
				componentInChildren2.text = text;
			}
		}
	}

	public void ActivateButton()
	{
		if (!CanInteract)
		{
			PlaySound(errorSound);
			return;
		}
		if (!HasValidTargets())
		{
			PlaySound(errorSound);
			return;
		}
		if (oneTimeUse)
		{
			hasBeenUsed = true;
		}
		if (toggleMode)
		{
			if (isPressed)
			{
				ReleaseButton();
			}
			else
			{
				PressButton();
			}
		}
		else
		{
			StartCoroutine(PressAndReleaseButton());
		}
		ActivateConnectedBlocks();
	}

	private void PressButton()
	{
		isPressed = true;
		if (useAnimator && buttonAnimator != null)
		{
			if (!string.IsNullOrEmpty(pressStateName))
			{
				buttonAnimator.Play(pressStateName);
			}
		}
		else if (animateButtonPress)
		{
			StartCoroutine(AnimateButtonToPosition(pressedPosition));
		}
		if (pressedMaterial != null && buttonRenderer != null)
		{
			buttonRenderer.material = pressedMaterial;
		}
		PlaySound(buttonPressSound);
		CreatePressEffect();
	}

	private void ReleaseButton()
	{
		isPressed = false;
		if (useAnimator && buttonAnimator != null)
		{
			if (!string.IsNullOrEmpty(releaseStateName))
			{
				buttonAnimator.Play(releaseStateName);
			}
		}
		else if (animateButtonPress)
		{
			StartCoroutine(AnimateButtonToPosition(originalPosition));
		}
		RestoreButtonMaterial();
		PlaySound(buttonReleaseSound);
	}

	private IEnumerator PressAndReleaseButton()
	{
		PressButton();
		yield return new WaitForSeconds(0.5f);
		if (!toggleMode)
		{
			ReleaseButton();
		}
	}

	private IEnumerator AnimateButtonToPosition(Vector3 targetPosition)
	{
		Vector3 startPosition = base.transform.localPosition;
		float duration = 0.2f;
		float elapsed = 0f;
		while (elapsed < duration)
		{
			elapsed += Time.deltaTime;
			float num = elapsed / duration;
			num = num * num * (3f - 2f * num);
			base.transform.localPosition = Vector3.Lerp(startPosition, targetPosition, num);
			yield return null;
		}
		base.transform.localPosition = targetPosition;
	}

	private void RestoreButtonMaterial()
	{
		if (!(buttonRenderer == null))
		{
			if (isPressed && pressedMaterial != null)
			{
				buttonRenderer.material = pressedMaterial;
			}
			else if (playerInRange && highlightMaterial != null)
			{
				buttonRenderer.material = highlightMaterial;
			}
			else if (originalMaterial != null)
			{
				buttonRenderer.material = originalMaterial;
			}
		}
	}

	private void ActivateConnectedBlocks()
	{
		if (connectedBlocks == null || connectedBlocks.Length == 0)
		{
			return;
		}
		SecretDoorBlock[] array;
		if (activateAllBlocks)
		{
			array = connectedBlocks;
			foreach (SecretDoorBlock secretDoorBlock in array)
			{
				if (secretDoorBlock != null)
				{
					secretDoorBlock.ActivateBlock();
				}
			}
			return;
		}
		array = connectedBlocks;
		foreach (SecretDoorBlock secretDoorBlock2 in array)
		{
			if (secretDoorBlock2 != null && secretDoorBlock2.CanActivate)
			{
				secretDoorBlock2.ActivateBlock();
				break;
			}
		}
	}

	private bool HasValidTargets()
	{
		if (connectedBlocks == null || connectedBlocks.Length == 0)
		{
			return false;
		}
		SecretDoorBlock[] array = connectedBlocks;
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i] != null)
			{
				return true;
			}
		}
		return false;
	}

	private void ValidateConnectedBlocks()
	{
		if (connectedBlocks == null || connectedBlocks.Length == 0)
		{
			return;
		}
		int num = 0;
		SecretDoorBlock[] array = connectedBlocks;
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i] != null)
			{
				num++;
			}
		}
	}

	private void CreatePressEffect()
	{
		if (pressEffect != null)
		{
			Object.Destroy(Object.Instantiate(pressEffect, base.transform.position, base.transform.rotation), 3f);
		}
		if (createSparkEffect)
		{
			StartCoroutine(CreateSparkParticles());
		}
	}

	private IEnumerator CreateSparkParticles()
	{
		int sparkCount = Random.Range(5, 10);
		for (int i = 0; i < sparkCount; i++)
		{
			StartCoroutine(CreateSpark());
			yield return new WaitForSeconds(0.05f);
		}
	}

	private IEnumerator CreateSpark()
	{
		GameObject spark = GameObject.CreatePrimitive(PrimitiveType.Sphere);
		spark.transform.localScale = Vector3.one * Random.Range(0.02f, 0.05f);
		spark.transform.position = base.transform.position + Random.insideUnitSphere * 0.3f;
		Renderer sparkRenderer = spark.GetComponent<Renderer>();
		sparkRenderer.material.color = Color.yellow;
		sparkRenderer.material.SetColor("_EmissionColor", Color.yellow);
		Object.Destroy(spark.GetComponent<Collider>());
		Vector3 velocity = Random.insideUnitSphere * 3f;
		float lifetime = Random.Range(0.5f, 1f);
		float elapsed = 0f;
		while (elapsed < lifetime && spark != null)
		{
			elapsed += Time.deltaTime;
			spark.transform.position += velocity * Time.deltaTime;
			velocity.y -= 9.81f * Time.deltaTime * 0.5f;
			Color color = sparkRenderer.material.color;
			color.a = 1f - elapsed / lifetime;
			sparkRenderer.material.color = color;
			yield return null;
		}
		if (spark != null)
		{
			Object.Destroy(spark);
		}
	}

	private void PlaySound(AudioClip clip)
	{
		if (clip != null && audioSource != null)
		{
			audioSource.clip = clip;
			audioSource.Play();
		}
	}

	public void ConnectToBlock(SecretDoorBlock block)
	{
		if (!(block == null))
		{
			if (connectedBlocks == null)
			{
				connectedBlocks = new SecretDoorBlock[1] { block };
				return;
			}
			SecretDoorBlock[] array = new SecretDoorBlock[connectedBlocks.Length + 1];
			connectedBlocks.CopyTo(array, 0);
			array[connectedBlocks.Length] = block;
			connectedBlocks = array;
		}
	}

	public void ResetButton()
	{
		hasBeenUsed = false;
		if (toggleMode && isPressed)
		{
			ReleaseButton();
		}
		base.transform.localPosition = originalPosition;
		RestoreButtonMaterial();
	}

	private void OnDrawGizmosSelected()
	{
		if (!showGizmos)
		{
			return;
		}
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireSphere(base.transform.position, interactionRange);
		if (connectedBlocks != null)
		{
			Gizmos.color = Color.cyan;
			SecretDoorBlock[] array = connectedBlocks;
			foreach (SecretDoorBlock secretDoorBlock in array)
			{
				if (secretDoorBlock != null)
				{
					Gizmos.DrawLine(base.transform.position, secretDoorBlock.transform.position);
				}
			}
		}
		Gizmos.color = Color.red;
		Gizmos.DrawWireCube(base.transform.position - base.transform.up * buttonPressDepth, base.transform.localScale * 0.9f);
	}

	[ContextMenu("Test Press")]
	public void TestPress()
	{
		ActivateButton();
	}

	[ContextMenu("Reset Button")]
	public void TestReset()
	{
		ResetButton();
	}
}
