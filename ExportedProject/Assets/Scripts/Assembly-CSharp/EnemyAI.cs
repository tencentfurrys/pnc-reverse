using System;
using System.Collections;
using Pathfinding;
using Pathfinding.ECS;
using PixelCrushers.GridController;
using UnityEngine;

public class EnemyAI : MonoBehaviour, IDamageable, IGrabHideable
{
	public enum AIState
	{
		Idle = 0,
		Chasing = 1,
		Attacking = 2,
		Grabbing = 3,
		Dead = 4
	}

	[Header("Health")]
	[Tooltip("Maximum hit points. The pool currentHealth starts at and the health bar fills against.")]
	public int maxHealth = 50;

	[Tooltip("Runtime health value. Initialized from maxHealth at Start; not a designer knob.")]
	public int currentHealth;

	[Header("Second Stage (Transformation)")]
	[Tooltip("If true, this enemy does not die at 0 HP the first time — it transforms into a stronger stage 2 and only dies when HP reaches 0 again after transforming. Leave false for normal single-stage enemies.")]
	public bool hasSecondStage;

	[Tooltip("Max HP given when the enemy transforms into stage 2. currentHealth is refilled to this.")]
	public int secondStageMaxHealth = 100;

	[Tooltip("A* movement speed (followerEntity.maxSpeed) applied in stage 2. Set higher than stage 1 for the speed boost.")]
	public float secondStageMoveSpeed = 5f;

	[Tooltip("Distance (world units) the transformed enemy will start its ranged attack from. Make larger than the melee attackRange so it shoots from a distance while still meleeing up close.")]
	public float secondStageRangedRange = 8f;

	[Tooltip("Attack commit range used once transformed. Leave at 0 to keep using attackRange. Set larger than attackRange when the stage 2 model or swing animation reaches further.")]
	public float secondStageAttackRange;

	[Tooltip("Melee hit-test reach used once transformed. Leave at 0 to keep using meleeHitRange. Relates to secondStageAttackRange exactly as meleeHitRange relates to attackRange.")]
	public float secondStageMeleeHitRange;

	[Tooltip("Grab commit range used once transformed. Leave at 0 to keep using grabRange.")]
	public float secondStageGrabRange;

	[Tooltip("Grab hit-test reach used once transformed. Leave at 0 to keep using grabHitRange. Keep at least as large as secondStageGrabRange or the grab will start but the raycast will never reach.")]
	public float secondStageGrabHitRange;

	[Tooltip("Dash lunge distance used once transformed. Leave at 0 to keep using dashDistance. Dash speed is dashDistance / dashDuration, so raising this alone makes the stage 2 lunge both longer and faster.")]
	public float secondStageDashDistance;

	[Tooltip("Dash duration used once transformed. Leave at 0 to keep using dashDuration. Lowering this alone makes the stage 2 lunge cover the same ground faster.")]
	public float secondStageDashDuration;

	[Tooltip("Animator bool set true when the enemy transforms to stage 2. Drives the transformed idle/walk/attack states.")]
	public string transformedBoolName = "IsTransformed";

	[Tooltip("Animator layer holding the stage-2 clips. Its weight is raised to 1 on transform so the second form overrides the base layer and never reverts.")]
	public string transformedLayerName = "Transformed";

	[Tooltip("Animator state on the transformed layer played once when the enemy transforms, before it falls through to the layer's idle. The enemy stands still and cannot act until the clip finishes. Leave empty to skip the animation and transform instantly.")]
	public string transformStateName = "Transformation";

	[Tooltip("Animator trigger fired for the stage-2 ranged attack (instead of the melee \"Attack\" trigger).")]
	public string rangedAttackTrigger = "RangedAttack";

	[Tooltip("Optional VFX spawned at the enemy when it transforms.")]
	public GameObject transformEffect;

	[Tooltip("Optional sound played when the enemy transforms. Fired by a PlayTransformSound Animation Event on the transform clip, so the clip needs that event or this stays silent. Move the event to change when in the animation it plays.")]
	public AudioClip transformSound;

	[Tooltip("Hurt sound used once transformed. Leave empty to keep using hurtSound.")]
	public AudioClip secondStageHurtSound;

	[Tooltip("Melee swing sound used once transformed. Leave empty to keep using attackSound.")]
	public AudioClip secondStageAttackSound;

	[Tooltip("Death sound used once transformed. Leave empty to keep using deathSound.")]
	public AudioClip secondStageDeathSound;

	[Tooltip("Looping idle/walk sound used once transformed. Leave empty to keep using idleMovingSound.")]
	public AudioClip secondStageIdleMovingSound;

	[Tooltip("Grab sound used once transformed. Leave empty to keep using grabSound.")]
	public AudioClip secondStageGrabSound;

	[Tooltip("Plays when the stage-2 ranged attack fires a projectile. Stage 2 only — there is no stage-1 ranged attack to fall back to, so this is silent when left empty.")]
	public AudioClip secondStageShootSound;

	[Tooltip("Seconds of invulnerability granted right after transforming, so the hit that 'killed' stage 1 doesn't immediately chip stage 2.")]
	public float transformInvulnDuration = 0.5f;

	[Header("Projectile Attack (Stage 2)")]
	[Tooltip("Projectile prefab fired in stage 2. Should have (or will be given) an EnemyProjectile component.")]
	public GameObject projectilePrefab;

	[Tooltip("Transform the projectile spawns from and aims out of. Place at the beast's mouth/hands.")]
	public Transform projectileSpawnPoint;

	[Tooltip("Damage each stage-2 projectile deals to the player.")]
	public int projectileDamage = 15;

	[Tooltip("Travel speed of stage-2 projectiles, world units per second.")]
	public float projectileSpeed = 10f;

	[Tooltip("Seconds before a fired projectile auto-despawns.")]
	public float projectileLifetime = 5f;

	[Tooltip("Seconds between ranged attacks in stage 2. While the ranged attack is on cooldown the enemy closes in to grab/melee instead of firing.")]
	public float rangedAttackCooldown = 3f;

	[Tooltip("Number of projectiles fired per ranged attack.")]
	public int projectileCount = 1;

	[Tooltip("Total spread angle (degrees) the projectiles are fanned across. 0 = single straight shot.")]
	public float projectileSpread;

	[Tooltip("If true, the shot still tracks the player horizontally but takes its vertical launch angle from the Projectile Spawn Point's pitch (X rotation) — for arcing/diagonal shots that still aim at the player. If false, aims straight at the player.")]
	public bool fireAlongSpawnPointRotation;

	[Header("AI Behavior")]
	[Tooltip("Distance (world units) at which this enemy will start an attack swing while chasing.")]
	public float attackRange = 2f;

	[Tooltip("Distance at which an idle enemy notices the player and begins chasing.")]
	public float detectionRange = 10f;

	[Tooltip("Distance at which a chasing enemy gives up and returns to idle. Keep larger than detectionRange to avoid flicker.")]
	public float loseTargetRange = 15f;

	[Header("Attack")]
	[Tooltip("Hit points removed from the player per landed melee hit.")]
	public int attackDamage = 10;

	[Tooltip("Seconds between consecutive attacks.")]
	public float attackCooldown = 1.5f;

	[Tooltip("Distance the melee hit-test reaches from the enemy. Slightly larger than attackRange so swings can connect on approach.")]
	public float meleeHitRange = 2f;

	[Tooltip("Layers the melee hit-test considers as valid targets. Default is everything.")]
	[SerializeField]
	private LayerMask hitboxMask = -1;

	[Tooltip("If true, the enemy keeps pathfinding toward the player during its attack animation instead of rooting in place.")]
	public bool attackWhileMoving;

	[Tooltip("Only used when attackWhileMoving is true. Minimum distance to keep from the player while approaching — prevents walking on top of / pushing them. Should be slightly less than meleeHitRange so swings still connect.")]
	public float attackMoveStopDistance = 1.5f;

	[Header("Dash")]
	[Tooltip("World units the enemy lunges forward during a dash attack. Dash speed is not authored directly — it is dashDistance / dashDuration.")]
	public float dashDistance = 2f;

	[Tooltip("Seconds the dash motion lasts before the enemy stops. Together with dashDistance this sets the dash speed.")]
	public float dashDuration = 0.3f;

	[Header("Physics")]
	[Tooltip("Rigidbody mass. Affects how much knockback the enemy takes from hits.")]
	public float enemyMass = 5f;

	[Tooltip("Rigidbody linear drag. Higher values make the enemy decelerate faster after knockback.")]
	public float enemyDrag = 8f;

	[Header("Grab Attack")]
	[Tooltip("If true, this enemy can attempt grab attacks when the player is within grabRange.")]
	public bool canGrab = true;

	[Tooltip("Distance (world units) at which a grab attack becomes possible.")]
	public float grabRange = 2.5f;

	[Tooltip("Length of the raycast used to confirm a grab hit during the damaging frames of the grab animation. Independent of grabRange so 'close enough to attempt' and 'close enough to land' can be tuned separately. Mirrors how meleeHitRange relates to attackRange.")]
	public float grabHitRange = 2.5f;

	[Tooltip("Seconds between consecutive grab attempts.")]
	public float grabCooldown = 5f;

	[Header("Grab Behavior")]
	[Tooltip("If true, this enemy is hidden (SetActive false) instead of destroyed when it grabs the player. It reappears in Idle if the player escapes the grab; if killed during the grab, it dies normally at EndGrab. Arena clearance still treats a hidden enemy as alive.")]
	public bool hideInsteadOfDestroyOnGrab;

	[Tooltip("If true, damage dealt during the grab screen does NOT carry over to the enemy's actual health. The grab still ends when the player drops the grab-screen HP bar to zero (the QTE still resolves), but the enemy reappears at the health it had before the grab. Only effective when hideInsteadOfDestroyOnGrab is also on — without hiding the enemy is destroyed at grab-start regardless.")]
	public bool preserveHealthDuringGrab;

	[Header("Grab Animation Settings")]
	[Tooltip("Animator controller swapped onto the grab screen when this enemy grabs the player.")]
	public RuntimeAnimatorController grabAnimatorController;

	[Tooltip("Animator controller swapped onto the grab screen when this enemy grabs the player while transformed (stage 2). If left blank, the normal grabAnimatorController is used.")]
	public RuntimeAnimatorController transformedGrabAnimatorController;

	[Header("Grab Camera Settings")]
	[Tooltip("Camera position offset (relative to enemy) used while the grab cinematic plays. Tune to frame the grab nicely.")]
	public Vector3 grabCameraPositionOffset = Vector3.zero;

	[Tooltip("Camera rotation offset (Euler angles) used while the grab cinematic plays.")]
	public Vector3 grabCameraRotationOffset = Vector3.zero;

	[Header("Grab Screen Audio")]
	[Tooltip("Per-animation sounds for the grab screen. animationName must match the Animator state name.")]
	public GrabAnimationData[] grabScreenAnimations;

	[Tooltip("Per-animation grab-screen sounds used while transformed (stage 2). animationName must match the transformed grab controller's state names. If empty, the normal grabScreenAnimations are used.")]
	public GrabAnimationData[] transformedGrabScreenAnimations;

	[Header("A* Pathfinding")]
	[Tooltip("A* Pathfinding follower used for movement. Auto-found via GetComponent at Start if left blank.")]
	public FollowerEntity followerEntity;

	[Tooltip("If true, GetComponent fallbacks run at Start to find followerEntity. Turn off only if you need fully manual references.")]
	[SerializeField]
	private bool autoFindPathfindingComponents = true;

	[Header("Audio")]
	[Tooltip("Plays when this enemy takes damage.")]
	public AudioClip hurtSound;

	[Tooltip("Plays at the moment of a melee swing.")]
	public AudioClip attackSound;

	[Tooltip("Plays when this enemy dies.")]
	public AudioClip deathSound;

	[Tooltip("Looping sound played while idle/walking. Stops during attacks and on death.")]
	public AudioClip idleMovingSound;

	[Tooltip("Plays when this enemy successfully grabs the player.")]
	public AudioClip grabSound;

	[Tooltip("One-shot AudioSource used for hurt/attack/death sounds. Auto-found or auto-added at Start if blank.")]
	public AudioSource audioSource;

	[Tooltip("Looping AudioSource used for the idle/walking sound. Auto-added at Start if blank.")]
	public AudioSource loopingAudioSource;

	[Header("Effects")]
	[Tooltip("VFX prefab spawned at the enemy's position when it dies.")]
	public GameObject deathEffect;

	[Tooltip("VFX prefab spawned at the hit point when this enemy takes damage.")]
	public GameObject hitEffect;

	[Header("Gallery")]
	[Tooltip("Unique ID for gallery unlock tracking. Should match EnemyGalleryEntry.enemyID")]
	public string galleryEnemyID;

	[Header("Health Bar")]
	[Tooltip("Optional health bar UI. Drives the fill amount from currentHealth/maxHealth.")]
	[SerializeField]
	private EnemyHealthBar healthBar;

	[Header("Mini Boss Bar")]
	[Tooltip("If true, this enemy claims the big screen-space mini boss health bar in PlayerUi as soon as it spawns, and releases it when it dies. Leave false for normal enemies — they only use the small world-space healthBar above their head.")]
	public bool isMiniBoss;

	[Tooltip("Name shown on the mini boss bar in stage 1. Only used when isMiniBoss is ticked.")]
	public string miniBossDisplayName = "Mini Boss";

	[Tooltip("Name the mini boss bar swaps to after this enemy transforms to stage 2 (e.g. \"Blinded Beast - Awakened\"). Leave blank to keep the stage-1 name.")]
	public string miniBossStageTwoDisplayName = "";

	[Header("References")]
	[Tooltip("Player transform. Auto-found by tag at Start if left blank.")]
	public Transform player;

	[Tooltip("Animator driving this enemy's clips. Auto-found via GetComponentInChildren at Start if blank.")]
	public Animator animator;

	[Tooltip("GrabScreen used during the grab cinematic. Auto-resolved via GrabScreen.Instance at Start if blank.")]
	public GrabScreen grabScreen;

	[Header("Detection Colliders")]
	[Tooltip("Sphere collider used to detect when the player is in melee range. Radius is synced to meleeHitRange.")]
	public SphereCollider attackDetectionCollider;

	[Tooltip("Sphere collider used to detect when the player is in grab range. Radius is synced to grabRange.")]
	public SphereCollider grabDetectionCollider;

	[Header("Coin Drop")]
	[Tooltip("Coin prefab launched on death, subject to coinDropChance.")]
	[SerializeField]
	private GameObject currencyPrefab;

	[Tooltip("Probability (0–1) that a coin spawns on death.")]
	[Range(0f, 1f)]
	public float coinDropChance = 0.01f;

	[Tooltip("Lower bound on the random coin value rolled on a successful drop.")]
	public int minCoinDropValue = 1;

	[Tooltip("Upper bound on the random coin value rolled on a successful drop.")]
	public int maxCoinDropValue = 1;

	[Tooltip("Minimum elevation angle (degrees) at which a dropped coin is launched.")]
	[SerializeField]
	private float minLaunchAngle = 30f;

	[Tooltip("Maximum elevation angle (degrees) at which a dropped coin is launched.")]
	[SerializeField]
	private float maxLaunchAngle = 60f;

	[Tooltip("Minimum impulse applied to a dropped coin.")]
	[SerializeField]
	private float minLaunchForce = 3f;

	[Tooltip("Maximum impulse applied to a dropped coin.")]
	[SerializeField]
	private float maxLaunchForce = 5f;

	[Tooltip("Visual peak height of the coin's launch arc, in world units.")]
	[SerializeField]
	private float arcHeight = 2f;

	private float lastAttackTime;

	private float lastRangedAttackTime = -999f;

	private float lastGrabTime;

	private bool isDead;

	private bool hasDealtDamageThisAttack;

	private bool isAttemptingGrab;

	private bool hasGrabbedThisAttempt;

	private bool isDashing;

	private bool isTransformed;

	private bool isTransforming;

	private bool hasSpawnedProjectileThisAttack;

	private bool isInvulnerable;

	private bool meleeInDamagingState;

	private bool grabInDamagingState;

	private bool wasPlayerGrabbedLastFrame;

	private DualWieldingSystem playerCombatSystem;

	private PlayerStats playerStats;

	private Rigidbody rb;

	private AIState currentState;

	private Coroutine attackCoroutine;

	private Coroutine grabCoroutine;

	private Coroutine flashCoroutine;

	private SpriteRenderer[] cachedSprites;

	private Color[] originalSpriteColors;

	private Renderer[] cachedRenderers;

	private Color[] originalRendererColors;

	private bool transformedLayerResolved;

	private int transformedLayerIndex;

	public bool IsTransformed => isTransformed;

	private bool RangedReady
	{
		get
		{
			if (isTransformed && projectilePrefab != null)
			{
				return Time.time >= lastRangedAttackTime + rangedAttackCooldown;
			}
			return false;
		}
	}

	private bool MeleeReady => Time.time >= lastAttackTime + attackCooldown;

	private float EffectiveMeleeRange
	{
		get
		{
			if (!isTransformed || !(secondStageAttackRange > 0f))
			{
				return attackRange;
			}
			return secondStageAttackRange;
		}
	}

	private float EffectiveGrabRange
	{
		get
		{
			if (!isTransformed || !(secondStageGrabRange > 0f))
			{
				return grabRange;
			}
			return secondStageGrabRange;
		}
	}

	private float EffectiveMeleeHitRange
	{
		get
		{
			if (!isTransformed || !(secondStageMeleeHitRange > 0f))
			{
				return meleeHitRange;
			}
			return secondStageMeleeHitRange;
		}
	}

	private float EffectiveGrabHitRange
	{
		get
		{
			if (!isTransformed || !(secondStageGrabHitRange > 0f))
			{
				return grabHitRange;
			}
			return secondStageGrabHitRange;
		}
	}

	private float EffectiveDashDistance
	{
		get
		{
			if (!isTransformed || !(secondStageDashDistance > 0f))
			{
				return dashDistance;
			}
			return secondStageDashDistance;
		}
	}

	private float EffectiveDashDuration
	{
		get
		{
			if (!isTransformed || !(secondStageDashDuration > 0f))
			{
				return dashDuration;
			}
			return secondStageDashDuration;
		}
	}

	private float EffectiveAttackRange
	{
		get
		{
			if (!RangedReady)
			{
				return EffectiveMeleeRange;
			}
			return secondStageRangedRange;
		}
	}

	private AudioClip EffectiveHurtSound
	{
		get
		{
			if (!isTransformed || !(secondStageHurtSound != null))
			{
				return hurtSound;
			}
			return secondStageHurtSound;
		}
	}

	private AudioClip EffectiveAttackSound
	{
		get
		{
			if (!isTransformed || !(secondStageAttackSound != null))
			{
				return attackSound;
			}
			return secondStageAttackSound;
		}
	}

	private AudioClip EffectiveDeathSound
	{
		get
		{
			if (!isTransformed || !(secondStageDeathSound != null))
			{
				return deathSound;
			}
			return secondStageDeathSound;
		}
	}

	private AudioClip EffectiveGrabSound
	{
		get
		{
			if (!isTransformed || !(secondStageGrabSound != null))
			{
				return grabSound;
			}
			return secondStageGrabSound;
		}
	}

	private AudioClip EffectiveIdleMovingSound
	{
		get
		{
			if (!isTransformed || !(secondStageIdleMovingSound != null))
			{
				return idleMovingSound;
			}
			return secondStageIdleMovingSound;
		}
	}

	public bool IsDead => isDead;

	public bool HasDealtDamageThisAttack => hasDealtDamageThisAttack;

	public bool HasGrabbedThisAttempt => hasGrabbedThisAttempt;

	public float HealthPercentage
	{
		get
		{
			if (maxHealth <= 0)
			{
				return 0f;
			}
			return (float)currentHealth / (float)maxHealth;
		}
	}

	public AIState CurrentState => currentState;

	private bool IsPlayerVulnerable
	{
		get
		{
			if (playerStats != null)
			{
				if (playerStats.IsAlive)
				{
					return playerStats.IsAttackLocked();
				}
				return true;
			}
			return false;
		}
	}

	public float DistanceToPlayer
	{
		get
		{
			if (!(player != null))
			{
				return float.MaxValue;
			}
			return Vector3.Distance(base.transform.position, player.position);
		}
	}

	private void Start()
	{
		InitializeComponents();
		SetupPathfinding();
		ConfigureCollider();
		SetupDetectionColliders();
		if (isMiniBoss)
		{
			MiniBossHealthBarUI.Register(this);
		}
	}

	private void Update()
	{
		if (isDead || player == null)
		{
			return;
		}
		bool flag = IsPlayerGrabbedByGrabScreen() || CinematicCameraSwapTrigger.IsAnyInCinematicView;
		if (flag)
		{
			PauseAIBehavior();
			wasPlayerGrabbedLastFrame = true;
			return;
		}
		if (wasPlayerGrabbedLastFrame && !flag)
		{
			ResumeAIBehavior();
			wasPlayerGrabbedLastFrame = false;
		}
		if (isTransforming)
		{
			UpdateAnimator();
			return;
		}
		UpdateStateMachine();
		HandleMeleeHit();
		HandleGrabHit();
		UpdateAnimator();
		UpdateIdleMovingSound();
	}

	private void FixedUpdate()
	{
		if (isDead || rb == null || (UnityEngine.Object)(object)followerEntity == null)
		{
			return;
		}
		if (!Physics.Raycast(base.transform.position + Vector3.up * 0.5f, Vector3.down, out var hitInfo, 1.5f))
		{
			followerEntity.position = base.transform.position;
			return;
		}
		followerEntity.position = base.transform.position;
		if ((currentState == AIState.Chasing || (currentState == AIState.Attacking && attackWhileMoving)) && followerEntity.simulateMovement)
		{
			Vector3 desiredVelocity = followerEntity.desiredVelocity;
			desiredVelocity = Vector3.ProjectOnPlane(desiredVelocity, hitInfo.normal).normalized * desiredVelocity.magnitude;
			rb.linearVelocity = desiredVelocity;
			if (desiredVelocity.x != 0f || desiredVelocity.z != 0f)
			{
				Vector3 forward = new Vector3(desiredVelocity.x, 0f, desiredVelocity.z);
				base.transform.rotation = Quaternion.LookRotation(forward);
			}
		}
		else if (!isDashing)
		{
			rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
		}
	}

	private void InitializeComponents()
	{
		currentHealth = maxHealth;
		if (player == null)
		{
			GameObject gameObject = GameObject.FindGameObjectWithTag("Player");
			if (gameObject != null)
			{
				player = gameObject.transform;
				playerCombatSystem = DualWieldingSystem.Instance;
				playerStats = player.GetComponent<PlayerStats>();
			}
		}
		if (grabScreen == null && canGrab)
		{
			grabScreen = GrabScreen.Instance;
			if (grabScreen == null)
			{
				canGrab = false;
			}
		}
		if (audioSource == null)
		{
			audioSource = GetComponent<AudioSource>();
			if (audioSource == null)
			{
				audioSource = base.gameObject.AddComponent<AudioSource>();
			}
		}
		if (loopingAudioSource == null)
		{
			loopingAudioSource = base.gameObject.AddComponent<AudioSource>();
			loopingAudioSource.loop = true;
			loopingAudioSource.playOnAwake = false;
		}
		if (autoFindPathfindingComponents && (UnityEngine.Object)(object)followerEntity == null)
		{
			followerEntity = GetComponent<FollowerEntity>();
		}
		rb = GetComponent<Rigidbody>();
		if (rb == null)
		{
			rb = base.gameObject.AddComponent<Rigidbody>();
		}
		rb.useGravity = true;
		rb.mass = enemyMass;
		rb.linearDamping = enemyDrag;
		rb.constraints = RigidbodyConstraints.FreezeRotation;
	}

	private void SetupPathfinding()
	{
		if (!((UnityEngine.Object)(object)followerEntity == null))
		{
			followerEntity.updatePosition = false;
			followerEntity.updateRotation = false;
			followerEntity.Teleport(base.transform.position);
			followerEntity.simulateMovement = false;
			Pathfinding.ECS.AutoRepathPolicy autoRepath = followerEntity.autoRepath;
			autoRepath.mode = Pathfinding.AutoRepathPolicy.Mode.EveryNSeconds;
			followerEntity.autoRepath = autoRepath;
			if (player != null)
			{
				followerEntity.destination = player.position;
			}
		}
	}

	private void ConfigureCollider()
	{
		if (GetComponent<Collider>() == null)
		{
			CapsuleCollider capsuleCollider = base.gameObject.AddComponent<CapsuleCollider>();
			capsuleCollider.height = 2f;
			capsuleCollider.radius = 0.5f;
			capsuleCollider.center = Vector3.up;
		}
		if (base.gameObject.layer == 0)
		{
			base.gameObject.layer = 8;
		}
	}

	private void SetupDetectionColliders()
	{
		if (attackDetectionCollider != null)
		{
			attackDetectionCollider.isTrigger = true;
			attackDetectionCollider.enabled = false;
			EnemyAttackDetector enemyAttackDetector = attackDetectionCollider.GetComponent<EnemyAttackDetector>();
			if (enemyAttackDetector == null)
			{
				enemyAttackDetector = attackDetectionCollider.gameObject.AddComponent<EnemyAttackDetector>();
			}
			enemyAttackDetector.Initialize(this);
		}
		if (grabDetectionCollider != null)
		{
			grabDetectionCollider.isTrigger = true;
			grabDetectionCollider.enabled = false;
			EnemyGrabDetector enemyGrabDetector = grabDetectionCollider.GetComponent<EnemyGrabDetector>();
			if (enemyGrabDetector == null)
			{
				enemyGrabDetector = grabDetectionCollider.gameObject.AddComponent<EnemyGrabDetector>();
			}
			enemyGrabDetector.Initialize(this);
		}
	}

	private void UpdateStateMachine()
	{
		if (!(player == null))
		{
			float distanceToPlayer = Vector3.Distance(base.transform.position, player.position);
			switch (currentState)
			{
			case AIState.Idle:
				HandleIdleState(distanceToPlayer);
				break;
			case AIState.Chasing:
				HandleChasingState(distanceToPlayer);
				break;
			case AIState.Attacking:
				HandleAttackingState(distanceToPlayer);
				break;
			case AIState.Grabbing:
				HandleGrabbingState(distanceToPlayer);
				break;
			case AIState.Dead:
				break;
			}
		}
	}

	private void HandleIdleState(float distanceToPlayer)
	{
		if (distanceToPlayer <= detectionRange)
		{
			TransitionToChasing();
		}
	}

	private bool PrefersRanged()
	{
		if (isTransformed && projectilePrefab != null && player != null)
		{
			return Vector3.Distance(base.transform.position, player.position) > EffectiveMeleeRange;
		}
		return false;
	}

	private void HandleChasingState(float distanceToPlayer)
	{
		if (canGrab && distanceToPlayer <= EffectiveGrabRange && CanGrab())
		{
			TransitionToGrabbing();
		}
		else if (!IsPlayerVulnerable && distanceToPlayer <= EffectiveAttackRange)
		{
			TransitionToAttacking();
		}
		else if (distanceToPlayer > loseTargetRange)
		{
			TransitionToIdle();
		}
		else if ((UnityEngine.Object)(object)followerEntity != null)
		{
			followerEntity.destination = player.position;
		}
	}

	private void HandleAttackingState(float distanceToPlayer)
	{
		if (attackWhileMoving && (UnityEngine.Object)(object)followerEntity != null && player != null)
		{
			Vector3 vector = base.transform.position - player.position;
			vector.y = 0f;
			if (vector.magnitude > attackMoveStopDistance)
			{
				Vector3 destination = player.position + vector.normalized * attackMoveStopDistance;
				destination.y = base.transform.position.y;
				followerEntity.destination = destination;
			}
			else
			{
				followerEntity.destination = base.transform.position;
			}
		}
		if (attackCoroutine == null)
		{
			if (distanceToPlayer > EffectiveAttackRange)
			{
				TransitionToChasing();
			}
			else if (CanAttack())
			{
				StartAttack();
			}
		}
	}

	private void HandleGrabbingState(float distanceToPlayer)
	{
		if (grabCoroutine == null)
		{
			if (distanceToPlayer > EffectiveGrabRange)
			{
				TransitionToChasing();
			}
			else if (CanGrab())
			{
				StartGrab();
			}
		}
	}

	private void TransitionToIdle()
	{
		currentState = AIState.Idle;
		StopPathfinding();
		StopAttack();
		StopGrab();
	}

	private void TransitionToChasing()
	{
		currentState = AIState.Chasing;
		StartPathfinding();
		StopAttack();
		StopGrab();
	}

	private void TransitionToAttacking()
	{
		currentState = AIState.Attacking;
		if (!attackWhileMoving)
		{
			StopPathfinding();
		}
		StopGrab();
	}

	private void TransitionToGrabbing()
	{
		currentState = AIState.Grabbing;
		StopPathfinding();
		StopAttack();
	}

	private void TransitionToDead()
	{
		currentState = AIState.Dead;
		StopPathfinding();
		StopAttack();
		StopGrab();
	}

	private void StartPathfinding()
	{
		if (!((UnityEngine.Object)(object)followerEntity == null))
		{
			followerEntity.Teleport(base.transform.position);
			followerEntity.simulateMovement = true;
			Pathfinding.ECS.AutoRepathPolicy autoRepath = followerEntity.autoRepath;
			autoRepath.mode = Pathfinding.AutoRepathPolicy.Mode.EveryNSeconds;
			followerEntity.autoRepath = autoRepath;
			if (player != null)
			{
				followerEntity.destination = player.position;
			}
		}
	}

	private void StopPathfinding()
	{
		if (!((UnityEngine.Object)(object)followerEntity == null))
		{
			followerEntity.simulateMovement = false;
		}
	}

	private bool CanAttack()
	{
		if (attackCoroutine != null)
		{
			return false;
		}
		if (!PrefersRanged())
		{
			return MeleeReady;
		}
		return RangedReady;
	}

	private void StartAttack()
	{
		if (attackCoroutine == null)
		{
			bool flag = PrefersRanged();
			if (flag)
			{
				lastRangedAttackTime = Time.time;
			}
			else
			{
				lastAttackTime = Time.time;
			}
			attackCoroutine = StartCoroutine(AttackSequence(flag));
		}
	}

	private void StopAttack()
	{
		if (attackCoroutine != null)
		{
			StopCoroutine(attackCoroutine);
			attackCoroutine = null;
		}
		if (attackDetectionCollider != null)
		{
			attackDetectionCollider.enabled = false;
		}
	}

	private IEnumerator AttackSequence(bool useRanged)
	{
		hasDealtDamageThisAttack = false;
		hasSpawnedProjectileThisAttack = false;
		if (!attackWhileMoving)
		{
			if ((UnityEngine.Object)(object)followerEntity != null)
			{
				followerEntity.simulateMovement = false;
			}
			if (rb != null)
			{
				rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
			}
			if (player != null)
			{
				Vector3 normalized = (player.position - base.transform.position).normalized;
				normalized.y = 0f;
				if (normalized != Vector3.zero)
				{
					base.transform.rotation = Quaternion.LookRotation(normalized);
				}
			}
		}
		if (animator != null)
		{
			animator.SetTrigger(useRanged ? rangedAttackTrigger : "Attack");
		}
		yield return new WaitForSeconds(attackCooldown);
		meleeInDamagingState = false;
		attackCoroutine = null;
		if (player != null && Vector3.Distance(base.transform.position, player.position) <= loseTargetRange)
		{
			TransitionToChasing();
		}
		else
		{
			TransitionToIdle();
		}
	}

	public void ResetDamageFlag()
	{
		hasDealtDamageThisAttack = false;
	}

	public void ResetGrabFlag()
	{
		hasGrabbedThisAttempt = false;
	}

	public void SpawnProjectile()
	{
		if (!hasSpawnedProjectileThisAttack && !(projectilePrefab == null) && !(projectileSpawnPoint == null) && !(player == null))
		{
			hasSpawnedProjectileThisAttack = true;
			Vector3 normalized;
			if (fireAlongSpawnPointRotation)
			{
				Vector3 vector = player.position + Vector3.up * 1.7f - projectileSpawnPoint.position;
				Vector3 vector2 = new Vector3(vector.x, 0f, vector.z);
				vector2 = ((vector2.sqrMagnitude > 0.0001f) ? vector2.normalized : projectileSpawnPoint.forward);
				float num = Mathf.Clamp(projectileSpawnPoint.forward.y, -1f, 1f);
				float num2 = Mathf.Sqrt(Mathf.Max(0f, 1f - num * num));
				normalized = (vector2 * num2 + Vector3.up * num).normalized;
			}
			else
			{
				normalized = (player.position + Vector3.up * 1.7f - projectileSpawnPoint.position).normalized;
			}
			EnemyProjectile.FireSpread(projectilePrefab, projectileSpawnPoint.position, normalized, projectileSpeed, projectileDamage, projectileLifetime, projectileCount, projectileSpread);
			PlaySound(secondStageShootSound);
		}
	}

	public void StopMovementForAttack()
	{
		StopIdleMovingSound();
		PlaySound(EffectiveAttackSound);
		if (!attackWhileMoving)
		{
			if ((UnityEngine.Object)(object)followerEntity != null)
			{
				followerEntity.simulateMovement = false;
			}
			if (rb != null)
			{
				rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
			}
		}
	}

	public void StopMovementForGrab()
	{
		if ((UnityEngine.Object)(object)followerEntity != null)
		{
			followerEntity.simulateMovement = false;
		}
		if (rb != null)
		{
			rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
		}
		StopIdleMovingSound();
		PlaySound(EffectiveGrabSound);
	}

	public void PlayTransformSound()
	{
		PlaySound(transformSound);
	}

	private bool CanGrab()
	{
		if (canGrab && grabScreen == null)
		{
			grabScreen = GrabScreen.Instance;
			if (grabScreen == null)
			{
				return false;
			}
		}
		if (canGrab && grabScreen != null && Time.time >= lastGrabTime + grabCooldown)
		{
			return grabCoroutine == null;
		}
		return false;
	}

	private void StartGrab()
	{
		if (grabCoroutine == null && canGrab && !(grabScreen == null))
		{
			lastGrabTime = Time.time;
			grabCoroutine = StartCoroutine(GrabSequence());
		}
	}

	private void StopGrab()
	{
		if (grabCoroutine != null)
		{
			StopCoroutine(grabCoroutine);
			grabCoroutine = null;
			isAttemptingGrab = false;
		}
		if (grabDetectionCollider != null)
		{
			grabDetectionCollider.enabled = false;
		}
	}

	private IEnumerator GrabSequence()
	{
		hasGrabbedThisAttempt = false;
		isAttemptingGrab = true;
		if ((UnityEngine.Object)(object)followerEntity != null)
		{
			followerEntity.simulateMovement = false;
		}
		if (rb != null)
		{
			rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
		}
		if (player != null)
		{
			Vector3 normalized = (player.position - base.transform.position).normalized;
			normalized.y = 0f;
			if (normalized != Vector3.zero)
			{
				base.transform.rotation = Quaternion.LookRotation(normalized);
			}
		}
		if (animator != null)
		{
			animator.SetTrigger("Grab");
		}
		yield return new WaitForSeconds(attackCooldown);
		grabInDamagingState = false;
		isAttemptingGrab = false;
		grabCoroutine = null;
		if (player != null && Vector3.Distance(base.transform.position, player.position) <= loseTargetRange)
		{
			TransitionToChasing();
		}
		else
		{
			TransitionToIdle();
		}
	}

	public void StartMeleeDamageState()
	{
		meleeInDamagingState = true;
	}

	public void EndMeleeDamageState()
	{
		meleeInDamagingState = false;
	}

	public void StartGrabDamageState()
	{
		grabInDamagingState = true;
	}

	public void EndGrabDamageState()
	{
		grabInDamagingState = false;
	}

	private void HandleMeleeHit()
	{
		if (!meleeInDamagingState || hasDealtDamageThisAttack || player == null)
		{
			return;
		}
		Vector3 vector = base.transform.position + Vector3.up;
		Vector3 vector2 = player.position + Vector3.up;
		Collider component = player.GetComponent<Collider>();
		if (component != null)
		{
			vector2 = component.bounds.center;
		}
		Vector3 vector3 = vector2 - vector;
		float effectiveMeleeHitRange = EffectiveMeleeHitRange;
		Debug.DrawRay(vector, vector3.normalized * effectiveMeleeHitRange, hasDealtDamageThisAttack ? Color.green : Color.red, 0.5f);
		RaycastHit[] array = Physics.RaycastAll(vector, vector3.normalized, effectiveMeleeHitRange, hitboxMask);
		Debug.Log($"[MeleeHit] Raycast from {vector}, dir {vector3.normalized}, range {effectiveMeleeHitRange}, hits: {array.Length}");
		for (int i = 0; i < array.Length; i++)
		{
			Debug.Log($"[MeleeHit] Hit: {array[i].collider.gameObject.name}, tag: {array[i].collider.tag}, layer: {array[i].collider.gameObject.layer}, root: {array[i].collider.transform.root.name}");
			if (!(array[i].collider.transform.root == base.transform.root) && array[i].collider.CompareTag("Player"))
			{
				Debug.Log($"[MeleeHit] DAMAGE! combatSystem={playerCombatSystem != null}, IDamageable={player.GetComponent<IDamageable>() != null}");
				hasDealtDamageThisAttack = true;
				meleeInDamagingState = false;
				if (playerCombatSystem != null)
				{
					playerCombatSystem.TakeDamage(attackDamage, base.gameObject);
				}
				else
				{
					player.GetComponent<IDamageable>()?.TakeDamage(attackDamage);
				}
				break;
			}
		}
	}

	private void GetGrabScreenAssets(out RuntimeAnimatorController controller, out GrabAnimationData[] audio)
	{
		bool flag = isTransformed && transformedGrabAnimatorController != null;
		controller = (flag ? transformedGrabAnimatorController : grabAnimatorController);
		audio = ((isTransformed && transformedGrabScreenAnimations != null && transformedGrabScreenAnimations.Length != 0) ? transformedGrabScreenAnimations : null);
	}

	private void HandleGrabHit()
	{
		if (!grabInDamagingState || hasGrabbedThisAttempt || !canGrab || player == null || grabScreen == null || grabScreen.IsGrabbed)
		{
			return;
		}
		Vector3 vector = base.transform.position + Vector3.up;
		Vector3 vector2 = player.position + Vector3.up;
		Collider component = player.GetComponent<Collider>();
		if (component != null)
		{
			vector2 = component.bounds.center;
		}
		Vector3 vector3 = vector2 - vector;
		float effectiveGrabHitRange = EffectiveGrabHitRange;
		Debug.DrawRay(vector, vector3.normalized * effectiveGrabHitRange, hasGrabbedThisAttempt ? Color.green : Color.magenta, 0.5f);
		RaycastHit[] array = Physics.RaycastAll(vector, vector3.normalized, effectiveGrabHitRange, hitboxMask);
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i].collider.transform.root == base.transform.root || !array[i].collider.CompareTag("Player"))
			{
				continue;
			}
			hasGrabbedThisAttempt = true;
			grabInDamagingState = false;
			GetGrabScreenAssets(out var controller, out var audio);
			grabScreen.StartGrab(base.gameObject, controller, grabCameraPositionOffset, grabCameraRotationOffset, keepPlayerRotation: false, audio);
			if (grabScreen.IsGrabbed)
			{
				if (grabCoroutine != null)
				{
					StopCoroutine(grabCoroutine);
					grabCoroutine = null;
				}
				RemoveEnemyAfterGrab();
			}
			break;
		}
	}

	public void OnPlayerAttackDetected()
	{
		if (!hasDealtDamageThisAttack)
		{
			hasDealtDamageThisAttack = true;
			if (attackDetectionCollider != null)
			{
				attackDetectionCollider.enabled = false;
			}
			if (playerCombatSystem != null)
			{
				playerCombatSystem.TakeDamage(attackDamage, base.gameObject);
			}
			else
			{
				player.GetComponent<IDamageable>()?.TakeDamage(attackDamage);
			}
		}
	}

	public void OnPlayerGrabDetected()
	{
		if (hasGrabbedThisAttempt || grabScreen == null || grabScreen.IsGrabbed)
		{
			return;
		}
		hasGrabbedThisAttempt = true;
		if (grabDetectionCollider != null)
		{
			grabDetectionCollider.enabled = false;
		}
		GetGrabScreenAssets(out var controller, out var audio);
		grabScreen.StartGrab(base.gameObject, controller, grabCameraPositionOffset, grabCameraRotationOffset, keepPlayerRotation: false, audio);
		if (grabScreen.IsGrabbed)
		{
			if (grabCoroutine != null)
			{
				StopCoroutine(grabCoroutine);
				grabCoroutine = null;
			}
			RemoveEnemyAfterGrab();
		}
	}

	private void RemoveEnemyAfterGrab()
	{
		StopAllCoroutines();
		StopIdleMovingSound();
		if (hideInsteadOfDestroyOnGrab)
		{
			ResetGrabTransientState();
			currentState = AIState.Idle;
			if (GrabScreen.Instance != null)
			{
				GrabScreen.Instance.RegisterHiddenEnemy(base.gameObject);
			}
			base.gameObject.SetActive(value: false);
		}
		else
		{
			TransitionToDead();
			if (deathEffect != null)
			{
				UnityEngine.Object.Instantiate(deathEffect, base.transform.position, Quaternion.identity);
			}
			PlaySound(EffectiveDeathSound);
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}

	private void ResetGrabTransientState()
	{
		hasGrabbedThisAttempt = false;
		hasDealtDamageThisAttack = false;
		grabInDamagingState = false;
		meleeInDamagingState = false;
		isAttemptingGrab = false;
		isDashing = false;
		isTransforming = false;
		if (grabDetectionCollider != null)
		{
			grabDetectionCollider.enabled = false;
		}
		if (attackDetectionCollider != null)
		{
			attackDetectionCollider.enabled = false;
		}
	}

	public void EndGrabHidden(float remainingHealth)
	{
		base.gameObject.SetActive(value: true);
		if (isTransformed)
		{
			ApplyTransformedAnimatorState();
		}
		lastGrabTime = Time.time;
		if (preserveHealthDuringGrab)
		{
			ResetGrabTransientState();
			currentState = AIState.Idle;
			if ((UnityEngine.Object)(object)followerEntity != null)
			{
				followerEntity.simulateMovement = true;
			}
		}
		else if (remainingHealth <= 0f)
		{
			currentHealth = 0;
			TransitionToDead();
			if (deathEffect != null)
			{
				UnityEngine.Object.Instantiate(deathEffect, base.transform.position, Quaternion.identity);
			}
			PlaySound(EffectiveDeathSound);
			UnityEngine.Object.Destroy(base.gameObject);
		}
		else
		{
			currentHealth = Mathf.RoundToInt(remainingHealth);
			ResetGrabTransientState();
			currentState = AIState.Idle;
			if ((UnityEngine.Object)(object)followerEntity != null)
			{
				followerEntity.simulateMovement = true;
			}
		}
	}

	public void PerformDash()
	{
		if (!(player == null) && !isDashing)
		{
			StartCoroutine(DashMovement());
		}
	}

	private IEnumerator DashMovement()
	{
		isDashing = true;
		Vector3 normalized = (player.position - base.transform.position).normalized;
		normalized.y = 0f;
		float effectiveDashDistance = EffectiveDashDistance;
		float effectiveDashDuration = EffectiveDashDuration;
		float num = ((effectiveDashDuration > 0f) ? (effectiveDashDistance / effectiveDashDuration) : 0f);
		rb.linearVelocity = normalized * num + new Vector3(0f, rb.linearVelocity.y, 0f);
		yield return new WaitForSeconds(effectiveDashDuration);
		rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
		isDashing = false;
	}

	private void UpdateAnimator()
	{
		if (animator == null)
		{
			return;
		}
		if (isTransformed)
		{
			ApplyTransformedAnimatorState();
		}
		if (!((UnityEngine.Object)(object)followerEntity == null))
		{
			float magnitude = ((rb != null) ? new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z) : Vector3.zero).magnitude;
			bool flag = magnitude > 0.1f;
			animator.SetBool("IsMoving", flag);
			animator.SetFloat("Speed", magnitude);
			if (followerEntity.maxSpeed > 0f)
			{
				animator.SetFloat("NormalizedSpeed", Mathf.Clamp01(magnitude / followerEntity.maxSpeed));
			}
			animator.SetBool("IsAttacking", attackCoroutine != null);
			animator.SetBool("IsGrabbing", isAttemptingGrab);
			animator.SetBool("IsDashing", isDashing);
			if (flag && player != null)
			{
				Vector3 normalized = (player.position - base.transform.position).normalized;
				animator.SetFloat("MoveX", normalized.x);
				animator.SetFloat("MoveZ", normalized.z);
			}
			else
			{
				animator.SetFloat("MoveX", 0f);
				animator.SetFloat("MoveZ", 0f);
			}
		}
	}

	public void TakeDamage(int damage)
	{
		if (isDead || isInvulnerable)
		{
			return;
		}
		currentHealth -= damage;
		currentHealth = Mathf.Max(0, currentHealth);
		if (healthBar != null)
		{
			healthBar.UpdateHealth(HealthPercentage);
		}
		PlaySound(EffectiveHurtSound);
		if (hitEffect != null)
		{
			GameObject instance = ((VFXPoolManager.Instance != null) ? VFXPoolManager.Instance.Get(hitEffect, base.transform.position + Vector3.up, Quaternion.identity) : UnityEngine.Object.Instantiate(hitEffect, base.transform.position + Vector3.up, Quaternion.identity));
			if (VFXPoolManager.Instance != null)
			{
				VFXPoolManager.Instance.ReturnAfterDelay(instance, 2f);
			}
		}
		if (DamageNumberManager.Instance != null)
		{
			DamageNumberManager.Instance.SpawnDamageNumber(base.transform.position, damage);
		}
		if (flashCoroutine != null)
		{
			StopCoroutine(flashCoroutine);
		}
		flashCoroutine = StartCoroutine(FlashDamageEffect());
		if (currentState == AIState.Idle && player != null)
		{
			TransitionToChasing();
		}
		if (currentHealth <= 0)
		{
			if (hasSecondStage && !isTransformed)
			{
				TransformToStageTwo();
			}
			else
			{
				Die();
			}
		}
	}

	private IEnumerator FlashDamageEffect()
	{
		if (cachedSprites == null)
		{
			cachedSprites = GetComponentsInChildren<SpriteRenderer>();
			if (cachedSprites.Length != 0)
			{
				originalSpriteColors = new Color[cachedSprites.Length];
				for (int i = 0; i < cachedSprites.Length; i++)
				{
					if (cachedSprites[i] != null)
					{
						originalSpriteColors[i] = cachedSprites[i].color;
					}
				}
			}
		}
		if (cachedSprites.Length != 0)
		{
			for (int j = 0; j < cachedSprites.Length; j++)
			{
				if (cachedSprites[j] != null)
				{
					cachedSprites[j].color = Color.red;
				}
			}
			yield return new WaitForSeconds(0.15f);
			for (int k = 0; k < cachedSprites.Length; k++)
			{
				if (cachedSprites[k] != null)
				{
					cachedSprites[k].color = originalSpriteColors[k];
				}
			}
		}
		else
		{
			if (cachedRenderers == null)
			{
				cachedRenderers = GetComponentsInChildren<Renderer>();
				if (cachedRenderers.Length != 0)
				{
					originalRendererColors = new Color[cachedRenderers.Length];
					for (int l = 0; l < cachedRenderers.Length; l++)
					{
						if (cachedRenderers[l] != null && cachedRenderers[l].material != null)
						{
							originalRendererColors[l] = cachedRenderers[l].material.color;
						}
					}
				}
			}
			if (cachedRenderers != null && cachedRenderers.Length != 0)
			{
				for (int m = 0; m < cachedRenderers.Length; m++)
				{
					if (cachedRenderers[m] != null && cachedRenderers[m].material != null)
					{
						cachedRenderers[m].material.color = Color.red;
					}
				}
				yield return new WaitForSeconds(0.15f);
				for (int n = 0; n < cachedRenderers.Length; n++)
				{
					if (cachedRenderers[n] != null && cachedRenderers[n].material != null)
					{
						cachedRenderers[n].material.color = originalRendererColors[n];
					}
				}
			}
		}
		flashCoroutine = null;
	}

	private void TransformToStageTwo()
	{
		if (!isTransformed)
		{
			isTransformed = true;
			int previousMaxHealth = maxHealth;
			maxHealth = secondStageMaxHealth;
			currentHealth = secondStageMaxHealth;
			if (healthBar != null)
			{
				healthBar.UpdateHealth(HealthPercentage);
			}
			if (isMiniBoss)
			{
				MiniBossHealthBarUI.NotifyStageChanged(this, previousMaxHealth);
			}
			if ((UnityEngine.Object)(object)followerEntity != null)
			{
				followerEntity.maxSpeed = secondStageMoveSpeed;
			}
			ApplyTransformedAnimatorState();
			StopAttack();
			StopGrab();
			StopIdleMovingSound();
			if (transformEffect != null)
			{
				UnityEngine.Object.Instantiate(transformEffect, base.transform.position, Quaternion.identity);
			}
			StartCoroutine(TransformInvulnerability());
			StartCoroutine(PlayTransformAnimation());
		}
	}

	private IEnumerator PlayTransformAnimation()
	{
		isTransforming = true;
		currentState = AIState.Idle;
		if (rb != null)
		{
			rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
		}
		float num = 0f;
		if (animator != null && !string.IsNullOrEmpty(transformStateName) && transformedLayerIndex > 0)
		{
			animator.ResetTrigger("Attack");
			animator.ResetTrigger("Grab");
			if (!string.IsNullOrEmpty(rangedAttackTrigger))
			{
				animator.ResetTrigger(rangedAttackTrigger);
			}
			animator.Play(transformStateName, transformedLayerIndex, 0f);
			yield return null;
			AnimatorStateInfo currentAnimatorStateInfo = animator.GetCurrentAnimatorStateInfo(transformedLayerIndex);
			num = ((!currentAnimatorStateInfo.IsName(transformStateName) || !(currentAnimatorStateInfo.length > 0f)) ? transformInvulnDuration : currentAnimatorStateInfo.length);
		}
		if (num > 0f)
		{
			yield return new WaitForSeconds(num);
		}
		isTransforming = false;
		if (player != null)
		{
			TransitionToChasing();
		}
	}

	private IEnumerator TransformInvulnerability()
	{
		isInvulnerable = true;
		yield return new WaitForSeconds(transformInvulnDuration);
		isInvulnerable = false;
	}

	private void ApplyTransformedAnimatorState()
	{
		if (!(animator == null))
		{
			animator.SetBool(transformedBoolName, value: true);
			if (!transformedLayerResolved)
			{
				transformedLayerIndex = animator.GetLayerIndex(transformedLayerName);
				transformedLayerResolved = true;
			}
			if (transformedLayerIndex > 0)
			{
				animator.SetLayerWeight(transformedLayerIndex, 1f);
			}
		}
	}

	private void OnDestroy()
	{
		if (isMiniBoss)
		{
			MiniBossHealthBarUI.Unregister(this);
		}
	}

	private void Die()
	{
		if (isDead)
		{
			return;
		}
		isDead = true;
		LevelStatsTracker.RecordEnemyKill();
		if (player != null)
		{
			PlayerStats component = player.GetComponent<PlayerStats>();
			if (component != null)
			{
				GridInventorySystem gridInventorySystem = player.GetComponent<GridInventorySystem>();
				if (gridInventorySystem == null)
				{
					gridInventorySystem = UnityEngine.Object.FindAnyObjectByType<GridInventorySystem>();
				}
				if (gridInventorySystem != null && gridInventorySystem.HasLifeStealOnKill())
				{
					int amount = Mathf.Min(Mathf.CeilToInt((float)maxHealth * 0.1f), 5);
					component.Heal(amount);
					if (DamageNumberManager.Instance != null)
					{
						DamageNumberManager.Instance.SpawnHealNumber(base.transform.position, amount);
					}
				}
			}
		}
		TransitionToDead();
		StopIdleMovingSound();
		PlaySound(EffectiveDeathSound);
		if (UnityEngine.Random.value <= coinDropChance)
		{
			int value = UnityEngine.Random.Range(minCoinDropValue, maxCoinDropValue + 1);
			Rigidbody component2 = CurrencyPickup.CreateCurrencyPickup(base.transform.position, value, currencyPrefab).GetComponent<Rigidbody>();
			if (component2 != null)
			{
				float f = UnityEngine.Random.Range(0f, 360f) * (MathF.PI / 180f);
				float f2 = UnityEngine.Random.Range(minLaunchAngle, maxLaunchAngle) * (MathF.PI / 180f);
				float num = UnityEngine.Random.Range(minLaunchForce, maxLaunchForce);
				Vector3 vector = new Vector3(Mathf.Cos(f), 0f, Mathf.Sin(f));
				component2.linearVelocity = vector * num * Mathf.Cos(f2) + Vector3.up * (num * Mathf.Sin(f2) + arcHeight);
				component2.AddTorque(new Vector3(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f)) * 3f, ForceMode.Impulse);
			}
		}
		if (deathEffect != null)
		{
			UnityEngine.Object.Instantiate(deathEffect, base.transform.position, Quaternion.identity);
		}
		UnityEngine.Object.Destroy(base.gameObject);
	}

	private bool IsPlayerGrabbedByGrabScreen()
	{
		if (grabScreen == null)
		{
			grabScreen = GrabScreen.Instance;
		}
		if (grabScreen == null)
		{
			return false;
		}
		return grabScreen.IsGrabbed;
	}

	private void PauseAIBehavior()
	{
		StopPathfinding();
		StopAttack();
		StopGrab();
		StopIdleMovingSound();
		if (animator != null)
		{
			animator.SetBool("IsMoving", value: false);
			animator.SetFloat("Speed", 0f);
			animator.SetFloat("NormalizedSpeed", 0f);
			animator.SetBool("IsAttacking", value: false);
			animator.SetBool("IsGrabbing", value: false);
			animator.SetBool("IsDashing", value: false);
			animator.SetFloat("MoveX", 0f);
			animator.SetFloat("MoveZ", 0f);
		}
	}

	private void ResumeAIBehavior()
	{
		if (currentState == AIState.Idle)
		{
			if (player != null && Vector3.Distance(base.transform.position, player.position) <= detectionRange)
			{
				TransitionToChasing();
			}
		}
		else
		{
			TransitionToChasing();
		}
	}

	private void PlaySound(AudioClip clip)
	{
		if (audioSource != null && clip != null)
		{
			audioSource.PlayOneShot(clip);
		}
	}

	private void UpdateIdleMovingSound()
	{
		if ((currentState == AIState.Idle || currentState == AIState.Chasing) && attackCoroutine == null && grabCoroutine == null && !isDead)
		{
			StartIdleMovingSound();
		}
		else
		{
			StopIdleMovingSound();
		}
	}

	private void StartIdleMovingSound()
	{
		AudioClip effectiveIdleMovingSound = EffectiveIdleMovingSound;
		if (loopingAudioSource != null && effectiveIdleMovingSound != null && (loopingAudioSource.clip != effectiveIdleMovingSound || !loopingAudioSource.isPlaying))
		{
			loopingAudioSource.clip = effectiveIdleMovingSound;
			loopingAudioSource.Play();
		}
	}

	private void StopIdleMovingSound()
	{
		if (loopingAudioSource != null && loopingAudioSource.isPlaying)
		{
			loopingAudioSource.Stop();
		}
	}

	public string GetMiniBossDisplayName()
	{
		if (isTransformed && !string.IsNullOrEmpty(miniBossStageTwoDisplayName))
		{
			return miniBossStageTwoDisplayName;
		}
		if (!string.IsNullOrEmpty(miniBossDisplayName))
		{
			return miniBossDisplayName;
		}
		return base.name;
	}

	public void ForceAggro()
	{
		if (!isDead && currentState == AIState.Idle)
		{
			TransitionToChasing();
		}
	}

	public void ForceTransform()
	{
		if (!isDead && !isTransformed)
		{
			if (!hasSecondStage)
			{
				Debug.LogWarning(base.name + ": ForceTransform called but hasSecondStage is false — nothing to transform into.", this);
			}
			else
			{
				TransformToStageTwo();
			}
		}
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireSphere(base.transform.position, detectionRange);
		Gizmos.color = Color.red;
		Gizmos.DrawWireSphere(base.transform.position, loseTargetRange);
		Gizmos.color = Color.red;
		Gizmos.DrawWireSphere(base.transform.position, attackRange);
		if (canGrab)
		{
			Gizmos.color = Color.magenta;
			Gizmos.DrawWireSphere(base.transform.position, grabRange);
		}
		if (hasSecondStage)
		{
			if (secondStageAttackRange > 0f)
			{
				Gizmos.color = Color.cyan;
				Gizmos.DrawWireSphere(base.transform.position, secondStageAttackRange);
			}
			if (canGrab && secondStageGrabRange > 0f)
			{
				Gizmos.color = Color.white;
				Gizmos.DrawWireSphere(base.transform.position, secondStageGrabRange);
			}
		}
		if (Application.isPlaying && maxHealth > 0)
		{
			Vector3 vector = base.transform.position + Vector3.up * 2.5f;
			Gizmos.color = Color.red;
			Gizmos.DrawWireCube(vector, new Vector3(2f, 0.2f, 0.1f));
			float healthPercentage = HealthPercentage;
			Gizmos.color = Color.green;
			Gizmos.DrawCube(size: new Vector3(2f * healthPercentage, 0.2f, 0.1f), center: vector + Vector3.left * (1f - healthPercentage));
		}
	}

	[ContextMenu("Force Aggro")]
	private void DebugForceAggro()
	{
		ForceAggro();
	}

	[ContextMenu("Take 10 Damage")]
	private void DebugTakeDamage()
	{
		TakeDamage(10);
	}

	[ContextMenu("Find GrabScreen")]
	private void DebugFindGrabScreen()
	{
		grabScreen = GrabScreen.Instance;
	}

	[ContextMenu("Force Transform")]
	private void DebugForceTransform()
	{
		ForceTransform();
	}
}
