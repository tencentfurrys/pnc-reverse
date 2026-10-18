using System;
using System.Collections;
using PixelCrushers.GridController;
using UnityEngine;

public class SpinningEnemyAI : MonoBehaviour, IDamageable
{
	public enum AIState
	{
		Idle,
		Spinning,
		Grabbing,
		Shooting,
		Dead
	}

	[Header("Health")]
	public int maxHealth = 40;

	public int currentHealth;

	[Header("AI Behavior - Ranges")]
	public float detectionRange = 14f;

	public float loseTargetRange = 18f;

	public float grabRange = 2.5f;

	public float spinMaxRange = 10f;

	[Header("Spin Movement")]
	public float spinSpeed = 4.5f;

	public float spinMaxDuration = 6f;

	public float spinCooldown = 1f;

	public float spinEndDuration = 0.4f;

	[Header("Cooldown Variance")]
	[Tooltip("Per-action jitter applied to spin/shoot/grab cooldowns. 0.2 = +/- 20%. Prevents same-prefab enemies from acting in lockstep.")]
	[Range(0f, 0.5f)]
	public float cooldownJitter = 0.2f;

	[Header("Spin Bounce")]
	[Tooltip("Max wall bounces before the spin ends early.")]
	public int maxBouncesPerSpin = 3;

	[Tooltip("0 = pure reflection off the wall; 1 = snap straight back at the player. Typical: 0.3-0.5.")]
	[Range(0f, 1f)]
	public float bouncePlayerBias = 0.4f;

	[Tooltip("Which layers count as walls. Leave at Everything to bounce off any collider (player/enemy collisions are still skipped by tag).")]
	public LayerMask bounceLayers = -1;

	[Tooltip("Minimum seconds between bounces; avoids multi-fire when sliding along a wall.")]
	public float bounceCooldown = 0.1f;

	[Header("Spin Hit")]
	[Tooltip("Damage dealt when Plantasha physically collides with the player during a spin. Ends the spin on first hit.")]
	public int spinHitDamage = 15;

	[Header("Grab Attack")]
	public bool canGrab = true;

	public float grabCooldown = 5f;

	public float grabSettleDuration = 1.2f;

	public RuntimeAnimatorController grabAnimatorController;

	public Vector3 grabCameraPositionOffset = Vector3.zero;

	public Vector3 grabCameraRotationOffset = Vector3.zero;

	[Tooltip("Per-animation sounds for the grab screen. animationName must match the Animator state name.")]
	public GrabAnimationData[] grabScreenAnimations;

	[Header("Grab Dash")]
	public float dashDistance = 2f;

	public float dashDuration = 0.3f;

	[Header("Projectile Attack")]
	public GameObject projectilePrefab;

	public Transform projectileSpawnPoint;

	public int projectileDamage = 12;

	public float projectileSpeed = 10f;

	public float projectileLifetime = 5f;

	public int projectileCount = 1;

	public float projectileSpread;

	public float shootCooldown = 2f;

	public LayerMask lineOfSightLayers = -1;

	[Header("Physics")]
	public float enemyMass = 5f;

	public float enemyDrag = 8f;

	[SerializeField]
	private LayerMask hitboxMask = -1;

	[Header("References")]
	public Transform player;

	public Animator animator;

	public GrabScreen grabScreen;

	[Header("Audio")]
	public AudioClip hurtSound;

	public AudioClip spinLoopSound;

	public AudioClip shootSound;

	public AudioClip grabSound;

	public AudioClip deathSound;

	public AudioClip idleMovingSound;

	public AudioSource audioSource;

	public AudioSource loopingAudioSource;

	[Header("Effects")]
	public GameObject deathEffect;

	public GameObject hitEffect;

	public GameObject muzzleFlashEffect;

	[Header("Gallery")]
	[Tooltip("Unique ID for gallery unlock tracking. Should match EnemyGalleryEntry.enemyID")]
	public string galleryEnemyID = "plantasha";

	[Header("Health Bar")]
	[SerializeField]
	private EnemyHealthBar healthBar;

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

	private float lastSpinEndTime = -999f;

	private float lastGrabTime = -999f;

	private float lastShootTime = -999f;

	private float spinStartedAtTime;

	private bool isDead;

	private bool hasGrabbedThisAttempt;

	private bool hasSpawnedProjectileThisAttack;

	private bool isAttemptingGrab;

	private bool isDashing;

	private bool grabInDamagingState;

	private bool spinMovementActive;

	private bool wasPlayerGrabbedLastFrame;

	private Vector3 lockedSpinDirection;

	private int bouncesThisSpin;

	private float lastBounceTime = -999f;

	private Rigidbody rb;

	private Coroutine spinCoroutine;

	private Coroutine grabCoroutine;

	private Coroutine shootCoroutine;

	private Coroutine flashCoroutine;

	private SpriteRenderer[] cachedSprites;

	private Color[] originalSpriteColors;

	private Renderer[] cachedRenderers;

	private Color[] originalRendererColors;

	private AIState currentState;

	public bool IsDead => isDead;

	public AIState CurrentState => currentState;

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
		HandleGrabHit();
		UpdateIdleMovingSound();
		UpdateSpinLoopSound();
	}

	private void FixedUpdate()
	{
		if (!isDead && !(rb == null))
		{
			if (spinMovementActive && !isDashing)
			{
				rb.linearVelocity = new Vector3(lockedSpinDirection.x * spinSpeed, rb.linearVelocity.y, lockedSpinDirection.z * spinSpeed);
			}
			else if (!isDashing && currentState != AIState.Grabbing)
			{
				rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
			}
		}
	}

	private void OnCollisionEnter(Collision collision)
	{
		if (isDead || !spinMovementActive)
		{
			return;
		}
		GameObject gameObject = collision.gameObject;
		if (gameObject.CompareTag("Player"))
		{
			if (DualWieldingSystem.Instance != null)
			{
				DualWieldingSystem.Instance.TakeDamage(spinHitDamage, base.gameObject);
			}
			else if (player != null)
			{
				player.GetComponent<IDamageable>()?.TakeDamage(spinHitDamage);
			}
			if (spinCoroutine != null)
			{
				StartCoroutine(EndSpinAndTransition(AIState.Idle));
			}
		}
		else
		{
			if (gameObject.CompareTag("Enemy") || Time.time < lastBounceTime + bounceCooldown || (bounceLayers.value & (1 << gameObject.layer)) == 0)
			{
				return;
			}
			Vector3 normal = collision.GetContact(0).normal;
			normal.y = 0f;
			if (normal.sqrMagnitude < 0.0001f)
			{
				return;
			}
			normal.Normalize();
			Vector3 a = Vector3.Reflect(lockedSpinDirection, normal);
			a.y = 0f;
			if (a.sqrMagnitude < 0.0001f)
			{
				return;
			}
			a.Normalize();
			if (player != null && bouncePlayerBias > 0f)
			{
				Vector3 b = player.position - transform.position;
				b.y = 0f;
				if (b.sqrMagnitude > 0.0001f)
				{
					b.Normalize();
					a = Vector3.Slerp(a, b, bouncePlayerBias).normalized;
				}
			}
			lockedSpinDirection = a;
			lastBounceTime = Time.time;
			bouncesThisSpin++;
			if (bouncesThisSpin >= maxBouncesPerSpin && spinCoroutine != null)
			{
				StartCoroutine(EndSpinAndTransition(AIState.Idle));
			}
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
		if (animator == null)
		{
			animator = GetComponent<Animator>();
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
		lastSpinEndTime = Time.time - UnityEngine.Random.Range(0f, spinCooldown);
		lastShootTime = Time.time - UnityEngine.Random.Range(0f, shootCooldown);
		lastGrabTime = Time.time - UnityEngine.Random.Range(0f, grabCooldown);
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
		float distanceToPlayer = Vector3.Distance(transform.position, player.position);
		switch (currentState)
		{
		case AIState.Idle:
			HandleIdleState(distanceToPlayer);
			break;
		case AIState.Spinning:
			HandleSpinningState(distanceToPlayer);
			break;
		case AIState.Grabbing:
		case AIState.Shooting:
		case AIState.Dead:
			break;
		}
	}

	private void HandleIdleState(float distanceToPlayer)
	{
		if (!(distanceToPlayer > detectionRange))
		{
			if (distanceToPlayer <= grabRange && CanGrab())
			{
				TransitionToGrabbing();
			}
			else if (distanceToPlayer <= spinMaxRange && CanSpin())
			{
				TransitionToSpinning();
			}
			else if (HasLineOfSight() && CanShoot())
			{
				TransitionToShooting();
			}
		}
	}

	private void HandleSpinningState(float distanceToPlayer)
	{
		if (spinCoroutine != null && Time.time - spinStartedAtTime >= spinMaxDuration)
		{
			StartCoroutine(EndSpinAndTransition(AIState.Idle));
		}
	}

	private void TransitionToIdle()
	{
		currentState = AIState.Idle;
		StopSpin();
		StopGrab();
		StopShooting();
	}

	private void TransitionToSpinning()
	{
		currentState = AIState.Spinning;
		StopGrab();
		StopShooting();
		StartSpin();
	}

	private void TransitionToGrabbing()
	{
		currentState = AIState.Grabbing;
		StopSpin();
		StopShooting();
		StartGrab();
	}

	private void TransitionToShooting()
	{
		currentState = AIState.Shooting;
		StopSpin();
		StopGrab();
		StartShooting();
	}

	private void TransitionToDead()
	{
		currentState = AIState.Dead;
		StopSpin();
		StopGrab();
		StopShooting();
	}

	private bool CanSpin()
	{
		if (Time.time >= lastSpinEndTime + spinCooldown)
		{
			return spinCoroutine == null;
		}
		return false;
	}

	private void StartSpin()
	{
		if (spinCoroutine == null && !(player == null))
		{
			Vector3 vector = player.position - transform.position;
			vector.y = 0f;
			if (vector.sqrMagnitude < 0.0001f)
			{
				vector = transform.forward;
			}
			lockedSpinDirection = vector.normalized;
			if (rb != null)
			{
				rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
			}
			if (animator != null)
			{
				animator.SetBool("Spinning?", value: true);
				animator.SetTrigger("SpinTrigger");
			}
			spinStartedAtTime = Time.time;
			bouncesThisSpin = 0;
			spinCoroutine = StartCoroutine(SpinSequence());
		}
	}

	private void StopSpin()
	{
		if (spinCoroutine != null)
		{
			StopCoroutine(spinCoroutine);
			spinCoroutine = null;
		}
		spinMovementActive = false;
		if (animator != null)
		{
			animator.SetBool("Spinning?", value: false);
			animator.ResetTrigger("SpinTrigger");
		}
	}

	private IEnumerator SpinSequence()
	{
		while (true)
		{
			yield return null;
		}
	}

	private IEnumerator EndSpinAndTransition(AIState next)
	{
		if (spinCoroutine != null)
		{
			StopCoroutine(spinCoroutine);
			spinCoroutine = null;
		}
		if (animator != null)
		{
			animator.SetBool("Spinning?", value: false);
		}
		spinMovementActive = false;
		if (rb != null)
		{
			rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
		}
		yield return new WaitForSeconds(spinEndDuration);
		lastSpinEndTime = Time.time + UnityEngine.Random.Range(0f - cooldownJitter, cooldownJitter) * spinCooldown;
		switch (next)
		{
		case AIState.Grabbing:
			TransitionToGrabbing();
			break;
		case AIState.Shooting:
			TransitionToShooting();
			break;
		default:
			TransitionToIdle();
			break;
		}
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
			lastGrabTime = Time.time + UnityEngine.Random.Range(0f - cooldownJitter, cooldownJitter) * grabCooldown;
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
		grabInDamagingState = false;
	}

	private IEnumerator GrabSequence()
	{
		hasGrabbedThisAttempt = false;
		isAttemptingGrab = true;
		if (rb != null)
		{
			rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
		}
		if (animator != null)
		{
			animator.SetTrigger("Grabbing");
		}
		yield return new WaitForSeconds(grabSettleDuration);
		grabInDamagingState = false;
		isAttemptingGrab = false;
		grabCoroutine = null;
		if (player != null)
		{
			float num = Vector3.Distance(transform.position, player.position);
			if (num <= spinMaxRange && CanSpin())
			{
				TransitionToSpinning();
			}
			else if (num <= loseTargetRange)
			{
				TransitionToIdle();
			}
			else
			{
				TransitionToIdle();
			}
		}
		else
		{
			TransitionToIdle();
		}
	}

	private void HandleGrabHit()
	{
		if (!grabInDamagingState || hasGrabbedThisAttempt || !canGrab || player == null || grabScreen == null || grabScreen.IsGrabbed)
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
		Debug.DrawRay(vector, vector3.normalized * grabRange, hasGrabbedThisAttempt ? Color.green : Color.magenta, 0.5f);
		RaycastHit[] array = Physics.RaycastAll(vector, vector3.normalized, grabRange, hitboxMask);
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i].collider.transform.root == transform.root || !array[i].collider.CompareTag("Player"))
			{
				continue;
			}
			hasGrabbedThisAttempt = true;
			grabInDamagingState = false;
			grabScreen.StartGrab(gameObject, grabAnimatorController, grabCameraPositionOffset, grabCameraRotationOffset);
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

	private void RemoveEnemyAfterGrab()
	{
		StopAllCoroutines();
		TransitionToDead();
		StopIdleMovingSound();
		StopSpinLoopSound();
		if (deathEffect != null)
		{
			UnityEngine.Object.Instantiate(deathEffect, transform.position, Quaternion.identity);
		}
		PlaySound(deathSound);
		UnityEngine.Object.Destroy(gameObject);
	}

	private bool CanShoot()
	{
		if (Time.time >= lastShootTime + shootCooldown)
		{
			return shootCoroutine == null;
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
			return hitInfo.collider.CompareTag("Player");
		}
		return true;
	}

	private void StartShooting()
	{
		if (shootCoroutine == null)
		{
			lastShootTime = Time.time + UnityEngine.Random.Range(0f - cooldownJitter, cooldownJitter) * shootCooldown;
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
		if (rb != null)
		{
			rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
		}
		if (animator != null)
		{
			animator.SetTrigger("Shoot");
		}
		yield return new WaitForSeconds(shootCooldown);
		shootCoroutine = null;
		if (player == null)
		{
			TransitionToIdle();
			yield break;
		}
		float num = Vector3.Distance(transform.position, player.position);
		if (num > loseTargetRange)
		{
			TransitionToIdle();
		}
		else if (num <= spinMaxRange && CanSpin())
		{
			TransitionToSpinning();
		}
		else if (num <= grabRange && CanGrab())
		{
			TransitionToGrabbing();
		}
		else
		{
			TransitionToIdle();
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
		Vector3 normalized = (player.position - transform.position).normalized;
		normalized.y = 0f;
		float num = dashDistance / dashDuration;
		if (rb != null)
		{
			rb.linearVelocity = normalized * num + new Vector3(0f, rb.linearVelocity.y, 0f);
		}
		yield return new WaitForSeconds(dashDuration);
		if (rb != null)
		{
			rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
		}
		isDashing = false;
	}

	public void StartSpinMovement()
	{
		spinMovementActive = true;
	}

	public void StopSpinMovement()
	{
		spinMovementActive = false;
	}

	public void StartGrabDamageState()
	{
		grabInDamagingState = true;
	}

	public void EndGrabDamageState()
	{
		grabInDamagingState = false;
	}

	public void ResetGrabFlag()
	{
		hasGrabbedThisAttempt = false;
	}

	public void StopMovementForGrab()
	{
		if (rb != null)
		{
			rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
		}
		StopIdleMovingSound();
		PlaySound(grabSound);
	}

	public void StopMovementForShoot()
	{
		if (rb != null)
		{
			rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
		}
		StopIdleMovingSound();
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
		if (!(muzzleFlashEffect == null) && !(projectileSpawnPoint == null))
		{
			GameObject instance = ((VFXPoolManager.Instance != null) ? VFXPoolManager.Instance.Get(muzzleFlashEffect, projectileSpawnPoint.position, projectileSpawnPoint.rotation) : UnityEngine.Object.Instantiate(muzzleFlashEffect, projectileSpawnPoint.position, projectileSpawnPoint.rotation));
			if (VFXPoolManager.Instance != null)
			{
				VFXPoolManager.Instance.ReturnAfterDelay(instance, 2f);
			}
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

	public void ResetProjectileFlag()
	{
		hasSpawnedProjectileThisAttack = false;
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
			if (Vector3.Distance(transform.position, player.position) <= spinMaxRange && CanSpin())
			{
				TransitionToSpinning();
			}
			else if (HasLineOfSight() && CanShoot())
			{
				TransitionToShooting();
			}
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
		StopSpinLoopSound();
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
		StopSpin();
		StopGrab();
		StopShooting();
		StopIdleMovingSound();
		StopSpinLoopSound();
		if (rb != null)
		{
			rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
		}
		if (animator != null)
		{
			animator.SetBool("Spinning?", value: false);
		}
	}

	private void ResumeAIBehavior()
	{
		if (!(player == null))
		{
			float num = Vector3.Distance(transform.position, player.position);
			if (num > detectionRange)
			{
				TransitionToIdle();
			}
			else if (num <= grabRange && CanGrab())
			{
				TransitionToGrabbing();
			}
			else if (num <= spinMaxRange && CanSpin())
			{
				TransitionToSpinning();
			}
			else if (HasLineOfSight() && CanShoot())
			{
				TransitionToShooting();
			}
			else
			{
				TransitionToIdle();
			}
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
		if (currentState == AIState.Idle && grabCoroutine == null && shootCoroutine == null && spinCoroutine == null && !isDead)
		{
			StartIdleMovingSound();
		}
		else if (loopingAudioSource != null && loopingAudioSource.clip == idleMovingSound)
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

	private void UpdateSpinLoopSound()
	{
		if (spinMovementActive && !isDead)
		{
			StartSpinLoopSound();
		}
		else if (loopingAudioSource != null && loopingAudioSource.clip == spinLoopSound)
		{
			StopSpinLoopSound();
		}
	}

	private void StartSpinLoopSound()
	{
		if (loopingAudioSource != null && spinLoopSound != null && (loopingAudioSource.clip != spinLoopSound || !loopingAudioSource.isPlaying))
		{
			loopingAudioSource.clip = spinLoopSound;
			loopingAudioSource.Play();
		}
	}

	private void StopSpinLoopSound()
	{
		if (loopingAudioSource != null && loopingAudioSource.isPlaying && loopingAudioSource.clip == spinLoopSound)
		{
			loopingAudioSource.Stop();
		}
	}

	public void ForceAggro()
	{
		if (!isDead && currentState == AIState.Idle && !(player == null))
		{
			if (DistanceToPlayer <= spinMaxRange && CanSpin())
			{
				TransitionToSpinning();
			}
			else if (HasLineOfSight() && CanShoot())
			{
				TransitionToShooting();
			}
		}
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireSphere(transform.position, detectionRange);
		Gizmos.color = Color.red;
		Gizmos.DrawWireSphere(transform.position, loseTargetRange);
		Gizmos.color = new Color(1f, 0.5f, 0f);
		Gizmos.DrawWireSphere(transform.position, spinMaxRange);
		if (canGrab)
		{
			Gizmos.color = Color.magenta;
			Gizmos.DrawWireSphere(transform.position, grabRange);
		}
		if (Application.isPlaying && player != null)
		{
			Gizmos.color = (HasLineOfSight() ? Color.green : Color.red);
			Gizmos.DrawLine(transform.position + Vector3.up, player.position + Vector3.up);
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
}
