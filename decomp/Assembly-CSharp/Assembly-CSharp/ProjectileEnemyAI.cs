using System;
using System.Collections;
using System.Collections.Generic;
using DunGen;
using Pathfinding;
using Pathfinding.ECS;
using PixelCrushers.GridController;
using UnityEngine;

public class ProjectileEnemyAI : MonoBehaviour, IDamageable, IGrabHideable
{
	public enum AIState
	{
		Idle,
		Chasing,
		Retreating,
		Shooting,
		Dead
	}

	[Header("Health")]
	public int maxHealth = 30;

	public int currentHealth;

	[Header("AI Behavior")]
	public float shootingRange = 8f;

	public float minRange = 4f;

	public float detectionRange = 12f;

	public float loseTargetRange = 18f;

	[Header("Panic Teleport")]
	public string retreatTriggerName = "Retreat";

	[Tooltip("Safety net: if the animation event never fires, give up after this long and resume chasing.")]
	public float teleportSafetyTimeout = 2f;

	[Tooltip("Reject A* nodes whose Y is more than this much above the enemy's current Y — prevents teleporting onto ledges or upper floors.")]
	public float maxTeleportYDelta = 0.5f;

	[Header("Projectile Attack")]
	public GameObject projectilePrefab;

	public Transform projectileSpawnPoint;

	public int projectileDamage = 15;

	public float projectileSpeed = 10f;

	public float shootCooldown = 2f;

	public float projectileLifetime = 5f;

	[Tooltip("Number of projectiles fired per ranged attack.")]
	public int projectileCount = 1;

	[Tooltip("Total spread angle (degrees) the projectiles are fanned across. 0 = single straight shot.")]
	public float projectileSpread;

	public LayerMask lineOfSightLayers = -1;

	[Header("Grab Projectile Attack")]
	public bool canGrab = true;

	public GameObject grabProjectilePrefab;

	public float grabRange = 10f;

	public float grabCooldown = 8f;

	public float grabProjectileSpeed = 8f;

	public float grabProjectileLifetime = 5f;

	[Header("Grab Behavior")]
	[Tooltip("If true, this enemy is hidden (SetActive false) instead of destroyed when it grabs the player. It reappears in Idle if the player escapes the grab; if killed during the grab, it dies normally at EndGrab. Arena clearance still treats a hidden enemy as alive.")]
	public bool hideInsteadOfDestroyOnGrab;

	[Tooltip("If true, damage dealt during the grab screen does NOT carry over to the enemy's actual health. The grab still ends when the player drops the grab-screen HP bar to zero (the QTE still resolves), but the enemy reappears at the health it had before the grab. Only effective when hideInsteadOfDestroyOnGrab is also on — without hiding the enemy is destroyed at grab-start regardless.")]
	public bool preserveHealthDuringGrab;

	[Header("Grab Animation Settings")]
	public RuntimeAnimatorController grabAnimatorController;

	[Header("Grab Camera Settings")]
	public Vector3 grabCameraPositionOffset = Vector3.zero;

	public Vector3 grabCameraRotationOffset = Vector3.zero;

	[Header("Grab Screen Audio")]
	[Tooltip("Per-animation sounds for the grab screen. animationName must match the Animator state name.")]
	public GrabAnimationData[] grabScreenAnimations;

	[Header("A* Pathfinding")]
	public FollowerEntity followerEntity;

	[SerializeField]
	private bool autoFindPathfindingComponents = true;

	[Header("Audio")]
	public AudioClip hurtSound;

	public AudioClip shootSound;

	public AudioClip deathSound;

	public AudioClip idleMovingSound;

	public AudioClip grabSound;

	[Header("Effects")]
	public GameObject deathEffect;

	public GameObject hitEffect;

	public GameObject muzzleFlashEffect;

	[Header("Gallery")]
	[Tooltip("Unique ID for gallery unlock tracking. Should match EnemyGalleryEntry.enemyID")]
	public string galleryEnemyID;

	[Header("Health Bar")]
	[SerializeField]
	private EnemyHealthBar healthBar;

	[Header("References")]
	public Transform player;

	public Animator animator;

	public GrabScreen grabScreen;

	[Header("Coin Drop")]
	[SerializeField]
	private GameObject currencyPrefab;

	[Range(0f, 1f)]
	public float coinDropChance = 0.01f;

	public int minCoinDropValue = 1;

	public int maxCoinDropValue = 1;

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

	private float lastShootTime;

	private float lastGrabTime;

	private bool isDead;

	private bool hasSpawnedProjectileThisAttack;

	private bool hasSpawnedGrabProjectileThisAttack;

	private bool isAttemptingGrab;

	private bool wasPlayerGrabbedLastFrame;

	private AudioSource audioSource;

	[SerializeField]
	private AudioSource loopingAudioSource;

	private PlayerStats playerStats;

	private AIState currentState;

	private Coroutine shootCoroutine;

	private Coroutine flashCoroutine;

	private Dungeon cachedDungeon;

	private EnemySpawner[] cachedSpawners;

	private ArenaEnemySpawner[] cachedArenas;

	private Vector3 pendingTeleportTarget;

	private bool hasPendingTeleport;

	private float retreatStartTime;

	private SpriteRenderer[] cachedSprites;

	private Color[] originalSpriteColors;

	private Renderer[] cachedRenderers;

	private Color[] originalRendererColors;

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
			return Vector3.Distance(transform.position, player.position);
		}
	}

	private void Start()
	{
		InitializeComponents();
		SetupPathfinding();
		ConfigureCollider();
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
		UpdateAnimator();
		UpdateIdleMovingSound();
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
				playerStats = player.GetComponent<PlayerStats>();
			}
		}
		audioSource = GetComponent<AudioSource>();
		if (audioSource == null)
		{
			audioSource = base.gameObject.AddComponent<AudioSource>();
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
		if ((UnityEngine.Object)(object)followerEntity == null && projectileSpawnPoint == null)
		{
			GameObject gameObject2 = new GameObject("ProjectileSpawnPoint");
			gameObject2.transform.SetParent(transform);
			gameObject2.transform.localPosition = Vector3.forward + Vector3.up;
			projectileSpawnPoint = gameObject2.transform;
		}
		if (grabScreen == null && canGrab)
		{
			grabScreen = GrabScreen.Instance;
			if (grabScreen == null)
			{
				canGrab = false;
			}
		}
	}

	private void SetupPathfinding()
	{
		if (!((UnityEngine.Object)(object)followerEntity == null))
		{
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
			case AIState.Retreating:
				HandleRetreatingState(distanceToPlayer);
				break;
			case AIState.Shooting:
				HandleShootingState(distanceToPlayer);
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
		}
		else if (!HasLineOfSight())
		{
			if ((UnityEngine.Object)(object)followerEntity != null)
			{
				followerEntity.destination = player.position;
			}
		}
		else if (distanceToPlayer < minRange)
		{
			TransitionToRetreating();
		}
		else if (distanceToPlayer <= grabRange && CanGrabShoot())
		{
			TransitionToShooting();
		}
		else if (!IsPlayerVulnerable && distanceToPlayer <= shootingRange)
		{
			if (CanShoot())
			{
				TransitionToShooting();
			}
			else if ((UnityEngine.Object)(object)followerEntity != null)
			{
				followerEntity.destination = player.position;
			}
		}
		else if ((UnityEngine.Object)(object)followerEntity != null)
		{
			followerEntity.destination = player.position;
		}
	}

	private void HandleRetreatingState(float distanceToPlayer)
	{
		if (Time.time - retreatStartTime > teleportSafetyTimeout)
		{
			hasPendingTeleport = false;
			TransitionToChasing();
		}
	}

	private void HandleShootingState(float distanceToPlayer)
	{
		if (shootCoroutine != null)
		{
			return;
		}
		if (distanceToPlayer < minRange)
		{
			TransitionToRetreating();
		}
		else if (distanceToPlayer <= grabRange && CanGrabShoot())
		{
			StartGrabShooting();
		}
		else if (!IsPlayerVulnerable && distanceToPlayer <= shootingRange && CanShoot())
		{
			StartShooting();
		}
		else if (distanceToPlayer > Mathf.Max(grabRange, shootingRange))
		{
			TransitionToChasing();
		}
		else if (!HasLineOfSight())
		{
			if ((UnityEngine.Object)(object)followerEntity != null)
			{
				followerEntity.destination = player.position;
			}
		}
		else if ((UnityEngine.Object)(object)followerEntity != null)
		{
			followerEntity.destination = player.position;
		}
	}

	private void TransitionToIdle()
	{
		currentState = AIState.Idle;
		StopPathfinding();
		StopShooting();
		hasPendingTeleport = false;
	}

	private void TransitionToChasing()
	{
		currentState = AIState.Chasing;
		StartPathfinding();
		StopShooting();
		hasPendingTeleport = false;
	}

	private void TransitionToRetreating()
	{
		currentState = AIState.Retreating;
		StopPathfinding();
		StopShooting();
		retreatStartTime = Time.time;
		hasPendingTeleport = TryFindFleeNodeOnAstar(out pendingTeleportTarget);
		if (!hasPendingTeleport)
		{
			hasPendingTeleport = TryFindPlayerRoomSpawn(out pendingTeleportTarget);
		}
		if (animator != null && !string.IsNullOrEmpty(retreatTriggerName))
		{
			animator.ResetTrigger(retreatTriggerName);
			animator.SetTrigger(retreatTriggerName);
		}
	}

	private void TransitionToShooting()
	{
		currentState = AIState.Shooting;
		hasPendingTeleport = false;
	}

	private void TransitionToDead()
	{
		currentState = AIState.Dead;
		StopPathfinding();
		StopShooting();
		hasPendingTeleport = false;
	}

	private void ChasePlayer()
	{
		if (!((UnityEngine.Object)(object)followerEntity == null) && !(player == null))
		{
			followerEntity.simulateMovement = true;
			Pathfinding.ECS.AutoRepathPolicy autoRepath = followerEntity.autoRepath;
			autoRepath.mode = Pathfinding.AutoRepathPolicy.Mode.EveryNSeconds;
			followerEntity.autoRepath = autoRepath;
			followerEntity.destination = player.position;
			FacePlayer();
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

	private void EnsureRoomLookupCache()
	{
		if (cachedDungeon == null)
		{
			DunGenAStarIntegration dunGenAStarIntegration = UnityEngine.Object.FindAnyObjectByType<DunGenAStarIntegration>();
			if (dunGenAStarIntegration != null)
			{
				cachedDungeon = dunGenAStarIntegration.GetCurrentDungeon();
			}
		}
		if (cachedSpawners == null || cachedSpawners.Length == 0)
		{
			cachedSpawners = UnityEngine.Object.FindObjectsByType<EnemySpawner>(FindObjectsSortMode.None);
		}
		if (cachedArenas == null || cachedArenas.Length == 0)
		{
			cachedArenas = UnityEngine.Object.FindObjectsByType<ArenaEnemySpawner>(FindObjectsSortMode.None);
		}
	}

	private bool IsInPlayerRoom(Vector3 pos)
	{
		if (player == null)
		{
			return true;
		}
		EnsureRoomLookupCache();
		Vector3 position = player.position;
		if (cachedArenas != null)
		{
			ArenaEnemySpawner[] array = cachedArenas;
			foreach (ArenaEnemySpawner arenaEnemySpawner in array)
			{
				if (!(arenaEnemySpawner == null) && arenaEnemySpawner.ContainsPoint(position))
				{
					return arenaEnemySpawner.ContainsPoint(pos);
				}
			}
		}
		if (cachedDungeon != null)
		{
			foreach (Tile allTile in cachedDungeon.AllTiles)
			{
				if (!(allTile == null) && allTile.Bounds.Contains(position))
				{
					return allTile.Bounds.Contains(pos);
				}
			}
		}
		return true;
	}

	private bool TryFindFleeNodeOnAstar(out Vector3 destination)
	{
		destination = default;
		if (player == null || (UnityEngine.Object)(object)AstarPath.active == null)
		{
			return false;
		}
		Vector3 vector = transform.position - player.position;
		vector.y = 0f;
		if (vector.sqrMagnitude < 0.0001f)
		{
			vector = transform.forward;
		}
		vector.Normalize();
		Quaternion quaternion = Quaternion.AngleAxis(-35f, Vector3.up);
		Quaternion quaternion2 = Quaternion.AngleAxis(35f, Vector3.up);
		Vector3[] array = new Vector3[3]
		{
			vector,
			quaternion * vector,
			quaternion2 * vector
		};
		float num = shootingRange * 0.9f;
		float num2 = shootingRange * 0.85f * (shootingRange * 0.85f);
		float y = transform.position.y;
		Vector3 position = player.position;
		position.y = 0f;
		bool result = false;
		float num3 = -1f;
		for (int i = 0; i < array.Length; i++)
		{
			Vector3 position2 = player.position + array[i] * num;
			position2.y = y;
			NNInfo nearest = AstarPath.active.GetNearest(position2, NNConstraint.Walkable);
			if (nearest.node == null)
			{
				continue;
			}
			Vector3 position3 = nearest.position;
			if (!(position3.y > y + maxTeleportYDelta) && IsInPlayerRoom(position3))
			{
				Vector3 vector2 = position3;
				vector2.y = 0f;
				float sqrMagnitude = (vector2 - position).sqrMagnitude;
				if (!(sqrMagnitude < num2) && sqrMagnitude > num3)
				{
					num3 = sqrMagnitude;
					destination = position3;
					result = true;
				}
			}
		}
		return result;
	}

	private bool TryFindPlayerRoomSpawn(out Vector3 destination)
	{
		destination = default;
		if (player == null)
		{
			return false;
		}
		EnsureRoomLookupCache();
		Vector3 position = player.position;
		if (cachedArenas != null)
		{
			ArenaEnemySpawner[] array = cachedArenas;
			foreach (ArenaEnemySpawner arenaEnemySpawner in array)
			{
				if (arenaEnemySpawner == null || !arenaEnemySpawner.ContainsPoint(position))
				{
					continue;
				}
				IReadOnlyList<Transform> spawnPositions = arenaEnemySpawner.SpawnPositions;
				if (spawnPositions == null)
				{
					return false;
				}
				float num = -1f;
				bool result = false;
				for (int j = 0; j < spawnPositions.Count; j++)
				{
					Transform transform = spawnPositions[j];
					if (!(transform == null))
					{
						Vector3 position2 = transform.position;
						float sqrMagnitude = (position2 - position).sqrMagnitude;
						if (sqrMagnitude > num)
						{
							num = sqrMagnitude;
							destination = position2;
							result = true;
						}
					}
				}
				return result;
			}
		}
		if (cachedDungeon == null)
		{
			return false;
		}
		Tile tile = null;
		foreach (Tile allTile in cachedDungeon.AllTiles)
		{
			if (!(allTile == null) && allTile.Bounds.Contains(position))
			{
				tile = allTile;
				break;
			}
		}
		if (tile == null)
		{
			return false;
		}
		Bounds bounds = tile.Bounds;
		float num2 = -1f;
		bool result2 = false;
		EnemySpawner[] array2 = cachedSpawners;
		foreach (EnemySpawner enemySpawner in array2)
		{
			if (enemySpawner == null || enemySpawner.SpawnPositions == null)
			{
				continue;
			}
			for (int k = 0; k < enemySpawner.SpawnPositions.Count; k++)
			{
				Transform transform2 = enemySpawner.SpawnPositions[k];
				if (transform2 == null)
				{
					continue;
				}
				Vector3 position3 = transform2.position;
				if (bounds.Contains(position3))
				{
					float sqrMagnitude2 = (position3 - position).sqrMagnitude;
					if (sqrMagnitude2 > num2)
					{
						num2 = sqrMagnitude2;
						destination = position3;
						result2 = true;
					}
				}
			}
		}
		return result2;
	}

	public void OnTeleportAnimationEvent()
	{
		if (isDead || currentState != AIState.Retreating)
		{
			return;
		}
		if (!hasPendingTeleport)
		{
			TransitionToChasing();
			return;
		}
		if ((UnityEngine.Object)(object)followerEntity != null)
		{
			followerEntity.Teleport(pendingTeleportTarget);
		}
		else
		{
			transform.position = pendingTeleportTarget;
		}
		hasPendingTeleport = false;
		TransitionToChasing();
	}

	private void StartPathfinding()
	{
		if (!((UnityEngine.Object)(object)followerEntity == null))
		{
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

	private bool CanShoot()
	{
		if (Time.time >= lastShootTime + shootCooldown && shootCoroutine == null && projectilePrefab != null)
		{
			return HasLineOfSight();
		}
		return false;
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
		float magnitude = vector3.magnitude;
		if (Physics.Raycast(vector2, vector3.normalized, out var hitInfo, magnitude, lineOfSightLayers))
		{
			if (hitInfo.collider.CompareTag("Player"))
			{
				if (!IsClearOfObstacles())
				{
					return false;
				}
				return true;
			}
			return false;
		}
		if (!IsClearOfObstacles())
		{
			return false;
		}
		return true;
	}

	private bool IsClearOfObstacles()
	{
		if (player == null)
		{
			return true;
		}
		Vector3 origin = transform.position + Vector3.up * 1f;
		Vector3 normalized = (player.position - transform.position).normalized;
		float maxDistance = 0.75f;
		Vector3 direction = -normalized;
		if (Physics.Raycast(origin, direction, out var hitInfo, maxDistance, lineOfSightLayers) && !hitInfo.collider.CompareTag("Player") && !hitInfo.collider.CompareTag("Enemy"))
		{
			return false;
		}
		return true;
	}

	private bool CanGrabShoot()
	{
		if (canGrab && Time.time >= lastGrabTime + grabCooldown && shootCoroutine == null && grabProjectilePrefab != null && grabScreen != null)
		{
			return HasLineOfSight();
		}
		return false;
	}

	private void StartGrabShooting()
	{
		if (shootCoroutine == null)
		{
			lastGrabTime = Time.time;
			shootCoroutine = StartCoroutine(GrabShootSequence());
		}
	}

	private IEnumerator GrabShootSequence()
	{
		hasSpawnedGrabProjectileThisAttack = false;
		isAttemptingGrab = true;
		if ((UnityEngine.Object)(object)followerEntity != null)
		{
			followerEntity.simulateMovement = false;
			followerEntity.velocity = Vector3.zero;
		}
		FacePlayer();
		if (animator != null)
		{
			animator.SetTrigger("Grab");
		}
		yield return new WaitForSeconds(grabCooldown * 0.3f);
		isAttemptingGrab = false;
		shootCoroutine = null;
		float num = Vector3.Distance(transform.position, player.position);
		if (player != null && num <= loseTargetRange)
		{
			TransitionToChasing();
		}
		else
		{
			TransitionToIdle();
		}
	}

	private void StartShooting()
	{
		if (shootCoroutine == null)
		{
			lastShootTime = Time.time;
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
		isAttemptingGrab = false;
	}

	private IEnumerator ShootSequence()
	{
		hasSpawnedProjectileThisAttack = false;
		if ((UnityEngine.Object)(object)followerEntity != null)
		{
			followerEntity.simulateMovement = false;
			followerEntity.velocity = Vector3.zero;
		}
		FacePlayer();
		if (animator != null)
		{
			animator.SetTrigger("Attack");
		}
		yield return new WaitForSeconds(shootCooldown);
		shootCoroutine = null;
		float num = Vector3.Distance(transform.position, player.position);
		if (player != null && num <= loseTargetRange)
		{
			TransitionToChasing();
		}
		else
		{
			TransitionToIdle();
		}
	}

	public void SpawnProjectile()
	{
		if (!hasSpawnedProjectileThisAttack && !(projectilePrefab == null) && !(projectileSpawnPoint == null) && !(player == null))
		{
			hasSpawnedProjectileThisAttack = true;
			Vector3 normalized = (player.position + Vector3.up * 1.7f - projectileSpawnPoint.position).normalized;
			EnemyProjectile.FireSpread(projectilePrefab, projectileSpawnPoint.position, normalized, projectileSpeed, projectileDamage, projectileLifetime, projectileCount, projectileSpread);
		}
	}

	public void SpawnGrabProjectile()
	{
		if (!hasSpawnedGrabProjectileThisAttack && !(grabProjectilePrefab == null) && !(projectileSpawnPoint == null) && !(player == null))
		{
			hasSpawnedGrabProjectileThisAttack = true;
			Vector3 normalized = (player.position + Vector3.up * 1.7f - projectileSpawnPoint.position).normalized;
			GameObject gameObject = UnityEngine.Object.Instantiate(grabProjectilePrefab, projectileSpawnPoint.position, Quaternion.LookRotation(normalized));
			GrabProjectile grabProjectile = gameObject.GetComponent<GrabProjectile>();
			if (grabProjectile == null)
			{
				grabProjectile = gameObject.AddComponent<GrabProjectile>();
			}
			grabProjectile.Initialize(normalized, grabProjectileSpeed, grabProjectileLifetime, this);
		}
	}

	public void OnGrabProjectileHitPlayer()
	{
		if (grabScreen == null)
		{
			grabScreen = GrabScreen.Instance;
			if (grabScreen == null)
			{
				return;
			}
		}
		if (grabScreen.IsGrabbed)
		{
			return;
		}
		grabScreen.StartGrab(gameObject, grabAnimatorController, grabCameraPositionOffset, grabCameraRotationOffset);
		if (grabScreen.IsGrabbed)
		{
			if (shootCoroutine != null)
			{
				StopCoroutine(shootCoroutine);
				shootCoroutine = null;
			}
			RemoveEnemyAfterGrab();
		}
	}

	private void RemoveEnemyAfterGrab()
	{
		StopAllCoroutines();
		shootCoroutine = null;
		StopIdleMovingSound();
		if (hideInsteadOfDestroyOnGrab)
		{
			ResetGrabTransientState();
			currentState = AIState.Idle;
			hasPendingTeleport = false;
			if (GrabScreen.Instance != null)
			{
				GrabScreen.Instance.RegisterHiddenEnemy(gameObject);
			}
			gameObject.SetActive(value: false);
		}
		else
		{
			TransitionToDead();
			if (deathEffect != null)
			{
				UnityEngine.Object.Instantiate(deathEffect, transform.position, Quaternion.identity);
			}
			PlaySound(deathSound);
			UnityEngine.Object.Destroy(gameObject);
		}
	}

	private void ResetGrabTransientState()
	{
		hasSpawnedProjectileThisAttack = false;
		hasSpawnedGrabProjectileThisAttack = false;
		isAttemptingGrab = false;
		wasPlayerGrabbedLastFrame = false;
	}

	public void EndGrabHidden(float remainingHealth)
	{
		gameObject.SetActive(value: true);
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
				UnityEngine.Object.Instantiate(deathEffect, transform.position, Quaternion.identity);
			}
			PlaySound(deathSound);
			UnityEngine.Object.Destroy(gameObject);
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

	public void ResetProjectileFlag()
	{
		hasSpawnedProjectileThisAttack = false;
	}

	public void ResetGrabProjectileFlag()
	{
		hasSpawnedGrabProjectileThisAttack = false;
	}

	public void StopMovementForShoot()
	{
		StopIdleMovingSound();
		if ((UnityEngine.Object)(object)followerEntity != null)
		{
			followerEntity.simulateMovement = false;
			followerEntity.velocity = Vector3.zero;
		}
	}

	public void PlayShootSound()
	{
		PlaySound(shootSound);
	}

	public void PlayGrabSound()
	{
		PlaySound(grabSound);
	}

	public void SpawnMuzzleFlash()
	{
		if (muzzleFlashEffect != null && projectileSpawnPoint != null)
		{
			GameObject instance = ((VFXPoolManager.Instance != null) ? VFXPoolManager.Instance.Get(muzzleFlashEffect, projectileSpawnPoint.position, projectileSpawnPoint.rotation) : UnityEngine.Object.Instantiate(muzzleFlashEffect, projectileSpawnPoint.position, projectileSpawnPoint.rotation));
			if (VFXPoolManager.Instance != null)
			{
				VFXPoolManager.Instance.ReturnAfterDelay(instance, 2f);
			}
		}
	}

	private void UpdateAnimator()
	{
		if (!(animator == null) && !((UnityEngine.Object)(object)followerEntity == null))
		{
			bool flag = (currentState == AIState.Chasing || currentState == AIState.Retreating) && followerEntity.simulateMovement;
			float num = (flag ? followerEntity.velocity.magnitude : 0f);
			animator.SetBool("IsMoving", flag);
			animator.SetFloat("Speed", num);
			if (followerEntity.maxSpeed > 0f)
			{
				animator.SetFloat("NormalizedSpeed", Mathf.Clamp01(num / followerEntity.maxSpeed));
			}
			animator.SetBool("IsAttacking", shootCoroutine != null);
			animator.SetBool("IsGrabbing", isAttemptingGrab);
			animator.SetBool("IsRetreating", currentState == AIState.Retreating);
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
		if (!isDead)
		{
			currentHealth -= damage;
			currentHealth = Mathf.Max(0, currentHealth);
			if (healthBar != null)
			{
				healthBar.UpdateHealth(HealthPercentage);
			}
			PlaySound(hurtSound);
			if (hitEffect != null)
			{
				UnityEngine.Object.Instantiate(hitEffect, transform.position + Vector3.up, Quaternion.identity);
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

	private void PlaySound(AudioClip clip)
	{
		if (audioSource != null && clip != null)
		{
			audioSource.PlayOneShot(clip);
		}
	}

	private void UpdateIdleMovingSound()
	{
		if ((currentState == AIState.Idle || currentState == AIState.Chasing || currentState == AIState.Retreating) && shootCoroutine == null && !isDead)
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
		if (loopingAudioSource != null && idleMovingSound != null && !loopingAudioSource.isPlaying)
		{
			loopingAudioSource.clip = idleMovingSound;
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

	public void ForceAggro()
	{
		if (!isDead && currentState == AIState.Idle)
		{
			TransitionToChasing();
		}
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireSphere(transform.position, detectionRange);
		Gizmos.color = Color.red;
		Gizmos.DrawWireSphere(transform.position, loseTargetRange);
		Gizmos.color = Color.blue;
		Gizmos.DrawWireSphere(transform.position, shootingRange);
		Gizmos.color = Color.cyan;
		Gizmos.DrawWireSphere(transform.position, minRange);
		if (canGrab)
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
		if (!Application.isPlaying || !(player != null))
		{
			return;
		}
		Vector3 vector2 = player.position + Vector3.up * 1f;
		Vector3 vector3 = transform.position + Vector3.up * 1f;
		Vector3 vector4 = vector2 - vector3;
		float magnitude = vector4.magnitude;
		if (Physics.Raycast(vector3, vector4.normalized, out var hitInfo, magnitude, lineOfSightLayers))
		{
			if (hitInfo.collider.CompareTag("Player"))
			{
				Gizmos.color = Color.green;
				Gizmos.DrawLine(vector3, hitInfo.point);
				Gizmos.DrawWireSphere(hitInfo.point, 0.2f);
			}
			else
			{
				Gizmos.color = Color.red;
				Gizmos.DrawLine(vector3, hitInfo.point);
				Gizmos.DrawWireSphere(hitInfo.point, 0.3f);
				Gizmos.color = Color.yellow;
				DrawDashedLine(hitInfo.point, vector2, 0.2f);
			}
		}
		else
		{
			Gizmos.color = Color.green;
			Gizmos.DrawLine(vector3, vector2);
			Gizmos.DrawWireSphere(vector2, 0.2f);
		}
	}

	private void DrawDashedLine(Vector3 start, Vector3 end, float dashLength)
	{
		Vector3 normalized = (end - start).normalized;
		float num = Vector3.Distance(start, end);
		int num2 = Mathf.CeilToInt(num / (dashLength * 2f));
		for (int i = 0; i < num2; i++)
		{
			float num3 = (float)i * dashLength * 2f;
			float num4 = Mathf.Min(num3 + dashLength, num);
			if (num3 < num)
			{
				Vector3 vector = start + normalized * num3;
				Vector3 to = start + normalized * num4;
				Gizmos.DrawLine(vector, to);
			}
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
		StopShooting();
		StopIdleMovingSound();
		if (animator != null)
		{
			animator.SetBool("IsMoving", value: false);
			animator.SetFloat("Speed", 0f);
			animator.SetFloat("NormalizedSpeed", 0f);
			animator.SetBool("IsAttacking", value: false);
			animator.SetBool("IsGrabbing", value: false);
			animator.SetBool("IsRetreating", value: false);
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
}
