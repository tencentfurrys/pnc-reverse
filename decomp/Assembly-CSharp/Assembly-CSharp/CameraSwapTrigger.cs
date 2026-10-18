using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using CMF;
using PixelCrushers.GridController;
using PixelCrushers.GridController.Glyphs;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class CameraSwapTrigger : MonoBehaviour, IPlayerInteractable
{
	public enum RewardType
	{
		None,
		HealPlayer,
		SpawnFromPool,
		BothWithPool
	}

	[Header("Camera References")]
	[SerializeField]
	protected Camera firstPersonCamera;

	[SerializeField]
	private Camera targetCamera;

	[SerializeField]
	protected FirstPersonController firstPersonController;

	[SerializeField]
	protected DualWieldingSystem dualWieldingSystem;

	[SerializeField]
	protected InventoryUI inventoryUI;

	[Header("Trigger Settings")]
	[SerializeField]
	private Collider triggerCollider;

	[SerializeField]
	protected KeyCode activationKey = KeyCode.E;

	[SerializeField]
	protected float interactionRange = 5f;

	[SerializeField]
	protected bool oneTimeUse;

	[SerializeField]
	private bool autoReturnToFirstPerson = true;

	[SerializeField]
	protected float autoReturnDelay = 3f;

	[Header("Swap Settings")]
	[SerializeField]
	protected float swapDuration = 1.5f;

	[SerializeField]
	protected AnimationCurve swapCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

	[SerializeField]
	protected bool disablePlayerControlsDuringSwap = true;

	[Header("Animation Settings")]
	[SerializeField]
	protected Animator animatorToTrigger;

	[SerializeField]
	protected AnimationEventHelper animationEventHelper;

	[SerializeField]
	protected string animationTriggerName = "PlayAnimation";

	[SerializeField]
	protected float animationDelay = 0.1f;

	[SerializeField]
	protected bool activateAnimatorObject = true;

	[SerializeField]
	protected bool useAnimationEvents = true;

	[Header("Audio System")]
	[SerializeField]
	protected AudioClip[] animationSounds;

	[SerializeField]
	protected AudioSource audioSource;

	[SerializeField]
	private bool useGlobalVolume = true;

	[SerializeField]
	private float volumeMultiplier = 1f;

	[SerializeField]
	private bool allowOverlappingSounds = true;

	[SerializeField]
	private float pitchVariation;

	[Header("Audio Events")]
	public UnityEvent<AudioClip> OnAudioPlayed;

	public UnityEvent<int> OnAudioPlayedByIndex;

	[Header("UI Feedback")]
	[SerializeField]
	protected GameObject promptUI;

	[SerializeField]
	private string promptText = "Press E to view";

	[Header("Post-Animation Rewards")]
	[SerializeField]
	protected bool enableRewards;

	[SerializeField]
	protected RewardType rewardType;

	[Header("Gallery Unlock")]
	[SerializeField]
	private string galleryEntryID = "";

	[Header("Item Pool System")]
	[SerializeField]
	private ChestItemPool itemPool;

	[SerializeField]
	private int minItemsFromPool = 1;

	[SerializeField]
	private int maxItemsFromPool = 3;

	[SerializeField]
	private bool useGuaranteedRare;

	[SerializeField]
	private bool allowDuplicateItems = true;

	[SerializeField]
	private ItemRarity minimumRarity;

	[Header("Item Spawning")]
	[SerializeField]
	private GameObject itemPickupPrefab;

	[SerializeField]
	private Transform itemSpawnPoint;

	[SerializeField]
	private Vector3 itemSpawnOffset = new Vector3(0f, 1f, 2f);

	[SerializeField]
	private bool addToInventoryDirectly;

	[SerializeField]
	private bool useRandomSpawnPositions = true;

	[SerializeField]
	private float spawnRadius = 1.5f;

	[Header("Launch Physics")]
	[SerializeField]
	private bool useLaunchArc = true;

	[SerializeField]
	private float launchForce = 3f;

	[SerializeField]
	private Vector3 launchDirection = new Vector3(0f, 0.5f, 1f);

	[SerializeField]
	private float launchVariation = 0.2f;

	[Header("Healing Settings")]
	[SerializeField]
	private int healAmount = 100;

	[SerializeField]
	protected bool healToFullHealth = true;

	[SerializeField]
	private AudioClip healSound;

	[SerializeField]
	private GameObject healEffect;

	[SerializeField]
	private bool showHealMessage = true;

	[SerializeField]
	private string healMessage = "You feel rejuvenated!";

	[Header("Reward Audio & Effects")]
	[SerializeField]
	private AudioClip itemSpawnSound;

	[SerializeField]
	private GameObject itemSpawnEffect;

	[SerializeField]
	private bool playRewardSoundOnReturn = true;

	[SerializeField]
	private float rewardEffectDelay = 0.5f;

	[SerializeField]
	private float itemSpawnDelay = 0.2f;

	[Header("Events")]
	public UnityEvent OnTriggerActivated;

	public UnityEvent OnSwapCompleted;

	public UnityEvent OnAnimationTriggered;

	public UnityEvent OnReturnedToFirstPerson;

	public UnityEvent OnRewardGiven;

	public UnityEvent<InventoryItem> OnItemSpawned;

	public UnityEvent<int> OnPlayerHealed;

	public UnityEvent<int> OnMultipleItemsSpawned;

	[Header("Debug")]
	[SerializeField]
	private bool debugAudioEvents = true;

	[SerializeField]
	private bool debugRewards = true;

	[SerializeField]
	private bool debugItemPool = true;

	protected bool isBeingLookedAt;

	protected bool isSwapping;

	protected bool hasBeenUsed;

	protected bool isInCinematicView;

	protected bool rewardGiven;

	protected PlayerStats playerStats;

	protected GridInventorySystem playerInventory;

	private CameraController cmfCameraController;

	private SmoothPosition cmfSmoothPosition;

	private SmoothRotation cmfSmoothRotation;

	private Transform cameraRoot;

	private Vector3 originalCameraRootLocalPos;

	private Quaternion originalCameraRootLocalRot;

	public virtual bool CanInteract
	{
		get
		{
			if ((!oneTimeUse || !hasBeenUsed) && !isInCinematicView)
			{
				return !isSwapping;
			}
			return false;
		}
	}

	public float InteractionRange => interactionRange;

	private void Awake()
	{
		if (triggerCollider == null)
		{
			triggerCollider = GetComponent<Collider>();
		}
		if (firstPersonController == null)
		{
			firstPersonController = Object.FindAnyObjectByType<FirstPersonController>();
		}
		if (dualWieldingSystem == null)
		{
			dualWieldingSystem = DualWieldingSystem.Instance;
		}
		if (inventoryUI == null)
		{
			inventoryUI = Object.FindAnyObjectByType<InventoryUI>();
		}
		if (firstPersonCamera == null && firstPersonController != null)
		{
			firstPersonCamera = firstPersonController.cameraTransform?.GetComponentInChildren<Camera>();
		}
		if (firstPersonCamera != null)
		{
			cameraRoot = firstPersonCamera.transform;
			while (cameraRoot.parent != null && cameraRoot.parent != firstPersonController.transform)
			{
				cameraRoot = cameraRoot.parent;
			}
			cmfCameraController = firstPersonCamera.GetComponentInParent<CameraController>();
			cmfSmoothPosition = cameraRoot.GetComponent<SmoothPosition>();
			cmfSmoothRotation = cameraRoot.GetComponent<SmoothRotation>();
		}
		if (animationEventHelper == null && animatorToTrigger != null)
		{
			animationEventHelper = animatorToTrigger.GetComponent<AnimationEventHelper>();
		}
		if (audioSource == null)
		{
			audioSource = GetComponent<AudioSource>();
		}
		if (audioSource == null)
		{
			audioSource = gameObject.AddComponent<AudioSource>();
		}
		if (firstPersonController != null)
		{
			playerStats = firstPersonController.GetComponent<PlayerStats>();
			playerInventory = firstPersonController.GetComponent<GridInventorySystem>();
		}
		if (playerStats == null)
		{
			playerStats = Object.FindAnyObjectByType<PlayerStats>();
		}
		if (playerInventory == null)
		{
			playerInventory = Object.FindAnyObjectByType<GridInventorySystem>();
		}
	}

	private void Start()
	{
		if (triggerCollider != null)
		{
			triggerCollider.isTrigger = true;
		}
		if (targetCamera != null)
		{
			targetCamera.enabled = false;
		}
		if (promptUI != null)
		{
			promptUI.SetActive(value: false);
		}
		if (animatorToTrigger != null && activateAnimatorObject)
		{
			animatorToTrigger.gameObject.SetActive(value: false);
		}
		if (animationEventHelper != null)
		{
			animationEventHelper.SetCameraSwapTrigger(this);
		}
		if (audioSource != null && useGlobalVolume)
		{
			audioSource.playOnAwake = false;
			audioSource.volume = volumeMultiplier;
		}
		ValidateRewardSetup();
	}

	protected virtual void Update()
	{
	}

	private void ValidateRewardSetup()
	{
		if (enableRewards)
		{
			switch (rewardType)
			{
			case RewardType.SpawnFromPool:
				ValidatePoolItemSpawning();
				break;
			case RewardType.BothWithPool:
				ValidatePoolItemSpawning();
				break;
			}
		}
	}

	private void ValidatePoolItemSpawning()
	{
		if (!(itemPool == null) && itemPool.HasItems())
		{
			if (minItemsFromPool < 1)
			{
				minItemsFromPool = 1;
			}
			if (maxItemsFromPool < minItemsFromPool)
			{
				maxItemsFromPool = minItemsFromPool;
			}
		}
	}

	protected virtual void GiveRewards()
	{
		if (enableRewards && !rewardGiven)
		{
			rewardGiven = true;
			StartCoroutine(RewardSequence());
		}
	}

	private IEnumerator RewardSequence()
	{
		yield return new WaitForSeconds(rewardEffectDelay);
		switch (rewardType)
		{
		case RewardType.HealPlayer:
			HealPlayer();
			break;
		case RewardType.SpawnFromPool:
			yield return StartCoroutine(SpawnItemsFromPool());
			break;
		case RewardType.BothWithPool:
			HealPlayer();
			yield return new WaitForSeconds(0.5f);
			yield return StartCoroutine(SpawnItemsFromPool());
			break;
		}
		OnRewardGiven?.Invoke();
	}

	protected void HealPlayer()
	{
		if (!(playerStats == null))
		{
			int num;
			if (healToFullHealth)
			{
				num = playerStats.MaxHealth - playerStats.CurrentHealth;
				playerStats.Heal(num);
			}
			else
			{
				num = Mathf.Min(healAmount, playerStats.MaxHealth - playerStats.CurrentHealth);
				playerStats.Heal(healAmount);
			}
			if (healSound != null)
			{
				PlayAudioClip(healSound);
			}
			if (healEffect != null)
			{
				Vector3 position = ((firstPersonController != null) ? firstPersonController.transform.position : transform.position);
				Object.Destroy(Object.Instantiate(healEffect, position, Quaternion.identity), 5f);
			}
			OnPlayerHealed?.Invoke(num);
		}
	}

	private IEnumerator SpawnItemsFromPool()
	{
		if (itemPool == null || !itemPool.HasItems())
		{
			yield break;
		}
		int count = Random.Range(minItemsFromPool, maxItemsFromPool + 1);
		List<InventoryItem> itemsToSpawn = GetItemsFromPool(count);
		if (itemsToSpawn.Count == 0)
		{
			yield break;
		}
		for (int i = 0; i < itemsToSpawn.Count; i++)
		{
			InventoryItem inventoryItem = itemsToSpawn[i];
			if (inventoryItem != null)
			{
				if (addToInventoryDirectly)
				{
					AddItemToInventory(inventoryItem);
				}
				else
				{
					SpawnItemInWorld(inventoryItem, i);
				}
				OnItemSpawned?.Invoke(inventoryItem);
				if (i < itemsToSpawn.Count - 1 && itemSpawnDelay > 0f)
				{
					yield return new WaitForSeconds(itemSpawnDelay);
				}
			}
		}
		if (itemSpawnSound != null && playRewardSoundOnReturn)
		{
			PlayAudioClip(itemSpawnSound);
		}
		OnMultipleItemsSpawned?.Invoke(itemsToSpawn.Count);
	}

	private List<InventoryItem> GetItemsFromPool(int count)
	{
		List<InventoryItem> list = new List<InventoryItem>();
		if (useGuaranteedRare && itemPool.HasItemsOfRarity(ItemRarity.Rare))
		{
			InventoryItem randomRareItem = itemPool.GetRandomRareItem();
			if (randomRareItem != null)
			{
				list.Add(randomRareItem);
				count--;
			}
		}
		if (count > 0)
		{
			if (minimumRarity > ItemRarity.Common)
			{
				for (int i = 0; i < count; i++)
				{
					InventoryItem randomItemOfRarity = itemPool.GetRandomItemOfRarity(minimumRarity);
					if (randomItemOfRarity != null && (allowDuplicateItems || !list.Contains(randomItemOfRarity)))
					{
						list.Add(randomItemOfRarity);
					}
					else if (!allowDuplicateItems && list.Contains(randomItemOfRarity))
					{
						int num = 0;
						while (list.Contains(randomItemOfRarity) && num < 5)
						{
							randomItemOfRarity = itemPool.GetRandomItemOfRarity(minimumRarity);
							num++;
						}
						if (randomItemOfRarity != null && !list.Contains(randomItemOfRarity))
						{
							list.Add(randomItemOfRarity);
						}
					}
				}
			}
			else
			{
				List<InventoryItem> randomItems = itemPool.GetRandomItems(count, allowDuplicateItems);
				list.AddRange(randomItems);
			}
		}
		return list;
	}

	private void AddItemToInventory(InventoryItem item)
	{
		if (!(playerInventory == null) && !playerInventory.AddItem(item))
		{
			SpawnItemInWorld(item, 0);
		}
	}

	private void SpawnItemInWorld(InventoryItem item, int itemIndex)
	{
		Vector3 itemSpawnPosition = GetItemSpawnPosition(itemIndex);
		GameObject gameObject = CreatePickupObject(item, itemSpawnPosition);
		if (gameObject != null && useLaunchArc)
		{
			ApplyLaunchForce(gameObject, itemIndex);
		}
		if (itemSpawnEffect != null)
		{
			Object.Destroy(Object.Instantiate(itemSpawnEffect, itemSpawnPosition, Quaternion.identity), 3f);
		}
	}

	private Vector3 GetItemSpawnPosition(int itemIndex)
	{
		Vector3 result;
		if (itemSpawnPoint != null)
		{
			result = itemSpawnPoint.position;
		}
		else
		{
			result = ((!(firstPersonController != null)) ? (transform.position + itemSpawnOffset) : (firstPersonController.transform.position + itemSpawnOffset));
		}
		if (useRandomSpawnPositions && (itemIndex > 0 || maxItemsFromPool > 1))
		{
			Vector2 vector = Random.insideUnitCircle * spawnRadius;
			Vector3 vector2 = new Vector3(vector.x, Random.Range(0f, 0.5f), vector.y);
			result += vector2;
		}
		else if (itemIndex > 0)
		{
			Vector3 vector3 = new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(0f, 0.3f), Random.Range(-0.5f, 0.5f));
			result += vector3;
		}
		return result;
	}

	private GameObject CreatePickupObject(InventoryItem item, Vector3 position)
	{
		GameObject gameObject;
		if (itemPickupPrefab != null)
		{
			gameObject = ((!(PickupPoolManager.Instance != null)) ? Object.Instantiate(itemPickupPrefab, position, Quaternion.identity) : PickupPoolManager.Instance.GetItemPickup(position, Quaternion.identity));
			InventoryItemPickup component = gameObject.GetComponent<InventoryItemPickup>();
			if (component != null)
			{
				component.SetItemData(item);
			}
			Rigidbody component2 = gameObject.GetComponent<Rigidbody>();
			if (component2 == null)
			{
				component2 = gameObject.AddComponent<Rigidbody>();
				ConfigureRigidbodyForBouncing(component2);
			}
			else
			{
				component2.useGravity = true;
				component2.isKinematic = false;
			}
		}
		else
		{
			gameObject = CreateBasicPickup(item, position);
		}
		return gameObject;
	}

	private GameObject CreateBasicPickup(InventoryItem item, Vector3 position)
	{
		GameObject gameObject = new GameObject("Pickup_" + item.itemName);
		gameObject.transform.position = position;
		gameObject.AddComponent<InventoryItemPickup>().SetItemData(item);
		SphereCollider sphereCollider = gameObject.AddComponent<SphereCollider>();
		sphereCollider.isTrigger = true;
		sphereCollider.radius = 1f;
		SphereCollider sphereCollider2 = gameObject.AddComponent<SphereCollider>();
		sphereCollider2.isTrigger = false;
		sphereCollider2.radius = 0.2f;
		sphereCollider2.material = CreateBouncyPhysicsMaterial();
		Rigidbody rb = gameObject.AddComponent<Rigidbody>();
		ConfigureRigidbodyForBouncing(rb);
		return gameObject;
	}

	private void ApplyLaunchForce(GameObject pickup, int itemIndex)
	{
		Rigidbody component = pickup.GetComponent<Rigidbody>();
		if (!(component == null))
		{
			Vector3 vector;
			if (itemSpawnPoint != null)
			{
				vector = itemSpawnPoint.TransformDirection(launchDirection.normalized);
			}
			else
			{
				vector = ((!(firstPersonController != null)) ? transform.TransformDirection(launchDirection.normalized) : firstPersonController.transform.TransformDirection(launchDirection.normalized));
			}
			if (maxItemsFromPool > 1 || itemIndex > 0)
			{
				Vector3 vector2 = new Vector3(Random.Range(0f - launchVariation, launchVariation), Random.Range((0f - launchVariation) * 0.5f, launchVariation * 0.5f), Random.Range(0f - launchVariation, launchVariation));
				vector += vector2;
				vector.Normalize();
			}
			component.AddForce(vector * launchForce, ForceMode.Impulse);
			Vector3 torque = new Vector3(Random.Range(-2f, 2f), Random.Range(-5f, 5f), Random.Range(-2f, 2f));
			component.AddTorque(torque, ForceMode.Impulse);
		}
	}

	private void ConfigureRigidbodyForBouncing(Rigidbody rb)
	{
		rb.mass = 1f;
		rb.linearDamping = 0.5f;
		rb.angularDamping = 2f;
		rb.useGravity = true;
		rb.interpolation = RigidbodyInterpolation.Interpolate;
		rb.freezeRotation = false;
		rb.constraints = (RigidbodyConstraints)80;
	}

	private PhysicsMaterial CreateBouncyPhysicsMaterial()
	{
		return new PhysicsMaterial("BouncyItem")
		{
			bounciness = 0.4f,
			dynamicFriction = 0.4f,
			staticFriction = 0.4f,
			frictionCombine = PhysicsMaterialCombine.Average,
			bounceCombine = PhysicsMaterialCombine.Average
		};
	}

	public void ResetRewards()
	{
		rewardGiven = false;
	}

	public void GiveRewardsManually()
	{
		if (enableRewards)
		{
			rewardGiven = false;
			GiveRewards();
		}
	}

	public void PlayAudioByIndex(int index)
	{
		if (animationSounds != null && index >= 0 && index < animationSounds.Length)
		{
			AudioClip audioClip = animationSounds[index];
			if (!(audioClip == null))
			{
				PlayAudioClip(audioClip, index);
			}
		}
	}

	public void PlayAudioClip(AudioClip clip)
	{
		PlayAudioClip(clip, -1);
	}

	protected void PlayAudioClip(AudioClip clip, int index = -1)
	{
		if (!(audioSource == null) && !(clip == null))
		{
			if (pitchVariation > 0f)
			{
				float pitch = 1f + Random.Range(0f - pitchVariation, pitchVariation);
				audioSource.pitch = pitch;
			}
			else
			{
				audioSource.pitch = 1f;
			}
			if (allowOverlappingSounds)
			{
				audioSource.PlayOneShot(clip, volumeMultiplier);
			}
			else
			{
				audioSource.Stop();
				audioSource.clip = clip;
				audioSource.Play();
			}
			OnAudioPlayed?.Invoke(clip);
			if (index >= 0)
			{
				OnAudioPlayedByIndex?.Invoke(index);
			}
		}
	}

	public void StopAudio()
	{
		if (audioSource != null)
		{
			audioSource.Stop();
		}
	}

	public void PlayRandomAudio()
	{
		if (animationSounds != null && animationSounds.Length != 0)
		{
			int index = Random.Range(0, animationSounds.Length);
			PlayAudioByIndex(index);
		}
	}

	public void SetVolume(float volume)
	{
		volumeMultiplier = Mathf.Clamp01(volume);
		if (audioSource != null)
		{
			audioSource.volume = volumeMultiplier;
		}
	}

	public void AddAudioClip(AudioClip clip)
	{
		if (!(clip == null))
		{
			if (animationSounds == null)
			{
				animationSounds = new AudioClip[0];
			}
			AudioClip[] array = new AudioClip[animationSounds.Length + 1];
			animationSounds.CopyTo(array, 0);
			array[animationSounds.Length] = clip;
			animationSounds = array;
		}
	}

	public AudioClip GetAudioClip(int index)
	{
		if (animationSounds == null || index < 0 || index >= animationSounds.Length)
		{
			return null;
		}
		return animationSounds[index];
	}

	public int GetAudioClipCount()
	{
		return animationSounds?.Length ?? 0;
	}

	public void OnLookedAt(GameObject player)
	{
		isBeingLookedAt = true;
		ShowPrompt();
	}

	public void OnLookedAway()
	{
		isBeingLookedAt = false;
		HidePrompt();
	}

	public virtual void OnInteracted()
	{
		if ((!oneTimeUse || !hasBeenUsed) && !isSwapping)
		{
			ActivateTrigger();
		}
	}

	protected void ShowPrompt()
	{
		if (!(promptUI != null))
		{
			return;
		}
		promptUI.SetActive(value: true);
		InputPromptLabel componentInChildren = promptUI.GetComponentInChildren<InputPromptLabel>(includeInactive: true);
		if (componentInChildren != null)
		{
			componentInChildren.Refresh();
			return;
		}
		Text componentInChildren2 = promptUI.GetComponentInChildren<Text>();
		if (componentInChildren2 != null)
		{
			componentInChildren2.text = promptText;
		}
		TextMeshProUGUI componentInChildren3 = promptUI.GetComponentInChildren<TextMeshProUGUI>();
		if (componentInChildren3 != null)
		{
			componentInChildren3.text = promptText;
		}
	}

	protected void HidePrompt()
	{
		if (promptUI != null)
		{
			promptUI.SetActive(value: false);
		}
	}

	protected virtual void ActivateTrigger()
	{
		if (!(firstPersonCamera == null) && !(targetCamera == null))
		{
			hasBeenUsed = true;
			HidePrompt();
			if (!string.IsNullOrEmpty(galleryEntryID))
			{
				GalleryProgressManager.Instance?.UnlockEnemy(galleryEntryID);
			}
			OnTriggerActivated?.Invoke();
			StartCoroutine(PerformCameraSwap());
		}
	}

	protected IEnumerator PerformCameraSwap()
	{
		isSwapping = true;
		DisableCombatSystems();
		if (inventoryUI != null)
		{
			if (inventoryUI.IsInventoryOpen)
			{
				inventoryUI.CloseInventory();
			}
			inventoryUI.SetInventoryEnabled(enabled: false);
		}
		if (firstPersonController != null)
		{
			firstPersonController.enabled = false;
		}
		Rigidbody rigidbody = ((firstPersonController != null) ? firstPersonController.GetComponent<Rigidbody>() : null);
		if (rigidbody != null)
		{
			rigidbody.linearVelocity = Vector3.zero;
		}
		if (cmfCameraController != null)
		{
			cmfCameraController.enabled = false;
		}
		if (cmfSmoothPosition != null)
		{
			cmfSmoothPosition.enabled = false;
		}
		if (cmfSmoothRotation != null)
		{
			cmfSmoothRotation.enabled = false;
		}
		if (cameraRoot != null)
		{
			originalCameraRootLocalPos = cameraRoot.localPosition;
			originalCameraRootLocalRot = cameraRoot.localRotation;
		}
		Vector3 vector = firstPersonCamera.transform.position - cameraRoot.position;
		Quaternion rotation = Quaternion.Inverse(cameraRoot.rotation) * firstPersonCamera.transform.rotation;
		Vector3 startPosition = cameraRoot.position;
		Quaternion startRotation = cameraRoot.rotation;
		float startFOV = firstPersonCamera.fieldOfView;
		Vector3 targetPosition = targetCamera.transform.position - targetCamera.transform.rotation * Quaternion.Inverse(rotation) * Quaternion.Inverse(cameraRoot.rotation) * vector;
		Quaternion targetRotation = targetCamera.transform.rotation * Quaternion.Inverse(rotation);
		float targetFOV = targetCamera.fieldOfView;
		float elapsedTime = 0f;
		while (elapsedTime < swapDuration)
		{
			elapsedTime += Time.deltaTime;
			float time = elapsedTime / swapDuration;
			float t = swapCurve.Evaluate(time);
			if (cameraRoot != null)
			{
				cameraRoot.position = Vector3.Lerp(startPosition, targetPosition, t);
				cameraRoot.rotation = Quaternion.Lerp(startRotation, targetRotation, t);
			}
			firstPersonCamera.fieldOfView = Mathf.Lerp(startFOV, targetFOV, t);
			yield return null;
		}
		if (cameraRoot != null)
		{
			cameraRoot.position = targetPosition;
			cameraRoot.rotation = targetRotation;
		}
		firstPersonCamera.fieldOfView = targetFOV;
		isInCinematicView = true;
		isSwapping = false;
		OnSwapCompleted?.Invoke();
		if (animatorToTrigger != null && !string.IsNullOrEmpty(animationTriggerName))
		{
			yield return new WaitForSeconds(animationDelay);
			TriggerAnimation();
			if (!useAnimationEvents)
			{
				yield return new WaitForSeconds(3f);
				DeactivateAnimator();
			}
		}
	}

	private void ReturnToFirstPerson()
	{
		if (isInCinematicView && !isSwapping)
		{
			StartCoroutine(ReturnToFirstPersonCoroutine());
		}
	}

	protected virtual IEnumerator ReturnToFirstPersonCoroutine()
	{
		isSwapping = true;
		Vector3 startPosition = ((cameraRoot != null) ? cameraRoot.position : firstPersonCamera.transform.position);
		Quaternion startRotation = ((cameraRoot != null) ? cameraRoot.rotation : firstPersonCamera.transform.rotation);
		float startFOV = firstPersonCamera.fieldOfView;
		Vector3 targetPosition = firstPersonController.transform.TransformPoint(originalCameraRootLocalPos);
		Quaternion targetRotation = firstPersonController.transform.rotation * originalCameraRootLocalRot;
		float targetFOV = 60f;
		float elapsedTime = 0f;
		while (elapsedTime < swapDuration)
		{
			elapsedTime += Time.deltaTime;
			float time = elapsedTime / swapDuration;
			float t = swapCurve.Evaluate(time);
			if (cameraRoot != null)
			{
				cameraRoot.position = Vector3.Lerp(startPosition, targetPosition, t);
				cameraRoot.rotation = Quaternion.Lerp(startRotation, targetRotation, t);
			}
			firstPersonCamera.fieldOfView = Mathf.Lerp(startFOV, targetFOV, t);
			yield return null;
		}
		if (cameraRoot != null)
		{
			cameraRoot.localPosition = originalCameraRootLocalPos;
			cameraRoot.localRotation = originalCameraRootLocalRot;
		}
		firstPersonCamera.fieldOfView = targetFOV;
		if (cmfSmoothPosition != null)
		{
			cmfSmoothPosition.enabled = true;
			cmfSmoothPosition.ResetCurrentPosition();
		}
		if (cmfSmoothRotation != null)
		{
			cmfSmoothRotation.enabled = true;
			cmfSmoothRotation.ResetCurrentRotation();
		}
		if (cmfCameraController != null)
		{
			cmfCameraController.enabled = true;
		}
		if (firstPersonController != null)
		{
			firstPersonController.enabled = true;
		}
		EnableCombatSystems();
		if (inventoryUI != null)
		{
			inventoryUI.SetInventoryEnabled(enabled: true);
		}
		isInCinematicView = false;
		isSwapping = false;
		OnReturnedToFirstPerson?.Invoke();
		if (animatorToTrigger != null && activateAnimatorObject)
		{
			DeactivateAnimator();
		}
		GiveRewards();
	}

	protected virtual void TriggerAnimation()
	{
		if (animatorToTrigger != null && !string.IsNullOrEmpty(animationTriggerName))
		{
			if (activateAnimatorObject)
			{
				animatorToTrigger.gameObject.SetActive(value: true);
			}
			animatorToTrigger.SetTrigger(animationTriggerName);
			OnAnimationTriggered?.Invoke();
		}
	}

	protected void DeactivateAnimator()
	{
		if (animatorToTrigger != null)
		{
			animatorToTrigger.gameObject.SetActive(value: false);
		}
	}

	public void OnAnimationStart()
	{
	}

	public void OnAnimationMidPoint()
	{
	}

	public void OnAnimationComplete()
	{
	}

	public void ReturnToFirstPersonFromAnimation()
	{
		if (isInCinematicView && !isSwapping)
		{
			ReturnToFirstPerson();
		}
	}

	public void OnCustomAnimationEvent(string eventName)
	{
		switch (eventName.ToLower())
		{
		case "enablehands":
			EnableCombatSystems();
			break;
		case "disablehands":
			DisableCombatSystems();
			break;
		case "return":
			ReturnToFirstPersonFromAnimation();
			break;
		case "givereward":
		case "reward":
			GiveRewardsManually();
			break;
		}
	}

	protected void DisableCombatSystems()
	{
		if (dualWieldingSystem == null)
		{
			return;
		}
		dualWieldingSystem.StopAllAttacks();
		dualWieldingSystem.StopBlocking();
		dualWieldingSystem.enabled = false;
		if (dualWieldingSystem.IsUsingBothHandsWeapon())
		{
			Animator bothHandsAnimator = GetBothHandsAnimator();
			if (bothHandsAnimator != null)
			{
				bothHandsAnimator.gameObject.SetActive(value: false);
			}
			return;
		}
		Animator leftHandAnimator = GetLeftHandAnimator();
		Animator rightHandAnimator = GetRightHandAnimator();
		if (leftHandAnimator != null)
		{
			leftHandAnimator.gameObject.SetActive(value: false);
		}
		if (rightHandAnimator != null)
		{
			rightHandAnimator.gameObject.SetActive(value: false);
		}
	}

	protected void EnableCombatSystems()
	{
		if (dualWieldingSystem == null)
		{
			return;
		}
		if (dualWieldingSystem.IsUsingBothHandsWeapon())
		{
			Animator bothHandsAnimator = GetBothHandsAnimator();
			if (bothHandsAnimator != null)
			{
				bothHandsAnimator.gameObject.SetActive(value: true);
			}
		}
		else
		{
			Animator leftHandAnimator = GetLeftHandAnimator();
			Animator rightHandAnimator = GetRightHandAnimator();
			if (leftHandAnimator != null)
			{
				leftHandAnimator.gameObject.SetActive(value: true);
			}
			if (rightHandAnimator != null)
			{
				rightHandAnimator.gameObject.SetActive(value: true);
			}
		}
		dualWieldingSystem.enabled = true;
	}

	private Animator GetLeftHandAnimator()
	{
		if (dualWieldingSystem == null)
		{
			return null;
		}
		return typeof(DualWieldingSystem).GetField("leftHandAnimator", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(dualWieldingSystem) as Animator;
	}

	private Animator GetRightHandAnimator()
	{
		if (dualWieldingSystem == null)
		{
			return null;
		}
		return typeof(DualWieldingSystem).GetField("rightHandAnimator", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(dualWieldingSystem) as Animator;
	}

	private Animator GetBothHandsAnimator()
	{
		if (dualWieldingSystem == null)
		{
			return null;
		}
		return typeof(DualWieldingSystem).GetField("bothHandsAnimator", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(dualWieldingSystem) as Animator;
	}

	public void ResetTrigger()
	{
		hasBeenUsed = false;
		ResetRewards();
	}

	public bool CanBeActivated()
	{
		if (isBeingLookedAt && !isSwapping)
		{
			if (oneTimeUse)
			{
				return !hasBeenUsed;
			}
			return true;
		}
		return false;
	}

	public void SetItemPool(ChestItemPool pool)
	{
		itemPool = pool;
	}

	public void SetPoolSpawningSettings(int minItems, int maxItems, bool guaranteedRare = false, ItemRarity minRarity = ItemRarity.Common)
	{
		minItemsFromPool = Mathf.Max(1, minItems);
		maxItemsFromPool = Mathf.Max(minItemsFromPool, maxItems);
		useGuaranteedRare = guaranteedRare;
		minimumRarity = minRarity;
	}

	public void EnablePoolRewards(ChestItemPool pool, int minItems = 1, int maxItems = 3)
	{
		SetItemPool(pool);
		SetPoolSpawningSettings(minItems, maxItems);
		enableRewards = true;
		rewardType = RewardType.SpawnFromPool;
	}

	public void SetHealingReward(int amount, bool fullHeal = false)
	{
		healAmount = amount;
		healToFullHealth = fullHeal;
		if (rewardType == RewardType.SpawnFromPool)
		{
			rewardType = RewardType.BothWithPool;
		}
		else
		{
			rewardType = RewardType.HealPlayer;
		}
		enableRewards = true;
	}

	public void DisableRewards()
	{
		enableRewards = false;
		rewardType = RewardType.None;
	}

	public string GetRewardDebugInfo()
	{
		if (!enableRewards)
		{
			return "Rewards disabled";
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("=== Reward System Debug ===");
		stringBuilder.AppendLine($"Reward Type: {rewardType}");
		stringBuilder.AppendLine($"Reward Given: {rewardGiven}");
		stringBuilder.AppendLine($"Effect Delay: {rewardEffectDelay}s");
		stringBuilder.AppendLine($"Item Spawn Delay: {itemSpawnDelay}s");
		if (rewardType == RewardType.HealPlayer || rewardType == RewardType.BothWithPool)
		{
			stringBuilder.AppendLine("Healing: " + (healToFullHealth ? "Full Health" : (healAmount + " HP")));
			stringBuilder.AppendLine($"Player Stats Found: {playerStats != null}");
		}
		if (rewardType == RewardType.SpawnFromPool || rewardType == RewardType.BothWithPool)
		{
			stringBuilder.AppendLine("=== Pool System ===");
			stringBuilder.AppendLine("Item Pool: " + (itemPool?.name ?? "None"));
			if (itemPool != null)
			{
				stringBuilder.AppendLine($"Pool Items: {itemPool.GetTotalItems()}");
				stringBuilder.AppendLine($"Pool Has Items: {itemPool.HasItems()}");
				foreach (KeyValuePair<ItemRarity, float> rarityProbability in itemPool.GetRarityProbabilities())
				{
					stringBuilder.AppendLine($"  {rarityProbability.Key}: {rarityProbability.Value:P1}");
				}
			}
			stringBuilder.AppendLine($"Item Count: {minItemsFromPool}-{maxItemsFromPool}");
			stringBuilder.AppendLine($"Min Rarity: {minimumRarity}");
			stringBuilder.AppendLine($"Guaranteed Rare: {useGuaranteedRare}");
			stringBuilder.AppendLine($"Allow Duplicates: {allowDuplicateItems}");
			stringBuilder.AppendLine($"Random Spawn Positions: {useRandomSpawnPositions}");
			if (useRandomSpawnPositions)
			{
				stringBuilder.AppendLine($"Spawn Radius: {spawnRadius}m");
			}
		}
		stringBuilder.AppendLine($"Add to Inventory: {addToInventoryDirectly}");
		stringBuilder.AppendLine($"Use Launch Arc: {useLaunchArc}");
		if (useLaunchArc)
		{
			stringBuilder.AppendLine($"Launch Force: {launchForce}");
			stringBuilder.AppendLine($"Launch Direction: {launchDirection}");
			stringBuilder.AppendLine($"Launch Variation: {launchVariation}");
		}
		stringBuilder.AppendLine("Pickup Prefab: " + (itemPickupPrefab?.name ?? "Auto-generated"));
		stringBuilder.AppendLine($"Player Inventory Found: {playerInventory != null}");
		return stringBuilder.ToString();
	}

	[ContextMenu("Test All Audio Clips")]
	public void TestAllAudioClips()
	{
		if (Application.isPlaying)
		{
			StartCoroutine(TestAudioSequence());
		}
	}

	private IEnumerator TestAudioSequence()
	{
		for (int i = 0; i < GetAudioClipCount(); i++)
		{
			if (animationSounds[i] != null)
			{
				PlayAudioByIndex(i);
				yield return new WaitForSeconds(animationSounds[i].length + 0.5f);
			}
		}
	}

	[ContextMenu("Test Reward System")]
	public void TestRewardSystem()
	{
		if (Application.isPlaying && enableRewards)
		{
			GiveRewardsManually();
		}
	}

	[ContextMenu("Reset Rewards Only")]
	public void ResetRewardsOnly()
	{
		ResetRewards();
	}

	private void OnDrawGizmos()
	{
		Gizmos.color = (isBeingLookedAt ? Color.green : Color.yellow);
		if (triggerCollider != null)
		{
			Gizmos.matrix = transform.localToWorldMatrix;
			if (triggerCollider is BoxCollider boxCollider)
			{
				Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
			}
			else if (triggerCollider is SphereCollider sphereCollider)
			{
				Gizmos.DrawWireSphere(sphereCollider.center, sphereCollider.radius);
			}
		}
		if (targetCamera != null)
		{
			Gizmos.color = Color.cyan;
			Gizmos.DrawWireSphere(targetCamera.transform.position, 0.5f);
			Gizmos.DrawRay(targetCamera.transform.position, targetCamera.transform.forward * 3f);
			Gizmos.color = Color.blue;
			Gizmos.DrawLine(transform.position, targetCamera.transform.position);
		}
		if (enableRewards && (rewardType == RewardType.SpawnFromPool || rewardType == RewardType.BothWithPool))
		{
			Vector3 itemSpawnPosition = GetItemSpawnPosition(0);
			Gizmos.color = Color.magenta;
			Gizmos.DrawWireSphere(itemSpawnPosition, 0.3f);
			Gizmos.DrawLine(transform.position, itemSpawnPosition);
			if (useRandomSpawnPositions && maxItemsFromPool > 1)
			{
				Gizmos.color = Color.magenta * 0.5f;
				Gizmos.DrawWireSphere(itemSpawnPosition, spawnRadius);
			}
			if (useLaunchArc)
			{
				Gizmos.color = Color.cyan;
				Vector3 vector;
				if (itemSpawnPoint != null)
				{
					vector = itemSpawnPoint.TransformDirection(launchDirection.normalized);
				}
				else
				{
					vector = ((!(firstPersonController != null)) ? transform.TransformDirection(launchDirection.normalized) : firstPersonController.transform.TransformDirection(launchDirection.normalized));
				}
				Gizmos.DrawRay(itemSpawnPosition, vector * (launchForce * 0.5f));
				Vector3 center = itemSpawnPosition + vector * launchForce * 0.3f;
				Gizmos.color = Color.yellow;
				Gizmos.DrawWireSphere(center, 0.15f);
			}
			if (itemSpawnPoint != null)
			{
				Gizmos.color = Color.red;
				Gizmos.DrawLine(transform.position, itemSpawnPoint.position);
			}
		}
		if (Application.isPlaying)
		{
			Gizmos.color = (hasBeenUsed ? Color.red : Color.white);
		}
	}

	private void OnValidate()
	{
		if (healAmount < 0)
		{
			healAmount = 0;
		}
		if (minItemsFromPool < 1)
		{
			minItemsFromPool = 1;
		}
		if (maxItemsFromPool < minItemsFromPool)
		{
			maxItemsFromPool = minItemsFromPool;
		}
		if (maxItemsFromPool > 20 && spawnRadius < 0f)
		{
			spawnRadius = 0.5f;
		}
		if (rewardEffectDelay < 0f)
		{
			rewardEffectDelay = 0f;
		}
		if (itemSpawnDelay < 0f)
		{
			itemSpawnDelay = 0f;
		}
		if (launchForce < 0f)
		{
			launchForce = 0f;
		}
		if (launchVariation < 0f)
		{
			launchVariation = 0f;
		}
		if (launchDirection == Vector3.zero)
		{
			launchDirection = new Vector3(0f, 0.5f, 1f);
		}
		if (enableRewards)
		{
			RewardType rewardType = this.rewardType;
			_ = rewardType - 2;
			_ = 1;
		}
	}
}
