using System.Collections;
using System.Reflection;
using CMF;
using PixelCrushers.GridController;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class GrabScreen : MonoBehaviour
{
	[Header("Grab Settings")]
	[SerializeField]
	private bool stopPlayerMovement = true;

	[SerializeField]
	private float movementReduction;

	[Header("UI Elements")]
	[SerializeField]
	private GameObject grabUI;

	[SerializeField]
	private GameObject grabOverlay;

	[SerializeField]
	private GameObject grabImage;

	[SerializeField]
	private Text grabText;

	[SerializeField]
	private Button struggleButton;

	[Header("Enemy Health Combat")]
	[SerializeField]
	private Text enemyHealthText;

	[SerializeField]
	private Image enemyHealthFillImage;

	[SerializeField]
	private GameObject enemyHealthUI;

	[Header("Animation Controller")]
	[SerializeField]
	private Animator grabScreenAnimator;

	[SerializeField]
	private RuntimeAnimatorController defaultController;

	[Header("Audio")]
	[SerializeField]
	private AudioClip grabStartSound;

	[SerializeField]
	private AudioClip grabLoopSound;

	[SerializeField]
	private AudioClip grabEndSound;

	[SerializeField]
	private AudioMixerGroup sfxMixerGroup;

	[Header("Visual Effects")]
	[SerializeField]
	private GameObject grabEffect;

	[SerializeField]
	private Color grabOverlayColor = new Color(1f, 0f, 0f, 0.3f);

	[Header("Heat Buildup System")]
	[SerializeField]
	private bool enableHeatBuildup = true;

	[SerializeField]
	private float heatBuildupRate = 15f;

	[SerializeField]
	private float maxHeatDamage = 15f;

	[SerializeField]
	private float heatCooldownRate = 25f;

	[SerializeField]
	[Range(0f, 1f)]
	private float grabDamagePercent = 0.1f;

	[Header("Teleport System")]
	[SerializeField]
	private bool enableTeleport;

	[SerializeField]
	private Vector3 teleportDestination = Vector3.zero;

	[SerializeField]
	private Transform teleportPoint;

	private bool isGrabbed;

	private GameObject grabbingEnemy;

	private GameObject hiddenEnemyToRestore;

	private PlayerStats playerStats;

	private FirstPersonController playerController;

	private CameraController cameraController;

	private SmoothPosition cmfSmoothPosition;

	private SmoothRotation cmfSmoothRotation;

	private Transform cameraRoot;

	private Vector3 originalCameraRootLocalPos;

	private Quaternion originalCameraRootLocalRot;

	private DualWieldingSystem dualWieldingSystem;

	private GridInventorySystem playerInventory;

	private InventoryUI inventoryUI;

	private AudioSource audioSource;

	private float originalMovementMultiplier;

	private Image overlayImage;

	private RuntimeAnimatorController originalController;

	private RuntimeAnimatorController currentGrabController;

	private Camera playerCamera;

	private Rigidbody playerRigidbody;

	private float originalCameraY;

	private Vector3 originalCameraPosition;

	private Quaternion originalCameraRotation;

	private float originalCameraPitch;

	private float originalCameraYaw;

	private bool originalCursorLockState;

	private CursorLockMode originalCursorMode;

	private float enemyCurrentHealth;

	private float enemyMaxHealth;

	private string enemyName;

	private CanvasGroup leftHandCanvasGroup;

	private CanvasGroup rightHandCanvasGroup;

	private CanvasGroup bothHandsCanvasGroup;

	private bool handUIHidden;

	private float lastHeatBuildup;

	private bool isCoolingDown;

	private bool maxHeatAnimationPlaying;

	private bool heatIncreaseBlocked;

	private bool heatFullyCooled;

	private bool isDragonGrab;

	private WeaponData savedLeftHandWeapon;

	private WeaponData savedRightHandWeapon;

	private WeaponData savedBothHandsWeapon;

	private bool weaponSwappedForGrab;

	private GrabAnimationData[] currentGrabAnimations;

	private int currentAnimStateHash;

	public static GrabScreen Instance { get; private set; }

	public bool IsGrabbed => isGrabbed;

	public GameObject GrabbingEnemy => grabbingEnemy;

	public Button StruggleButton => struggleButton;

	public Text GrabText => grabText;

	private void Awake()
	{
		Instance = this;
		GameObject gameObject = GameObject.FindGameObjectWithTag("Player");
		if (gameObject != null)
		{
			playerStats = gameObject.GetComponent<PlayerStats>();
			playerStats.OnPlayerDeath.AddListener(HideEnemyHealthUI);
			playerController = gameObject.GetComponent<FirstPersonController>();
			playerRigidbody = gameObject.GetComponent<Rigidbody>();
			if (playerController != null && playerController.cameraTransform != null)
			{
				cameraController = playerController.cameraTransform.GetComponent<CameraController>();
				cameraRoot = playerController.cameraTransform;
				while (cameraRoot.parent != null && cameraRoot.parent != playerController.transform)
				{
					cameraRoot = cameraRoot.parent;
				}
				cmfSmoothPosition = cameraRoot.GetComponent<SmoothPosition>();
				cmfSmoothRotation = cameraRoot.GetComponent<SmoothRotation>();
			}
			dualWieldingSystem = gameObject.GetComponent<DualWieldingSystem>();
			playerInventory = gameObject.GetComponent<GridInventorySystem>();
			inventoryUI = gameObject.GetComponent<InventoryUI>();
			playerCamera = gameObject.GetComponentInChildren<Camera>();
			if (playerCamera == null)
			{
				GameObject gameObject2 = GameObject.FindGameObjectWithTag("MainCamera");
				if (gameObject2 != null)
				{
					playerCamera = gameObject2.GetComponent<Camera>();
				}
			}
		}
		if (dualWieldingSystem == null)
		{
			dualWieldingSystem = GetComponentInParent<DualWieldingSystem>();
		}
		if (playerInventory == null)
		{
			playerInventory = Object.FindAnyObjectByType<GridInventorySystem>();
		}
		if (inventoryUI == null)
		{
			inventoryUI = Object.FindAnyObjectByType<InventoryUI>();
		}
		if (enableTeleport)
		{
			FindTeleportPoint();
		}
		audioSource = GetComponent<AudioSource>();
		if (audioSource == null)
		{
			audioSource = base.gameObject.AddComponent<AudioSource>();
		}
		if (audioSource != null && sfxMixerGroup != null)
		{
			audioSource.outputAudioMixerGroup = sfxMixerGroup;
		}
		InitializeUI();
		if (struggleButton != null)
		{
			struggleButton.onClick.AddListener(OnStruggleButtonPressed);
		}
		if (grabScreenAnimator != null)
		{
			originalController = grabScreenAnimator.runtimeAnimatorController;
			if (defaultController == null)
			{
				defaultController = originalController;
			}
		}
	}

	private void Update()
	{
		if (isGrabbed && enableHeatBuildup && playerStats != null)
		{
			HandleHeatBuildup();
		}
		if (isGrabbed)
		{
			CheckAnimationStateSound();
		}
	}

	private void InitializeUI()
	{
		if (grabUI != null)
		{
			grabUI.SetActive(value: false);
		}
		if (grabOverlay != null)
		{
			overlayImage = grabOverlay.GetComponent<Image>();
			if (overlayImage != null)
			{
				overlayImage.color = grabOverlayColor;
			}
			grabOverlay.SetActive(value: false);
		}
		if (grabImage != null)
		{
			grabImage.SetActive(value: false);
		}
		if (enemyHealthUI != null)
		{
			enemyHealthUI.SetActive(value: false);
		}
		UpdateGrabText();
	}

	public void StartGrab(GameObject enemy, RuntimeAnimatorController grabController = null, Vector3 positionOffset = default(Vector3), Vector3 rotationOffset = default(Vector3), bool keepPlayerRotation = false, GrabAnimationData[] overrideAnimations = null)
	{
		if (isGrabbed || playerStats == null || !playerStats.CanBeGrabbed)
		{
			return;
		}
		isGrabbed = true;
		LevelStatsTracker.RecordGrab();
		grabbingEnemy = enemy;
		isDragonGrab = enemy.GetComponent<DragonEnemyAI>() != null || enemy.GetComponent<ProximityDragonEnemyAI>() != null;
		if (overrideAnimations != null && overrideAnimations.Length != 0)
		{
			currentGrabAnimations = overrideAnimations;
		}
		else
		{
			ResolveGrabAnimations(enemy);
		}
		currentAnimStateHash = 0;
		UnlockGalleryEntry(enemy);
		if (playerInventory != null)
		{
			int totalThornsDamage = playerInventory.GetTotalThornsDamage();
			if (totalThornsDamage > 0)
			{
				enemy.GetComponent<IDamageable>()?.TakeDamage(totalThornsDamage);
			}
		}
		TransferEnemyHealth(enemy);
		if (enemyCurrentHealth <= 0f)
		{
			if (enemyHealthUI != null)
			{
				enemyHealthUI.SetActive(value: false);
			}
			isGrabbed = false;
			grabbingEnemy = null;
			return;
		}
		SwapAnimationController(grabController);
		if (playerStats != null)
		{
			originalMovementMultiplier = playerStats.GetExternalMovementMultiplier();
		}
		ApplyMovementRestriction();
		if (playerStats != null)
		{
			playerStats.SetInvulnerable(invulnerable: true);
		}
		ApplyPlayerPositioning(enemy, positionOffset, rotationOffset, keepPlayerRotation);
		HandleWeaponSwapForGrab();
		DisablePlayerControls();
		ShowGrabUI();
		PlayGrabAnimation();
		PlayGrabAudio();
		ShowGrabEffects();
	}

	private void ApplyMovementRestriction()
	{
		if (playerStats == null)
		{
			return;
		}
		if (stopPlayerMovement)
		{
			playerStats.SetExternalMovementMultiplier(0f);
		}
		else
		{
			float externalMovementMultiplier = originalMovementMultiplier * movementReduction;
			playerStats.SetExternalMovementMultiplier(externalMovementMultiplier);
		}
		if (playerController != null)
		{
			Rigidbody component = playerController.GetComponent<Rigidbody>();
			if (component != null)
			{
				component.linearVelocity = Vector3.zero;
				component.angularVelocity = Vector3.zero;
			}
		}
	}

	private void ApplyPlayerPositioning(GameObject enemy, Vector3 positionOffset, Vector3 rotationOffset, bool keepPlayerRotation = false)
	{
		if (playerCamera == null || enemy == null)
		{
			return;
		}
		if (cameraRoot != null)
		{
			originalCameraRootLocalPos = cameraRoot.localPosition;
			originalCameraRootLocalRot = cameraRoot.localRotation;
		}
		if (cameraController != null)
		{
			originalCameraPitch = cameraController.GetCurrentXAngle();
			originalCameraYaw = cameraController.GetCurrentYAngle();
		}
		if (!keepPlayerRotation && !(cameraRoot == null))
		{
			Vector3 vector = playerCamera.transform.position - cameraRoot.position;
			Quaternion rotation = Quaternion.Inverse(cameraRoot.rotation) * playerCamera.transform.rotation;
			Vector3 vector2 = enemy.transform.position + enemy.transform.TransformDirection(positionOffset);
			Quaternion quaternion = enemy.transform.rotation * Quaternion.Euler(rotationOffset) * Quaternion.Inverse(rotation);
			Vector3 position = vector2 - quaternion * (Quaternion.Inverse(cameraRoot.rotation) * vector);
			if (positionOffset != Vector3.zero)
			{
				cameraRoot.position = position;
			}
			if (rotationOffset != Vector3.zero)
			{
				cameraRoot.rotation = quaternion;
			}
		}
	}

	private void DisablePlayerControls()
	{
		if (cameraRoot != null && originalCameraRootLocalPos == Vector3.zero)
		{
			originalCameraRootLocalPos = cameraRoot.localPosition;
			originalCameraRootLocalRot = cameraRoot.localRotation;
		}
		originalCursorMode = Cursor.lockState;
		Cursor.lockState = CursorLockMode.None;
		if (playerController != null)
		{
			playerController.SetMomentum(Vector3.zero);
		}
		if (playerRigidbody != null)
		{
			playerRigidbody.linearVelocity = Vector3.zero;
			playerRigidbody.isKinematic = true;
		}
		if (playerController != null)
		{
			playerController.enabled = false;
		}
		if (cameraController != null)
		{
			cameraController.enabled = false;
		}
		if (cmfSmoothPosition != null)
		{
			cmfSmoothPosition.enabled = false;
		}
		if (cmfSmoothRotation != null)
		{
			cmfSmoothRotation.enabled = false;
		}
		DisableHandUIOnly();
		DisableInventoryAccess();
		if (playerStats != null)
		{
			playerStats.SetExternalMovementMultiplier(0f);
		}
	}

	private void RestorePlayerControls()
	{
		if (cameraRoot != null)
		{
			cameraRoot.localPosition = originalCameraRootLocalPos;
			cameraRoot.localRotation = originalCameraRootLocalRot;
		}
		if (cameraController != null)
		{
			cameraController.SetRotationAngles(originalCameraPitch, originalCameraYaw);
		}
		Cursor.lockState = originalCursorMode;
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
		if (cameraController != null)
		{
			cameraController.enabled = true;
		}
		if (playerRigidbody != null)
		{
			playerRigidbody.isKinematic = false;
		}
		if (playerController != null)
		{
			playerController.enabled = true;
		}
		EnableHandUI();
		EnableInventoryAccess();
	}

	private void ShowGrabUI()
	{
		if (grabUI != null)
		{
			grabUI.SetActive(value: true);
		}
		if (grabOverlay != null)
		{
			grabOverlay.SetActive(value: true);
		}
		if (grabImage != null)
		{
			grabImage.SetActive(value: true);
		}
		UpdateGrabText();
	}

	private void UpdateGrabText()
	{
		if (grabText != null)
		{
			if (isGrabbed && grabbingEnemy != null)
			{
				string text = "Grabbed by " + grabbingEnemy.name + "!\nPress button to struggle!";
				grabText.text = text;
			}
			else
			{
				grabText.text = "Press button to struggle!";
			}
		}
	}

	private void SwapAnimationController(RuntimeAnimatorController grabController)
	{
		if (!(grabScreenAnimator == null))
		{
			originalController = grabScreenAnimator.runtimeAnimatorController;
			if (grabController != null)
			{
				currentGrabController = grabController;
				grabScreenAnimator.runtimeAnimatorController = grabController;
			}
			else
			{
				currentGrabController = originalController;
			}
		}
	}

	private void PlayGrabAnimation()
	{
	}

	private void PlayGrabAudio()
	{
		if (!(audioSource == null) && (currentGrabAnimations == null || currentGrabAnimations.Length == 0))
		{
			if (grabStartSound != null)
			{
				audioSource.PlayOneShot(grabStartSound);
			}
			if (grabLoopSound != null)
			{
				audioSource.clip = grabLoopSound;
				audioSource.loop = true;
				audioSource.Play();
			}
		}
	}

	private void ShowGrabEffects()
	{
		if (grabEffect != null && playerStats != null)
		{
			Object.Instantiate(grabEffect, playerStats.transform.position, Quaternion.identity);
		}
	}

	private void OnStruggleButtonPressed()
	{
		if (isGrabbed)
		{
			EndGrab();
		}
	}

	public void RegisterHiddenEnemy(GameObject enemy)
	{
		hiddenEnemyToRestore = enemy;
	}

	public void EndGrab()
	{
		if (isGrabbed)
		{
			if (hiddenEnemyToRestore != null)
			{
				hiddenEnemyToRestore.GetComponent<IGrabHideable>()?.EndGrabHidden(enemyCurrentHealth);
				hiddenEnemyToRestore = null;
			}
			isGrabbed = false;
			RestoreMovement();
			if (playerStats != null)
			{
				playerStats.SetInvulnerable(invulnerable: false);
			}
			RestorePlayerControls();
			RestoreWeaponAfterGrab();
			HideGrabUI();
			HideEnemyHealthUI();
			PlayEndAnimation();
			PlayEndAudio();
			grabbingEnemy = null;
			currentGrabAnimations = null;
			currentAnimStateHash = 0;
			lastHeatBuildup = 0f;
			isCoolingDown = false;
			maxHeatAnimationPlaying = false;
			heatIncreaseBlocked = false;
			heatFullyCooled = false;
			isDragonGrab = false;
			if (grabScreenAnimator != null)
			{
				grabScreenAnimator.SetBool("MaxHeat", value: false);
			}
			originalCameraRootLocalPos = Vector3.zero;
			originalCameraRootLocalRot = Quaternion.identity;
			if (playerStats != null)
			{
				StartCoroutine(GrabImmunity());
			}
		}
	}

	private IEnumerator GrabImmunity()
	{
		playerStats.CanBeGrabbed = false;
		yield return new WaitForSeconds(1f);
		playerStats.CanBeGrabbed = true;
	}

	private void RestoreMovement()
	{
		if (playerStats != null)
		{
			playerStats.SetExternalMovementMultiplier(originalMovementMultiplier);
		}
	}

	private void HideGrabUI()
	{
		if (grabUI != null)
		{
			grabUI.SetActive(value: false);
		}
		if (grabOverlay != null)
		{
			grabOverlay.SetActive(value: false);
		}
		if (grabImage != null)
		{
			grabImage.SetActive(value: false);
		}
	}

	private void PlayEndAnimation()
	{
		RestoreAnimationController();
	}

	private void RestoreAnimationController()
	{
		if (!(grabScreenAnimator == null))
		{
			RuntimeAnimatorController runtimeAnimatorController = ((originalController != null) ? originalController : defaultController);
			if (runtimeAnimatorController != null)
			{
				grabScreenAnimator.runtimeAnimatorController = runtimeAnimatorController;
			}
			currentGrabController = null;
		}
	}

	private void PlayEndAudio()
	{
		if (!(audioSource == null))
		{
			audioSource.loop = false;
			audioSource.Stop();
			if (grabEndSound != null)
			{
				audioSource.PlayOneShot(grabEndSound);
			}
		}
	}

	private void HandleWeaponSwapForGrab()
	{
		if (dualWieldingSystem == null)
		{
			return;
		}
		weaponSwappedForGrab = false;
		savedLeftHandWeapon = null;
		savedRightHandWeapon = null;
		savedBothHandsWeapon = null;
		if (dualWieldingSystem.IsUsingBothHandsWeapon())
		{
			WeaponData bothHandsWeapon = dualWieldingSystem.GetBothHandsWeapon();
			if (bothHandsWeapon != null && bothHandsWeapon.IsProjectileWeapon)
			{
				savedBothHandsWeapon = bothHandsWeapon;
				dualWieldingSystem.ForceUnequipBothHandsWeapon();
				weaponSwappedForGrab = true;
			}
			return;
		}
		WeaponData weaponInHand = dualWieldingSystem.GetWeaponInHand(HandSlot.LeftHand);
		if (weaponInHand != null && weaponInHand.IsProjectileWeapon && !dualWieldingSystem.IsHandUnarmed(HandSlot.LeftHand))
		{
			savedLeftHandWeapon = weaponInHand;
			dualWieldingSystem.HandleEmptyWeaponSlot(HandSlot.LeftHand);
			weaponSwappedForGrab = true;
		}
		WeaponData weaponInHand2 = dualWieldingSystem.GetWeaponInHand(HandSlot.RightHand);
		if (weaponInHand2 != null && weaponInHand2.IsProjectileWeapon && !dualWieldingSystem.IsHandUnarmed(HandSlot.RightHand))
		{
			savedRightHandWeapon = weaponInHand2;
			dualWieldingSystem.HandleEmptyWeaponSlot(HandSlot.RightHand);
			weaponSwappedForGrab = true;
		}
	}

	private void RestoreWeaponAfterGrab()
	{
		if (!weaponSwappedForGrab || dualWieldingSystem == null)
		{
			return;
		}
		if (savedBothHandsWeapon != null)
		{
			dualWieldingSystem.EquipWeapon(savedBothHandsWeapon, HandSlot.BothHands);
		}
		else
		{
			if (savedLeftHandWeapon != null)
			{
				dualWieldingSystem.EquipWeapon(savedLeftHandWeapon, HandSlot.LeftHand);
			}
			if (savedRightHandWeapon != null)
			{
				dualWieldingSystem.EquipWeapon(savedRightHandWeapon, HandSlot.RightHand);
			}
		}
		savedLeftHandWeapon = null;
		savedRightHandWeapon = null;
		savedBothHandsWeapon = null;
		weaponSwappedForGrab = false;
	}

	public void ForceEndGrab()
	{
		if (isGrabbed)
		{
			EndGrab();
		}
	}

	public void SetMovementReduction(float reduction)
	{
		movementReduction = Mathf.Clamp01(reduction);
	}

	public void SetStopPlayerMovement(bool stopMovement)
	{
		stopPlayerMovement = stopMovement;
	}

	private void DisableHandUIOnly()
	{
		if (dualWieldingSystem == null)
		{
			return;
		}
		Animator leftHandAnimator = GetLeftHandAnimator();
		Animator rightHandAnimator = GetRightHandAnimator();
		Animator bothHandsAnimator = GetBothHandsAnimator();
		if (leftHandAnimator != null && leftHandAnimator.transform.parent != null)
		{
			GameObject gameObject = leftHandAnimator.transform.parent.gameObject;
			leftHandCanvasGroup = gameObject.GetComponent<CanvasGroup>();
			if (leftHandCanvasGroup == null)
			{
				leftHandCanvasGroup = gameObject.AddComponent<CanvasGroup>();
			}
			leftHandCanvasGroup.alpha = 0f;
		}
		if (rightHandAnimator != null && rightHandAnimator.transform.parent != null)
		{
			GameObject gameObject2 = rightHandAnimator.transform.parent.gameObject;
			rightHandCanvasGroup = gameObject2.GetComponent<CanvasGroup>();
			if (rightHandCanvasGroup == null)
			{
				rightHandCanvasGroup = gameObject2.AddComponent<CanvasGroup>();
			}
			rightHandCanvasGroup.alpha = 0f;
		}
		if (bothHandsAnimator != null && bothHandsAnimator.transform.parent != null)
		{
			GameObject gameObject3 = bothHandsAnimator.transform.parent.gameObject;
			bothHandsCanvasGroup = gameObject3.GetComponent<CanvasGroup>();
			if (bothHandsCanvasGroup == null)
			{
				bothHandsCanvasGroup = gameObject3.AddComponent<CanvasGroup>();
			}
			bothHandsCanvasGroup.alpha = 0f;
		}
		handUIHidden = true;
	}

	private void DisableHandUI()
	{
		if (!(dualWieldingSystem == null))
		{
			dualWieldingSystem.StopAllAttacks();
			dualWieldingSystem.StopBlocking();
			dualWieldingSystem.enabled = false;
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
	}

	private void EnableHandUI()
	{
		if (handUIHidden)
		{
			if (leftHandCanvasGroup != null)
			{
				leftHandCanvasGroup.alpha = 1f;
			}
			if (rightHandCanvasGroup != null)
			{
				rightHandCanvasGroup.alpha = 1f;
			}
			if (bothHandsCanvasGroup != null)
			{
				bothHandsCanvasGroup.alpha = 1f;
			}
			handUIHidden = false;
		}
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

	private void HideHandRenderers(GameObject handObject)
	{
		if (handObject == null)
		{
			return;
		}
		Renderer[] componentsInChildren = handObject.GetComponentsInChildren<Renderer>();
		foreach (Renderer renderer in componentsInChildren)
		{
			if (renderer != null)
			{
				renderer.enabled = false;
			}
		}
	}

	private void ShowHandRenderers(GameObject handObject)
	{
		if (handObject == null)
		{
			return;
		}
		Renderer[] componentsInChildren = handObject.GetComponentsInChildren<Renderer>();
		foreach (Renderer renderer in componentsInChildren)
		{
			if (renderer != null)
			{
				renderer.enabled = true;
			}
		}
	}

	private void ResolveGrabAnimations(GameObject enemy)
	{
		currentGrabAnimations = null;
		EnemyAI component = enemy.GetComponent<EnemyAI>();
		if (component != null)
		{
			currentGrabAnimations = component.grabScreenAnimations;
			return;
		}
		ProjectileEnemyAI component2 = enemy.GetComponent<ProjectileEnemyAI>();
		if (component2 != null)
		{
			currentGrabAnimations = component2.grabScreenAnimations;
			return;
		}
		DragonEnemyAI component3 = enemy.GetComponent<DragonEnemyAI>();
		if (component3 != null)
		{
			currentGrabAnimations = component3.grabScreenAnimations;
			return;
		}
		ProximityDragonEnemyAI component4 = enemy.GetComponent<ProximityDragonEnemyAI>();
		if (component4 != null)
		{
			currentGrabAnimations = component4.grabScreenAnimations;
			return;
		}
		SpinningEnemyAI component5 = enemy.GetComponent<SpinningEnemyAI>();
		if (component5 != null)
		{
			currentGrabAnimations = component5.grabScreenAnimations;
			return;
		}
		ChargingEnemyAI component6 = enemy.GetComponent<ChargingEnemyAI>();
		if (component6 != null)
		{
			currentGrabAnimations = component6.grabScreenAnimations;
			return;
		}
		MimicEnemy component7 = enemy.GetComponent<MimicEnemy>();
		if (component7 != null)
		{
			currentGrabAnimations = component7.grabScreenAnimations;
		}
	}

	private void CheckAnimationStateSound()
	{
		if (grabScreenAnimator == null || currentGrabAnimations == null || currentGrabAnimations.Length == 0)
		{
			return;
		}
		AnimatorStateInfo currentAnimatorStateInfo = grabScreenAnimator.GetCurrentAnimatorStateInfo(0);
		if (currentAnimatorStateInfo.shortNameHash == currentAnimStateHash)
		{
			return;
		}
		currentAnimStateHash = currentAnimatorStateInfo.shortNameHash;
		GrabAnimationData[] array = currentGrabAnimations;
		foreach (GrabAnimationData grabAnimationData in array)
		{
			if (grabAnimationData != null && !string.IsNullOrEmpty(grabAnimationData.animationName) && currentAnimatorStateInfo.IsName(grabAnimationData.animationName))
			{
				if (grabAnimationData.sound != null && audioSource != null)
				{
					audioSource.Stop();
					audioSource.clip = grabAnimationData.sound;
					audioSource.loop = true;
					audioSource.Play();
				}
				break;
			}
		}
	}

	private void UnlockGalleryEntry(GameObject enemy)
	{
		string text = null;
		EnemyAI component = enemy.GetComponent<EnemyAI>();
		if (component != null)
		{
			text = component.galleryEnemyID;
		}
		else
		{
			ProjectileEnemyAI component2 = enemy.GetComponent<ProjectileEnemyAI>();
			DragonEnemyAI component3 = enemy.GetComponent<DragonEnemyAI>();
			ProximityDragonEnemyAI component4 = enemy.GetComponent<ProximityDragonEnemyAI>();
			SpinningEnemyAI component5 = enemy.GetComponent<SpinningEnemyAI>();
			ChargingEnemyAI component6 = enemy.GetComponent<ChargingEnemyAI>();
			BrawlerEnemyAI component7 = enemy.GetComponent<BrawlerEnemyAI>();
			MimicEnemy component8 = enemy.GetComponent<MimicEnemy>();
			if (component2 != null)
			{
				text = component2.galleryEnemyID;
			}
			else if (component3 != null)
			{
				text = component3.galleryEnemyID;
			}
			else if (component4 != null)
			{
				text = component4.galleryEnemyID;
			}
			else if (component5 != null)
			{
				text = component5.galleryEnemyID;
			}
			else if (component6 != null)
			{
				text = component6.galleryEnemyID;
			}
			else if (component7 != null)
			{
				text = component7.galleryEnemyID;
			}
			else if (component8 != null)
			{
				text = component8.galleryEnemyID;
			}
		}
		if (!string.IsNullOrEmpty(text))
		{
			GalleryProgressManager.Instance?.UnlockEnemy(text);
		}
	}

	private void TransferEnemyHealth(GameObject enemy)
	{
		EnemyAI component = enemy.GetComponent<EnemyAI>();
		if (component != null)
		{
			enemyCurrentHealth = component.currentHealth;
			enemyMaxHealth = component.maxHealth;
			enemyName = enemy.name;
		}
		else
		{
			ProjectileEnemyAI component2 = enemy.GetComponent<ProjectileEnemyAI>();
			DragonEnemyAI component3 = enemy.GetComponent<DragonEnemyAI>();
			ProximityDragonEnemyAI component4 = enemy.GetComponent<ProximityDragonEnemyAI>();
			SpinningEnemyAI component5 = enemy.GetComponent<SpinningEnemyAI>();
			ChargingEnemyAI component6 = enemy.GetComponent<ChargingEnemyAI>();
			BrawlerEnemyAI component7 = enemy.GetComponent<BrawlerEnemyAI>();
			if (component2 != null)
			{
				enemyCurrentHealth = component2.currentHealth;
				enemyMaxHealth = component2.maxHealth;
			}
			else if (component3 != null)
			{
				enemyCurrentHealth = component3.currentHealth;
				enemyMaxHealth = component3.maxHealth;
			}
			else if (component4 != null)
			{
				enemyCurrentHealth = component4.currentHealth;
				enemyMaxHealth = component4.maxHealth;
			}
			else if (component5 != null)
			{
				enemyCurrentHealth = component5.currentHealth;
				enemyMaxHealth = component5.maxHealth;
			}
			else if (component6 != null)
			{
				enemyCurrentHealth = component6.currentHealth;
				enemyMaxHealth = component6.maxHealth;
			}
			else if (component7 != null)
			{
				enemyCurrentHealth = component7.currentHealth;
				enemyMaxHealth = component7.maxHealth;
			}
			else
			{
				enemyCurrentHealth = 100f;
				enemyMaxHealth = 100f;
			}
			enemyName = enemy.name;
		}
		UpdateEnemyHealthUI();
		ShowEnemyHealthUI();
	}

	private void UpdateEnemyHealthUI()
	{
		if (enemyHealthText != null)
		{
			enemyHealthText.text = $"{enemyName}: {enemyCurrentHealth:F0}/{enemyMaxHealth:F0} HP";
		}
		if (enemyHealthFillImage != null)
		{
			enemyHealthFillImage.fillAmount = enemyCurrentHealth / enemyMaxHealth;
		}
	}

	private void ShowEnemyHealthUI()
	{
		if (enemyHealthUI != null && playerStats != null && playerStats.IsAlive)
		{
			enemyHealthUI.SetActive(value: true);
		}
	}

	private void HideEnemyHealthUI()
	{
		if (enemyHealthUI != null)
		{
			enemyHealthUI.SetActive(value: false);
		}
	}

	public void DamageEnemy(float damage)
	{
		if (isGrabbed)
		{
			enemyCurrentHealth -= damage;
			enemyCurrentHealth = Mathf.Max(0f, enemyCurrentHealth);
			UpdateEnemyHealthUI();
			if (enemyCurrentHealth <= 0f)
			{
				EndGrab();
			}
		}
	}

	public void OnPlayerAttackDuringGrab(float damage)
	{
		if (TrinketManager.Instance != null)
		{
			damage += TrinketManager.Instance.GetGrabDamageBonus();
		}
		DamageEnemy(damage);
	}

	private void DisableInventoryAccess()
	{
		if (!(inventoryUI == null))
		{
			if (inventoryUI.IsInventoryOpen)
			{
				inventoryUI.ForceCloseInventory();
			}
			inventoryUI.enabled = false;
			if (playerInventory != null)
			{
				playerInventory.enabled = false;
			}
		}
	}

	private void EnableInventoryAccess()
	{
		if (inventoryUI != null)
		{
			inventoryUI.enabled = true;
		}
		if (playerInventory != null)
		{
			playerInventory.enabled = true;
		}
	}

	private void OnDestroy()
	{
		if (playerStats != null)
		{
			playerStats.OnPlayerDeath.RemoveListener(HideEnemyHealthUI);
		}
		if (isGrabbed && playerStats != null)
		{
			playerStats.SetExternalMovementMultiplier(originalMovementMultiplier);
		}
		if (isGrabbed)
		{
			RestorePlayerControls();
			RestoreAnimationController();
		}
	}

	private void HandleHeatBuildup()
	{
		float time = Time.time;
		if (isCoolingDown)
		{
			if (!(time - lastHeatBuildup >= 0.05f))
			{
				return;
			}
			float amount = heatCooldownRate * 0.05f;
			playerStats.CoolHeat(amount);
			lastHeatBuildup = time;
			UpdateGrabOverlayIntensity();
			if (playerStats.CurrentHeat <= 0f)
			{
				if (maxHeatAnimationPlaying)
				{
					heatFullyCooled = true;
				}
				else
				{
					isCoolingDown = false;
				}
			}
		}
		else if (time - lastHeatBuildup >= 0.1f && !heatIncreaseBlocked)
		{
			float amount2 = heatBuildupRate * 0.1f;
			playerStats.GenerateHeat(amount2);
			lastHeatBuildup = time;
			UpdateGrabOverlayIntensity();
			if (playerStats.CurrentHeat >= playerStats.MaxHeat && !maxHeatAnimationPlaying)
			{
				TriggerMaxHeatAnimation();
			}
		}
	}

	private void UpdateGrabOverlayIntensity()
	{
		if (!(overlayImage == null) && !(playerStats == null))
		{
			float heatPercentage = playerStats.HeatPercentage;
			Color color = grabOverlayColor;
			if (isCoolingDown)
			{
				float t = Mathf.Sin(Time.time * 5f) * 0.5f + 0.5f;
				Color color2 = Color.Lerp(Color.cyan, Color.white, t);
				color2.a = color.a + 0.2f;
				overlayImage.color = color2;
			}
			else if (heatPercentage >= 0.8f)
			{
				Color red = Color.red;
				red.a = Mathf.Lerp(color.a, 0.7f, (heatPercentage - 0.8f) / 0.2f);
				overlayImage.color = red;
			}
			else if (heatPercentage >= 0.6f)
			{
				Color color3 = new Color(1f, 0.5f, 0f, color.a);
				float t2 = (heatPercentage - 0.6f) / 0.2f;
				color3.a = Mathf.Lerp(color.a, 0.5f, t2);
				overlayImage.color = color3;
			}
			else
			{
				Color color4 = color;
				color4.a = color.a + heatPercentage * 0.2f;
				overlayImage.color = color4;
			}
		}
	}

	public void SetHeatBuildupSettings(bool enabled, float buildupRate, float maxDamage = 15f, float cooldownRate = 25f)
	{
		enableHeatBuildup = enabled;
		heatBuildupRate = Mathf.Max(0f, buildupRate);
		maxHeatDamage = Mathf.Max(0f, maxDamage);
		heatCooldownRate = Mathf.Max(0f, cooldownRate);
	}

	private void TriggerMaxHeatAnimation()
	{
		if (maxHeatAnimationPlaying)
		{
			return;
		}
		maxHeatAnimationPlaying = true;
		heatIncreaseBlocked = true;
		isCoolingDown = true;
		heatFullyCooled = false;
		int damage = Mathf.RoundToInt(maxHeatDamage);
		bool flag = TrinketManager.Instance != null && TrinketManager.Instance.TryConsumeMaxHeatGrabImmunity();
		if (playerStats != null && !flag)
		{
			playerStats.SetInvulnerable(invulnerable: false);
			playerStats.TakeDamage(damage);
			if (!isDragonGrab && grabDamagePercent > 0f)
			{
				int num = Mathf.RoundToInt((float)playerStats.MaxHealth * grabDamagePercent);
				if (num > 0)
				{
					playerStats.TakeDamage(num);
				}
			}
			playerStats.SetInvulnerable(invulnerable: true);
		}
		if (grabScreenAnimator != null)
		{
			grabScreenAnimator.SetBool("MaxHeat", value: true);
		}
		else
		{
			OnMaxHeatAnimationComplete();
		}
	}

	public void OnMaxHeatAnimationComplete()
	{
		if (heatFullyCooled)
		{
			if (grabScreenAnimator != null)
			{
				grabScreenAnimator.SetBool("MaxHeat", value: false);
			}
			maxHeatAnimationPlaying = false;
			heatIncreaseBlocked = false;
			isCoolingDown = false;
			heatFullyCooled = false;
		}
	}

	[ContextMenu("Test Start Grab")]
	private void DebugStartGrab()
	{
		if (Application.isPlaying)
		{
			GameObject enemy = new GameObject("TestEnemy");
			StartGrab(enemy);
		}
	}

	[ContextMenu("Test End Grab")]
	private void DebugEndGrab()
	{
		if (Application.isPlaying)
		{
			EndGrab();
		}
	}

	private void FindTeleportPoint()
	{
		if (teleportPoint == null)
		{
			GameObject gameObject = GameObject.Find("Teleport Point");
			if (gameObject != null)
			{
				teleportPoint = gameObject.transform;
			}
			else
			{
				enableTeleport = false;
			}
		}
	}

	public void TeleportPlayer()
	{
		if (!enableTeleport)
		{
			return;
		}
		GameObject gameObject = GameObject.FindGameObjectWithTag("Player");
		if (!(gameObject == null))
		{
			Vector3 position = GetTeleportDestination();
			CharacterController component = gameObject.GetComponent<CharacterController>();
			Rigidbody component2 = gameObject.GetComponent<Rigidbody>();
			if (component != null)
			{
				component.enabled = false;
			}
			gameObject.transform.position = position;
			if (component != null)
			{
				component.enabled = true;
			}
			if (component2 != null)
			{
				component2.linearVelocity = Vector3.zero;
				component2.angularVelocity = Vector3.zero;
			}
		}
	}

	private Vector3 GetTeleportDestination()
	{
		if (teleportPoint != null)
		{
			return teleportPoint.position;
		}
		return teleportDestination;
	}

	public void SetTeleportDestination(Vector3 newDestination)
	{
		teleportDestination = newDestination;
		enableTeleport = true;
	}

	public void SetTeleportPoint(Transform newTeleportPoint)
	{
		teleportPoint = newTeleportPoint;
		enableTeleport = true;
	}
}
