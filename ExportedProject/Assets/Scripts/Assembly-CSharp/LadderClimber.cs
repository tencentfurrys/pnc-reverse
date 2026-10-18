using System;
using System.Collections;
using PixelCrushers.GridController;
using PixelCrushers.GridController.Glyphs;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LadderClimber : MonoBehaviour
{
	[Header("Interaction Settings")]
	[SerializeField]
	private Transform destinationPoint;

	[SerializeField]
	private Vector3 destinationOffset = Vector3.zero;

	[SerializeField]
	private bool useDestinationTag = true;

	[SerializeField]
	private string destinationTag = "LadderTop";

	[Header("Climbing Animation")]
	[SerializeField]
	private float climbHeight = 5f;

	[SerializeField]
	private float climbDuration = 2f;

	[SerializeField]
	private AnimationCurve climbCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

	[SerializeField]
	private bool usePhysicsMovement;

	[Header("Visual Effects")]
	[SerializeField]
	private GameObject climbStartEffect;

	[SerializeField]
	private GameObject climbEndEffect;

	[SerializeField]
	private bool bobDuringClimb = true;

	[SerializeField]
	private float bobAmplitude = 0.1f;

	[SerializeField]
	private float bobFrequency = 3f;

	[Header("Audio")]
	[SerializeField]
	private AudioSource audioSource;

	[SerializeField]
	private AudioClip climbStartSound;

	[SerializeField]
	private AudioClip climbingLoopSound;

	[SerializeField]
	private AudioClip climbEndSound;

	[SerializeField]
	private bool loopClimbingSound = true;

	[Header("Fade Transition")]
	[SerializeField]
	private bool useFadeTransition = true;

	[SerializeField]
	private float fadeOutDuration = 0.5f;

	[SerializeField]
	private float fadeInDuration = 0.5f;

	[SerializeField]
	private Color fadeColor = Color.black;

	[SerializeField]
	private float fadeStartDelay = 1f;

	[Header("Player Control")]
	[SerializeField]
	private bool disableMovementDuringClimb = true;

	[SerializeField]
	private bool disableCombatDuringClimb = true;

	[SerializeField]
	private bool lockCursorDuringClimb = true;

	[Header("UI Prompt")]
	[SerializeField]
	private GameObject interactionPrompt;

	[Tooltip("Wording for the prompt. If it contains {0}, it drives the InputPromptLabel template and {0} is replaced by the device glyph.")]
	[SerializeField]
	private string promptText = "{0}";

	[Header("Debug")]
	private bool isClimbing;

	private bool playerInRange;

	private GameObject currentPlayer;

	private Camera playerCamera;

	private CanvasGroup fadeCanvasGroup;

	private GameObject fadeCanvas;

	private Coroutine climbingSoundCoroutine;

	private Text cachedPromptText;

	private TMP_Text cachedPromptTMP;

	private InputPromptLabel cachedGlyphLabel;

	private bool promptShown;

	public bool IsCurrentlyClimbing => isClimbing;

	public bool PlayerInRange => playerInRange;

	private void Awake()
	{
		if (audioSource == null)
		{
			audioSource = GetComponent<AudioSource>();
		}
		if (audioSource == null)
		{
			audioSource = base.gameObject.AddComponent<AudioSource>();
		}
		playerCamera = Camera.main;
		if (playerCamera == null)
		{
			playerCamera = UnityEngine.Object.FindAnyObjectByType<Camera>();
		}
		if (useFadeTransition)
		{
			CreateFadeCanvas();
		}
		if (interactionPrompt != null)
		{
			interactionPrompt.SetActive(value: false);
			promptShown = false;
			cachedPromptText = interactionPrompt.GetComponentInChildren<Text>(includeInactive: true);
			cachedPromptTMP = interactionPrompt.GetComponentInChildren<TMP_Text>(includeInactive: true);
			cachedGlyphLabel = interactionPrompt.GetComponentInChildren<InputPromptLabel>(includeInactive: true);
			if (cachedGlyphLabel != null && !string.IsNullOrEmpty(promptText) && promptText.Contains("{0}"))
			{
				cachedGlyphLabel.SetTemplate(promptText);
			}
		}
		ValidateSetup();
	}

	private void Start()
	{
		if (destinationPoint == null && useDestinationTag)
		{
			FindDestinationPoint();
		}
	}

	private void Update()
	{
		if (playerInRange && !isClimbing && InputManager.Interact != null && InputManager.Interact.WasPressedThisFrame())
		{
			StartClimbing();
		}
		UpdateInteractionPrompt();
	}

	private void CreateFadeCanvas()
	{
		fadeCanvas = new GameObject("LadderFadeCanvas");
		fadeCanvas.transform.SetParent(base.transform);
		Canvas canvas = fadeCanvas.AddComponent<Canvas>();
		canvas.renderMode = RenderMode.ScreenSpaceOverlay;
		canvas.sortingOrder = 9999;
		fadeCanvasGroup = fadeCanvas.AddComponent<CanvasGroup>();
		fadeCanvasGroup.alpha = 0f;
		fadeCanvasGroup.blocksRaycasts = false;
		GameObject obj = new GameObject("FadeImage");
		obj.transform.SetParent(fadeCanvas.transform);
		obj.AddComponent<Image>().color = fadeColor;
		RectTransform component = obj.GetComponent<RectTransform>();
		component.anchorMin = Vector2.zero;
		component.anchorMax = Vector2.one;
		component.offsetMin = Vector2.zero;
		component.offsetMax = Vector2.zero;
	}

	private void FindDestinationPoint()
	{
		GameObject gameObject = GameObject.FindGameObjectWithTag(destinationTag);
		if (gameObject != null)
		{
			destinationPoint = gameObject.transform;
			return;
		}
		string text = base.gameObject.name;
		gameObject = GameObject.Find(text + "_Top");
		if (gameObject == null)
		{
			gameObject = GameObject.Find(text + "_Destination");
		}
		if (gameObject == null)
		{
			gameObject = GameObject.Find("LadderTop");
		}
		if (gameObject != null)
		{
			destinationPoint = gameObject.transform;
		}
	}

	private void ValidateSetup()
	{
		if (climbCurve == null || climbCurve.keys.Length == 0)
		{
			climbCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
		}
	}

	private void UpdateInteractionPrompt()
	{
		if (interactionPrompt == null)
		{
			return;
		}
		bool flag = playerInRange && !isClimbing && currentPlayer != null;
		if (flag == promptShown && interactionPrompt.activeSelf == flag)
		{
			return;
		}
		promptShown = flag;
		interactionPrompt.SetActive(flag);
		if (flag && !(cachedGlyphLabel != null))
		{
			if (cachedPromptText != null)
			{
				cachedPromptText.text = promptText;
			}
			if (cachedPromptTMP != null)
			{
				cachedPromptTMP.text = promptText;
			}
		}
	}

	private void StartClimbing()
	{
		if (!(currentPlayer == null) && !isClimbing)
		{
			StartCoroutine(ClimbSequence(currentPlayer));
		}
	}

	private IEnumerator ClimbSequence(GameObject player)
	{
		isClimbing = true;
		FirstPersonController fpsController = player.GetComponent<FirstPersonController>();
		CharacterController characterController = player.GetComponent<CharacterController>();
		DualWieldingSystem combatSystem = player.GetComponent<DualWieldingSystem>();
		bool originalFpsEnabled = fpsController != null && fpsController.enabled;
		bool originalCharEnabled = characterController != null && characterController.enabled;
		bool originalCombatEnabled = combatSystem != null && combatSystem.enabled;
		CursorLockMode originalCursorLock = Cursor.lockState;
		bool originalCursorVisible = Cursor.visible;
		if (disableMovementDuringClimb && fpsController != null)
		{
			fpsController.enabled = false;
		}
		if (disableCombatDuringClimb && combatSystem != null)
		{
			combatSystem.enabled = false;
		}
		if (lockCursorDuringClimb)
		{
			Cursor.lockState = CursorLockMode.Locked;
			Cursor.visible = false;
		}
		if (interactionPrompt != null)
		{
			interactionPrompt.SetActive(value: false);
			promptShown = false;
		}
		PlaySound(climbStartSound);
		if (climbStartEffect != null)
		{
			UnityEngine.Object.Destroy(UnityEngine.Object.Instantiate(climbStartEffect, player.transform.position, Quaternion.identity), 3f);
		}
		if (climbingLoopSound != null && loopClimbingSound)
		{
			climbingSoundCoroutine = StartCoroutine(PlayClimbingLoop());
		}
		Vector3 startPosition = player.transform.position;
		Vector3 targetPosition = startPosition + Vector3.up * climbHeight;
		Coroutine fadeCoroutine = null;
		if (useFadeTransition && fadeCanvasGroup != null)
		{
			fadeCoroutine = StartCoroutine(DelayedFadeOut());
		}
		float elapsed = 0f;
		while (elapsed < climbDuration)
		{
			elapsed += Time.deltaTime;
			float time = elapsed / climbDuration;
			float t = climbCurve.Evaluate(time);
			Vector3 vector = Vector3.Lerp(startPosition, targetPosition, t);
			if (bobDuringClimb)
			{
				float num = Mathf.Sin(elapsed * bobFrequency * MathF.PI * 2f) * bobAmplitude;
				vector.x += num;
			}
			if (usePhysicsMovement && characterController != null)
			{
				if (!characterController.enabled)
				{
					characterController.enabled = true;
				}
				Vector3 motion = vector - player.transform.position;
				characterController.Move(motion);
			}
			else
			{
				player.transform.position = vector;
			}
			yield return null;
		}
		if (climbingSoundCoroutine != null)
		{
			StopCoroutine(climbingSoundCoroutine);
			climbingSoundCoroutine = null;
		}
		if (fadeCoroutine != null)
		{
			yield return fadeCoroutine;
		}
		Vector3 finalDestination = GetDestinationPosition();
		if (usePhysicsMovement && characterController != null)
		{
			characterController.enabled = false;
			player.transform.position = finalDestination;
			yield return null;
			if (originalCharEnabled)
			{
				characterController.enabled = true;
			}
		}
		else
		{
			player.transform.position = finalDestination;
		}
		PlaySound(climbEndSound);
		if (climbEndEffect != null)
		{
			UnityEngine.Object.Destroy(UnityEngine.Object.Instantiate(climbEndEffect, finalDestination, Quaternion.identity), 3f);
		}
		if (useFadeTransition && fadeCanvasGroup != null)
		{
			yield return StartCoroutine(FadeIn());
		}
		if (disableMovementDuringClimb && fpsController != null)
		{
			fpsController.enabled = originalFpsEnabled;
		}
		if (disableCombatDuringClimb && combatSystem != null)
		{
			combatSystem.enabled = originalCombatEnabled;
		}
		if (lockCursorDuringClimb)
		{
			Cursor.lockState = originalCursorLock;
			Cursor.visible = originalCursorVisible;
		}
		if (characterController != null)
		{
			characterController.enabled = originalCharEnabled;
		}
		isClimbing = false;
	}

	private Vector3 GetDestinationPosition()
	{
		if (destinationPoint != null)
		{
			return destinationPoint.position + destinationOffset;
		}
		if (useDestinationTag)
		{
			FindDestinationPoint();
			if (destinationPoint != null)
			{
				return destinationPoint.position + destinationOffset;
			}
		}
		return base.transform.position + Vector3.up * (climbHeight + 2f) + destinationOffset;
	}

	private IEnumerator DelayedFadeOut()
	{
		yield return new WaitForSeconds(fadeStartDelay);
		yield return StartCoroutine(FadeOut());
	}

	private IEnumerator FadeOut()
	{
		if (!(fadeCanvasGroup == null))
		{
			float elapsed = 0f;
			fadeCanvasGroup.blocksRaycasts = true;
			while (elapsed < fadeOutDuration)
			{
				elapsed += Time.deltaTime;
				float alpha = Mathf.Clamp01(elapsed / fadeOutDuration);
				fadeCanvasGroup.alpha = alpha;
				yield return null;
			}
			fadeCanvasGroup.alpha = 1f;
		}
	}

	private IEnumerator FadeIn()
	{
		if (!(fadeCanvasGroup == null))
		{
			float elapsed = 0f;
			while (elapsed < fadeInDuration)
			{
				elapsed += Time.deltaTime;
				float alpha = 1f - Mathf.Clamp01(elapsed / fadeInDuration);
				fadeCanvasGroup.alpha = alpha;
				yield return null;
			}
			fadeCanvasGroup.alpha = 0f;
			fadeCanvasGroup.blocksRaycasts = false;
		}
	}

	private IEnumerator PlayClimbingLoop()
	{
		while (isClimbing && climbingLoopSound != null)
		{
			audioSource.PlayOneShot(climbingLoopSound);
			yield return new WaitForSeconds(climbingLoopSound.length * 0.8f);
		}
	}

	private void PlaySound(AudioClip clip)
	{
		if (audioSource != null && clip != null)
		{
			audioSource.PlayOneShot(clip);
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (!isClimbing && IsPlayer(other))
		{
			currentPlayer = other.gameObject;
			playerInRange = true;
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (other.gameObject == currentPlayer)
		{
			playerInRange = false;
			currentPlayer = null;
			if (interactionPrompt != null)
			{
				interactionPrompt.SetActive(value: false);
				promptShown = false;
			}
		}
	}

	private bool IsPlayer(Collider other)
	{
		if (!other.CompareTag("Player") && !(other.GetComponent<FirstPersonController>() != null) && !(other.GetComponent<CharacterController>() != null) && !(other.GetComponent<GridInventorySystem>() != null))
		{
			return other.GetComponent<PlayerStats>() != null;
		}
		return true;
	}

	public void TriggerClimb(GameObject player)
	{
		if (!isClimbing && IsPlayer(player.GetComponent<Collider>()))
		{
			currentPlayer = player;
			StartClimbing();
		}
	}

	public void SetDestinationPoint(Transform newDestination)
	{
		destinationPoint = newDestination;
	}

	public void SetClimbParameters(float height, float duration)
	{
		climbHeight = height;
		climbDuration = duration;
	}

	public Vector3 GetCurrentDestinationPosition()
	{
		return GetDestinationPosition();
	}

	[ContextMenu("Test Climb")]
	public void TestClimb()
	{
		if (Application.isPlaying)
		{
			GameObject gameObject = GameObject.FindGameObjectWithTag("Player");
			if (gameObject == null)
			{
				gameObject = UnityEngine.Object.FindAnyObjectByType<FirstPersonController>()?.gameObject;
			}
			if (gameObject != null)
			{
				TriggerClimb(gameObject);
			}
		}
	}

	[ContextMenu("Find Destination")]
	public void FindDestinationFromMenu()
	{
		FindDestinationPoint();
	}

	[ContextMenu("Validate Setup")]
	public void ValidateSetupFromMenu()
	{
		ValidateSetup();
	}
}
