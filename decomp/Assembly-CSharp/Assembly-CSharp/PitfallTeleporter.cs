using System.Collections;
using CMF;
using Pathfinding;
using PixelCrushers.GridController;
using UnityEngine;
using UnityEngine.UI;

public class PitfallTeleporter : MonoBehaviour
{
	[Header("Teleport Settings")]
	[SerializeField]
	private Transform respawnPoint;

	[SerializeField]
	private Vector3 respawnOffset = Vector3.zero;

	[SerializeField]
	private bool useRoomStart = true;

	[SerializeField]
	private string roomStartTag = "RoomStart";

	[Header("Damage Settings")]
	[SerializeField]
	private int pitfallDamage = 10;

	[SerializeField]
	private bool percentageDamage;

	[SerializeField]
	private float damagePercentage = 0.1f;

	[SerializeField]
	private bool canKillPlayer;

	[SerializeField]
	private int minimumHealthAfterDamage = 1;

	[Header("Visual Effects")]
	[SerializeField]
	private GameObject fallEffect;

	[SerializeField]
	private GameObject teleportEffect;

	[SerializeField]
	private bool screenShake = true;

	[SerializeField]
	private float shakeIntensity = 0.5f;

	[SerializeField]
	private float shakeDuration = 0.3f;

	[Header("Audio")]
	[SerializeField]
	private AudioSource audioSource;

	[SerializeField]
	private AudioClip fallSound;

	[SerializeField]
	private AudioClip teleportSound;

	[SerializeField]
	private AudioClip damageSound;

	[Header("Teleport Animation")]
	[SerializeField]
	private bool useFadeTransition = true;

	[SerializeField]
	private float fadeOutDuration = 0.5f;

	[SerializeField]
	private float fadeInDuration = 0.5f;

	[SerializeField]
	private Color fadeColor = Color.black;

	[Header("Trigger")]
	[SerializeField]
	private Collider triggerCollider;

	[Header("Debug")]
	[SerializeField]
	private bool showGizmos = true;

	private bool isProcessingFall;

	private Camera playerCamera;

	private CanvasGroup fadeCanvasGroup;

	private GameObject fadeCanvas;

	private void Awake()
	{
		if (audioSource == null)
		{
			audioSource = GetComponent<AudioSource>();
		}
		if (audioSource == null)
		{
			audioSource = gameObject.AddComponent<AudioSource>();
		}
		playerCamera = Camera.main;
		if (playerCamera == null)
		{
			playerCamera = Object.FindAnyObjectByType<Camera>();
		}
		if (useFadeTransition)
		{
			CreateFadeCanvas();
		}
		if (triggerCollider != null && triggerCollider.gameObject != gameObject)
		{
			PitfallTriggerForwarder pitfallTriggerForwarder = triggerCollider.GetComponent<PitfallTriggerForwarder>();
			if (pitfallTriggerForwarder == null)
			{
				pitfallTriggerForwarder = triggerCollider.gameObject.AddComponent<PitfallTriggerForwarder>();
			}
			pitfallTriggerForwarder.Initialize(this);
		}
		ValidateSetup();
	}

	private void Start()
	{
		if (respawnPoint == null && useRoomStart)
		{
			FindRoomStart();
		}
	}

	private void CreateFadeCanvas()
	{
		fadeCanvas = new GameObject("PitfallFadeCanvas");
		fadeCanvas.transform.SetParent(transform);
		Canvas canvas = fadeCanvas.AddComponent<Canvas>();
		canvas.renderMode = RenderMode.ScreenSpaceOverlay;
		canvas.sortingOrder = 9999;
		fadeCanvasGroup = fadeCanvas.AddComponent<CanvasGroup>();
		fadeCanvasGroup.alpha = 0f;
		fadeCanvasGroup.blocksRaycasts = false;
		GameObject gameObject = new GameObject("FadeImage");
		gameObject.transform.SetParent(fadeCanvas.transform);
		gameObject.AddComponent<Image>().color = fadeColor;
		RectTransform component = gameObject.GetComponent<RectTransform>();
		component.anchorMin = Vector2.zero;
		component.anchorMax = Vector2.one;
		component.offsetMin = Vector2.zero;
		component.offsetMax = Vector2.zero;
	}

	private void FindRoomStart()
	{
		GameObject gameObject = GameObject.FindGameObjectWithTag(roomStartTag);
		if (gameObject != null)
		{
			respawnPoint = gameObject.transform;
			return;
		}
		gameObject = GameObject.Find("RoomStart");
		if (gameObject == null)
		{
			gameObject = GameObject.Find("SpawnPoint");
		}
		if (gameObject == null)
		{
			gameObject = GameObject.Find("PlayerStart");
		}
		if (gameObject != null)
		{
			respawnPoint = gameObject.transform;
		}
	}

	private void ValidateSetup()
	{
		if (minimumHealthAfterDamage < 1)
		{
			minimumHealthAfterDamage = 1;
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		TryHandlePitfall(other);
	}

	private void OnTriggerStay(Collider other)
	{
		TryHandlePitfall(other);
	}

	public void ForwardTrigger(Collider other)
	{
		TryHandlePitfall(other);
	}

	private void TryHandlePitfall(Collider other)
	{
		GameObject gameObject = other.transform.root.gameObject;
		DragonEnemyAI component = gameObject.GetComponent<DragonEnemyAI>();
		if (component != null && !component.IsDead)
		{
			TeleportDragon(gameObject, component.followerEntity);
			return;
		}
		ProximityDragonEnemyAI component2 = gameObject.GetComponent<ProximityDragonEnemyAI>();
		if (component2 != null && !component2.IsDead)
		{
			TeleportDragon(gameObject, component2.followerEntity);
			return;
		}
		IDamageable component3 = gameObject.GetComponent<IDamageable>();
		if (component3 != null && !IsPlayer(other))
		{
			component3.TakeDamage(99999);
		}
		else if (!isProcessingFall && IsPlayer(other))
		{
			GameObject player = gameObject;
			StartCoroutine(HandlePitfall(player));
		}
	}

	private void TeleportDragon(GameObject dragonObj, FollowerEntity follower)
	{
		Vector3 respawnPosition = GetRespawnPosition();
		dragonObj.transform.position = respawnPosition;
		Rigidbody component = dragonObj.GetComponent<Rigidbody>();
		if (component != null)
		{
			component.linearVelocity = Vector3.zero;
		}
		if ((Object)(object)follower != null)
		{
			follower.position = respawnPosition;
		}
	}

	private bool IsPlayer(Collider other)
	{
		GameObject gameObject = other.transform.root.gameObject;
		if (!gameObject.CompareTag("Player") && !(gameObject.GetComponent<FirstPersonController>() != null))
		{
			return gameObject.GetComponent<PlayerStats>() != null;
		}
		return true;
	}

	private IEnumerator HandlePitfall(GameObject player)
	{
		isProcessingFall = true;
		player = player.transform.root.gameObject;
		FirstPersonController fpsController = player.GetComponent<FirstPersonController>();
		PlayerStats playerStats = player.GetComponent<PlayerStats>();
		CharacterController characterController = player.GetComponent<CharacterController>();
		AdvancedWalkerController walker = player.GetComponent<AdvancedWalkerController>();
		Rigidbody playerRb = player.GetComponent<Rigidbody>();
		if (walker != null)
		{
			walker.SetMomentum(Vector3.zero);
			walker.enabled = false;
		}
		if (playerRb != null)
		{
			playerRb.linearVelocity = Vector3.zero;
			playerRb.isKinematic = true;
		}
		if (fpsController != null)
		{
			fpsController.enabled = false;
		}
		if (characterController != null)
		{
			characterController.enabled = false;
		}
		PlaySound(fallSound);
		if (fallEffect != null)
		{
			Object.Destroy(Object.Instantiate(fallEffect, player.transform.position, Quaternion.identity), 3f);
		}
		if (screenShake && playerCamera != null)
		{
			StartCoroutine(ScreenShake());
		}
		if (useFadeTransition && fadeCanvasGroup != null)
		{
			yield return StartCoroutine(FadeOut());
		}
		else
		{
			yield return new WaitForSeconds(0.5f);
		}
		GridInventorySystem gridInventorySystem = player.GetComponent<GridInventorySystem>() ?? Object.FindAnyObjectByType<GridInventorySystem>();
		int num = ((!(gridInventorySystem != null) || !gridInventorySystem.HasPitfallImmunity()) ? CalculateSafeDamage(playerStats) : 0);
		if (playerStats != null && num > 0)
		{
			playerStats.TakeDamage(num);
		}
		PlaySound(damageSound);
		Vector3 respawnPos = GetRespawnPosition();
		Quaternion rotation = ((respawnPoint != null) ? respawnPoint.rotation : player.transform.rotation);
		Debug.Log($"[Pitfall] Teleporting to {respawnPos} (respawnPoint={respawnPoint != null})");
		player.transform.SetPositionAndRotation(respawnPos, rotation);
		Physics.SyncTransforms();
		yield return new WaitForFixedUpdate();
		PlaySound(teleportSound);
		if (teleportEffect != null)
		{
			Object.Destroy(Object.Instantiate(teleportEffect, respawnPos, Quaternion.identity), 3f);
		}
		if (useFadeTransition && fadeCanvasGroup != null)
		{
			yield return StartCoroutine(FadeIn());
		}
		if (characterController != null && !characterController.enabled)
		{
			characterController.enabled = true;
		}
		if (playerRb != null)
		{
			playerRb.isKinematic = false;
		}
		if (walker != null)
		{
			walker.enabled = true;
		}
		if (fpsController != null)
		{
			fpsController.enabled = true;
		}
		isProcessingFall = false;
	}

	private int CalculateDamage(PlayerStats playerStats)
	{
		if (percentageDamage && playerStats != null)
		{
			return Mathf.RoundToInt((float)playerStats.MaxHealth * damagePercentage);
		}
		return pitfallDamage;
	}

	private int CalculateSafeDamage(PlayerStats playerStats)
	{
		if (playerStats == null)
		{
			return 0;
		}
		int num = CalculateDamage(playerStats);
		if (canKillPlayer)
		{
			return num;
		}
		int b = playerStats.CurrentHealth - minimumHealthAfterDamage;
		return Mathf.Max(0, Mathf.Min(num, b));
	}

	private Vector3 GetRespawnPosition()
	{
		if (respawnPoint != null)
		{
			return respawnPoint.position + respawnOffset;
		}
		if (useRoomStart)
		{
			FindRoomStart();
			if (respawnPoint != null)
			{
				return respawnPoint.position + respawnOffset;
			}
		}
		Vector3 vector = transform.position + Vector3.up * 5f + respawnOffset;
		Debug.LogError($"[Pitfall] No respawn point assigned on '{name}' and useRoomStart={useRoomStart} failed to find one. Using fallback {vector}, which may re-trigger the pit.", this);
		return vector;
	}

	private IEnumerator ScreenShake()
	{
		if (!(playerCamera == null))
		{
			Vector3 originalPosition = playerCamera.transform.localPosition;
			float elapsed = 0f;
			while (elapsed < shakeDuration)
			{
				float x = Random.Range(-1f, 1f) * shakeIntensity;
				float y = Random.Range(-1f, 1f) * shakeIntensity;
				playerCamera.transform.localPosition = originalPosition + new Vector3(x, y, 0f);
				elapsed += Time.deltaTime;
				yield return null;
			}
			playerCamera.transform.localPosition = originalPosition;
		}
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

	private void PlaySound(AudioClip clip)
	{
		if (audioSource != null && clip != null)
		{
			audioSource.PlayOneShot(clip);
		}
	}

	public void TriggerPitfall(GameObject player)
	{
		if (!isProcessingFall && IsPlayer(player.GetComponent<Collider>()))
		{
			StartCoroutine(HandlePitfall(player));
		}
	}

	public void SetRespawnPoint(Transform newRespawnPoint)
	{
		respawnPoint = newRespawnPoint;
	}

	public void SetDamage(int damage)
	{
		pitfallDamage = damage;
		percentageDamage = false;
	}

	public void SetPercentageDamage(float percentage)
	{
		damagePercentage = Mathf.Clamp01(percentage);
		percentageDamage = true;
	}

	public void SetCanKillPlayer(bool canKill)
	{
		canKillPlayer = canKill;
	}

	public void SetMinimumHealthAfterDamage(int minHealth)
	{
		minimumHealthAfterDamage = Mathf.Max(1, minHealth);
	}

	public Vector3 GetCurrentRespawnPosition()
	{
		return GetRespawnPosition();
	}

	public int GetDamageForPlayer(PlayerStats playerStats)
	{
		return CalculateSafeDamage(playerStats);
	}

	private void OnDrawGizmosSelected()
	{
		if (!showGizmos)
		{
			return;
		}
		Collider component = GetComponent<Collider>();
		if (component != null)
		{
			Gizmos.color = Color.red;
			if (component is BoxCollider boxCollider)
			{
				Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
				Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
			}
			else if (component is SphereCollider sphereCollider)
			{
				Gizmos.DrawWireSphere(transform.position + sphereCollider.center, sphereCollider.radius);
			}
		}
		Vector3 respawnPosition = GetRespawnPosition();
		Gizmos.color = Color.green;
		Gizmos.DrawWireSphere(respawnPosition, 1f);
		Gizmos.DrawLine(respawnPosition, respawnPosition + Vector3.up * 2f);
		Gizmos.color = Color.yellow;
		Gizmos.DrawLine(transform.position, respawnPosition);
		if (respawnPoint != null && respawnOffset != Vector3.zero)
		{
			Gizmos.color = Color.cyan;
			Gizmos.DrawLine(respawnPoint.position, respawnPosition);
			Gizmos.DrawWireSphere(respawnPoint.position, 0.5f);
		}
	}

	[ContextMenu("Test Pitfall")]
	public void TestPitfall()
	{
		if (Application.isPlaying)
		{
			GameObject gameObject = GameObject.FindGameObjectWithTag("Player");
			if (gameObject == null)
			{
				gameObject = Object.FindAnyObjectByType<FirstPersonController>()?.gameObject;
			}
			if (gameObject != null)
			{
				TriggerPitfall(gameObject);
			}
		}
	}

	[ContextMenu("Find Room Start")]
	public void FindRoomStartFromMenu()
	{
		FindRoomStart();
	}
}
