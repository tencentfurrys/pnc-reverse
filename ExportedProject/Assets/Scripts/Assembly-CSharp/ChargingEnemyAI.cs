using System;
using System.Collections;
using Pathfinding;
using Pathfinding.ECS;
using PixelCrushers.GridController;
using UnityEngine;

public class ChargingEnemyAI : MonoBehaviour, IDamageable
{
	public enum AIState
	{
		Idle = 0,
		Chasing = 1,
		PreparingCharge = 2,
		Charging = 3,
		Dead = 4
	}

	[Header("Health")]
	public int maxHealth = 75;

	public int currentHealth;

	[Header("AI Behavior")]
	public float detectionRange = 12f;

	public float loseTargetRange = 18f;

	[Header("Charge Attack")]
	public float maxChargingDistance = 10f;

	public float minChargingDistance = 3f;

	public float chargeDuration = 0.8f;

	public float chargeSpeed = 15f;

	public float chargeCooldown = 3f;

	[SerializeField]
	private float chargeHitRange = 1.5f;

	[SerializeField]
	private LayerMask hitboxMask = -1;

	[Header("Physics")]
	public float enemyMass = 5f;

	public float enemyDrag = 8f;

	[Header("Ground Check")]
	[Tooltip("Layers that count as ground. Excludes Enemy/Player so enemies can't stand on each other. Mirrors the FollowerEntity's authored Raycast Ground Mask — A* gravity is switched off at runtime, so that field is no longer live and this one is the real answer.")]
	[SerializeField]
	private LayerMask groundMask = 16897;

	[Tooltip("How far below the feet still counts as grounded. Keep small — this is a contact test, not a ledge probe.")]
	[SerializeField]
	private float groundCheckDistance = 0.3f;

	[Tooltip("Ray start height above the pivot. Stays inside the collider so the ray can't hit the floor from underneath.")]
	[SerializeField]
	private float groundCheckOffset = 0.1f;

	[Tooltip("Linear damping while airborne. Enemy Drag is high to stop ground sliding; leaving it on in the air caps fall speed at about g/drag and makes the enemy float down.")]
	[SerializeField]
	private float airDrag;

	[Header("A* Pathfinding")]
	public FollowerEntity followerEntity;

	[SerializeField]
	private bool autoFindPathfindingComponents = true;

	[Header("Audio")]
	public AudioClip hurtSound;

	public AudioClip chargeSound;

	public AudioClip deathSound;

	public AudioClip idleMovingSound;

	[Header("Effects")]
	public GameObject deathEffect;

	public GameObject hitEffect;

	public GameObject chargeEffect;

	[Header("Gallery")]
	[Tooltip("Unique ID for gallery unlock tracking. Should match EnemyGalleryEntry.enemyID")]
	public string galleryEnemyID;

	[Header("Grapple Screen")]
	[Tooltip("Animator controller for the grapple screen. Doubles as this enemy's grapple identity: enemies sharing this asset can stack on the player, a different one is refused. Required — an enemy with no controller can't grapple.")]
	public RuntimeAnimatorController grappleAnimatorController;

	[Tooltip("Animator state names in grappleAnimatorController, by grappler count.")]
	public string grappleOneAnimation = "One";

	public string grappleTwoAnimation = "Two";

	public string grappleThreeAnimation = "Three";

	[Tooltip("Grapple screen loop sounds, by grappler count. Leaving these empty is fine — the loop is just silent.")]
	public AudioClip grappleLoopOne;

	public AudioClip grappleLoopTwo;

	public AudioClip grappleLoopThree;

	[Header("Trio Grab Overflow")]
	[Tooltip("Grab screen shown when maxGrappleCount of this enemy pin the player at max heat. Leave empty to opt this enemy out of trio overflow entirely.")]
	public RuntimeAnimatorController trioGrabAnimatorController;

	public Vector3 trioGrabCameraPositionOffset;

	public Vector3 trioGrabCameraRotationOffset;

	[Header("Grab Screen Audio")]
	[Tooltip("Per-animation sounds for the grab screen. animationName must match the Animator state name. Used when a trio-grab overflow escalates from GrappleScreen to GrabScreen.")]
	public GrabAnimationData[] grabScreenAnimations;

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

	[Header("Explode On Death")]
	[Tooltip("When enabled, this enemy bursts on death and leaves a lingering heat cloud behind.")]
	[SerializeField]
	private bool explodeOnDeath;

	[Tooltip("Explosion VFX spawned at death. Cosmetic only — the threat is the cloud it leaves.")]
	[SerializeField]
	private GameObject explosionEffect;

	[Tooltip("Cloud prefab left behind. Needs a LingeringHeatCloud and a trigger collider. All of its tuning lives on the prefab, not here.")]
	[SerializeField]
	private GameObject heatCloudPrefab;

	[Tooltip("Explosion sound. Played through AudioPool, not this enemy's AudioSource, which is destroyed on the same frame.")]
	[SerializeField]
	private AudioClip explosionSound;

	[Tooltip("How long the explosion VFX lives before it is returned to the pool.")]
	[SerializeField]
	private float explosionEffectLifetime = 3f;

	[Tooltip("Animator trigger fired on death to play a wind-up before detonating. The clip should call the OnExplodeAnimationEvent() animation event. Leave blank to explode instantly.")]
	[SerializeField]
	private string explodeTriggerName = "Explode";

	[Tooltip("Safety net: detonate this long after death even if the animation event never arrives. Must be longer than the wind-up clip, or the enemy explodes early.")]
	[SerializeField]
	private float explodeAnimationTimeout = 3f;

	[Header("Grapple Throw")]
	[Tooltip("Speed this enemy is flung at when the player shakes it off a grapple.")]
	[SerializeField]
	private float throwForce = 12f;

	[Tooltip("How long the enemy lies stunned after the throw settles, before it chases again.")]
	[SerializeField]
	private float throwRecoverySeconds = 0.8f;

	[Tooltip("Horizontal speed below which the slide counts as finished.")]
	[SerializeField]
	private float throwSettleSpeed = 0.5f;

	[Tooltip("Safety net: how long the enemy may sit in PreparingCharge waiting for the charge animation event before giving up and going back to chasing. Must be longer than the PrepareCharge animation.")]
	[SerializeField]
	private float chargePrepareTimeout = 2f;

	private float lastChargeTime;

	private bool isDead;

	private bool hasDealtChargeDamage;

	private bool isCharging;

	private bool isThrown;

	private bool wasPlayerGrabbedLastFrame;

	private AudioSource audioSource;

	[SerializeField]
	private AudioSource loopingAudioSource;

	private PlayerStats playerStats;

	private Rigidbody rb;

	private AIState currentState;

	private float prepareChargeStartTime;

	private bool hasExploded;

	private bool hasPlayedExplosionSound;

	private Coroutine chargeCoroutine;

	private Coroutine flashCoroutine;

	private SpriteRenderer[] cachedSprites;

	private Color[] originalSpriteColors;

	private Renderer[] cachedRenderers;

	private Color[] originalRendererColors;

	public float ThrowForce => throwForce;

	public bool IsDead => isDead;

	public bool IsCharging => isCharging;

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

	private bool IsPlayerVulnerable
	{
		get
		{
			if (playerStats != null && playerStats.IsAlive)
			{
				return playerStats.IsAttackLocked();
			}
			return false;
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
		if (grabScreen == null)
		{
			grabScreen = GrabScreen.Instance;
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
			followerEntity.enableGravity = false;
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
		CapsuleCollider component = GetComponent<CapsuleCollider>();
		if (component != null && (UnityEngine.Object)(object)followerEntity != null)
		{
			followerEntity.radius = component.radius;
			followerEntity.height = component.height;
		}
	}

	private void FixedUpdate()
	{
		if (isDead || rb == null || (UnityEngine.Object)(object)followerEntity == null)
		{
			return;
		}
		if (isCharging)
		{
			HandleChargeHit();
		}
		followerEntity.position = base.transform.position;
		bool flag = Physics.Raycast(base.transform.position + Vector3.up * groundCheckOffset, Vector3.down, out var hitInfo, groundCheckOffset + groundCheckDistance, groundMask, QueryTriggerInteraction.Ignore);
		rb.linearDamping = (flag ? enemyDrag : airDrag);
		if (!flag)
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
		else if (!isCharging && !isThrown)
		{
			rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
		}
	}

	private void UpdateStateMachine()
	{
		if (player == null)
		{
			return;
		}
		float distanceToPlayer = Vector3.Distance(base.transform.position, player.position);
		switch (currentState)
		{
		case AIState.Idle:
			HandleIdleState(distanceToPlayer);
			break;
		case AIState.Chasing:
			HandleChasingState(distanceToPlayer);
			break;
		case AIState.PreparingCharge:
			if (Time.time - prepareChargeStartTime > chargePrepareTimeout)
			{
				StopCharge();
				TransitionToChasing();
			}
			break;
		case AIState.Charging:
		case AIState.Dead:
			break;
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
		if (playerStats != null && !playerStats.IsAlive)
		{
			TransitionToIdle();
		}
		else if (distanceToPlayer > loseTargetRange)
		{
			TransitionToIdle();
		}
		else if (ShouldCharge(distanceToPlayer))
		{
			TransitionToCharging();
		}
		else if (distanceToPlayer < minChargingDistance)
		{
			StopPathfinding();
			Vector3 normalized = (player.position - base.transform.position).normalized;
			normalized.y = 0f;
			if (normalized != Vector3.zero)
			{
				base.transform.rotation = Quaternion.LookRotation(normalized);
			}
		}
		else if ((UnityEngine.Object)(object)followerEntity != null)
		{
			followerEntity.simulateMovement = true;
			followerEntity.destination = player.position;
		}
	}

	private bool ShouldCharge(float distanceToPlayer)
	{
		if (isCharging)
		{
			return false;
		}
		bool isPlayerVulnerable = IsPlayerVulnerable;
		if (!isPlayerVulnerable && Time.time < lastChargeTime + chargeCooldown)
		{
			return false;
		}
		bool flag = !AstarPath.active.Linecast(base.transform.position, player.position);
		if (isPlayerVulnerable)
		{
			return (distanceToPlayer <= maxChargingDistance) & flag;
		}
		return (distanceToPlayer <= maxChargingDistance && distanceToPlayer >= minChargingDistance) & flag;
	}

	private void TransitionToIdle()
	{
		currentState = AIState.Idle;
		StopPathfinding();
	}

	private void TransitionToChasing()
	{
		currentState = AIState.Chasing;
		StartPathfinding();
	}

	private void TransitionToCharging()
	{
		currentState = AIState.PreparingCharge;
		prepareChargeStartTime = Time.time;
		StopPathfinding();
		StartCharge();
	}

	private void TransitionToDead()
	{
		currentState = AIState.Dead;
		StopPathfinding();
		StopCharge();
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

	private void StartCharge()
	{
		if (chargeCoroutine == null && !(player == null))
		{
			lastChargeTime = Time.time;
			chargeCoroutine = StartCoroutine(ChargeSequence());
		}
	}

	private void StopCharge()
	{
		if (chargeCoroutine != null)
		{
			StopCoroutine(chargeCoroutine);
			chargeCoroutine = null;
		}
		isCharging = false;
	}

	private IEnumerator ChargeSequence()
	{
		StopIdleMovingSound();
		hasDealtChargeDamage = false;
		currentState = AIState.PreparingCharge;
		Vector3 normalized = (player.position - base.transform.position).normalized;
		normalized.y = 0f;
		if (normalized != Vector3.zero)
		{
			base.transform.rotation = Quaternion.LookRotation(normalized);
		}
		if (animator != null)
		{
			animator.SetTrigger("PrepareCharge");
		}
		yield return null;
	}

	public void PlayChargeSound()
	{
		PlaySound(chargeSound);
	}

	public void StartChargeMovement()
	{
		if (!(player == null) && chargeCoroutine != null)
		{
			StartCoroutine(ExecuteCharge());
		}
	}

	private IEnumerator ExecuteCharge()
	{
		currentState = AIState.Charging;
		isCharging = true;
		if ((UnityEngine.Object)(object)followerEntity != null)
		{
			followerEntity.simulateMovement = false;
		}
		if (chargeEffect != null)
		{
			UnityEngine.Object.Instantiate(chargeEffect, base.transform.position, base.transform.rotation);
		}
		Vector3 normalized = (player.position - base.transform.position).normalized;
		normalized.y = 0f;
		float num = maxChargingDistance / chargeDuration;
		rb.linearVelocity = normalized * num + new Vector3(0f, rb.linearVelocity.y, 0f);
		yield return new WaitForSeconds(chargeDuration);
		rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
		isCharging = false;
		currentState = AIState.Chasing;
		chargeCoroutine = null;
		StartPathfinding();
	}

	public void DealChargeDamage()
	{
		if (isCharging && !hasDealtChargeDamage && !(player == null))
		{
			hasDealtChargeDamage = true;
			GrappleScreenobject instance = GrappleScreenobject.Instance;
			if (!(instance != null) || instance.GrappleCount < 3)
			{
				PerformGrabAttack();
			}
		}
	}

	private void PerformGrabAttack()
	{
		GrappleScreenobject instance = GrappleScreenobject.Instance;
		if (!(instance == null) && instance.StartGrapple(base.gameObject))
		{
			base.gameObject.SetActive(value: false);
		}
	}

	public void ThrowAfterGrapple(Vector3 velocity)
	{
		if (!isDead)
		{
			base.gameObject.SetActive(value: true);
			base.enabled = true;
			Collider[] componentsInChildren = GetComponentsInChildren<Collider>(includeInactive: true);
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].enabled = true;
			}
			Renderer[] componentsInChildren2 = GetComponentsInChildren<Renderer>(includeInactive: true);
			for (int i = 0; i < componentsInChildren2.Length; i++)
			{
				componentsInChildren2[i].enabled = true;
			}
			StopCharge();
			hasDealtChargeDamage = false;
			currentState = AIState.Idle;
			lastChargeTime = Time.time;
			if ((UnityEngine.Object)(object)followerEntity != null)
			{
				followerEntity.simulateMovement = false;
			}
			isThrown = true;
			if (rb != null)
			{
				rb.isKinematic = false;
				rb.angularVelocity = Vector3.zero;
				rb.linearVelocity = velocity;
			}
			StartCoroutine(ThrowRecovery());
		}
	}

	private void OnDisable()
	{
		isThrown = false;
	}

	private IEnumerator ThrowRecovery()
	{
		float timeout = Time.time + 5f;
		yield return null;
		while (Time.time < timeout && rb != null && !(new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z).magnitude <= throwSettleSpeed))
		{
			yield return null;
		}
		yield return new WaitForSeconds(throwRecoverySeconds);
		isThrown = false;
		if (!isDead)
		{
			if (player != null)
			{
				TransitionToChasing();
			}
			else
			{
				TransitionToIdle();
			}
		}
	}

	private void HandleChargeHit()
	{
		if (hasDealtChargeDamage || player == null)
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
		Mathf.Min(vector3.magnitude, chargeHitRange);
		Debug.DrawRay(vector, vector3.normalized * chargeHitRange, Color.red, 0.1f);
		RaycastHit[] array = Physics.RaycastAll(vector, vector3.normalized, chargeHitRange, hitboxMask);
		for (int i = 0; i < array.Length; i++)
		{
			if (!(array[i].collider.transform.root == base.transform.root) && (array[i].collider.CompareTag("Player") || !(array[i].collider.GetComponent<CharacterController>() == null)))
			{
				DealChargeDamage();
				break;
			}
		}
	}

	private void UpdateAnimator()
	{
		if (!(animator == null) && !((UnityEngine.Object)(object)followerEntity == null))
		{
			float num = ((currentState == AIState.Chasing && followerEntity.simulateMovement && rb != null) ? rb.linearVelocity.magnitude : 0f);
			if (followerEntity.maxSpeed > 0f)
			{
				animator.SetFloat("Speed", Mathf.Clamp01(num / followerEntity.maxSpeed));
			}
			else
			{
				animator.SetFloat("Speed", 0f);
			}
			animator.SetBool("IsCharging", currentState == AIState.Charging);
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
						DamageNumberManager.Instance.SpawnHealNumber(base.transform.position, amount);
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
		if (explodeOnDeath)
		{
			BeginExplodeSequence();
			return;
		}
		if (deathEffect != null)
		{
			UnityEngine.Object.Instantiate(deathEffect, base.transform.position, Quaternion.identity);
		}
		UnityEngine.Object.Destroy(base.gameObject);
	}

	private void BeginExplodeSequence()
	{
		Collider[] componentsInChildren = GetComponentsInChildren<Collider>(includeInactive: true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].enabled = false;
		}
		if (rb != null)
		{
			rb.linearVelocity = Vector3.zero;
			rb.angularVelocity = Vector3.zero;
			rb.isKinematic = true;
		}
		if (healthBar != null)
		{
			healthBar.gameObject.SetActive(value: false);
		}
		if (animator == null || string.IsNullOrWhiteSpace(explodeTriggerName))
		{
			Detonate();
			return;
		}
		animator.SetTrigger(explodeTriggerName);
		StartCoroutine(ExplodeTimeoutFallback());
		UnityEngine.Object.Destroy(base.gameObject, explodeAnimationTimeout + 1f);
	}

	public void PlayExplosionSound()
	{
		if (!hasPlayedExplosionSound)
		{
			hasPlayedExplosionSound = true;
			if (explosionSound != null && AudioPool.Instance != null)
			{
				AudioPool.Instance.PlayAtPoint(explosionSound, base.transform.position);
			}
		}
	}

	public void OnExplodeAnimationEvent()
	{
		Detonate();
	}

	private IEnumerator ExplodeTimeoutFallback()
	{
		yield return new WaitForSeconds(explodeAnimationTimeout);
		Detonate();
	}

	private void Detonate()
	{
		if (!hasExploded)
		{
			hasExploded = true;
			PlayExplosionSound();
			SpawnDeathExplosion();
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}

	private void SpawnDeathExplosion()
	{
		Vector3 position = base.transform.position;
		if (explosionEffect != null)
		{
			GameObject gameObject = ((VFXPoolManager.Instance != null) ? VFXPoolManager.Instance.Get(explosionEffect, position, Quaternion.identity) : UnityEngine.Object.Instantiate(explosionEffect, position, Quaternion.identity));
			if (VFXPoolManager.Instance != null)
			{
				VFXPoolManager.Instance.ReturnAfterDelay(gameObject, explosionEffectLifetime);
			}
			else
			{
				UnityEngine.Object.Destroy(gameObject, explosionEffectLifetime);
			}
		}
		if (heatCloudPrefab != null)
		{
			UnityEngine.Object.Instantiate(heatCloudPrefab, position, Quaternion.identity);
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
		if ((currentState == AIState.Idle || currentState == AIState.Chasing) && chargeCoroutine == null && !isDead)
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
		Gizmos.DrawWireSphere(base.transform.position, detectionRange);
		Gizmos.color = Color.red;
		Gizmos.DrawWireSphere(base.transform.position, loseTargetRange);
		Gizmos.color = Color.blue;
		Gizmos.DrawWireSphere(base.transform.position, minChargingDistance);
		Gizmos.color = Color.cyan;
		Gizmos.DrawWireSphere(base.transform.position, maxChargingDistance);
		if (isCharging && player != null)
		{
			Gizmos.color = Color.magenta;
			Gizmos.DrawLine(base.transform.position, player.position);
		}
		Vector3 vector = base.transform.position + Vector3.up * groundCheckOffset;
		Vector3 vector2 = vector + Vector3.down * (groundCheckOffset + groundCheckDistance);
		Gizmos.color = (Physics.Raycast(vector, Vector3.down, groundCheckOffset + groundCheckDistance, groundMask, QueryTriggerInteraction.Ignore) ? Color.green : Color.red);
		Gizmos.DrawLine(vector, vector2);
		Gizmos.DrawWireSphere(vector2, 0.08f);
		if (Application.isPlaying && maxHealth > 0)
		{
			Vector3 vector3 = base.transform.position + Vector3.up * 2.5f;
			Gizmos.color = Color.red;
			Gizmos.DrawWireCube(vector3, new Vector3(2f, 0.2f, 0.1f));
			float healthPercentage = HealthPercentage;
			Gizmos.color = Color.green;
			Gizmos.DrawCube(size: new Vector3(2f * healthPercentage, 0.2f, 0.1f), center: vector3 + Vector3.left * (1f - healthPercentage));
		}
	}

	[ContextMenu("Force Charge")]
	private void DebugForceCharge()
	{
		if (currentState == AIState.Chasing)
		{
			TransitionToCharging();
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
		StopCharge();
		StopIdleMovingSound();
		if (animator != null)
		{
			animator.SetBool("IsCharging", value: false);
			animator.SetFloat("Speed", 0f);
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
}
