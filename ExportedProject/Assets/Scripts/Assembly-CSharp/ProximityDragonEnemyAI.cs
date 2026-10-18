using System.Collections;
using System.Collections.Generic;
using Pathfinding;
using Pathfinding.ECS;
using PixelCrushers.GridController;
using UnityEngine;
using UnityEngine.UI;

public class ProximityDragonEnemyAI : MonoBehaviour, IDamageable
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
	public int maxHealth = 150;

	public int currentHealth;

	[Header("AI Behavior")]
	public float attackRange = 3f;

	public float detectionRange = float.MaxValue;

	public float loseTargetRange = float.MaxValue;

	[Header("Attack")]
	public int attackDamage = 20;

	public float attackCooldown = 2f;

	[Header("Dash")]
	public float dashDistance = 3f;

	public float dashSpeed = 10f;

	public float dashDuration = 0.4f;

	[Header("Grab Attack")]
	public bool canGrab = true;

	public float grabRange = 3.5f;

	public float grabCooldown = 8f;

	public float grabAttemptDuration = 1.5f;

	[SerializeField]
	private LayerMask hitboxMask = -1;

	[Header("Grab Animation Settings")]
	public RuntimeAnimatorController grabAnimatorController;

	[Header("Grab Camera Settings")]
	public Vector3 grabCameraPositionOffset = Vector3.zero;

	public Vector3 grabCameraRotationOffset = Vector3.zero;

	[Header("Grab Screen Audio")]
	[Tooltip("Per-animation sounds for the grab screen. animationName must match the Animator state name.")]
	public GrabAnimationData[] grabScreenAnimations;

	[Header("Dragon Specific Settings")]
	[SerializeField]
	private float dragonGrabDuration = 10f;

	[SerializeField]
	private bool showDragonGrabMessage = true;

	[Header("Proximity Fade Settings")]
	[SerializeField]
	private float minFadeDistance = 2f;

	[SerializeField]
	private float maxFadeDistance = 15f;

	[SerializeField]
	private float minFadeAmount;

	[SerializeField]
	private float maxFadeAmount = 1f;

	[Header("Physics")]
	public float enemyMass = 5f;

	public float enemyDrag = 8f;

	[Header("A* Pathfinding")]
	public FollowerEntity followerEntity;

	[SerializeField]
	private bool autoFindPathfindingComponents = true;

	[Header("Audio")]
	public AudioClip hurtSound;

	public AudioClip attackSound;

	public AudioClip deathSound;

	public AudioClip idleMovingSound;

	public AudioClip grabSound;

	[Header("Effects")]
	public GameObject deathEffect;

	public GameObject hitEffect;

	[Header("Gallery")]
	[Tooltip("Unique ID for gallery unlock tracking. Should match EnemyGalleryEntry.enemyID")]
	public string galleryEnemyID;

	[Header("References")]
	public Transform player;

	public Animator animator;

	public GrabScreen grabScreen;

	[Header("Detection Colliders")]
	public SphereCollider attackDetectionCollider;

	public SphereCollider grabDetectionCollider;

	[HideInInspector]
	public bool frozenByArena;

	public static readonly List<ProximityDragonEnemyAI> ActiveDragons = new List<ProximityDragonEnemyAI>();

	private float lastAttackTime;

	private float lastGrabTime;

	private bool isDead;

	private bool hasDealtDamageThisAttack;

	private bool isAttemptingGrab;

	private bool hasGrabbedThisAttempt;

	private bool isDashing;

	private bool grabInDamagingState;

	private bool hasTriggeredDragonGrab;

	private bool wasPlayerGrabbedLastFrame;

	private AudioSource audioSource;

	[SerializeField]
	private AudioSource loopingAudioSource;

	private DualWieldingSystem playerCombatSystem;

	private Rigidbody rb;

	private PlayerStats cachedPlayerStats;

	private GameOverScreen cachedGameOverScreen;

	private SpriteRenderer[] spriteRenderers;

	private Material[] spriteMaterials;

	private AIState currentState;

	private Coroutine attackCoroutine;

	private Coroutine grabCoroutine;

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

	private void OnEnable()
	{
		ActiveDragons.Add(this);
	}

	private void OnDisable()
	{
		ActiveDragons.Remove(this);
	}

	private void Awake()
	{
		if ((Object)(object)followerEntity == null)
		{
			followerEntity = GetComponent<FollowerEntity>();
		}
		if ((Object)(object)followerEntity != null)
		{
			followerEntity.updatePosition = false;
		}
	}

	private void Start()
	{
		InitializeComponents();
		SetupPathfinding();
		ConfigureCollider();
		SetupDetectionColliders();
		InitializeProximityFade();
	}

	private void Update()
	{
		if (isDead || player == null)
		{
			return;
		}
		if (frozenByArena)
		{
			PauseAIBehavior();
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
		HandleGrabHit();
		UpdateAnimator();
		UpdateProximityFade();
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
				playerCombatSystem = DualWieldingSystem.Instance;
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
		cachedPlayerStats = ((player != null) ? player.GetComponent<PlayerStats>() : null);
		cachedGameOverScreen = GameOverScreen.Instance;
		audioSource = GetComponent<AudioSource>();
		if (audioSource == null)
		{
			audioSource = base.gameObject.AddComponent<AudioSource>();
		}
		if (loopingAudioSource == null)
		{
			loopingAudioSource = base.gameObject.AddComponent<AudioSource>();
		}
		loopingAudioSource.loop = true;
		loopingAudioSource.playOnAwake = false;
		if (autoFindPathfindingComponents && (Object)(object)followerEntity == null)
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
		if (!((Object)(object)followerEntity == null))
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
			capsuleCollider.height = 3f;
			capsuleCollider.radius = 1f;
			capsuleCollider.center = Vector3.up * 1.5f;
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
			ProximityDragonAttackDetector proximityDragonAttackDetector = attackDetectionCollider.GetComponent<ProximityDragonAttackDetector>();
			if (proximityDragonAttackDetector == null)
			{
				proximityDragonAttackDetector = attackDetectionCollider.gameObject.AddComponent<ProximityDragonAttackDetector>();
			}
			proximityDragonAttackDetector.Initialize(this);
		}
		if (grabDetectionCollider != null)
		{
			grabDetectionCollider.isTrigger = true;
			grabDetectionCollider.enabled = false;
			ProximityDragonGrabDetector proximityDragonGrabDetector = grabDetectionCollider.GetComponent<ProximityDragonGrabDetector>();
			if (proximityDragonGrabDetector == null)
			{
				proximityDragonGrabDetector = grabDetectionCollider.gameObject.AddComponent<ProximityDragonGrabDetector>();
			}
			proximityDragonGrabDetector.Initialize(this);
		}
	}

	private void InitializeProximityFade()
	{
		spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
		spriteMaterials = new Material[spriteRenderers.Length];
		for (int i = 0; i < spriteRenderers.Length; i++)
		{
			if (spriteRenderers[i] != null)
			{
				spriteMaterials[i] = spriteRenderers[i].material;
				if (spriteMaterials[i] != null)
				{
					spriteMaterials[i].SetFloat("_FadeAmount", minFadeAmount);
				}
			}
		}
	}

	private void UpdateProximityFade()
	{
		if (player == null || spriteMaterials == null || spriteMaterials.Length == 0)
		{
			return;
		}
		float num = Vector3.Distance(base.transform.position, player.position);
		float value;
		if (num <= minFadeDistance)
		{
			value = minFadeAmount;
		}
		else if (num >= maxFadeDistance)
		{
			value = maxFadeAmount;
		}
		else
		{
			float t = (num - minFadeDistance) / (maxFadeDistance - minFadeDistance);
			value = Mathf.Lerp(minFadeAmount, maxFadeAmount, t);
		}
		for (int i = 0; i < spriteMaterials.Length; i++)
		{
			if (spriteMaterials[i] != null)
			{
				spriteMaterials[i].SetFloat("_FadeAmount", value);
			}
		}
	}

	private void FixedUpdate()
	{
		if (isDead || rb == null || (Object)(object)followerEntity == null)
		{
			return;
		}
		if (!Physics.Raycast(base.transform.position + Vector3.up * 0.5f, Vector3.down, out var hitInfo, 1.5f))
		{
			followerEntity.position = base.transform.position;
			return;
		}
		followerEntity.position = base.transform.position;
		if (isDashing)
		{
			return;
		}
		if (currentState == AIState.Chasing && followerEntity.simulateMovement)
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
		else
		{
			rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
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

	private void HandleChasingState(float distanceToPlayer)
	{
		if (canGrab && distanceToPlayer <= grabRange && CanGrab())
		{
			TransitionToGrabbing();
		}
		else if (distanceToPlayer <= attackRange)
		{
			TransitionToAttacking();
		}
		else if (distanceToPlayer > loseTargetRange)
		{
			TransitionToIdle();
		}
		else if ((Object)(object)followerEntity != null)
		{
			followerEntity.destination = player.position;
		}
	}

	private void HandleAttackingState(float distanceToPlayer)
	{
		if (distanceToPlayer > attackRange)
		{
			TransitionToChasing();
		}
		else if (CanAttack())
		{
			StartAttack();
		}
	}

	private void HandleGrabbingState(float distanceToPlayer)
	{
		if (distanceToPlayer > grabRange)
		{
			TransitionToChasing();
		}
		else if (grabCoroutine == null && CanGrab())
		{
			StartGrab();
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
		StopPathfinding();
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
		if (!((Object)(object)followerEntity == null))
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
		if (!((Object)(object)followerEntity == null))
		{
			followerEntity.simulateMovement = false;
		}
	}

	private bool CanAttack()
	{
		if (Time.time >= lastAttackTime + attackCooldown)
		{
			return attackCoroutine == null;
		}
		return false;
	}

	private void StartAttack()
	{
		if (attackCoroutine == null)
		{
			lastAttackTime = Time.time;
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
		if (attackDetectionCollider != null)
		{
			attackDetectionCollider.enabled = false;
		}
	}

	private IEnumerator AttackSequence()
	{
		hasDealtDamageThisAttack = false;
		if ((Object)(object)followerEntity != null)
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
			animator.SetTrigger("Attack");
		}
		if (attackDetectionCollider != null)
		{
			attackDetectionCollider.enabled = true;
		}
		PlaySound(attackSound);
		yield return new WaitForSeconds(attackCooldown);
		if (currentState == AIState.Chasing && (Object)(object)followerEntity != null)
		{
			followerEntity.simulateMovement = true;
		}
		if (attackDetectionCollider != null)
		{
			attackDetectionCollider.enabled = false;
		}
		attackCoroutine = null;
	}

	public void OnPlayerAttackDetected()
	{
		if (attackCoroutine != null && !hasDealtDamageThisAttack)
		{
			hasDealtDamageThisAttack = true;
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

	public void ResetDamageFlag()
	{
		hasDealtDamageThisAttack = false;
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
		if ((Object)(object)followerEntity != null)
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
		StopIdleMovingSound();
		PlaySound(grabSound);
		yield return new WaitForSeconds(grabCooldown);
		grabInDamagingState = false;
		isAttemptingGrab = false;
		grabCoroutine = null;
	}

	public void StartGrabDamageState()
	{
		grabInDamagingState = true;
	}

	public void EndGrabDamageState()
	{
		grabInDamagingState = false;
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
		RaycastHit[] array = Physics.RaycastAll(vector, (vector2 - vector).normalized, grabRange, hitboxMask);
		for (int i = 0; i < array.Length; i++)
		{
			if (!(array[i].collider.transform.root == base.transform.root) && array[i].collider.CompareTag("Player"))
			{
				hasGrabbedThisAttempt = true;
				grabInDamagingState = false;
				grabScreen.StartGrab(base.gameObject, grabAnimatorController, grabCameraPositionOffset, grabCameraRotationOffset);
				if (!hasTriggeredDragonGrab)
				{
					hasTriggeredDragonGrab = true;
					ModifyGrabForDragon();
				}
				if (grabCoroutine != null)
				{
					StopCoroutine(grabCoroutine);
					grabCoroutine = null;
				}
				RemoveEnemyAfterGrab();
				break;
			}
		}
	}

	public void OnPlayerGrabDetected()
	{
		if (grabCoroutine != null && !hasGrabbedThisAttempt && !(grabScreen == null) && !grabScreen.IsGrabbed)
		{
			hasGrabbedThisAttempt = true;
			grabScreen.StartGrab(base.gameObject, grabAnimatorController, grabCameraPositionOffset, grabCameraRotationOffset);
			if (!hasTriggeredDragonGrab)
			{
				hasTriggeredDragonGrab = true;
				ModifyGrabForDragon();
			}
			if (grabCoroutine != null)
			{
				StopCoroutine(grabCoroutine);
				grabCoroutine = null;
			}
			RemoveEnemyAfterGrab();
		}
	}

	private void ModifyGrabForDragon()
	{
		if (cachedPlayerStats != null)
		{
			cachedPlayerStats.SetHealth(0);
		}
		if (cachedGameOverScreen != null)
		{
			cachedGameOverScreen.TriggerGameOver();
			return;
		}
		GrabScreen instance = GrabScreen.Instance;
		if (!(instance != null))
		{
			return;
		}
		Button struggleButton = instance.StruggleButton;
		if (!(struggleButton != null))
		{
			return;
		}
		struggleButton.interactable = false;
		if (showDragonGrabMessage)
		{
			Text grabText = instance.GrabText;
			if (grabText != null)
			{
				grabText.text = "The dragon's grip is too strong! You cannot break free!";
			}
		}
	}

	private void RemoveEnemyAfterGrab()
	{
		StopIdleMovingSound();
		StopAllCoroutines();
		TransitionToDead();
		if (deathEffect != null)
		{
			Object.Instantiate(deathEffect, base.transform.position, Quaternion.identity);
		}
		PlaySound(deathSound);
		Object.Destroy(base.gameObject);
	}

	public void ResetGrabFlag()
	{
		hasGrabbedThisAttempt = false;
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
		if ((Object)(object)followerEntity != null)
		{
			followerEntity.simulateMovement = false;
		}
		if (rb != null)
		{
			rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
		}
		yield return new WaitForSeconds(0.15f);
		Vector3 normalized = (player.position - base.transform.position).normalized;
		normalized.y = 0f;
		float num = dashDistance / dashDuration;
		rb.linearVelocity = normalized * num + new Vector3(0f, rb.linearVelocity.y, 0f);
		yield return new WaitForSeconds(dashDuration);
		rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
		isDashing = false;
		if ((Object)(object)followerEntity != null && currentState == AIState.Chasing)
		{
			followerEntity.simulateMovement = true;
		}
	}

	private void UpdateAnimator()
	{
		if (!(animator == null) && !((Object)(object)followerEntity == null))
		{
			bool flag = currentState == AIState.Chasing && followerEntity.simulateMovement;
			float num = ((flag && rb != null) ? rb.linearVelocity.magnitude : 0f);
			animator.SetBool("IsMoving", flag);
			animator.SetFloat("Speed", num);
			if (followerEntity.maxSpeed > 0f)
			{
				animator.SetFloat("NormalizedSpeed", Mathf.Clamp01(num / followerEntity.maxSpeed));
			}
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
		if (!isDead && DamageNumberManager.Instance != null)
		{
			DamageNumberManager.Instance.SpawnDamageNumber(base.transform.position, 0);
		}
	}

	private IEnumerator FlashDamageEffect()
	{
		SpriteRenderer[] sprites = GetComponentsInChildren<SpriteRenderer>();
		Color[] damageColors;
		if (sprites.Length != 0)
		{
			damageColors = new Color[sprites.Length];
			for (int i = 0; i < sprites.Length; i++)
			{
				if (sprites[i] != null)
				{
					damageColors[i] = sprites[i].color;
					sprites[i].color = Color.red;
				}
			}
			yield return new WaitForSeconds(0.15f);
			for (int j = 0; j < sprites.Length; j++)
			{
				if (sprites[j] != null)
				{
					sprites[j].color = damageColors[j];
				}
			}
			yield break;
		}
		Renderer[] renderers = GetComponentsInChildren<Renderer>();
		if (renderers.Length == 0)
		{
			yield break;
		}
		damageColors = new Color[renderers.Length];
		for (int k = 0; k < renderers.Length; k++)
		{
			if (renderers[k] != null && renderers[k].material != null)
			{
				damageColors[k] = renderers[k].material.color;
				renderers[k].material.color = Color.red;
			}
		}
		yield return new WaitForSeconds(0.15f);
		for (int l = 0; l < renderers.Length; l++)
		{
			if (renderers[l] != null && renderers[l].material != null)
			{
				renderers[l].material.color = damageColors[l];
			}
		}
	}

	private void Die()
	{
		if (!isDead)
		{
			isDead = true;
			LevelStatsTracker.RecordEnemyKill();
			TransitionToDead();
			StopIdleMovingSound();
			PlaySound(deathSound);
			if (deathEffect != null)
			{
				Object.Instantiate(deathEffect, base.transform.position, Quaternion.identity);
			}
			Object.Destroy(base.gameObject);
		}
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
		if (!isDead && !frozenByArena)
		{
			if ((currentState == AIState.Idle || currentState == AIState.Chasing) && !isAttemptingGrab)
			{
				StartIdleMovingSound();
			}
			else
			{
				StopIdleMovingSound();
			}
		}
	}

	private void StartIdleMovingSound()
	{
		if (!(loopingAudioSource == null) && !(idleMovingSound == null) && !loopingAudioSource.isPlaying)
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

	private void OnCollisionEnter(Collision collision)
	{
		BreakableObject component = collision.gameObject.GetComponent<BreakableObject>();
		if (component != null)
		{
			component.TakeDamage(1);
		}
	}

	private void OnDestroy()
	{
		GrabScreen instance = GrabScreen.Instance;
		if (instance != null && instance.StruggleButton != null)
		{
			instance.StruggleButton.interactable = true;
		}
	}
}
