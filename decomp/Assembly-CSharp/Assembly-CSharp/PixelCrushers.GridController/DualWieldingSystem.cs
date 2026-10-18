using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace PixelCrushers.GridController;

public class DualWieldingSystem : MonoBehaviour
{
	[Serializable]
	private class DebugAttackData
	{
		public Vector3 origin;

		public Vector3 direction;

		public float range;

		public float sphereRadius;

		public float timestamp;

		public bool hitDetected;

		public Vector3[] hitPoints;

		public string[] hitObjectNames;

		public HandSlot attackingHand;

		public string weaponName;

		public Color debugColor;

		public bool wasProjectile;

		public float heatGenerated;

		public DebugAttackData()
		{
			hitPoints = new Vector3[0];
			hitObjectNames = new string[0];
		}
	}

	[Header("Weapon Slots")]
	[SerializeField]
	private WeaponSlot leftHandSlot = new WeaponSlot();

	[SerializeField]
	private WeaponSlot rightHandSlot = new WeaponSlot();

	[SerializeField]
	private WeaponSlot bothHandsSlot = new WeaponSlot();

	[Header("2D Hand Animators")]
	[SerializeField]
	private Animator leftHandAnimator;

	[SerializeField]
	private Animator rightHandAnimator;

	[SerializeField]
	private Animator bothHandsAnimator;

	[Header("Default Weapons")]
	[SerializeField]
	private WeaponData leftUnarmedData;

	[SerializeField]
	private WeaponData rightUnarmedData;

	[Header("Combat Settings")]
	[SerializeField]
	private LayerMask enemyLayerMask = 256;

	[SerializeField]
	private bool isBlocking;

	[Header("Attack Origin Settings")]
	[SerializeField]
	private Transform attackOrigin;

	[SerializeField]
	private float sphereCastRadius = 0.5f;

	[SerializeField]
	private bool usePlayerCameraForAiming = true;

	[Header("Projectile Settings")]
	[SerializeField]
	private ProjectileWeaponLauncher projectileLauncher;

	[SerializeField]
	private Transform leftHandLaunchPoint;

	[SerializeField]
	private Transform rightHandLaunchPoint;

	[SerializeField]
	private Transform bothHandsLaunchPoint;

	[SerializeField]
	private bool showProjectileDebug = true;

	[Header("Heat System")]
	[SerializeField]
	private float baseHeatPerShot = 5f;

	[SerializeField]
	private float projectileHeatMultiplier = 1.5f;

	[SerializeField]
	private float rapidFireHeatMultiplier = 2f;

	[SerializeField]
	private float lastShotCooldownTime = 1f;

	[SerializeField]
	private bool showHeatWarnings = true;

	[Header("2D Player Animation")]
	[SerializeField]
	private Animator playerAnimator;

	[Header("Audio")]
	[SerializeField]
	private AudioSource audioSource;

	[Header("Component References")]
	[SerializeField]
	private PlayerStats playerStats;

	[SerializeField]
	private Camera playerCamera;

	private BuffDebuffSystem buffDebuffSystem;

	private TrinketManager trinketManager;

	private GrabScreen cachedGrabScreen;

	private GrappleScreenobject cachedGrappleScreen;

	private GridInventorySystem cachedInventory;

	private InventoryUI cachedInventoryUI;

	private PauseMenuManager cachedPauseMenu;

	[Header("Events")]
	public CombatEvents combatEvents;

	[Header("Enhanced Debug Visualization")]
	[SerializeField]
	private bool enableDebugVisualization = true;

	[SerializeField]
	private bool showRealTimeRaycasts = true;

	[SerializeField]
	private bool showAttackHistory = true;

	[SerializeField]
	private bool showHitDetection = true;

	[SerializeField]
	private bool showCameraAimRay = true;

	[SerializeField]
	private bool showOnScreenUI = true;

	[SerializeField]
	private float debugLineWidth = 0.05f;

	[SerializeField]
	private float debugDisplayDuration = 2f;

	[SerializeField]
	private int maxDebugHistory = 10;

	private bool canAttackWhileMoving = true;

	private float leftHandLastShotTime;

	private float rightHandLastShotTime;

	private List<DebugAttackData> debugAttackHistory = new List<DebugAttackData>();

	private DebugAttackData currentDebugData;

	private static readonly RaycastHit[] sphereCastBuffer = new RaycastHit[16];

	private readonly List<RaycastHit> cachedAllHits = new List<RaycastHit>();

	private readonly List<Vector3> cachedHitPoints = new List<Vector3>();

	private readonly List<string> cachedHitNames = new List<string>();

	private Vector3 debugAttackOrigin;

	private Vector3 debugAttackDirection;

	private float debugAttackRange;

	private float debugSphereRadius;

	private float debugAttackTime;

	private const float DEBUG_ATTACK_DISPLAY_DURATION = 1f;

	public static DualWieldingSystem Instance { get; private set; }

	[Header("Debug Logging")]
	public WeaponData LeftHandWeapon => leftHandSlot.weaponData;

	public WeaponData RightHandWeapon => rightHandSlot.weaponData;

	public bool IsLeftHandAttacking => leftHandSlot.isAttacking;

	public bool IsRightHandAttacking => rightHandSlot.isAttacking;

	public bool IsAttacking
	{
		get
		{
			if (!leftHandSlot.isAttacking)
			{
				return rightHandSlot.isAttacking;
			}
			return true;
		}
	}

	public bool IsBlocking => isBlocking;

	private void Awake()
	{
		Instance = this;
		if (playerStats == null)
		{
			playerStats = GetComponent<PlayerStats>();
		}
		buffDebuffSystem = UnityEngine.Object.FindAnyObjectByType<BuffDebuffSystem>();
		trinketManager = GetComponent<TrinketManager>();
		if (trinketManager == null)
		{
			trinketManager = UnityEngine.Object.FindAnyObjectByType<TrinketManager>();
		}
		cachedGrabScreen = GrabScreen.Instance;
		cachedGrappleScreen = GrappleScreenobject.Instance;
		cachedInventory = UnityEngine.Object.FindAnyObjectByType<GridInventorySystem>();
		cachedInventoryUI = UnityEngine.Object.FindAnyObjectByType<InventoryUI>();
		cachedPauseMenu = UnityEngine.Object.FindAnyObjectByType<PauseMenuManager>();
		leftHandSlot.handAnimator = leftHandAnimator;
		rightHandSlot.handAnimator = rightHandAnimator;
		bothHandsSlot.handAnimator = bothHandsAnimator;
		if (audioSource == null)
		{
			audioSource = GetComponent<AudioSource>();
		}
		if (attackOrigin == null)
		{
			if (playerCamera != null)
			{
				attackOrigin = playerCamera.transform;
			}
			else
			{
				attackOrigin = transform;
			}
		}
		if (playerCamera == null && usePlayerCameraForAiming)
		{
			playerCamera = Camera.main;
			if (playerCamera == null)
			{
				playerCamera = UnityEngine.Object.FindAnyObjectByType<Camera>();
			}
		}
		if (projectileLauncher == null)
		{
			projectileLauncher = GetComponent<ProjectileWeaponLauncher>();
			if (projectileLauncher == null)
			{
				projectileLauncher = gameObject.AddComponent<ProjectileWeaponLauncher>();
			}
		}
		if (leftHandLaunchPoint != null)
		{
			projectileLauncher.SetLaunchPoint(HandSlot.LeftHand, leftHandLaunchPoint);
		}
		if (rightHandLaunchPoint != null)
		{
			projectileLauncher.SetLaunchPoint(HandSlot.RightHand, rightHandLaunchPoint);
		}
		EquipWeapon(leftUnarmedData, HandSlot.LeftHand);
		EquipWeapon(rightUnarmedData, HandSlot.RightHand);
	}

	private void Start()
	{
		if (playerStats != null)
		{
			playerStats.OnPlayerDeath.AddListener(OnPlayerDeath);
		}
	}

	private void OnDestroy()
	{
		if (playerStats != null)
		{
			playerStats.OnPlayerDeath.RemoveListener(OnPlayerDeath);
		}
	}

	private void Update()
	{
		HandleMouseInput();
	}

	public bool EquipWeapon(WeaponData weaponData, HandSlot handSlot)
	{
		if (weaponData == null)
		{
			return false;
		}
		if (weaponData.handSlot == HandSlot.BothHands || weaponData.twoHanded)
		{
			return EquipBothHandsWeapon(weaponData, handSlot);
		}
		WeaponSlot weaponSlot = GetWeaponSlot(handSlot);
		if (weaponSlot == null)
		{
			return false;
		}
		if (HasBothHandsWeapon())
		{
			UnequipBothHandsWeapon();
		}
		weaponSlot.ForceStopAttack();
		weaponSlot.weaponData = weaponData;
		weaponSlot.EquipWeaponAnimator(weaponData);
		if (leftHandAnimator != null)
		{
			leftHandAnimator.gameObject.SetActive(value: true);
		}
		if (rightHandAnimator != null)
		{
			rightHandAnimator.gameObject.SetActive(value: true);
		}
		combatEvents.OnWeaponEquipped?.Invoke(weaponData, handSlot);
		return true;
	}

	public void UnequipWeapon(HandSlot handSlot)
	{
		WeaponSlot weaponSlot = GetWeaponSlot(handSlot);
		if (weaponSlot != null && !(weaponSlot.weaponData == null))
		{
			WeaponData weaponData = weaponSlot.weaponData;
			weaponSlot.ForceStopAttack();
			weaponSlot.UnequipWeaponAnimator();
			WeaponData weaponData2 = ((handSlot == HandSlot.LeftHand) ? leftUnarmedData : rightUnarmedData);
			if (weaponData2 != null)
			{
				weaponSlot.weaponData = weaponData2;
				weaponSlot.EquipWeaponAnimator(weaponData2);
			}
			else
			{
				weaponSlot.weaponData = null;
			}
			combatEvents.OnWeaponUnequipped?.Invoke(weaponData, handSlot);
		}
	}

	public void HandleEmptyWeaponSlot(HandSlot handSlot)
	{
		WeaponSlot weaponSlot = GetWeaponSlot(handSlot);
		if (weaponSlot != null)
		{
			WeaponData weaponData = ((handSlot == HandSlot.LeftHand) ? leftUnarmedData : rightUnarmedData);
			if (weaponData != null)
			{
				weaponSlot.ForceStopAttack();
				weaponSlot.weaponData = weaponData;
				weaponSlot.EquipWeaponAnimator(weaponData);
				combatEvents.OnWeaponEquipped?.Invoke(weaponData, handSlot);
			}
			else
			{
				weaponSlot.weaponData = null;
				weaponSlot.UnequipWeaponAnimator();
			}
		}
	}

	public void SyncWithInventorySystem(InventoryItem leftWeaponItem, InventoryItem rightWeaponItem)
	{
		if (leftWeaponItem == null || !leftWeaponItem.IsWeapon)
		{
			HandleEmptyWeaponSlot(HandSlot.LeftHand);
		}
		else if (leftWeaponItem.weaponData != leftHandSlot.weaponData)
		{
			EquipWeapon(leftWeaponItem.weaponData, HandSlot.LeftHand);
		}
		if (rightWeaponItem == null || !rightWeaponItem.IsWeapon)
		{
			HandleEmptyWeaponSlot(HandSlot.RightHand);
		}
		else if (rightWeaponItem.weaponData != rightHandSlot.weaponData)
		{
			EquipWeapon(rightWeaponItem.weaponData, HandSlot.RightHand);
		}
	}

	public void ForceUnarmedState()
	{
		HandleEmptyWeaponSlot(HandSlot.LeftHand);
		HandleEmptyWeaponSlot(HandSlot.RightHand);
	}

	public bool IsHandUnarmed(HandSlot handSlot)
	{
		WeaponSlot weaponSlot = GetWeaponSlot(handSlot);
		if (weaponSlot?.weaponData == null)
		{
			return true;
		}
		WeaponData weaponData = ((handSlot == HandSlot.LeftHand) ? leftUnarmedData : rightUnarmedData);
		return weaponSlot.weaponData == weaponData;
	}

	public WeaponData GetUnarmedWeaponData(HandSlot handSlot)
	{
		if (handSlot != HandSlot.LeftHand)
		{
			return rightUnarmedData;
		}
		return leftUnarmedData;
	}

	private bool EquipBothHandsWeapon(WeaponData weaponData, HandSlot requestedSlot)
	{
		if (weaponData == null)
		{
			return false;
		}
		DisplaceExistingWeaponsToInventory();
		leftHandSlot.ForceStopAttack();
		rightHandSlot.ForceStopAttack();
		bothHandsSlot.ForceStopAttack();
		leftHandSlot.weaponData = null;
		rightHandSlot.weaponData = null;
		leftHandSlot.UnequipWeaponAnimator();
		rightHandSlot.UnequipWeaponAnimator();
		bothHandsSlot.weaponData = weaponData;
		bothHandsSlot.EquipWeaponAnimator(weaponData);
		if (leftHandAnimator != null)
		{
			leftHandAnimator.gameObject.SetActive(value: false);
		}
		if (rightHandAnimator != null)
		{
			rightHandAnimator.gameObject.SetActive(value: false);
		}
		if (bothHandsAnimator != null)
		{
			bothHandsAnimator.gameObject.SetActive(value: true);
		}
		combatEvents.OnWeaponEquipped?.Invoke(weaponData, HandSlot.BothHands);
		return true;
	}

	private bool HasBothHandsWeapon()
	{
		return bothHandsSlot.weaponData != null;
	}

	private void UnequipBothHandsWeapon()
	{
		if (bothHandsSlot.weaponData != null)
		{
			combatEvents.OnWeaponUnequipped?.Invoke(bothHandsSlot.weaponData, HandSlot.BothHands);
		}
		bothHandsSlot.ForceStopAttack();
		bothHandsSlot.weaponData = null;
		bothHandsSlot.UnequipWeaponAnimator();
		HandleEmptyWeaponSlot(HandSlot.LeftHand);
		HandleEmptyWeaponSlot(HandSlot.RightHand);
		if (bothHandsAnimator != null)
		{
			bothHandsAnimator.gameObject.SetActive(value: false);
		}
		if (leftHandAnimator != null)
		{
			leftHandAnimator.gameObject.SetActive(value: true);
		}
		if (rightHandAnimator != null)
		{
			rightHandAnimator.gameObject.SetActive(value: true);
		}
	}

	private void DisplaceExistingWeaponsToInventory()
	{
		GridInventorySystem component = GetComponent<GridInventorySystem>();
		if (component != null)
		{
			InventoryItem equippedItem = component.GetEquippedItem(EquipmentSlotType.LeftWeapon);
			InventoryItem equippedItem2 = component.GetEquippedItem(EquipmentSlotType.RightWeapon);
			if (equippedItem != null && equippedItem.IsWeapon && !IsUnarmedWeapon(equippedItem))
			{
				component.SetEquippedItemToNull(EquipmentSlotType.LeftWeapon);
				component.AddItem(equippedItem);
			}
			if (equippedItem2 != null && equippedItem2.IsWeapon && !IsUnarmedWeapon(equippedItem2))
			{
				component.SetEquippedItemToNull(EquipmentSlotType.RightWeapon);
				component.AddItem(equippedItem2);
			}
		}
	}

	private InventoryItem CreateInventoryItemFromWeapon(WeaponData weaponData)
	{
		if (weaponData == null)
		{
			return null;
		}
		InventoryItem inventoryItem = ScriptableObject.CreateInstance<InventoryItem>();
		inventoryItem.itemName = weaponData.weaponName;
		inventoryItem.weaponData = weaponData;
		return inventoryItem;
	}

	private bool IsUnarmedWeapon(InventoryItem item)
	{
		if (item == null || !item.IsWeapon || item.weaponData == null)
		{
			return true;
		}
		if (!(item.weaponData == leftUnarmedData))
		{
			return item.weaponData == rightUnarmedData;
		}
		return true;
	}

	private void HandleMouseInput()
	{
		if ((cachedInventoryUI != null && cachedInventoryUI.IsInventoryOpen) || (cachedPauseMenu != null && cachedPauseMenu.IsPaused) || LevelEndScreen.IsActive || (NoteViewer.Instance != null && NoteViewer.Instance.IsOpen))
		{
			return;
		}
		if (InputManager.Attack != null && InputManager.Attack.WasPressedThisFrame())
		{
			if (HasBothHandsWeapon())
			{
				PerformAttack(HandSlot.BothHands);
			}
			else
			{
				AttackWithLeftHand();
			}
		}
		if (InputManager.AttackSecondary != null && InputManager.AttackSecondary.WasPressedThisFrame())
		{
			if (HasBothHandsWeapon())
			{
				PerformSpecialAttack(HandSlot.BothHands);
			}
			else
			{
				AttackWithRightHand();
			}
		}
	}

	public void AttackWithLeftHand()
	{
		PerformAttack(HandSlot.LeftHand);
	}

	public void AttackWithRightHand()
	{
		PerformAttack(HandSlot.RightHand);
	}

	public void PerformAttack(HandSlot handSlot)
	{
		if (!CanPerformAction())
		{
			return;
		}
		WeaponSlot weaponSlot = GetWeaponSlot(handSlot);
		if (weaponSlot == null || weaponSlot.weaponData == null || !weaponSlot.CanAttack)
		{
			return;
		}
		if (HasBothHandsWeapon())
		{
			handSlot = HandSlot.BothHands;
			weaponSlot = bothHandsSlot;
		}
		bool flag = false;
		if (weaponSlot.weaponData.UsesAttackStages && weaponSlot.weaponData.AttackStageCount > 0)
		{
			WeaponAttackStage stage = weaponSlot.weaponData.GetStage(weaponSlot.nextStageIndex);
			flag = stage != null && stage.StageType == WeaponAttackStageType.HeatDrainProjectile;
		}
		if (!flag && weaponSlot.weaponData.UsesHeat && !weaponSlot.weaponData.HeatOnSpecialOnly && playerStats != null && !playerStats.CanAttackNow())
		{
			if (weaponSlot.weaponData.OverheatedSound != null)
			{
				PlayWeaponSound(weaponSlot.weaponData.OverheatedSound);
			}
			return;
		}
		float amount = CalculateHeatCost(weaponSlot.weaponData, handSlot);
		if (flag || !weaponSlot.weaponData.UsesHeat || weaponSlot.weaponData.HeatOnSpecialOnly || !(playerStats != null) || playerStats.CanGenerateHeat(amount))
		{
			StartAttack(weaponSlot, handSlot);
		}
	}

	public void PerformSpecialAttack(HandSlot handSlot)
	{
		if (!CanPerformAction())
		{
			return;
		}
		WeaponSlot weaponSlot = GetWeaponSlot(handSlot);
		if (weaponSlot == null || weaponSlot.weaponData == null || !weaponSlot.CanAttack || handSlot != HandSlot.BothHands || !HasBothHandsWeapon())
		{
			return;
		}
		GrabScreen grabScreen = cachedGrabScreen ?? (cachedGrabScreen = GrabScreen.Instance);
		if (grabScreen != null && grabScreen.IsGrabbed && weaponSlot.weaponData.SpecialAttackUsesProjectiles)
		{
			return;
		}
		if ((weaponSlot.weaponData.UsesHeat || weaponSlot.weaponData.SpecialAttackHeatCost > 0f) && playerStats != null && !playerStats.CanAttackNow())
		{
			if (weaponSlot.weaponData.OverheatedSound != null)
			{
				PlayWeaponSound(weaponSlot.weaponData.OverheatedSound);
			}
			return;
		}
		float amount = ((!(weaponSlot.weaponData.SpecialAttackHeatCost > 0f)) ? (CalculateHeatCost(weaponSlot.weaponData, handSlot) * 1.5f) : weaponSlot.weaponData.SpecialAttackHeatCost);
		if ((!weaponSlot.weaponData.UsesHeat && !(weaponSlot.weaponData.SpecialAttackHeatCost > 0f)) || !(playerStats != null) || playerStats.CanGenerateHeat(amount))
		{
			StartSpecialAttack(weaponSlot, handSlot);
		}
	}

	private void StartAttack(WeaponSlot slot, HandSlot handSlot)
	{
		slot.StartAttack();
		if (slot.weaponData != null && slot.weaponData.UsesAttackStages && slot.weaponData.AttackStageCount > 0)
		{
			slot.nextStageIndex = (slot.nextStageIndex + 1) % slot.weaponData.AttackStageCount;
		}
		if (playerAnimator != null)
		{
			playerAnimator.SetTrigger("Attack");
			playerAnimator.SetInteger("AttackHand", handSlot switch
			{
				HandSlot.RightHand => 1, 
				HandSlot.LeftHand => 0, 
				_ => 2, 
			});
		}
	}

	private void StartSpecialAttack(WeaponSlot slot, HandSlot handSlot)
	{
		slot.StartSpecialAttack();
		if (playerAnimator != null)
		{
			playerAnimator.SetTrigger("SpecialAttack");
			playerAnimator.SetInteger("AttackHand", handSlot switch
			{
				HandSlot.RightHand => 1, 
				HandSlot.LeftHand => 0, 
				_ => 2, 
			});
		}
	}

	public void StartBlocking()
	{
		if (CanPerformAction() && ((leftHandSlot.weaponData != null && leftHandSlot.weaponData.canBlock) || (rightHandSlot.weaponData != null && rightHandSlot.weaponData.canBlock)))
		{
			isBlocking = true;
			if (leftHandSlot.weaponData != null && leftHandSlot.weaponData.canBlock)
			{
				leftHandSlot.PlayBlockAnimation(blocking: true);
			}
			if (rightHandSlot.weaponData != null && rightHandSlot.weaponData.canBlock)
			{
				rightHandSlot.PlayBlockAnimation(blocking: true);
			}
			if (playerAnimator != null)
			{
				playerAnimator.SetBool("IsBlocking", value: true);
			}
		}
	}

	public void StopBlocking()
	{
		isBlocking = false;
		leftHandSlot.PlayBlockAnimation(blocking: false);
		rightHandSlot.PlayBlockAnimation(blocking: false);
		if (playerAnimator != null)
		{
			playerAnimator.SetBool("IsBlocking", value: false);
		}
	}

	private float CalculateHeatCost(WeaponData weapon, HandSlot handSlot)
	{
		if (!weapon.UsesHeat)
		{
			return 0f;
		}
		float num = baseHeatPerShot;
		if (weapon.heatPerShot > 0f)
		{
			num = weapon.heatPerShot;
		}
		if (weapon.IsProjectileWeapon)
		{
			num *= projectileHeatMultiplier;
			if (weapon.ProjectileCount > 1)
			{
				num *= (float)weapon.ProjectileCount;
			}
		}
		float num2 = ((handSlot == HandSlot.LeftHand) ? leftHandLastShotTime : rightHandLastShotTime);
		if (Time.time - num2 < lastShotCooldownTime)
		{
			num *= rapidFireHeatMultiplier;
		}
		return num;
	}

	private void GenerateHeatForAttack(WeaponData weapon, HandSlot handSlot)
	{
		if (playerStats == null || !weapon.UsesHeat)
		{
			return;
		}
		float num = CalculateHeatCost(weapon, handSlot);
		if (num > 0f)
		{
			playerStats.GenerateHeat(num);
			if (handSlot == HandSlot.LeftHand)
			{
				leftHandLastShotTime = Time.time;
			}
			else
			{
				rightHandLastShotTime = Time.time;
			}
		}
	}

	public void OnAttackStart(HandSlot handSlot)
	{
		WeaponSlot weaponSlot = GetWeaponSlot(handSlot);
		if (weaponSlot != null && !(weaponSlot.weaponData == null))
		{
			weaponSlot.OnAnimationStart();
			AudioClip audioClip = weaponSlot.weaponData.swingSound;
			if (weaponSlot.isSpecialAttacking && weaponSlot.weaponData.specialAttackSound != null)
			{
				audioClip = weaponSlot.weaponData.specialAttackSound;
			}
			else if (weaponSlot.weaponData.twoHanded && weaponSlot.weaponData.twoHandedSwingSound != null)
			{
				audioClip = weaponSlot.weaponData.twoHandedSwingSound;
			}
			if (audioClip != null)
			{
				PlayWeaponSound(audioClip);
			}
		}
	}

	public void OnAttackHit(HandSlot handSlot)
	{
		WeaponSlot weaponSlot = GetWeaponSlot(handSlot);
		if (weaponSlot == null || weaponSlot.weaponData == null || !weaponSlot.isAttacking)
		{
			return;
		}
		weaponSlot.OnAnimationHit();
		if (!weaponSlot.weaponData.HeatOnSpecialOnly)
		{
			GenerateHeatForAttack(weaponSlot.weaponData, handSlot);
		}
		if (weaponSlot.weaponData.attackType == AttackType.Melee)
		{
			GrappleScreenobject grappleScreenobject = cachedGrappleScreen ?? (cachedGrappleScreen = GrappleScreenobject.Instance);
			if (grappleScreenobject != null && grappleScreenobject.IsPlayerGrappled())
			{
				grappleScreenobject.OnMeleeAttack();
			}
			GrabScreen grabScreen = cachedGrabScreen ?? (cachedGrabScreen = GrabScreen.Instance);
			if (grabScreen != null && grabScreen.IsGrabbed)
			{
				grabScreen.OnPlayerAttackDuringGrab(weaponSlot.weaponData.damage);
			}
		}
		if (weaponSlot.weaponData.IsProjectileWeapon)
		{
			FireProjectiles(weaponSlot, handSlot);
			return;
		}
		PerformDamageCheck(weaponSlot, handSlot);
		if (trinketManager != null)
		{
			TrinketMeleeContext ctx = new TrinketMeleeContext
			{
				weapon = weaponSlot.weaponData,
				hand = handSlot,
				launcher = projectileLauncher,
				firePoint = GetLaunchPointForHand(handSlot)
			};
			trinketManager.OnMeleeAttack(in ctx);
		}
	}

	public void OnAttackHitStage(HandSlot handSlot, int stageIndex)
	{
		if (HasBothHandsWeapon())
		{
			handSlot = HandSlot.BothHands;
		}
		WeaponSlot weaponSlot = GetWeaponSlot(handSlot);
		if (weaponSlot == null || weaponSlot.weaponData == null || !weaponSlot.isAttacking)
		{
			return;
		}
		weaponSlot.OnAnimationHit();
		WeaponData weaponData = weaponSlot.weaponData;
		if (!weaponData.UsesAttackStages || weaponData.AttackStageCount == 0)
		{
			OnAttackHit(handSlot);
			return;
		}
		WeaponAttackStage stage = weaponData.GetStage(stageIndex);
		if (stage == null)
		{
			return;
		}
		if (stage.StageType == WeaponAttackStageType.HeatDrainProjectile)
		{
			if (!(stage.ProjectileData == null))
			{
				FireProjectiles(weaponSlot, handSlot, stage.ProjectileData, stage.ProjectileCount, stage.ProjectileSpread, isSpecialAttack: false, stage.AlwaysCrit ? new float?(1f) : ((float?)null), isHeatDrain: true);
			}
			return;
		}
		if (!weaponData.HeatOnSpecialOnly)
		{
			GenerateHeatForAttack(weaponData, handSlot);
		}
		if (!(stage.ProjectileData == null))
		{
			FireProjectiles(weaponSlot, handSlot, stage.ProjectileData, stage.ProjectileCount, stage.ProjectileSpread, isSpecialAttack: false, stage.AlwaysCrit ? new float?(1f) : ((float?)null));
		}
	}

	private Transform GetLaunchPointForHand(HandSlot handSlot)
	{
		return handSlot switch
		{
			HandSlot.BothHands => bothHandsLaunchPoint, 
			HandSlot.LeftHand => leftHandLaunchPoint, 
			HandSlot.RightHand => rightHandLaunchPoint, 
			_ => null, 
		};
	}

	public void OnSpecialAttackHit(HandSlot handSlot)
	{
		WeaponSlot weaponSlot = GetWeaponSlot(handSlot);
		if (weaponSlot == null || weaponSlot.weaponData == null || !weaponSlot.isAttacking)
		{
			return;
		}
		weaponSlot.OnAnimationHit();
		if (weaponSlot.weaponData.SpecialAttackHeatCost > 0f)
		{
			if (playerStats != null)
			{
				playerStats.GenerateHeat(weaponSlot.weaponData.SpecialAttackHeatCost);
			}
		}
		else
		{
			GenerateHeatForAttack(weaponSlot.weaponData, handSlot);
		}
		if (weaponSlot.weaponData.attackType == AttackType.Melee)
		{
			GrappleScreenobject grappleScreenobject = cachedGrappleScreen ?? (cachedGrappleScreen = GrappleScreenobject.Instance);
			if (grappleScreenobject != null && grappleScreenobject.IsPlayerGrappled())
			{
				grappleScreenobject.OnMeleeAttack();
			}
			GrabScreen grabScreen = cachedGrabScreen ?? (cachedGrabScreen = GrabScreen.Instance);
			if (grabScreen != null && grabScreen.IsGrabbed)
			{
				int num = ((weaponSlot.weaponData.SpecialAttackDamage > 0) ? weaponSlot.weaponData.SpecialAttackDamage : weaponSlot.weaponData.damage);
				grabScreen.OnPlayerAttackDuringGrab(num);
			}
		}
		if (weaponSlot.weaponData.SpecialAttackUsesProjectiles)
		{
			FireProjectiles(weaponSlot, handSlot, isSpecialAttack: true);
		}
		else if (weaponSlot.weaponData.IsProjectileWeapon)
		{
			FireProjectiles(weaponSlot, handSlot);
		}
		else
		{
			PerformDamageCheck(weaponSlot, handSlot);
		}
	}

	public void OnAttackEnd(HandSlot handSlot)
	{
		GetWeaponSlot(handSlot)?.OnAnimationEnd();
	}

	private void FireProjectiles(WeaponSlot slot, HandSlot handSlot, bool isSpecialAttack = false)
	{
		ProjectileData projectileData;
		int projectileCount;
		float projectileSpread;
		if (isSpecialAttack && slot.weaponData.SpecialAttackUsesProjectiles)
		{
			projectileData = slot.weaponData.SpecialProjectileData;
			projectileCount = slot.weaponData.SpecialProjectileCount;
			projectileSpread = slot.weaponData.SpecialProjectileSpread;
		}
		else
		{
			projectileData = slot.weaponData.ProjectileData;
			projectileCount = slot.weaponData.ProjectileCount;
			projectileSpread = slot.weaponData.ProjectileSpread;
		}
		FireProjectiles(slot, handSlot, projectileData, projectileCount, projectileSpread, isSpecialAttack);
	}

	private void FireProjectiles(WeaponSlot slot, HandSlot handSlot, ProjectileData projectileData, int projectileCount, float projectileSpread, bool isSpecialAttack, float? stageForcedCrit = null, bool isHeatDrain = false)
	{
		if (projectileLauncher == null || projectileData == null)
		{
			return;
		}
		float extraCritMultiplier = 1f;
		bool flag = trinketManager != null && trinketManager.PeekGuaranteedCrit(out extraCritMultiplier);
		float? num = (flag ? new float?(extraCritMultiplier) : stageForcedCrit);
		int num2;
		bool isCritical;
		if (isSpecialAttack && slot.weaponData.SpecialAttackDamage > 0)
		{
			num2 = CalculateDamage(slot.weaponData, out isCritical, slot.weaponData.SpecialAttackDamage, allowTrinketCrit: false, num);
		}
		else
		{
			WeaponData weaponData = slot.weaponData;
			float? forcedCritMultiplier = num;
			num2 = CalculateDamage(weaponData, out isCritical, null, allowTrinketCrit: false, forcedCritMultiplier);
			if (isSpecialAttack)
			{
				num2 = Mathf.RoundToInt((float)num2 * 1.5f);
			}
		}
		Transform transform = null;
		switch (handSlot)
		{
		case HandSlot.BothHands:
			transform = bothHandsLaunchPoint;
			break;
		case HandSlot.LeftHand:
			transform = leftHandLaunchPoint;
			break;
		case HandSlot.RightHand:
			transform = rightHandLaunchPoint;
			break;
		}
		Vector3 vector = ((transform != null) ? transform.forward : GetAttackDirection());
		for (int i = 0; i < projectileCount; i++)
		{
			Vector3 direction = vector;
			if (projectileCount > 1 && projectileSpread > 0f)
			{
				direction = ApplySpread(vector, projectileSpread, i, projectileCount);
			}
			Projectile projectile = projectileLauncher.LaunchProjectile(projectileData, handSlot, direction, num2, transform, isCritical, slot.weaponData, isSpecialAttack);
			if (flag && projectile != null)
			{
				projectile.MarkPendingGuaranteedCrit();
			}
			if (isHeatDrain && projectile != null)
			{
				projectile.MarkHeatDrain(playerStats);
			}
		}
		if (slot.weaponData.MuzzleEffect != null)
		{
			Transform transform2 = ((transform != null) ? transform : projectileLauncher.GetLaunchPoint(handSlot));
			GameObject gameObject = ((VFXPoolManager.Instance != null) ? VFXPoolManager.Instance.Get(slot.weaponData.MuzzleEffect, transform2.position, transform2.rotation) : UnityEngine.Object.Instantiate(slot.weaponData.MuzzleEffect, transform2.position, transform2.rotation));
			if (VFXPoolManager.Instance != null)
			{
				VFXPoolManager.Instance.ReturnAfterDelay(gameObject, 2f);
			}
			else
			{
				UnityEngine.Object.Destroy(gameObject, 2f);
			}
		}
		DebugAttackData debugAttackData = new DebugAttackData();
		debugAttackData.origin = GetAttackOrigin();
		debugAttackData.direction = vector;
		debugAttackData.range = slot.weaponData.GetEffectiveRange();
		debugAttackData.sphereRadius = 0.1f;
		debugAttackData.timestamp = Time.time;
		debugAttackData.attackingHand = handSlot;
		debugAttackData.weaponName = slot.weaponData.weaponName + (isSpecialAttack ? " (Special)" : "");
		ref Color debugColor = ref debugAttackData.debugColor;
		Color color;
		if (isSpecialAttack)
		{
			color = Color.magenta;
		}
		else
		{
			color = ((handSlot == HandSlot.LeftHand) ? Color.green : Color.cyan);
		}
		debugColor = color;
		debugAttackData.wasProjectile = true;
		debugAttackData.hitDetected = true;
		debugAttackData.hitPoints = new Vector3[1] { GetAttackOrigin() + vector * slot.weaponData.GetEffectiveRange() };
		debugAttackData.hitObjectNames = new string[1] { isSpecialAttack ? "Special Projectile Target" : "Projectile Target" };
		debugAttackData.heatGenerated = CalculateHeatCost(slot.weaponData, handSlot);
		DebugAttackData item = debugAttackData;
		if (showAttackHistory)
		{
			debugAttackHistory.Add(item);
			while (debugAttackHistory.Count > maxDebugHistory)
			{
				debugAttackHistory.RemoveAt(0);
			}
		}
		currentDebugData = item;
		combatEvents.OnAttackPerformed?.Invoke(slot.weaponData, handSlot, num2);
	}

	private Vector3 ApplySpread(Vector3 baseDirection, float spreadAngle, int projectileIndex, int totalProjectiles)
	{
		if (totalProjectiles == 1)
		{
			return baseDirection;
		}
		float num = spreadAngle / (float)(totalProjectiles - 1);
		float num2 = (0f - spreadAngle) * 0.5f + num * (float)projectileIndex;
		float num3 = UnityEngine.Random.Range((0f - num) * 0.3f, num * 0.3f);
		Quaternion quaternion = Quaternion.AngleAxis(num2 + num3, Vector3.up);
		return Quaternion.AngleAxis(UnityEngine.Random.Range((0f - spreadAngle) * 0.2f, spreadAngle * 0.2f), Vector3.right) * quaternion * baseDirection;
	}

	private void PerformDamageCheck(WeaponSlot slot, HandSlot handSlot)
	{
		if (slot.weaponData.attackType == AttackType.Melee || slot.weaponData.attackType == AttackType.Ranged)
		{
			PerformMeleeAttackWithDebug(slot, handSlot);
		}
	}

	private void PerformMeleeAttackWithDebug(WeaponSlot slot, HandSlot handSlot)
	{
		Ray centerScreenRay = GetCenterScreenRay();
		Vector3 origin = centerScreenRay.origin;
		Vector3 direction = centerScreenRay.direction;
		float range = slot.weaponData.range;
		DebugAttackData debugAttackData = new DebugAttackData
		{
			origin = origin,
			direction = direction,
			range = range,
			sphereRadius = sphereCastRadius,
			timestamp = Time.time,
			attackingHand = handSlot,
			weaponName = slot.weaponData.weaponName,
			debugColor = ((handSlot == HandSlot.LeftHand) ? Color.red : Color.blue),
			wasProjectile = false,
			heatGenerated = CalculateHeatCost(slot.weaponData, handSlot)
		};
		debugAttackOrigin = origin;
		debugAttackDirection = direction;
		debugAttackRange = range;
		debugSphereRadius = sphereCastRadius;
		debugAttackTime = Time.time;
		bool flag = Physics.Raycast(centerScreenRay, out var hitInfo, range, (int)enemyLayerMask | 0x40000);
		int num = Physics.SphereCastNonAlloc(centerScreenRay, sphereCastRadius, sphereCastBuffer, range, (int)enemyLayerMask | 0x40000);
		cachedAllHits.Clear();
		cachedHitPoints.Clear();
		cachedHitNames.Clear();
		List<RaycastHit> list = cachedAllHits;
		List<Vector3> list2 = cachedHitPoints;
		List<string> list3 = cachedHitNames;
		if (flag)
		{
			list.Add(hitInfo);
			list2.Add(hitInfo.point);
			list3.Add(hitInfo.collider.name + " (Raycast)");
		}
		for (int i = 0; i < num; i++)
		{
			RaycastHit item = sphereCastBuffer[i];
			bool flag2 = false;
			foreach (RaycastHit item2 in list)
			{
				if (item2.collider == item.collider)
				{
					flag2 = true;
					break;
				}
			}
			if (!flag2)
			{
				list.Add(item);
				list2.Add(item.point);
				list3.Add(item.collider.name + " (SphereCast)");
			}
		}
		debugAttackData.hitDetected = list.Count > 0;
		if (enableDebugVisualization)
		{
			debugAttackData.hitPoints = list2.ToArray();
			debugAttackData.hitObjectNames = list3.ToArray();
		}
		if (showAttackHistory)
		{
			debugAttackHistory.Add(debugAttackData);
			while (debugAttackHistory.Count > maxDebugHistory)
			{
				debugAttackHistory.RemoveAt(0);
			}
		}
		currentDebugData = debugAttackData;
		int num2 = 0;
		bool isCritical2;
		foreach (RaycastHit item3 in list)
		{
			if (!IsValidAttackTarget(item3.collider.gameObject))
			{
				continue;
			}
			IDamageable component = item3.collider.GetComponent<IDamageable>();
			if (component != null)
			{
				bool flag3 = TrinketManager.IsTrinketTarget(item3.collider.gameObject);
				bool isCritical;
				int damage;
				if (slot.isSpecialAttacking && slot.weaponData.SpecialAttackDamage > 0)
				{
					damage = CalculateDamage(slot.weaponData, out isCritical, slot.weaponData.SpecialAttackDamage, flag3);
				}
				else
				{
					WeaponData weaponData = slot.weaponData;
					isCritical2 = flag3;
					damage = CalculateDamage(weaponData, out isCritical, null, isCritical2);
				}
				TrinketDamageContext ctx = new TrinketDamageContext
				{
					target = item3.collider.gameObject,
					damage = damage,
					weapon = slot.weaponData,
					hand = handSlot,
					isProjectile = false,
					isCritical = isCritical
				};
				trinketManager?.ModifyDamage(ref ctx);
				damage = ctx.damage;
				if (DamageNumberManager.Instance != null)
				{
					DamageNumberManager.Instance.SetNextCritical(isCritical);
				}
				component.TakeDamage(damage);
				num2 += damage;
				ctx.damage = damage;
				trinketManager?.OnDealtDamage(in ctx);
				AudioClip clip = slot.weaponData.hitSound;
				if (slot.isSpecialAttacking && slot.weaponData.specialAttackHitSound != null)
				{
					clip = slot.weaponData.specialAttackHitSound;
				}
				else if (slot.weaponData.twoHanded && slot.weaponData.twoHandedHitSound != null)
				{
					clip = slot.weaponData.twoHandedHitSound;
				}
				PlayWeaponSound(clip);
				if (slot.weaponData.hitEffect2D != null)
				{
					UnityEngine.Object.Instantiate(slot.weaponData.hitEffect2D, item3.point, Quaternion.LookRotation(item3.normal));
				}
				if (!(slot.isSpecialAttacking ? slot.weaponData.SpecialAttackIsMultiHit : slot.weaponData.IsMultiHit))
				{
					break;
				}
			}
		}
		combatEvents.OnAttackPerformed?.Invoke(slot.weaponData, handSlot, CalculateDamage(slot.weaponData, out isCritical2));
	}

	private bool IsValidAttackTarget(GameObject targetObject)
	{
		if (targetObject.transform == transform)
		{
			return false;
		}
		GrabScreen grabScreen = cachedGrabScreen ?? (cachedGrabScreen = GrabScreen.Instance);
		if (grabScreen != null && grabScreen.IsGrabbed)
		{
			GameObject grabbingEnemy = grabScreen.GrabbingEnemy;
			if (grabbingEnemy == null)
			{
				return false;
			}
			return targetObject == grabbingEnemy;
		}
		return true;
	}

	public void TakeDamage(int damage)
	{
		if (!(playerStats == null))
		{
			if (isBlocking)
			{
				float bestBlockReduction = GetBestBlockReduction();
				damage = Mathf.RoundToInt((float)damage * (1f - bestBlockReduction));
				PlayBlockEffects();
			}
			playerStats.TakeDamage(damage);
			combatEvents.OnDamageTaken?.Invoke(damage);
		}
	}

	public void TakeDamage(int damage, GameObject attacker)
	{
		if (playerStats == null)
		{
			return;
		}
		if (isBlocking)
		{
			float bestBlockReduction = GetBestBlockReduction();
			damage = Mathf.RoundToInt((float)damage * (1f - bestBlockReduction));
			PlayBlockEffects();
		}
		playerStats.TakeDamage(damage);
		combatEvents.OnDamageTaken?.Invoke(damage);
		if (attacker == null)
		{
			return;
		}
		GridInventorySystem gridInventorySystem = cachedInventory ?? (cachedInventory = UnityEngine.Object.FindAnyObjectByType<GridInventorySystem>());
		if (!(gridInventorySystem == null))
		{
			int totalThornsDamage = gridInventorySystem.GetTotalThornsDamage();
			if (totalThornsDamage > 0)
			{
				attacker.GetComponent<IDamageable>()?.TakeDamage(totalThornsDamage);
			}
		}
	}

	private float GetBestBlockReduction()
	{
		float num = 0f;
		if (leftHandSlot.weaponData != null && leftHandSlot.weaponData.canBlock)
		{
			num = Mathf.Max(num, leftHandSlot.weaponData.blockReduction);
		}
		if (rightHandSlot.weaponData != null && rightHandSlot.weaponData.canBlock)
		{
			num = Mathf.Max(num, rightHandSlot.weaponData.blockReduction);
		}
		return num;
	}

	private void PlayBlockEffects()
	{
		WeaponData weaponData = null;
		if (leftHandSlot.weaponData != null && leftHandSlot.weaponData.canBlock)
		{
			weaponData = leftHandSlot.weaponData;
		}
		else if (rightHandSlot.weaponData != null && rightHandSlot.weaponData.canBlock)
		{
			weaponData = rightHandSlot.weaponData;
		}
		if (weaponData != null)
		{
			PlayWeaponSound(weaponData.blockSound);
			combatEvents.OnBlockPerformed?.Invoke(weaponData, (!(leftHandSlot.weaponData == weaponData)) ? HandSlot.RightHand : HandSlot.LeftHand);
		}
	}

	private void OnPlayerDeath()
	{
		StopBlocking();
		leftHandSlot.ForceStopAttack();
		rightHandSlot.ForceStopAttack();
		bothHandsSlot.ForceStopAttack();
		if (projectileLauncher != null)
		{
			projectileLauncher.DestroyAllProjectiles();
		}
		combatEvents.OnPlayerDeath?.Invoke();
	}

	private WeaponSlot GetWeaponSlot(HandSlot handSlot)
	{
		return handSlot switch
		{
			HandSlot.LeftHand => leftHandSlot, 
			HandSlot.RightHand => rightHandSlot, 
			HandSlot.BothHands => bothHandsSlot, 
			_ => rightHandSlot, 
		};
	}

	private int CalculateDamage(WeaponData weapon, out bool isCritical, int? baseDamageOverride = null, bool allowTrinketCrit = false, float? forcedCritMultiplier = null)
	{
		int num = baseDamageOverride ?? weapon.damage;
		float num2 = 0f;
		float num3 = 1f;
		float num4 = 1f;
		int num5 = 0;
		if (buffDebuffSystem != null)
		{
			StatModifiers currentModifiers = buffDebuffSystem.GetCurrentModifiers();
			num2 = currentModifiers.criticalChanceBonus;
			num3 = currentModifiers.criticalDamageMultiplier;
			num4 = currentModifiers.attackMultiplier;
			num5 = currentModifiers.bonusAttackDamage;
		}
		float num6 = ((trinketManager != null) ? trinketManager.GetCritChanceBonus() : 0f);
		isCritical = UnityEngine.Random.value <= weapon.criticalChance + num2 + num6;
		float num7 = 1f;
		if (allowTrinketCrit && trinketManager != null && trinketManager.TryConsumeGuaranteedCrit(out var extraCritMultiplier))
		{
			isCritical = true;
			num7 = extraCritMultiplier;
		}
		else if (forcedCritMultiplier.HasValue)
		{
			isCritical = true;
			num7 = forcedCritMultiplier.Value;
		}
		if (isCritical)
		{
			num = Mathf.RoundToInt((float)num * weapon.criticalMultiplier * num3 * num7);
		}
		return Mathf.RoundToInt((float)num * num4) + num5;
	}

	private bool CanPerformAction()
	{
		if (playerStats != null && !playerStats.IsAlive)
		{
			return false;
		}
		return true;
	}

	private void PlayWeaponSound(AudioClip clip)
	{
		if (audioSource != null && clip != null)
		{
			audioSource.PlayOneShot(clip);
		}
	}

	private Vector3 GetAttackOrigin()
	{
		if (usePlayerCameraForAiming && playerCamera != null)
		{
			return playerCamera.transform.position;
		}
		if (attackOrigin != null)
		{
			return attackOrigin.position;
		}
		return transform.position + Vector3.up * 1.6f;
	}

	private Vector3 GetAttackDirection()
	{
		if (usePlayerCameraForAiming && playerCamera != null)
		{
			return playerCamera.transform.forward;
		}
		if (attackOrigin != null)
		{
			return attackOrigin.forward;
		}
		return transform.forward;
	}

	private Ray GetCenterScreenRay()
	{
		if (playerCamera != null)
		{
			Vector3 pos = new Vector3((float)Screen.width * 0.5f, (float)Screen.height * 0.5f, 0f);
			return playerCamera.ScreenPointToRay(pos);
		}
		Vector3 origin = GetAttackOrigin();
		Vector3 attackDirection = GetAttackDirection();
		return new Ray(origin, attackDirection);
	}

	private void OnDrawGizmos()
	{
		if (enableDebugVisualization)
		{
			if (showCameraAimRay && !IsAttacking && playerCamera != null)
			{
				DrawRealTimeAimRay();
			}
			if (showAttackHistory)
			{
				DrawAttackHistory();
			}
			if (currentDebugData != null && Time.time - currentDebugData.timestamp < debugDisplayDuration)
			{
				DrawAttackData(currentDebugData, isCurrent: true);
			}
			DrawReferencePoints();
			DrawProjectileLaunchPoints();
		}
	}

	private void DrawRealTimeAimRay()
	{
		Ray centerScreenRay = GetCenterScreenRay();
		Vector3 origin = centerScreenRay.origin;
		Vector3 direction = centerScreenRay.direction;
		float num = 1f;
		WeaponData weaponData = rightHandSlot.weaponData ?? leftHandSlot.weaponData;
		if (weaponData != null)
		{
			num = weaponData.GetEffectiveRange();
		}
		Vector3 vector = origin + direction * num;
		Gizmos.color = new Color(0f, 1f, 1f, 0.7f);
		Gizmos.DrawLine(origin, vector);
		if (weaponData != null && !weaponData.IsProjectileWeapon)
		{
			Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
			int num2 = 5;
			for (int i = 0; i <= num2; i++)
			{
				float t = (float)i / (float)num2;
				Gizmos.DrawWireSphere(Vector3.Lerp(origin, vector, t), sphereCastRadius);
			}
		}
		Gizmos.color = Color.cyan;
		float num3 = 0.2f;
		Gizmos.DrawLine(vector + Vector3.up * num3, vector - Vector3.up * num3);
		Gizmos.DrawLine(vector + Vector3.right * num3, vector - Vector3.right * num3);
		Gizmos.DrawLine(vector + Vector3.forward * num3, vector - Vector3.forward * num3);
	}

	private void DrawAttackHistory()
	{
		for (int i = 0; i < debugAttackHistory.Count; i++)
		{
			DebugAttackData debugAttackData = debugAttackHistory[i];
			float num = Time.time - debugAttackData.timestamp;
			if (!(num > debugDisplayDuration))
			{
				float num2 = 1f - num / debugDisplayDuration;
				num2 = Mathf.Clamp01(num2 * 0.5f);
				DrawAttackData(debugAttackData, isCurrent: false, num2);
			}
		}
	}

	private void DrawAttackData(DebugAttackData data, bool isCurrent, float alpha = 1f)
	{
		Vector3 vector = data.origin + data.direction * data.range;
		Color debugColor = data.debugColor;
		Gizmos.color = new Color(debugColor.r, debugColor.g, debugColor.b, alpha);
		Gizmos.DrawLine(data.origin, vector);
		if (data.wasProjectile)
		{
			Gizmos.color = new Color(debugColor.r, debugColor.g, debugColor.b, alpha * 0.8f);
			Gizmos.DrawRay(data.origin, data.direction * data.range);
			int num = (isCurrent ? 10 : 5);
			for (int i = 0; i <= num; i++)
			{
				float t = (float)i / (float)num;
				Gizmos.DrawWireSphere(Vector3.Lerp(data.origin, vector, t), 0.05f);
			}
		}
		else
		{
			Gizmos.color = new Color(debugColor.r, debugColor.g, debugColor.b, alpha * 0.3f);
			int num2 = (isCurrent ? 8 : 4);
			for (int j = 0; j <= num2; j++)
			{
				float t2 = (float)j / (float)num2;
				Gizmos.DrawWireSphere(Vector3.Lerp(data.origin, vector, t2), data.sphereRadius);
			}
		}
		if (showHitDetection && data.hitDetected)
		{
			Gizmos.color = new Color(0f, 1f, 0f, alpha);
			for (int k = 0; k < data.hitPoints.Length; k++)
			{
				Vector3 vector2 = data.hitPoints[k];
				Gizmos.DrawWireSphere(vector2, 0.1f);
				Gizmos.DrawLine(data.origin, vector2);
				if (isCurrent)
				{
					float num3 = 0.15f;
					Gizmos.DrawLine(vector2 + Vector3.up * num3, vector2 - Vector3.up * num3);
					Gizmos.DrawLine(vector2 + Vector3.right * num3, vector2 - Vector3.right * num3);
				}
			}
		}
		Gizmos.color = new Color(1f, 1f, 0f, alpha);
		Gizmos.DrawWireSphere(data.origin, 0.05f);
		Gizmos.color = new Color(debugColor.r, debugColor.g, debugColor.b, alpha);
		Gizmos.DrawWireSphere(vector, 0.08f);
	}

	private void DrawReferencePoints()
	{
		if (attackOrigin != null)
		{
			Gizmos.color = Color.blue;
			Gizmos.DrawWireCube(attackOrigin.position, Vector3.one * 0.1f);
			Gizmos.DrawRay(attackOrigin.position, attackOrigin.forward * 0.5f);
		}
		if (playerCamera != null)
		{
			Gizmos.color = Color.magenta;
			Gizmos.DrawWireCube(playerCamera.transform.position, Vector3.one * 0.15f);
			Gizmos.DrawRay(playerCamera.transform.position, playerCamera.transform.forward * 1f);
			Ray centerScreenRay = GetCenterScreenRay();
			Gizmos.color = new Color(1f, 0f, 1f, 0.7f);
			Gizmos.DrawRay(centerScreenRay.origin, centerScreenRay.direction * 2f);
		}
	}

	private void DrawProjectileLaunchPoints()
	{
		if (!(projectileLauncher == null))
		{
			Transform launchPoint = projectileLauncher.GetLaunchPoint(HandSlot.LeftHand);
			if (launchPoint != null)
			{
				Gizmos.color = Color.red;
				Gizmos.DrawWireSphere(launchPoint.position, 0.1f);
				Gizmos.DrawRay(launchPoint.position, launchPoint.forward * 0.5f);
			}
			Transform launchPoint2 = projectileLauncher.GetLaunchPoint(HandSlot.RightHand);
			if (launchPoint2 != null)
			{
				Gizmos.color = Color.blue;
				Gizmos.DrawWireSphere(launchPoint2.position, 0.1f);
				Gizmos.DrawRay(launchPoint2.position, launchPoint2.forward * 0.5f);
			}
		}
	}

	private void OnGUI()
	{
		if (!enableDebugVisualization || !showOnScreenUI)
		{
			return;
		}
		GUILayout.BeginArea(new Rect(10f, 10f, 450f, 500f));
		GUILayout.BeginVertical("box");
		GUILayout.Label("Dual Wielding Debug (Heat System)", GUI.skin.label);
		string text = leftHandSlot.weaponData?.weaponName ?? "None";
		string text2 = rightHandSlot.weaponData?.weaponName ?? "None";
		GUILayout.Label("Left Hand: " + text);
		if (leftHandSlot.weaponData != null && leftHandSlot.weaponData.IsProjectileWeapon)
		{
			GUILayout.Label("  Type: Projectile");
		}
		if (leftHandSlot.isAttacking)
		{
			GUILayout.Label($"  Attack State: Started={leftHandSlot.animationStarted}, Hit={leftHandSlot.hitEventTriggered}");
		}
		GUILayout.Label("Right Hand: " + text2);
		if (rightHandSlot.weaponData != null && rightHandSlot.weaponData.IsProjectileWeapon)
		{
			GUILayout.Label("  Type: Projectile");
		}
		if (rightHandSlot.isAttacking)
		{
			GUILayout.Label($"  Attack State: Started={rightHandSlot.animationStarted}, Hit={rightHandSlot.hitEventTriggered}");
		}
		GUILayout.Label($"Is Attacking: {IsAttacking}");
		GUILayout.Label($"Is Blocking: {IsBlocking}");
		if (playerStats != null)
		{
			GUILayout.Space(10f);
			GUILayout.Label("Heat System:", GUI.skin.label);
			GUILayout.Label($"Current Heat: {playerStats.CurrentHeat:F1}/{playerStats.MaxHeat:F1}");
			if (playerStats.IsInHeatOverflow)
			{
				GUILayout.Label($"OVERFLOW: {playerStats.CurrentHeat:F1}/{playerStats.MaxHeatWithOverflow:F1}");
				GUILayout.Label($"Movement Speed: {playerStats.MovementSpeedMultiplier:P0}");
			}
			GUILayout.Label($"Heat Percentage: {playerStats.HeatPercentage:P1}");
			GUILayout.Label($"Is Overheating: {playerStats.IsOverheating}");
			GUILayout.Label($"Is Critical: {playerStats.IsCriticallyHot}");
			GUILayout.Label($"Is in Overflow: {playerStats.IsInHeatOverflow}");
			GUILayout.Label($"Can Attack: {playerStats.CanAttackNow()}");
			if (!playerStats.CanAttackNow())
			{
				string attackRestrictionReason = playerStats.GetAttackRestrictionReason();
				GUILayout.Label("Restriction: " + attackRestrictionReason);
			}
			GUILayout.Label($"Attack Locked: {playerStats.IsAttackLocked()}");
			if (playerStats.IsAttackLocked())
			{
				float timeUntilCanAttack = playerStats.GetTimeUntilCanAttack();
				GUILayout.Label($"Attack Ready In: {timeUntilCanAttack:F1}s");
			}
			if (leftHandSlot.weaponData != null)
			{
				float num = CalculateHeatCost(leftHandSlot.weaponData, HandSlot.LeftHand);
				GUILayout.Label(leftHandSlot.weaponData.UsesHeat ? $"Left Hand Next Shot Heat: {num:F1}" : "Left Hand: No Heat");
			}
			if (rightHandSlot.weaponData != null)
			{
				float num2 = CalculateHeatCost(rightHandSlot.weaponData, HandSlot.RightHand);
				GUILayout.Label(rightHandSlot.weaponData.UsesHeat ? $"Right Hand Next Shot Heat: {num2:F1}" : "Right Hand: No Heat");
			}
		}
		GUILayout.Space(10f);
		GUILayout.Label($"Left Hand Unarmed: {IsHandUnarmed(HandSlot.LeftHand)}");
		GUILayout.Label($"Right Hand Unarmed: {IsHandUnarmed(HandSlot.RightHand)}");
		if (projectileLauncher != null)
		{
			GUILayout.Label($"Active Projectiles: {projectileLauncher.GetActiveProjectileCount()}");
		}
		GUILayout.Space(10f);
		if (leftHandSlot.weaponData != null)
		{
			float attackCooldown = GetAttackCooldown(HandSlot.LeftHand);
			GUILayout.Label($"Left Hand Cooldown: {attackCooldown:F2}s");
		}
		if (rightHandSlot.weaponData != null)
		{
			float attackCooldown2 = GetAttackCooldown(HandSlot.RightHand);
			GUILayout.Label($"Right Hand Cooldown: {attackCooldown2:F2}s");
		}
		GUILayout.Space(10f);
		if (currentDebugData != null && Time.time - currentDebugData.timestamp < debugDisplayDuration)
		{
			GUILayout.Label("Last Attack:", GUI.skin.label);
			GUILayout.Label($"  Hand: {currentDebugData.attackingHand}");
			GUILayout.Label("  Weapon: " + currentDebugData.weaponName);
			GUILayout.Label("  Type: " + (currentDebugData.wasProjectile ? "Projectile" : "Traditional"));
			GUILayout.Label($"  Hits: {currentDebugData.hitPoints.Length}");
			GUILayout.Label($"  Heat Generated: {currentDebugData.heatGenerated:F1}");
			GUILayout.Label($"  Age: {Time.time - currentDebugData.timestamp:F2}s");
			if (currentDebugData.hitDetected)
			{
				GUILayout.Label("  Hit Objects:");
				for (int i = 0; i < currentDebugData.hitObjectNames.Length; i++)
				{
					GUILayout.Label("    " + currentDebugData.hitObjectNames[i]);
				}
			}
		}
		GUILayout.Space(10f);
		if (playerCamera != null)
		{
			Ray centerScreenRay = GetCenterScreenRay();
			GUILayout.Label("Camera Ray:");
			GUILayout.Label($"  Origin: {centerScreenRay.origin}");
			GUILayout.Label($"  Direction: {centerScreenRay.direction}");
		}
		GUILayout.Space(5f);
		GUILayout.Label($"Enemy Layer Mask: {enemyLayerMask.value}");
		GUILayout.Label($"Sphere Cast Radius: {sphereCastRadius:F2}");
		GUILayout.Label("Heat System: ENABLED (Overflow Allowed)");
		GUILayout.Label("Attack Restriction: Can attack until overheated");
		GUILayout.Label("Recovery: Must cool to 0 after overheating");
		GUILayout.Label("Animation Event Based: TRUE");
		GUILayout.EndVertical();
		GUILayout.EndArea();
	}

	public bool HasWeaponInHand(HandSlot handSlot)
	{
		WeaponSlot weaponSlot = GetWeaponSlot(handSlot);
		WeaponData weaponData = ((handSlot == HandSlot.LeftHand) ? leftUnarmedData : rightUnarmedData);
		if (weaponSlot?.weaponData != null)
		{
			return weaponSlot.weaponData != weaponData;
		}
		return false;
	}

	public bool CanEquipWeapon(WeaponData weaponData, HandSlot handSlot)
	{
		if (weaponData == null)
		{
			return false;
		}
		if (weaponData.twoHanded && handSlot == HandSlot.LeftHand)
		{
			return false;
		}
		return true;
	}

	public float GetAttackCooldown(HandSlot handSlot)
	{
		WeaponSlot weaponSlot = GetWeaponSlot(handSlot);
		if (weaponSlot?.weaponData == null)
		{
			return 0f;
		}
		float num = Time.time - weaponSlot.lastAttackTime;
		float attackCooldown = weaponSlot.weaponData.GetAttackCooldown();
		return Mathf.Max(0f, attackCooldown - num);
	}

	public WeaponData GetWeaponInHand(HandSlot handSlot)
	{
		return GetWeaponSlot(handSlot)?.weaponData;
	}

	public bool CanBlock()
	{
		if (!(leftHandSlot.weaponData != null) || !leftHandSlot.weaponData.canBlock)
		{
			if (rightHandSlot.weaponData != null)
			{
				return rightHandSlot.weaponData.canBlock;
			}
			return false;
		}
		return true;
	}

	public void StopAllAttacks()
	{
		StopAllCoroutines();
		leftHandSlot.ForceStopAttack();
		rightHandSlot.ForceStopAttack();
		bothHandsSlot.ForceStopAttack();
	}

	public ProjectileWeaponLauncher GetProjectileLauncher()
	{
		return projectileLauncher;
	}

	public bool IsHandReadyForHitDetection(HandSlot handSlot)
	{
		return GetWeaponSlot(handSlot)?.IsReadyForHitDetection() ?? false;
	}

	public void ForceEndAttack(HandSlot handSlot)
	{
		GetWeaponSlot(handSlot)?.EndAttack();
	}

	public bool CanHandAttack(HandSlot handSlot)
	{
		WeaponSlot weaponSlot = GetWeaponSlot(handSlot);
		bool num = weaponSlot != null && weaponSlot.CanAttack && CanPerformAction();
		bool flag = true;
		if (weaponSlot != null && weaponSlot.weaponData?.UsesHeat == true && playerStats != null)
		{
			flag = playerStats.CanAttackNow();
		}
		return num & flag;
	}

	public bool IsUsingBothHandsWeapon()
	{
		return HasBothHandsWeapon();
	}

	public WeaponData GetBothHandsWeapon()
	{
		return bothHandsSlot.weaponData;
	}

	public bool AreHandAnimatorsHidden()
	{
		if (!(leftHandAnimator != null) || leftHandAnimator.gameObject.activeInHierarchy)
		{
			if (rightHandAnimator != null)
			{
				return !rightHandAnimator.gameObject.activeInHierarchy;
			}
			return false;
		}
		return true;
	}

	public void ForceUnequipBothHandsWeapon()
	{
		if (HasBothHandsWeapon())
		{
			UnequipBothHandsWeapon();
		}
	}

	public bool IsBothHandsAnimatorActive()
	{
		if (bothHandsAnimator != null)
		{
			return bothHandsAnimator.gameObject.activeInHierarchy;
		}
		return false;
	}

	public string GetDebugInfo()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("=== DualWieldingSystem Debug Info ===");
		stringBuilder.AppendLine("Left Hand: " + (leftHandSlot.weaponData?.weaponName ?? "None"));
		stringBuilder.AppendLine($"  - Unarmed: {IsHandUnarmed(HandSlot.LeftHand)}");
		stringBuilder.AppendLine($"  - Attacking: {leftHandSlot.isAttacking}");
		stringBuilder.AppendLine($"  - Can Attack: {CanHandAttack(HandSlot.LeftHand)}");
		stringBuilder.AppendLine("Right Hand: " + (rightHandSlot.weaponData?.weaponName ?? "None"));
		stringBuilder.AppendLine($"  - Unarmed: {IsHandUnarmed(HandSlot.RightHand)}");
		stringBuilder.AppendLine($"  - Attacking: {rightHandSlot.isAttacking}");
		stringBuilder.AppendLine($"  - Can Attack: {CanHandAttack(HandSlot.RightHand)}");
		stringBuilder.AppendLine("Both Hands: " + (bothHandsSlot.weaponData?.weaponName ?? "None"));
		stringBuilder.AppendLine($"  - Attacking: {bothHandsSlot.isAttacking}");
		stringBuilder.AppendLine($"  - Can Attack: {bothHandsSlot.CanAttack}");
		stringBuilder.AppendLine($"Blocking: {isBlocking}");
		stringBuilder.AppendLine($"Can Block: {CanBlock()}");
		stringBuilder.AppendLine($"Using Both Hands Weapon: {IsUsingBothHandsWeapon()}");
		stringBuilder.AppendLine($"Hand Animators Hidden: {AreHandAnimatorsHidden()}");
		stringBuilder.AppendLine($"Both Hands Animator Active: {IsBothHandsAnimatorActive()}");
		if (playerStats != null)
		{
			stringBuilder.AppendLine($"Heat: {playerStats.CurrentHeat:F1}/{playerStats.MaxHeat:F1} ({playerStats.HeatPercentage:P1})");
			if (playerStats.IsInHeatOverflow)
			{
				stringBuilder.AppendLine($"HEAT OVERFLOW: {playerStats.CurrentHeat:F1}/{playerStats.MaxHeatWithOverflow:F1}");
				stringBuilder.AppendLine($"Movement Speed: {playerStats.MovementSpeedMultiplier:P0}");
			}
			stringBuilder.AppendLine($"Overheating: {playerStats.IsOverheating}");
			stringBuilder.AppendLine($"Critical Heat: {playerStats.IsCriticallyHot}");
			stringBuilder.AppendLine($"Can Attack (Until Overheated): {playerStats.CanAttackNow()}");
			if (playerStats.IsAttackLocked())
			{
				stringBuilder.AppendLine("ATTACK LOCKED - Must cool to 0");
				stringBuilder.AppendLine($"Attack Ready In: {playerStats.GetTimeUntilCanAttack():F1}s");
			}
		}
		return stringBuilder.ToString();
	}

	[ContextMenu("Force Unarmed State")]
	public void DebugForceUnarmedState()
	{
		ForceUnarmedState();
	}

	[ContextMenu("Test Left Hand Unarmed")]
	public void DebugTestLeftHandUnarmed()
	{
		HandleEmptyWeaponSlot(HandSlot.LeftHand);
	}

	[ContextMenu("Test Right Hand Unarmed")]
	public void DebugTestRightHandUnarmed()
	{
		HandleEmptyWeaponSlot(HandSlot.RightHand);
	}

	[ContextMenu("Stop All Attacks")]
	public void DebugStopAllAttacks()
	{
		StopAllAttacks();
	}

	[ContextMenu("Generate Test Heat")]
	public void DebugGenerateHeat()
	{
		if (playerStats != null)
		{
			playerStats.GenerateHeat(10f);
		}
	}

	[ContextMenu("Cool All Heat")]
	public void DebugCoolAllHeat()
	{
		if (playerStats != null)
		{
			playerStats.CoolAllHeat();
		}
	}
}
