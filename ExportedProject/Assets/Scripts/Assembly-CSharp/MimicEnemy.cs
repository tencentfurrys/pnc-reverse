using PixelCrushers.GridController;
using UnityEngine;

public class MimicEnemy : MonoBehaviour
{
	[Header("Mimic Settings")]
	[SerializeField]
	private KeyCode interactionKey = KeyCode.E;

	[SerializeField]
	private float interactionRange = 3f;

	[Header("Chest Appearance")]
	[SerializeField]
	private SpriteRenderer chestSpriteRenderer;

	[SerializeField]
	private Sprite chestSprite;

	[Header("Outline Effect")]
	[SerializeField]
	private Material outlineMaterial;

	[SerializeField]
	private float outlineAlphaInRange = 1f;

	[SerializeField]
	private float outlineAlphaOutOfRange;

	[Header("Grab System")]
	[SerializeField]
	private GrabScreen grabScreen;

	[SerializeField]
	private RuntimeAnimatorController grabAnimatorController;

	[Header("Grab Camera Settings")]
	[SerializeField]
	private Vector3 grabCameraPositionOffset = Vector3.zero;

	[SerializeField]
	private Vector3 grabCameraRotationOffset = Vector3.zero;

	[SerializeField]
	private bool usePlayerCurrentRotation;

	[Header("Grab Screen Audio")]
	[Tooltip("Per-animation sounds for the grab screen. animationName must match the Animator state name.")]
	public GrabAnimationData[] grabScreenAnimations;

	[Header("Audio")]
	[SerializeField]
	private AudioClip revealSound;

	[Header("Effects")]
	[SerializeField]
	private GameObject revealEffect;

	[Header("Gallery")]
	[Tooltip("Unique ID for gallery unlock tracking. Should match EnemyGalleryEntry.enemyID")]
	public string galleryEnemyID;

	private bool playerInRange;

	private bool hasTriggeredGrab;

	private Transform player;

	private AudioSource audioSource;

	private Material outlineMaterialInstance;

	private float currentOutlineAlpha;

	public bool PlayerInRange => playerInRange;

	public bool HasTriggeredGrab => hasTriggeredGrab;

	private void Awake()
	{
		InitializeComponents();
		InitializeOutlineMaterial();
	}

	private void Start()
	{
		FindPlayer();
		SetupCollider();
		if (chestSpriteRenderer != null && chestSprite != null)
		{
			chestSpriteRenderer.sprite = chestSprite;
		}
	}

	private void Update()
	{
		if (!(player == null))
		{
			CheckPlayerRange();
			UpdateOutlineAlpha();
			if (playerInRange && !hasTriggeredGrab && InputManager.Interact != null && InputManager.Interact.WasPressedThisFrame())
			{
				TriggerMimicGrab();
			}
		}
	}

	private void InitializeComponents()
	{
		if (chestSpriteRenderer == null)
		{
			chestSpriteRenderer = GetComponent<SpriteRenderer>();
		}
		audioSource = GetComponent<AudioSource>();
		if (audioSource == null)
		{
			audioSource = base.gameObject.AddComponent<AudioSource>();
		}
		if (grabScreen == null)
		{
			grabScreen = GrabScreen.Instance;
		}
	}

	private void FindPlayer()
	{
		GameObject gameObject = GameObject.FindGameObjectWithTag("Player");
		if (gameObject != null)
		{
			player = gameObject.transform;
		}
	}

	private void SetupCollider()
	{
		Collider component = GetComponent<Collider>();
		if (component == null)
		{
			SphereCollider sphereCollider = base.gameObject.AddComponent<SphereCollider>();
			sphereCollider.isTrigger = true;
			sphereCollider.radius = interactionRange;
			sphereCollider.center = Vector3.zero;
		}
		else
		{
			component.isTrigger = true;
		}
		if (base.gameObject.layer == 0)
		{
			base.gameObject.layer = 8;
		}
	}

	private void CheckPlayerRange()
	{
		if (!(player == null))
		{
			float num = Vector3.Distance(base.transform.position, player.position);
			playerInRange = num <= interactionRange;
		}
	}

	private void TriggerMimicGrab()
	{
		if (!hasTriggeredGrab && !(grabScreen == null) && !grabScreen.IsGrabbed)
		{
			hasTriggeredGrab = true;
			PlaySound(revealSound);
			if (revealEffect != null)
			{
				Object.Instantiate(revealEffect, base.transform.position + Vector3.up, Quaternion.identity);
			}
			grabScreen.StartGrab(base.gameObject, grabAnimatorController, grabCameraPositionOffset, grabCameraRotationOffset, usePlayerCurrentRotation);
			Object.Destroy(base.gameObject);
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (IsPlayer(other))
		{
			playerInRange = true;
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (IsPlayer(other))
		{
			playerInRange = false;
		}
	}

	private bool IsPlayer(Collider other)
	{
		if (!other.CompareTag("Player") && !(other.GetComponent<FirstPersonController>() != null) && !(other.GetComponent<CharacterController>() != null))
		{
			return other.GetComponent<GridInventorySystem>() != null;
		}
		return true;
	}

	private void InitializeOutlineMaterial()
	{
		if (outlineMaterial != null && chestSpriteRenderer != null)
		{
			outlineMaterialInstance = new Material(outlineMaterial);
			chestSpriteRenderer.material = outlineMaterialInstance;
			currentOutlineAlpha = outlineAlphaOutOfRange;
			outlineMaterialInstance.SetFloat("_OutlineAlpha", currentOutlineAlpha);
		}
	}

	private void UpdateOutlineAlpha()
	{
		if (!(outlineMaterialInstance == null))
		{
			float num = (playerInRange ? outlineAlphaInRange : outlineAlphaOutOfRange);
			currentOutlineAlpha = num;
			outlineMaterialInstance.SetFloat("_OutlineAlpha", currentOutlineAlpha);
		}
	}

	private void PlaySound(AudioClip clip)
	{
		if (audioSource != null && clip != null)
		{
			audioSource.PlayOneShot(clip);
		}
	}

	private void OnDestroy()
	{
		if (outlineMaterialInstance != null)
		{
			if (Application.isPlaying)
			{
				Object.Destroy(outlineMaterialInstance);
			}
			else
			{
				Object.DestroyImmediate(outlineMaterialInstance);
			}
		}
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireSphere(base.transform.position, interactionRange);
		if (player != null)
		{
			Gizmos.color = ((Vector3.Distance(base.transform.position, player.position) <= interactionRange) ? Color.green : Color.red);
			Gizmos.DrawLine(base.transform.position, player.position);
		}
	}

	private void OnValidate()
	{
		if (chestSpriteRenderer == null)
		{
			chestSpriteRenderer = GetComponent<SpriteRenderer>();
		}
		if (chestSpriteRenderer == null && chestSprite == null && outlineMaterial == null && interactionRange <= 0f)
		{
			interactionRange = 3f;
		}
	}

	[ContextMenu("Test Grab")]
	public void TestGrab()
	{
		if (Application.isPlaying && !hasTriggeredGrab)
		{
			TriggerMimicGrab();
		}
	}
}
