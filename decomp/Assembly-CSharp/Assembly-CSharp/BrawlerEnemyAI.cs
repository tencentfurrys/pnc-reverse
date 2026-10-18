using System;
using System.Collections;
using CMF;
using Pathfinding;
using Pathfinding.ECS;
using PixelCrushers.GridController;
using UnityEngine;

public class BrawlerEnemyAI : MonoBehaviour, IDamageable
{
	public enum AIState
	{
		Idle,
		Chasing,
		Attacking,
		Shooting,
		Hypnotising,
		Dead
	}

	[Header("Health")]
	[Tooltip("Maximum hit points. The pool currentHealth starts at and the health bar fills against.")]
	public int maxHealth = 50;

	[Tooltip("Runtime health value. Initialized from maxHealth at Start; not a designer knob.")]
	public int currentHealth;

	[Header("AI Behavior — Distance Bands")]
	[Tooltip("Distance at which an idle enemy notices the player and begins chasing. Keep at or below shootRange or the enemy aggros from outside its own weapon reach.")]
	public float detectionRange = 14f;

	[Tooltip("Distance at which a chasing enemy gives up and returns to idle. Keep larger than detectionRange to avoid flicker.")]
	public float loseTargetRange = 18f;

	[Header("Hypnosis Attack")]
	[Tooltip("If true, this enemy can perform its hypnosis attack. When it does, it drags the player's camera onto itself while it's on-screen and advances until it lands a grab.")]
	public bool canHypnotise = true;

	[Tooltip("Medium range at which the enemy begins hypnosis (needs line of sight). Larger than attackRange so the mesmerize-and-advance has room to matter, and it outranks melee/shoot.")]
	public float hypnosisStartRange = 8f;

	[Tooltip("Distance at which the advancing enemy actually grabs the player and starts the grab cutscene. Must be smaller than attackRange.")]
	public float grabRange = 2f;

	[Tooltip("Seconds between hypnosis attempts. Long — this is the signature opener.")]
	public float hypnosisCooldown = 9f;

	[Tooltip("Safety cap. If the enemy can't reach the player within this many seconds (e.g. the player keeps looking away), it breaks off and returns to chasing.")]
	public float maxHypnosisDuration = 8f;

	[Tooltip("How fast (deg/sec) the player's camera is dragged toward this enemy while it's on-screen. The whole difficulty knob — higher is harder to look away from.")]
	public float hypnosisLookSpeed = 120f;

	[Tooltip("Height up the enemy the gaze is pulled toward and the view check samples (the 'eyes').")]
	public float hypnosisLookHeight = 1.2f;

	[Tooltip("Animator state name of the looping hypnosis clip. The gaze-pull, forward advance and grab are active only while this state is playing — so the entry (Hypnosis Start) reads as a pure wind-up telegraph and the exit (Hypnosis End) as a clean wind-down.")]
	public string hypnosisLoopStateName = "Hypnosis Loop";

	[Tooltip("Animator state name of the hypnosis wind-down clip. When hypnosis ends without a grab, the enemy stays committed until this clip finishes so its shoot/melee can't cut it off.")]
	public string hypnosisEndStateName = "Hypnosis End";

	[Header("Attack")]
	[Tooltip("Melee band. Inside this the enemy always swings and never shoots, even with the shot off cooldown. Sits between grabRange and shootRange.")]
	public float attackRange = 3.5f;

	[Tooltip("Hit points removed from the player per landed melee hit.")]
	public int attackDamage = 10;

	[Tooltip("Seconds between consecutive melee swings.")]
	public float attackCooldown = 1.5f;

	[Tooltip("Seconds the melee swing animation locks the enemy. Should match the Attack clip length, and must be shorter than attackCooldown.")]
	public float meleeAttackDuration = 0.8f;

	[Tooltip("Distance the melee hit-test reaches from the enemy. Slightly larger than attackRange so swings can connect on approach.")]
	public float meleeHitRange = 4f;

	[Tooltip("Layers the melee/grab hit-tests consider as valid targets. Must include the player's layer.")]
	[SerializeField]
	private LayerMask hitboxMask = -1;

	[Tooltip("If true, the enemy keeps pathfinding toward the player during its melee animation instead of rooting in place. Shooting always roots regardless.")]
	public bool attackWhileMoving;

	[Tooltip("Only used when attackWhileMoving is true. Minimum distance to keep from the player while swinging — prevents walking on top of / pushing them. Should be slightly less than meleeHitRange so swings still connect. This is a one-sided clamp: the enemy stops closing, it never backs away.")]
	public float attackMoveStopDistance = 1.5f;

	[Header("Projectile Attack")]
	[Tooltip("Outer band. The enemy only shoots when the player is strictly further than attackRange and no further than this. Must be larger than attackRange.")]
	public float shootRange = 12f;

	[Tooltip("Projectile prefab fired at range. Should have (or will be given) an EnemyProjectile component.")]
	public GameObject projectilePrefab;

	[Tooltip("Transform the projectile spawns from and aims out of. Place at the muzzle/mouth/hands.")]
	public Transform projectileSpawnPoint;

	[Tooltip("Damage each projectile deals to the player.")]
	public int projectileDamage = 15;

	[Tooltip("Travel speed of projectiles, world units per second.")]
	public float projectileSpeed = 10f;

	[Tooltip("Seconds before a fired projectile auto-despawns.")]
	public float projectileLifetime = 5f;

	[Tooltip("Seconds between ranged attacks. While the shot is on cooldown the enemy keeps closing in to melee/grab — it never hovers at range.")]
	public float shootCooldown = 3f;

	[Tooltip("Seconds the shoot animation locks the enemy. Should match the RangedAttack clip length, and must be shorter than shootCooldown.")]
	public float shootDuration = 0.6f;

	[Tooltip("Number of projectiles fired per ranged attack.")]
	public int projectileCount = 1;

	[Tooltip("Total spread angle (degrees) the projectiles are fanned across. 0 = single straight shot.")]
	public float projectileSpread;

	[Tooltip("If true, the shot still tracks the player horizontally but takes its vertical launch angle from the Projectile Spawn Point's pitch (X rotation) — for arcing/diagonal shots that still aim at the player. If false, aims straight at the player.")]
	public bool fireAlongSpawnPointRotation;

	[Tooltip("Animator trigger fired for the ranged attack (instead of the melee \"Attack\" trigger).")]
	public string rangedAttackTrigger = "RangedAttack";

	[Tooltip("What blocks the enemy's line of fire. The enemy only shoots when it can see the player through these layers; with no clear shot it simply keeps closing in. Must exclude this enemy's own layer or it blocks its own shot.")]
	public LayerMask lineOfSightLayers = -1;

	[Header("Cooldown Variance")]
	[Tooltip("Per-action jitter applied to the melee/shoot/grab cooldowns. 0.2 = +/- 20%. Prevents a pack of the same prefab from acting in lockstep.")]
	[Range(0f, 0.5f)]
	public float cooldownJitter = 0.2f;

	[Header("Dash")]
	[Tooltip("World units the enemy lunges forward during a dash attack. Fired from an Animation Event.")]
	public float dashDistance = 2f;

	[Tooltip("Seconds the dash motion lasts before the enemy stops.")]
	public float dashDuration = 0.3f;

	[Header("Physics")]
	[Tooltip("Rigidbody mass. Affects how much knockback the enemy takes from hits.")]
	public float enemyMass = 5f;

	[Tooltip("Rigidbody linear drag. Higher values make the enemy decelerate faster after knockback.")]
	public float enemyDrag = 8f;

	[Header("Grab Animation Settings")]
	[Tooltip("Animator controller swapped onto the grab screen when this enemy grabs the player.")]
	public RuntimeAnimatorController grabAnimatorController;

	[Header("Grab Camera Settings")]
	[Tooltip("Camera position offset (relative to enemy) used while the grab cinematic plays. Tune to frame the grab nicely.")]
	public Vector3 grabCameraPositionOffset = Vector3.zero;

	[Tooltip("Camera rotation offset (Euler angles) used while the grab cinematic plays.")]
	public Vector3 grabCameraRotationOffset = Vector3.zero;

	[Header("Grab Screen Audio")]
	[Tooltip("Per-animation sounds for the grab screen. animationName must match the Animator state name. Passed to GrabScreen.StartGrab explicitly — see LandGrab.")]
	public GrabAnimationData[] grabScreenAnimations;

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

	[Tooltip("Plays at the moment of a ranged shot.")]
	public AudioClip shootSound;

	[Tooltip("Plays when this enemy dies.")]
	public AudioClip deathSound;

	[Tooltip("Looping sound played while idle/walking. Stops during attacks and on death.")]
	public AudioClip idleMovingSound;

	[Tooltip("Plays when this enemy attempts a grab.")]
	public AudioClip grabSound;

	[Tooltip("One-shot played as hypnosis begins, over the Hypnosis Start wind-up clip.")]
	public AudioClip hypnosisEnterSound;

	[Tooltip("Looping sound played for as long as the Hypnosis Loop clip is the active animator state. Shares loopingAudioSource with the idle sound.")]
	public AudioClip hypnosisLoopSound;

	[Tooltip("One-shot played as hypnosis winds down, over the Hypnosis End clip. Not played when the hypnosis ends in a successful grab.")]
	public AudioClip hypnosisExitSound;

	[Tooltip("One-shot AudioSource used for hurt/attack/death sounds. Auto-found or auto-added at Start if blank.")]
	public AudioSource audioSource;

	[Tooltip("Looping AudioSource used for the idle/walking and hypnosis loop sounds. Auto-added at Start if blank.")]
	public AudioSource loopingAudioSource;

	[Header("Effects")]
	[Tooltip("VFX prefab spawned at the enemy's position when it dies.")]
	public GameObject deathEffect;

	[Tooltip("VFX prefab spawned at the hit point when this enemy takes damage.")]
	public GameObject hitEffect;

	[Tooltip("Optional VFX spawned at the projectile spawn point when firing. Triggered by an Animation Event.")]
	public GameObject muzzleFlashEffect;

	[Header("Gallery")]
	[Tooltip("Unique ID for gallery unlock tracking. Should match EnemyGalleryEntry.enemyID")]
	public string galleryEnemyID;

	[Header("Health Bar")]
	[Tooltip("Optional health bar UI. Drives the fill amount from currentHealth/maxHealth.")]
	[SerializeField]
	private EnemyHealthBar healthBar;

	[Header("References")]
	[Tooltip("Player transform. Auto-found by tag at Start if left blank.")]
	public Transform player;

	[Tooltip("Animator driving this enemy's clips. Auto-found via GetComponentInChildren at Start if blank.")]
	public Animator animator;

	[Tooltip("GrabScreen used during the grab cinematic. Auto-resolved via GrabScreen.Instance at Start if blank.")]
	public GrabScreen grabScreen;

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

	private float lastAttackTime = -999f;

	private float lastShootTime = -999f;

	private float lastHypnosisTime = -999f;

	private bool isDead;

	private bool hasDealtDamageThisAttack;

	private bool isDashing;

	private bool hasSpawnedProjectileThisAttack;

	private bool meleeInDamagingState;

	private bool wasPlayerGrabbedLastFrame;

	private DualWieldingSystem playerCombatSystem;

	private PlayerStats playerStats;

	private Rigidbody rb;

	private CameraController cameraController;

	private Camera playerCamera;

	private bool hypnosisInView;

	private bool hypnosisEnding;

	private float hypnosisStartedAt;

	private static BrawlerEnemyAI currentHypnotist;

	private AIState currentState;

	private Coroutine attackCoroutine;

	private Coroutine shootCoroutine;

	private Coroutine hypnosisCoroutine;

	private Coroutine flashCoroutine;

	private SpriteRenderer[] cachedSprites;

	private Color[] originalSpriteColors;

	private Renderer[] cachedRenderers;

	private Color[] originalRendererColors;

	private bool HypnosisReady
	{
		get
		{
			if (canHypnotise && grabScreen != null && hypnosisCoroutine == null)
			{
				return Time.time >= lastHypnosisTime + hypnosisCooldown;
			}
			return false;
		}
	}

	private bool MeleeReady
	{
		get
		{
			if (attackCoroutine == null)
			{
				return Time.time >= lastAttackTime + attackCooldown;
			}
			return false;
		}
	}

	private bool ShootReady
	{
		get
		{
			if (projectilePrefab != null && projectileSpawnPoint != null && shootCoroutine == null)
			{
				return Time.time >= lastShootTime + shootCooldown;
			}
			return false;
		}
	}

	public bool IsDead => isDead;

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

	private bool IsPlayerUnattackable
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

	private bool IsPlayerDead
	{
		get
		{
			if (playerStats != null)
			{
				return !playerStats.IsAlive;
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
			return Vector3.Distance(transform.position, player.position);
		}
	}

	private void Start()
	{
		InitializeComponents();
		SetupPathfinding();
		ConfigureCollider();
	}

	private void OnValidate()
	{
		grabRange = Mathf.Max(0.1f, grabRange);
		attackRange = Mathf.Max(grabRange + 0.1f, attackRange);
		shootRange = Mathf.Max(attackRange + 0.1f, shootRange);
		hypnosisStartRange = Mathf.Max(attackRange + 0.1f, hypnosisStartRange);
		meleeAttackDuration = Mathf.Min(meleeAttackDuration, attackCooldown);
		shootDuration = Mathf.Min(shootDuration, shootCooldown);
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
		UpdateStateMachine();
		HandleMeleeHit();
		UpdateAnimator();
		UpdateIdleMovingSound();
	}

	private void OnDestroy()
	{
		if (currentHypnotist == this)
		{
			currentHypnotist = null;
		}
	}

	private void LateUpdate()
	{
		if (currentState == AIState.Hypnotising && hypnosisInView && InHypnosisLoop() && currentHypnotist == this && cameraController != null)
		{
			cameraController.RotateTowardPosition(transform.position + Vector3.up * hypnosisLookHeight, hypnosisLookSpeed);
		}
	}

	private void FixedUpdate()
	{
		if (isDead || rb == null || (UnityEngine.Object)(object)followerEntity == null)
		{
			return;
		}
		if (!Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out var hitInfo, 1.5f))
		{
			followerEntity.position = transform.position;
			return;
		}
		followerEntity.position = transform.position;
		if ((currentState == AIState.Chasing || (currentState == AIState.Attacking && attackWhileMoving) || (currentState == AIState.Hypnotising && hypnosisInView && InHypnosisLoop())) && followerEntity.simulateMovement)
		{
			Vector3 desiredVelocity = followerEntity.desiredVelocity;
			desiredVelocity = Vector3.ProjectOnPlane(desiredVelocity, hitInfo.normal).normalized * desiredVelocity.magnitude;
			rb.linearVelocity = desiredVelocity;
			if (desiredVelocity.x != 0f || desiredVelocity.z != 0f)
			{
				Vector3 forward = new Vector3(desiredVelocity.x, 0f, desiredVelocity.z);
				transform.rotation = Quaternion.LookRotation(forward);
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
		if (player != null)
		{
			if (playerCamera == null)
			{
				playerCamera = player.GetComponentInChildren<Camera>();
			}
			if (cameraController == null)
			{
				cameraController = player.GetComponentInChildren<CameraController>();
			}
		}
		if (animator == null)
		{
			animator = GetComponentInChildren<Animator>();
		}
		if (grabScreen == null && canHypnotise)
		{
			grabScreen = GrabScreen.Instance;
			if (grabScreen == null)
			{
				canHypnotise = false;
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
			followerEntity.Teleport(transform.position);
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
			CapsuleCollider capsuleCollider = gameObject.AddComponent<CapsuleCollider>();
			capsuleCollider.height = 2f;
			capsuleCollider.radius = 0.5f;
			capsuleCollider.center = Vector3.up;
		}
		if (gameObject.layer == 0)
		{
			gameObject.layer = 8;
		}
	}

	private void UpdateStateMachine()
	{
		if (!(player == null))
		{
			float distanceToPlayer = Vector3.Distance(transform.position, player.position);
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
			case AIState.Shooting:
				HandleShootingState(distanceToPlayer);
				break;
			case AIState.Hypnotising:
				HandleHypnotisingState(distanceToPlayer);
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

	private void HandleChasingState(float distanceToPlayer)
	{
		if (distanceToPlayer > loseTargetRange)
		{
			TransitionToIdle();
			return;
		}
		if (IsPlayerDead && canHypnotise && grabScreen != null && distanceToPlayer <= grabRange)
		{
			LandGrab();
			return;
		}
		if (!IsPlayerUnattackable)
		{
			if (distanceToPlayer <= hypnosisStartRange && HypnosisReady && HasLineOfSight())
			{
				TransitionToHypnotising();
				return;
			}
			if (distanceToPlayer <= attackRange && MeleeReady)
			{
				TransitionToAttacking();
				return;
			}
			if (distanceToPlayer > attackRange && distanceToPlayer <= shootRange && ShootReady && HasLineOfSight())
			{
				TransitionToShooting();
				return;
			}
		}
		if ((UnityEngine.Object)(object)followerEntity != null)
		{
			followerEntity.destination = player.position;
		}
	}

	private void HandleAttackingState(float distanceToPlayer)
	{
		if (attackWhileMoving && (UnityEngine.Object)(object)followerEntity != null && player != null)
		{
			Vector3 vector = transform.position - player.position;
			vector.y = 0f;
			if (vector.magnitude > attackMoveStopDistance)
			{
				Vector3 destination = player.position + vector.normalized * attackMoveStopDistance;
				destination.y = transform.position.y;
				followerEntity.destination = destination;
			}
			else
			{
				followerEntity.destination = transform.position;
			}
		}
		if (attackCoroutine == null)
		{
			TransitionToChasing();
		}
	}

	private void HandleShootingState(float distanceToPlayer)
	{
		if (shootCoroutine == null)
		{
			TransitionToChasing();
		}
	}

	private void HandleHypnotisingState(float distanceToPlayer)
	{
		if (hypnosisEnding)
		{
			return;
		}
		if (distanceToPlayer > loseTargetRange || Time.time - hypnosisStartedAt > maxHypnosisDuration)
		{
			StartCoroutine(EndHypnosis());
			return;
		}
		hypnosisInView = IsPlayerDead || IsInPlayerView();
		bool flag = InHypnosisLoop();
		if (flag)
		{
			StartHypnosisLoopSound();
		}
		else
		{
			StopHypnosisLoopSound();
		}
		if (flag && distanceToPlayer <= grabRange)
		{
			LandGrab();
		}
		else if ((UnityEngine.Object)(object)followerEntity != null)
		{
			followerEntity.destination = player.position;
			followerEntity.simulateMovement = flag && hypnosisInView;
		}
	}

	private void TransitionToIdle()
	{
		currentState = AIState.Idle;
		StopPathfinding();
		StopAttack();
		StopShooting();
		StopHypnosis();
	}

	private void TransitionToChasing()
	{
		currentState = AIState.Chasing;
		StartPathfinding();
		StopAttack();
		StopShooting();
		StopHypnosis();
	}

	private void TransitionToAttacking()
	{
		currentState = AIState.Attacking;
		if (!attackWhileMoving)
		{
			StopPathfinding();
		}
		StopShooting();
		StopHypnosis();
		StartAttack();
	}

	private void TransitionToShooting()
	{
		currentState = AIState.Shooting;
		StopPathfinding();
		StopAttack();
		StopHypnosis();
		StartShooting();
	}

	private void TransitionToHypnotising()
	{
		currentState = AIState.Hypnotising;
		StopAttack();
		StopShooting();
		StartHypnosis();
	}

	private void TransitionToDead()
	{
		currentState = AIState.Dead;
		StopPathfinding();
		StopAttack();
		StopShooting();
		StopHypnosis();
	}

	private void StartPathfinding()
	{
		if (!((UnityEngine.Object)(object)followerEntity == null))
		{
			followerEntity.Teleport(transform.position);
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

	private void RootInPlace()
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

	private void FacePlayer()
	{
		if (!(player == null))
		{
			Vector3 normalized = (player.position - transform.position).normalized;
			normalized.y = 0f;
			if (normalized != Vector3.zero)
			{
				transform.rotation = Quaternion.LookRotation(normalized);
			}
		}
	}

	private float JitteredStamp(float cooldown)
	{
		return Time.time + UnityEngine.Random.Range(0f - cooldownJitter, cooldownJitter) * cooldown;
	}

	private void StartAttack()
	{
		if (attackCoroutine == null)
		{
			lastAttackTime = JitteredStamp(attackCooldown);
			attackCoroutine = StartCoroutine(AttackSequence());
		}
	}

	private void StopAttack()
	{
		if (attackCoroutine != null)
		{
			StopCoroutine(attackCoroutine);
			attackCoroutine = null;
		}
	}

	private IEnumerator AttackSequence()
	{
		hasDealtDamageThisAttack = false;
		if (!attackWhileMoving)
		{
			RootInPlace();
			FacePlayer();
		}
		if (animator != null)
		{
			animator.SetTrigger("Attack");
		}
		yield return new WaitForSeconds(meleeAttackDuration);
		meleeInDamagingState = false;
		attackCoroutine = null;
	}

	public void StartMeleeDamageState()
	{
		meleeInDamagingState = true;
	}

	public void EndMeleeDamageState()
	{
		meleeInDamagingState = false;
	}

	public void StopMovementForAttack()
	{
		StopIdleMovingSound();
		PlaySound(attackSound);
		if (!attackWhileMoving)
		{
			RootInPlace();
		}
	}

	public void ResetDamageFlag()
	{
		hasDealtDamageThisAttack = false;
	}

	private void HandleMeleeHit()
	{
		if (!meleeInDamagingState || hasDealtDamageThisAttack || player == null)
		{
			return;
		}
		Vector3 vector = transform.position + Vector3.up;
		Vector3 vector2 = player.position + Vector3.up;
		Collider component = player.GetComponent<Collider>();
		if (component != null)
		{
			vector2 = component.bounds.center;
		}
		Vector3 vector3 = vector2 - vector;
		Debug.DrawRay(vector, vector3.normalized * meleeHitRange, hasDealtDamageThisAttack ? Color.green : Color.red, 0.5f);
		RaycastHit[] array = Physics.RaycastAll(vector, vector3.normalized, meleeHitRange, hitboxMask);
		for (int i = 0; i < array.Length; i++)
		{
			if (!(array[i].collider.transform.root == transform.root) && array[i].collider.CompareTag("Player"))
			{
				hasDealtDamageThisAttack = true;
				meleeInDamagingState = false;
				if (playerCombatSystem != null)
				{
					playerCombatSystem.TakeDamage(attackDamage, gameObject);
				}
				else
				{
					player.GetComponent<IDamageable>()?.TakeDamage(attackDamage);
				}
				break;
			}
		}
	}

	private void StartShooting()
	{
		if (shootCoroutine == null)
		{
			lastShootTime = JitteredStamp(shootCooldown);
			shootCoroutine = StartCoroutine(ShootSequence());
		}
	}

	private void StopShooting()
	{
		if (shootCoroutine != null)
		{
			StopCoroutine(shootCoroutine);
			shootCoroutine = null;
		}
	}

	private IEnumerator ShootSequence()
	{
		hasSpawnedProjectileThisAttack = false;
		RootInPlace();
		FacePlayer();
		if (animator != null)
		{
			animator.SetTrigger(rangedAttackTrigger);
		}
		yield return new WaitForSeconds(shootDuration);
		shootCoroutine = null;
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
		}
	}

	public void ResetProjectileFlag()
	{
		hasSpawnedProjectileThisAttack = false;
	}

	public void StopMovementForShoot()
	{
		StopIdleMovingSound();
		RootInPlace();
	}

	public void PlayShootSound()
	{
		PlaySound(shootSound);
	}

	public void SpawnMuzzleFlash()
	{
		if (!(muzzleFlashEffect == null) && !(projectileSpawnPoint == null))
		{
			UnityEngine.Object.Instantiate(muzzleFlashEffect, projectileSpawnPoint.position, projectileSpawnPoint.rotation);
		}
	}

	private bool HasLineOfSight()
	{
		if (player == null)
		{
			return false;
		}
		Vector3 vector = player.position + Vector3.up * 1f;
		Vector3 vector2 = transform.position + Vector3.up * 1f;
		Vector3 vector3 = vector - vector2;
		if (Physics.Raycast(vector2, vector3.normalized, out var hitInfo, vector3.magnitude, lineOfSightLayers))
		{
			return hitInfo.collider.CompareTag("Player");
		}
		return true;
	}

	private void StartHypnosis()
	{
		if (hypnosisCoroutine == null && canHypnotise && !(grabScreen == null))
		{
			lastHypnosisTime = JitteredStamp(hypnosisCooldown);
			hypnosisStartedAt = Time.time;
			hypnosisInView = false;
			if (currentHypnotist == null)
			{
				currentHypnotist = this;
			}
			StartPathfinding();
			if (animator != null)
			{
				animator.ResetTrigger("Hypnotise");
				animator.SetTrigger("Hypnotise");
				animator.SetBool("Hypnotising?", value: true);
			}
			PlaySound(hypnosisEnterSound);
			hypnosisCoroutine = StartCoroutine(HypnosisHold());
		}
	}

	private IEnumerator HypnosisHold()
	{
		while (true)
		{
			yield return null;
		}
	}

	private void StopHypnosis()
	{
		if (hypnosisCoroutine != null)
		{
			StopCoroutine(hypnosisCoroutine);
			hypnosisCoroutine = null;
		}
		hypnosisInView = false;
		StopHypnosisLoopSound();
		if (animator != null)
		{
			animator.SetBool("Hypnotising?", value: false);
			animator.ResetTrigger("Hypnotise");
		}
		if (currentHypnotist == this)
		{
			currentHypnotist = null;
		}
	}

	private bool InHypnosisLoop()
	{
		if (animator == null)
		{
			return false;
		}
		return animator.GetCurrentAnimatorStateInfo(0).IsName(hypnosisLoopStateName);
	}

	private IEnumerator EndHypnosis()
	{
		hypnosisEnding = true;
		StopHypnosis();
		PlaySound(hypnosisExitSound);
		if (animator != null)
		{
			float safety = Time.time + 2f;
			while (Time.time < safety && !animator.GetCurrentAnimatorStateInfo(0).IsName(hypnosisEndStateName))
			{
				yield return null;
			}
			while (Time.time < safety && animator.GetCurrentAnimatorStateInfo(0).IsName(hypnosisEndStateName) && animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 0.95f)
			{
				yield return null;
			}
		}
		hypnosisEnding = false;
		if (!isDead)
		{
			TransitionToChasing();
		}
	}

	private bool IsInPlayerView()
	{
		if (playerCamera == null)
		{
			return false;
		}
		Vector3 vector = transform.position + Vector3.up * hypnosisLookHeight;
		Vector3 vector2 = playerCamera.WorldToViewportPoint(vector);
		if (!(vector2.z > 0f) || !(vector2.x >= 0f) || !(vector2.x <= 1f) || !(vector2.y >= 0f) || !(vector2.y <= 1f))
		{
			return false;
		}
		Vector3 position = playerCamera.transform.position;
		Vector3 vector3 = vector - position;
		if (Physics.Raycast(position, vector3.normalized, out var hitInfo, vector3.magnitude, lineOfSightLayers))
		{
			return hitInfo.collider.transform.root == transform.root;
		}
		return true;
	}

	private void LandGrab()
	{
		if (!(grabScreen == null) && !grabScreen.IsGrabbed)
		{
			grabScreen.StartGrab(gameObject, grabAnimatorController, grabCameraPositionOffset, grabCameraRotationOffset, keepPlayerRotation: false, grabScreenAnimations);
			if (grabScreen.IsGrabbed)
			{
				PlaySound(grabSound);
				StopHypnosis();
				RemoveEnemyAfterGrab();
			}
		}
	}

	private void RemoveEnemyAfterGrab()
	{
		StopAllCoroutines();
		StopIdleMovingSound();
		TransitionToDead();
		if (deathEffect != null)
		{
			UnityEngine.Object.Instantiate(deathEffect, transform.position, Quaternion.identity);
		}
		PlaySound(deathSound);
		UnityEngine.Object.Destroy(gameObject);
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
		Vector3 normalized = (player.position - transform.position).normalized;
		normalized.y = 0f;
		float num = dashDistance / dashDuration;
		rb.linearVelocity = normalized * num + new Vector3(0f, rb.linearVelocity.y, 0f);
		yield return new WaitForSeconds(dashDuration);
		rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
		isDashing = false;
	}

	private void UpdateAnimator()
	{
		if (!(animator == null) && !((UnityEngine.Object)(object)followerEntity == null))
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
			animator.SetBool("IsShooting", shootCoroutine != null);
			animator.SetBool("IsDashing", isDashing);
			if (flag && player != null)
			{
				Vector3 normalized = (player.position - transform.position).normalized;
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
		if (isDead)
		{
			return;
		}
		currentHealth -= damage;
		currentHealth = Mathf.Max(0, currentHealth);
		if (healthBar != null)
		{
			healthBar.UpdateHealth(HealthPercentage);
		}
		PlaySound(hurtSound);
		if (hitEffect != null)
		{
			GameObject instance = ((VFXPoolManager.Instance != null) ? VFXPoolManager.Instance.Get(hitEffect, transform.position + Vector3.up, Quaternion.identity) : UnityEngine.Object.Instantiate(hitEffect, transform.position + Vector3.up, Quaternion.identity));
			if (VFXPoolManager.Instance != null)
			{
				VFXPoolManager.Instance.ReturnAfterDelay(instance, 2f);
			}
		}
		if (DamageNumberManager.Instance != null)
		{
			DamageNumberManager.Instance.SpawnDamageNumber(transform.position, damage);
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
			Die();
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
						DamageNumberManager.Instance.SpawnHealNumber(transform.position, amount);
					}
				}
			}
		}
		TransitionToDead();
		StopIdleMovingSound();
		PlaySound(deathSound);
		if (UnityEngine.Random.value <= coinDropChance)
		{
			int value = UnityEngine.Random.Range(minCoinDropValue, maxCoinDropValue + 1);
			Rigidbody component2 = CurrencyPickup.CreateCurrencyPickup(transform.position, value, currencyPrefab).GetComponent<Rigidbody>();
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
			UnityEngine.Object.Instantiate(deathEffect, transform.position, Quaternion.identity);
		}
		UnityEngine.Object.Destroy(gameObject);
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
		StopShooting();
		StopHypnosis();
		StopIdleMovingSound();
		if (animator != null)
		{
			animator.SetBool("IsMoving", value: false);
			animator.SetFloat("Speed", 0f);
			animator.SetFloat("NormalizedSpeed", 0f);
			animator.SetBool("IsAttacking", value: false);
			animator.SetBool("IsShooting", value: false);
			animator.SetBool("IsDashing", value: false);
			animator.SetFloat("MoveX", 0f);
			animator.SetFloat("MoveZ", 0f);
		}
	}

	private void ResumeAIBehavior()
	{
		if (currentState == AIState.Idle)
		{
			if (player != null && Vector3.Distance(transform.position, player.position) <= detectionRange)
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
		if ((currentState == AIState.Idle || currentState == AIState.Chasing) && attackCoroutine == null && shootCoroutine == null && hypnosisCoroutine == null && !hypnosisEnding && !isDead)
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
		if (loopingAudioSource != null && idleMovingSound != null && (loopingAudioSource.clip != idleMovingSound || !loopingAudioSource.isPlaying))
		{
			loopingAudioSource.clip = idleMovingSound;
			loopingAudioSource.Play();
		}
	}

	private void StopIdleMovingSound()
	{
		if (loopingAudioSource != null && loopingAudioSource.isPlaying && loopingAudioSource.clip == idleMovingSound)
		{
			loopingAudioSource.Stop();
		}
	}

	private void StartHypnosisLoopSound()
	{
		if (loopingAudioSource != null && hypnosisLoopSound != null && (loopingAudioSource.clip != hypnosisLoopSound || !loopingAudioSource.isPlaying))
		{
			loopingAudioSource.clip = hypnosisLoopSound;
			loopingAudioSource.Play();
		}
	}

	private void StopHypnosisLoopSound()
	{
		if (loopingAudioSource != null && loopingAudioSource.isPlaying && loopingAudioSource.clip == hypnosisLoopSound)
		{
			loopingAudioSource.Stop();
		}
	}

	public void ForceAggro()
	{
		if (!isDead && currentState == AIState.Idle)
		{
			TransitionToChasing();
		}
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.red;
		Gizmos.DrawWireSphere(transform.position, loseTargetRange);
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireSphere(transform.position, detectionRange);
		Gizmos.color = Color.cyan;
		Gizmos.DrawWireSphere(transform.position, shootRange);
		if (canHypnotise)
		{
			Gizmos.color = new Color(0.6f, 0f, 1f);
			Gizmos.DrawWireSphere(transform.position, hypnosisStartRange);
		}
		Gizmos.color = Color.red;
		Gizmos.DrawWireSphere(transform.position, attackRange);
		if (canHypnotise)
		{
			Gizmos.color = Color.magenta;
			Gizmos.DrawWireSphere(transform.position, grabRange);
		}
		if (Application.isPlaying && maxHealth > 0)
		{
			Vector3 vector = transform.position + Vector3.up * 2.5f;
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
}
