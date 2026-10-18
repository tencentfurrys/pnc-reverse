using System;
using System.Collections;
using PixelCrushers.GridController;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class PlayerStats : MonoBehaviour, IDamageable
{
	[Serializable]
	public struct MovementSpeeds
	{
		public float walkSpeed;

		public float crouchSpeed;

		public float dashForce;

		public float jumpHeight;

		public float dashDuration;

		public float dashCooldown;

		public float speedMultiplier;

		public int maxDashCharges;
	}

	[Serializable]
	public struct PlayerStatsData
	{
		public int currentHealth;

		public int maxHealth;

		public float currentHeat;

		public float maxHeat;

		public float maxHeatOverflow;

		public float baseWalkSpeed;

		public float baseCrouchSpeed;

		public float baseDashForce;

		public float jumpHeight;

		public float dashDuration;

		public float dashCooldown;

		public int bonusDashCharges;

		public float overheatingThreshold;

		public float criticalHeatThreshold;

		public float healthRegenRate;

		public float heatCooldownRate;

		public float externalMovementMultiplier;
	}

	[Header("Health Settings")]
	[SerializeField]
	private int maxHealth = 100;

	[SerializeField]
	private int currentHealth;

	[SerializeField]
	private float healthRegenRate = 1f;

	[SerializeField]
	private float healthRegenDelay = 3f;

	[SerializeField]
	private bool isInvulnerable;

	[Header("Movement Settings")]
	[SerializeField]
	private float baseWalkSpeed = 5f;

	[SerializeField]
	private float baseCrouchSpeed = 2.5f;

	[SerializeField]
	private float baseDashForce = 15f;

	[SerializeField]
	private float jumpHeight = 2f;

	[SerializeField]
	private float dashDuration = 0.2f;

	[SerializeField]
	private float dashCooldown = 1f;

	[Header("Heat System")]
	[SerializeField]
	private float maxHeat = 100f;

	[SerializeField]
	private float currentHeat;

	[SerializeField]
	private float heatCooldownRate = 10f;

	[SerializeField]
	private float heatCooldownDelay = 1f;

	[SerializeField]
	private float overheatingThreshold = 80f;

	[SerializeField]
	private float criticalHeatThreshold = 100f;

	[SerializeField]
	private bool allowHeatOverflow = true;

	[SerializeField]
	private float maxHeatOverflow = 150f;

	[SerializeField]
	private float overflowMovementPenalty = 0.5f;

	[Header("Health Bar UI")]
	[SerializeField]
	private Image healthBarFill;

	[SerializeField]
	private TextMeshProUGUI healthText;

	[SerializeField]
	private Slider healthSlider;

	[SerializeField]
	private bool smoothHealthBarTransition = true;

	[SerializeField]
	private float healthBarTransitionSpeed = 5f;

	[Header("Max Health Increase Animation")]
	[SerializeField]
	private bool animateMaxHealthIncrease = true;

	[SerializeField]
	private float maxHealthIncreaseAnimationDuration = 1f;

	[SerializeField]
	private bool healToFillNewHealth = true;

	[SerializeField]
	private AnimationCurve maxHealthIncreaseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

	[Header("Heat Bar UI")]
	[SerializeField]
	private Image heatBarFill;

	[SerializeField]
	private TextMeshProUGUI heatText;

	[SerializeField]
	private Slider heatSlider;

	[SerializeField]
	private bool smoothHeatBarTransition = true;

	[SerializeField]
	private float heatBarTransitionSpeed = 8f;

	[SerializeField]
	private Color normalHeatColor = Color.blue;

	[SerializeField]
	private Color warningHeatColor = Color.yellow;

	[SerializeField]
	private Color criticalHeatColor = Color.red;

	[Header("Damage Sound")]
	[SerializeField]
	private AudioSource audioSource;

	[SerializeField]
	private AudioClip damageTakenSound;

	[Header("Animation")]
	[SerializeField]
	private Animator healthAnimator;

	[SerializeField]
	private Animator heatAnimator;

	[Header("Events")]
	public UnityEvent<int, int> OnHealthChanged;

	public UnityEvent<int> OnDamageTaken;

	public UnityEvent<int> OnHealed;

	public UnityEvent OnPlayerDeath;

	public UnityEvent<float> OnHealthBarUpdated;

	public UnityEvent<float, float> OnHeatChanged;

	public UnityEvent<float> OnHeatGenerated;

	public UnityEvent<float> OnHeatCooled;

	public UnityEvent OnOverheatingStarted;

	public UnityEvent OnOverheatingStopped;

	public UnityEvent OnCriticalHeatReached;

	public UnityEvent OnHeatOverflowStarted;

	public UnityEvent OnHeatOverflowStopped;

	public UnityEvent OnMovementSlowed;

	public UnityEvent OnMovementRestored;

	public UnityEvent<float> OnHeatBarUpdated;

	private float heatBarVelocity;

	public UnityEvent<float> OnMovementSpeedChanged;

	private float lastDamageTime;

	private float lastHeatGenerationTime;

	private bool isDead;

	private bool wasOverheating;

	private bool wasCriticallyHot;

	private bool wasInHeatOverflow;

	private bool movementWasSlowed;

	private bool hasBeenOverheated;

	private float targetHealthPercentage;

	private float targetHeatPercentage;

	private float currentDisplayedHealth;

	private float currentDisplayedHeat;

	private MovementSpeeds lastMovementSpeeds;

	private Coroutine maxHealthIncreaseCoroutine;

	private int lastDisplayedHealthValue = -1;

	private int lastDisplayedMaxHealthValue = -1;

	private int lastDisplayedHeatInt = -1;

	private int lastDisplayedMaxHeatInt = -1;

	private bool lastDisplayedOverflow;

	private float overheatThresholdOffset;

	private float criticalThresholdOffset;

	private bool heatOffsetsInitialized;

	private float externalMovementMultiplier = 1f;

	private float dashForceMultiplier = 1f;

	private float dashDurationMultiplier = 1f;

	private int bonusDashCharges;

	private GridInventorySystem cachedInventory;

	private BuffDebuffSystem buffDebuffSystem;

	private bool showDebugInfo = true;

	public int CurrentHealth => currentHealth;

	public int MaxHealth => maxHealth;

	public float HealthPercentage => (float)currentHealth / (float)maxHealth;

	public bool IsAlive => currentHealth > 0;

	public bool IsInvulnerable => isInvulnerable;

	[field: SerializeField]
	public bool CanBeGrabbed { get; set; } = true;

	public float CurrentHeat => currentHeat;

	public float MaxHeat => maxHeat;

	public float MaxHeatWithOverflow => maxHeatOverflow;

	public float HeatPercentage => currentHeat / maxHeat;

	public float OverflowHeatPercentage => currentHeat / maxHeatOverflow;

	public bool IsOverheating => currentHeat >= overheatingThreshold;

	public bool IsCriticallyHot => currentHeat >= criticalHeatThreshold;

	public bool IsInHeatOverflow => currentHeat > maxHeat;

	public bool CanPerformActions
	{
		get
		{
			if (hasBeenOverheated)
			{
				return currentHeat <= 0f;
			}
			return true;
		}
	}

	public float MovementSpeedMultiplier => CalculateTotalMovementMultiplier();

	public bool CanDash
	{
		get
		{
			if (hasBeenOverheated)
			{
				return currentHeat <= 0f;
			}
			return true;
		}
	}

	public float WalkSpeed => baseWalkSpeed * MovementSpeedMultiplier;

	public float CrouchSpeed => baseCrouchSpeed * MovementSpeedMultiplier;

	public float DashForce => baseDashForce * dashForceMultiplier * MovementSpeedMultiplier;

	public float JumpHeight => jumpHeight;

	public float DashDuration => dashDuration * dashDurationMultiplier;

	public float DashCooldown => dashCooldown;

	public int MaxDashCharges => 1 + bonusDashCharges;

	private void Awake()
	{
		currentHealth = maxHealth;
		targetHealthPercentage = 1f;
		currentDisplayedHealth = currentHealth;
		currentHeat = 0f;
		targetHeatPercentage = 0f;
		currentDisplayedHeat = 0f;
		CacheHeatOffsets();
		if (maxHealthIncreaseCurve == null || maxHealthIncreaseCurve.keys.Length == 0)
		{
			maxHealthIncreaseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
		}
		lastMovementSpeeds = GetCurrentMovementSpeeds();
		if (audioSource == null)
		{
			audioSource = GetComponent<AudioSource>();
		}
		if (audioSource == null)
		{
			audioSource = base.gameObject.AddComponent<AudioSource>();
		}
		cachedInventory = GetComponent<GridInventorySystem>();
		if (cachedInventory == null)
		{
			cachedInventory = UnityEngine.Object.FindAnyObjectByType<GridInventorySystem>();
		}
		buffDebuffSystem = UnityEngine.Object.FindAnyObjectByType<BuffDebuffSystem>();
	}

	private void Start()
	{
		UpdateHealthBarImmediate();
		UpdateHeatBarImmediate();
		OnHealthChanged?.Invoke(currentHealth, maxHealth);
		OnHeatChanged?.Invoke(currentHeat, maxHeat);
		UpdateAnimationBools();
	}

	private void Update()
	{
		HandleHeatCooldown();
		HandleHeatBarAnimation();
		HandleOverheatingStates();
		HandleHealthBarAnimation();
		if (!isDead)
		{
			HandleHealthRegeneration();
			CheckMovementSpeedChanges();
			UpdateAnimationBools();
		}
	}

	private float CalculateTotalMovementMultiplier()
	{
		float num = 1f;
		if (hasBeenOverheated && currentHeat > 0f)
		{
			num *= overflowMovementPenalty;
		}
		return num * externalMovementMultiplier;
	}

	public void SetExternalMovementMultiplier(float multiplier)
	{
		externalMovementMultiplier = Mathf.Max(0.1f, multiplier);
		CheckMovementSpeedChanges();
	}

	public float GetExternalMovementMultiplier()
	{
		return externalMovementMultiplier;
	}

	public void SetDashForceMultiplier(float multiplier)
	{
		dashForceMultiplier = Mathf.Max(0.1f, multiplier);
		CheckMovementSpeedChanges();
	}

	public float GetDashForceMultiplier()
	{
		return dashForceMultiplier;
	}

	public void SetDashDurationMultiplier(float multiplier)
	{
		dashDurationMultiplier = Mathf.Max(0.1f, multiplier);
		CheckMovementSpeedChanges();
	}

	public float GetDashDurationMultiplier()
	{
		return dashDurationMultiplier;
	}

	public void AddBonusDashCharges(int amount)
	{
		bonusDashCharges = Mathf.Max(0, bonusDashCharges + amount);
		CheckMovementSpeedChanges();
	}

	public void SetBaseMovementSpeeds(float walkSpeed, float crouchSpeed, float dashForce)
	{
		baseWalkSpeed = Mathf.Max(0.1f, walkSpeed);
		baseCrouchSpeed = Mathf.Max(0.1f, crouchSpeed);
		baseDashForce = Mathf.Max(0.1f, dashForce);
		CheckMovementSpeedChanges();
	}

	private void CheckMovementSpeedChanges()
	{
		MovementSpeeds currentMovementSpeeds = GetCurrentMovementSpeeds();
		if (currentMovementSpeeds.walkSpeed != lastMovementSpeeds.walkSpeed || currentMovementSpeeds.crouchSpeed != lastMovementSpeeds.crouchSpeed || currentMovementSpeeds.dashForce != lastMovementSpeeds.dashForce || currentMovementSpeeds.jumpHeight != lastMovementSpeeds.jumpHeight || currentMovementSpeeds.dashDuration != lastMovementSpeeds.dashDuration || currentMovementSpeeds.dashCooldown != lastMovementSpeeds.dashCooldown || currentMovementSpeeds.speedMultiplier != lastMovementSpeeds.speedMultiplier || currentMovementSpeeds.maxDashCharges != lastMovementSpeeds.maxDashCharges)
		{
			OnMovementSpeedChanged?.Invoke(currentMovementSpeeds.speedMultiplier);
			lastMovementSpeeds = currentMovementSpeeds;
		}
	}

	public MovementSpeeds GetCurrentMovementSpeeds()
	{
		return new MovementSpeeds
		{
			walkSpeed = WalkSpeed,
			crouchSpeed = CrouchSpeed,
			dashForce = DashForce,
			jumpHeight = JumpHeight,
			dashDuration = DashDuration,
			dashCooldown = DashCooldown,
			speedMultiplier = MovementSpeedMultiplier,
			maxDashCharges = MaxDashCharges
		};
	}

	public void TakeDamage(int damage)
	{
		if (isDead || isInvulnerable)
		{
			return;
		}
		FirstPersonController component = GetComponent<FirstPersonController>();
		if (!(component != null) || !component.invincible)
		{
			int num = 0;
			if (cachedInventory != null)
			{
				num = cachedInventory.GetTotalDefense();
			}
			if (buffDebuffSystem != null)
			{
				StatModifiers currentModifiers = buffDebuffSystem.GetCurrentModifiers();
				num = Mathf.RoundToInt((float)num * currentModifiers.defenseMultiplier) + currentModifiers.bonusDefense;
			}
			damage -= num;
			damage = Mathf.Max(1, damage);
			currentHealth -= damage;
			currentHealth = Mathf.Max(0, currentHealth);
			lastDamageTime = Time.time;
			if (audioSource != null && damageTakenSound != null)
			{
				audioSource.PlayOneShot(damageTakenSound);
			}
			targetHealthPercentage = HealthPercentage;
			OnDamageTaken?.Invoke(damage);
			OnHealthChanged?.Invoke(currentHealth, maxHealth);
			OnHealthBarUpdated?.Invoke(HealthPercentage);
			UpdateHealthBar();
			UpdateAnimationBools();
			if (currentHealth <= 0 && !isDead)
			{
				Die();
			}
		}
	}

	public void TakeRawDamage(int damage)
	{
		if (isDead || isInvulnerable)
		{
			return;
		}
		FirstPersonController component = GetComponent<FirstPersonController>();
		if (!(component != null) || !component.invincible)
		{
			damage = Mathf.Max(0, damage);
			currentHealth -= damage;
			currentHealth = Mathf.Max(0, currentHealth);
			lastDamageTime = Time.time;
			if (damage > 0 && audioSource != null && damageTakenSound != null)
			{
				audioSource.PlayOneShot(damageTakenSound);
			}
			targetHealthPercentage = HealthPercentage;
			OnDamageTaken?.Invoke(damage);
			OnHealthChanged?.Invoke(currentHealth, maxHealth);
			OnHealthBarUpdated?.Invoke(HealthPercentage);
			UpdateHealthBar();
			UpdateAnimationBools();
			if (currentHealth <= 0 && !isDead)
			{
				Die();
			}
		}
	}

	public void Heal(int amount)
	{
		if (!isDead)
		{
			int num = Mathf.Min(amount, maxHealth - currentHealth);
			if (num > 0)
			{
				currentHealth += num;
				targetHealthPercentage = HealthPercentage;
				OnHealed?.Invoke(num);
				OnHealthChanged?.Invoke(currentHealth, maxHealth);
				OnHealthBarUpdated?.Invoke(HealthPercentage);
				UpdateHealthBar();
				UpdateAnimationBools();
			}
		}
	}

	public void SetHealth(int health)
	{
		currentHealth = Mathf.Clamp(health, 0, maxHealth);
		targetHealthPercentage = HealthPercentage;
		OnHealthChanged?.Invoke(currentHealth, maxHealth);
		OnHealthBarUpdated?.Invoke(HealthPercentage);
		UpdateHealthBar();
		UpdateAnimationBools();
		if (currentHealth <= 0 && !isDead)
		{
			Die();
		}
		else if (currentHealth > 0 && isDead)
		{
			Revive();
		}
	}

	public void SetMaxHealth(int newMaxHealth, bool heal = true)
	{
		int num = maxHealth;
		int num2 = currentHealth;
		maxHealth = Mathf.Max(1, newMaxHealth);
		if ((maxHealth > num && animateMaxHealthIncrease) & heal)
		{
			if (maxHealthIncreaseCoroutine != null)
			{
				StopCoroutine(maxHealthIncreaseCoroutine);
			}
			maxHealthIncreaseCoroutine = StartCoroutine(AnimateMaxHealthIncrease(num, num2, maxHealth));
		}
		else
		{
			float num3 = (float)num2 / (float)num;
			currentHealth = Mathf.Clamp(Mathf.RoundToInt((float)maxHealth * num3), 1, maxHealth);
			targetHealthPercentage = HealthPercentage;
			currentDisplayedHealth = num2;
			OnHealthChanged?.Invoke(currentHealth, maxHealth);
			OnHealthBarUpdated?.Invoke(HealthPercentage);
			UpdateHealthBar();
		}
	}

	public void SetInvulnerable(bool invulnerable)
	{
		isInvulnerable = invulnerable;
	}

	private void HandleHealthRegeneration()
	{
		if (currentHealth < maxHealth && !(Time.time - lastDamageTime < healthRegenDelay))
		{
			float num = healthRegenRate * Time.deltaTime;
			if (num >= 1f)
			{
				Heal(Mathf.FloorToInt(num));
			}
		}
	}

	public void GenerateHeat(float amount)
	{
		float num = currentHeat;
		if (allowHeatOverflow)
		{
			currentHeat = Mathf.Min(maxHeatOverflow, currentHeat + amount);
		}
		else
		{
			currentHeat = Mathf.Min(maxHeat, currentHeat + amount);
		}
		if (currentHeat > maxHeat && !hasBeenOverheated)
		{
			hasBeenOverheated = true;
		}
		lastHeatGenerationTime = Time.time;
		targetHeatPercentage = HeatPercentage;
		float num2 = currentHeat - num;
		if (num2 > 0f)
		{
			OnHeatGenerated?.Invoke(num2);
			OnHeatChanged?.Invoke(currentHeat, maxHeat);
			OnHeatBarUpdated?.Invoke(HeatPercentage);
			UpdateHeatBar();
			UpdateAnimationBools();
		}
	}

	public void CoolHeat(float amount)
	{
		float num = currentHeat;
		currentHeat = Mathf.Max(0f, currentHeat - amount);
		targetHeatPercentage = HeatPercentage;
		float num2 = num - currentHeat;
		if (num2 > 0f)
		{
			OnHeatCooled?.Invoke(num2);
			OnHeatChanged?.Invoke(currentHeat, maxHeat);
			OnHeatBarUpdated?.Invoke(HeatPercentage);
			UpdateHeatBar();
			UpdateAnimationBools();
		}
	}

	public void ClearOverheatIfBelowMax()
	{
		if (hasBeenOverheated && currentHeat <= maxHeat)
		{
			hasBeenOverheated = false;
		}
	}

	public void SetHeat(float heat)
	{
		float max = (allowHeatOverflow ? maxHeatOverflow : maxHeat);
		currentHeat = Mathf.Clamp(heat, 0f, max);
		targetHeatPercentage = HeatPercentage;
		OnHeatChanged?.Invoke(currentHeat, maxHeat);
		OnHeatBarUpdated?.Invoke(HeatPercentage);
		UpdateHeatBar();
		UpdateAnimationBools();
	}

	private void CacheHeatOffsets()
	{
		if (!heatOffsetsInitialized)
		{
			overheatThresholdOffset = overheatingThreshold - maxHeat;
			criticalThresholdOffset = criticalHeatThreshold - maxHeat;
			heatOffsetsInitialized = true;
		}
	}

	public void SetMaxHeat(float newMaxHeat)
	{
		CacheHeatOffsets();
		maxHeat = Mathf.Max(1f, newMaxHeat);
		overheatingThreshold = Mathf.Max(0f, maxHeat + overheatThresholdOffset);
		criticalHeatThreshold = Mathf.Max(overheatingThreshold, maxHeat + criticalThresholdOffset);
		maxHeatOverflow = overheatingThreshold + 50f;
		float max = (allowHeatOverflow ? maxHeatOverflow : maxHeat);
		currentHeat = Mathf.Clamp(currentHeat, 0f, max);
		targetHeatPercentage = HeatPercentage;
		OnHeatChanged?.Invoke(currentHeat, maxHeat);
		OnHeatBarUpdated?.Invoke(HeatPercentage);
		UpdateHeatBarImmediate();
	}

	public float GetBaseWalkSpeed()
	{
		return baseWalkSpeed;
	}

	public void SetBaseWalkSpeed(float newWalkSpeed)
	{
		baseWalkSpeed = Mathf.Max(0.1f, newWalkSpeed);
		CheckMovementSpeedChanges();
	}

	public float GetBaseCrouchSpeed()
	{
		return baseCrouchSpeed;
	}

	public void SetBaseCrouchSpeed(float newCrouchSpeed)
	{
		baseCrouchSpeed = Mathf.Max(0.1f, newCrouchSpeed);
		CheckMovementSpeedChanges();
	}

	public float GetBaseDashForce()
	{
		return baseDashForce;
	}

	public void SetBaseDashForce(float newDashForce)
	{
		baseDashForce = Mathf.Max(0.1f, newDashForce);
		CheckMovementSpeedChanges();
	}

	public void SetDashDuration(float newDuration)
	{
		dashDuration = Mathf.Max(0.01f, newDuration);
		CheckMovementSpeedChanges();
	}

	public void SetDashCooldown(float newCooldown)
	{
		dashCooldown = Mathf.Max(0f, newCooldown);
		CheckMovementSpeedChanges();
	}

	public void SetJumpHeight(float newJumpHeight)
	{
		jumpHeight = Mathf.Max(0.1f, newJumpHeight);
	}

	public bool CanGenerateHeat(float amount = 0f)
	{
		if (!allowHeatOverflow)
		{
			return currentHeat + amount <= maxHeat;
		}
		return currentHeat + amount <= maxHeatOverflow;
	}

	public float GetRemainingHeatCapacity()
	{
		return (allowHeatOverflow ? maxHeatOverflow : maxHeat) - currentHeat;
	}

	public void CoolAllHeat()
	{
		SetHeat(0f);
	}

	private void HandleHeatCooldown()
	{
		if (!(currentHeat <= 0f) && !(Time.time - lastHeatGenerationTime < heatCooldownDelay))
		{
			float amount = heatCooldownRate * Time.deltaTime;
			CoolHeat(amount);
		}
	}

	private void HandleOverheatingStates()
	{
		bool isOverheating = IsOverheating;
		bool isCriticallyHot = IsCriticallyHot;
		bool isInHeatOverflow = IsInHeatOverflow;
		bool flag = hasBeenOverheated && currentHeat > 0f;
		if (isInHeatOverflow && !hasBeenOverheated)
		{
			hasBeenOverheated = true;
		}
		if (hasBeenOverheated && currentHeat <= 0f)
		{
			hasBeenOverheated = false;
		}
		if (isOverheating && !wasOverheating)
		{
			OnOverheatingStarted?.Invoke();
		}
		else if (!isOverheating && wasOverheating)
		{
			OnOverheatingStopped?.Invoke();
		}
		if (isCriticallyHot && !wasCriticallyHot)
		{
			OnCriticalHeatReached?.Invoke();
		}
		if (isInHeatOverflow && !wasInHeatOverflow)
		{
			OnHeatOverflowStarted?.Invoke();
			CheckMovementSpeedChanges();
		}
		else if (!isInHeatOverflow && wasInHeatOverflow)
		{
			OnHeatOverflowStopped?.Invoke();
		}
		if (flag && !movementWasSlowed)
		{
			OnMovementSlowed?.Invoke();
			CheckMovementSpeedChanges();
		}
		else if (!flag && movementWasSlowed)
		{
			OnMovementRestored?.Invoke();
			CheckMovementSpeedChanges();
		}
		wasOverheating = isOverheating;
		wasCriticallyHot = isCriticallyHot;
		wasInHeatOverflow = isInHeatOverflow;
		movementWasSlowed = flag;
	}

	private void UpdateHealthBar()
	{
		if (!smoothHealthBarTransition)
		{
			UpdateHealthBarImmediate();
		}
	}

	private void UpdateHealthBarImmediate()
	{
		float healthPercentage = HealthPercentage;
		currentDisplayedHealth = currentHealth;
		if (healthBarFill != null)
		{
			healthBarFill.fillAmount = healthPercentage;
		}
		if (healthSlider != null)
		{
			healthSlider.value = healthPercentage;
		}
		UpdateHealthText();
	}

	private void HandleHealthBarAnimation()
	{
		if (!smoothHealthBarTransition)
		{
			return;
		}
		if (Mathf.Abs(currentDisplayedHealth - (float)currentHealth) > 0.1f)
		{
			currentDisplayedHealth = Mathf.Lerp(currentDisplayedHealth, currentHealth, Time.unscaledDeltaTime * healthBarTransitionSpeed);
			float num = currentDisplayedHealth / (float)maxHealth;
			if (healthBarFill != null)
			{
				healthBarFill.fillAmount = num;
			}
			if (healthSlider != null)
			{
				healthSlider.value = num;
			}
			UpdateHealthText();
		}
		else
		{
			currentDisplayedHealth = currentHealth;
		}
	}

	private IEnumerator AnimateMaxHealthIncrease(int oldMaxHealth, int oldCurrentHealth, int newMaxHealth)
	{
		int healthIncrease = newMaxHealth - oldMaxHealth;
		int targetHealth = (healToFillNewHealth ? newMaxHealth : oldCurrentHealth);
		bool originalSmoothTransition = smoothHealthBarTransition;
		smoothHealthBarTransition = false;
		float elapsed = 0f;
		while (elapsed < maxHealthIncreaseAnimationDuration)
		{
			elapsed += Time.deltaTime;
			float time = elapsed / maxHealthIncreaseAnimationDuration;
			float t = maxHealthIncreaseCurve.Evaluate(time);
			currentHealth = Mathf.RoundToInt(Mathf.Lerp(oldCurrentHealth, targetHealth, t));
			float num = (float)currentHealth / (float)newMaxHealth;
			if (healthBarFill != null)
			{
				healthBarFill.fillAmount = num;
			}
			if (healthSlider != null)
			{
				healthSlider.value = num;
			}
			UpdateHealthText();
			OnHealthChanged?.Invoke(currentHealth, newMaxHealth);
			OnHealthBarUpdated?.Invoke(num);
			yield return null;
		}
		currentHealth = targetHealth;
		currentDisplayedHealth = currentHealth;
		targetHealthPercentage = HealthPercentage;
		if (healthBarFill != null)
		{
			healthBarFill.fillAmount = HealthPercentage;
		}
		if (healthSlider != null)
		{
			healthSlider.value = HealthPercentage;
		}
		UpdateHealthText();
		OnHealthChanged?.Invoke(currentHealth, maxHealth);
		OnHealthBarUpdated?.Invoke(HealthPercentage);
		if (healToFillNewHealth && healthIncrease > 0)
		{
			OnHealed?.Invoke(healthIncrease);
		}
		smoothHealthBarTransition = originalSmoothTransition;
		maxHealthIncreaseCoroutine = null;
	}

	private void UpdateHeatBar()
	{
		if (!smoothHeatBarTransition)
		{
			UpdateHeatBarImmediate();
		}
	}

	private void UpdateHeatBarImmediate()
	{
		float heatPercentage = HeatPercentage;
		currentDisplayedHeat = currentHeat;
		if (heatBarFill != null)
		{
			heatBarFill.fillAmount = Mathf.Min(1f, heatPercentage);
			if (IsInHeatOverflow)
			{
				heatBarFill.color = Color.red;
			}
			else if (IsCriticallyHot)
			{
				heatBarFill.color = criticalHeatColor;
			}
			else if (IsOverheating)
			{
				heatBarFill.color = warningHeatColor;
			}
			else
			{
				heatBarFill.color = normalHeatColor;
			}
		}
		if (heatSlider != null)
		{
			heatSlider.value = Mathf.Min(1f, heatPercentage);
		}
		UpdateHeatText();
	}

	private void HandleHeatBarAnimation()
	{
		if (!smoothHeatBarTransition)
		{
			return;
		}
		if (Mathf.Abs(currentDisplayedHeat - currentHeat) > 0.01f)
		{
			currentDisplayedHeat = Mathf.SmoothDamp(currentDisplayedHeat, currentHeat, ref heatBarVelocity, 1f / heatBarTransitionSpeed, float.PositiveInfinity, Time.unscaledDeltaTime);
			float b = currentDisplayedHeat / maxHeat;
			if (heatBarFill != null)
			{
				heatBarFill.fillAmount = Mathf.Min(1f, b);
				if (currentDisplayedHeat > maxHeat)
				{
					heatBarFill.color = Color.red;
				}
				else if (currentDisplayedHeat >= criticalHeatThreshold)
				{
					heatBarFill.color = criticalHeatColor;
				}
				else if (currentDisplayedHeat >= overheatingThreshold)
				{
					heatBarFill.color = warningHeatColor;
				}
				else
				{
					heatBarFill.color = normalHeatColor;
				}
			}
			if (heatSlider != null)
			{
				heatSlider.value = Mathf.Min(1f, b);
			}
			UpdateHeatText();
		}
		else
		{
			currentDisplayedHeat = currentHeat;
			heatBarVelocity = 0f;
		}
	}

	public void SetHealthBarReferences(Image fillImage, TextMeshProUGUI textDisplay = null, Slider slider = null)
	{
		healthBarFill = fillImage;
		healthText = textDisplay;
		healthSlider = slider;
		UpdateHealthBarImmediate();
	}

	public void SetHeatBarReferences(Image fillImage, TextMeshProUGUI textDisplay = null, Slider slider = null)
	{
		heatBarFill = fillImage;
		heatText = textDisplay;
		heatSlider = slider;
		UpdateHeatBarImmediate();
	}

	private void UpdateHealthText()
	{
		if (healthText != null && (currentHealth != lastDisplayedHealthValue || maxHealth != lastDisplayedMaxHealthValue))
		{
			lastDisplayedHealthValue = currentHealth;
			lastDisplayedMaxHealthValue = maxHealth;
			healthText.text = $"{currentHealth}/{maxHealth}";
		}
	}

	private void UpdateHeatText()
	{
		if (heatText != null)
		{
			int num = Mathf.RoundToInt(currentHeat);
			int num2 = Mathf.RoundToInt(maxHeat);
			bool isInHeatOverflow = IsInHeatOverflow;
			if (num != lastDisplayedHeatInt || num2 != lastDisplayedMaxHeatInt || isInHeatOverflow != lastDisplayedOverflow)
			{
				lastDisplayedHeatInt = num;
				lastDisplayedMaxHeatInt = num2;
				lastDisplayedOverflow = isInHeatOverflow;
				heatText.text = $"{currentHeat:F0}/{maxHeat:F0}";
			}
		}
	}

	public void SetSmoothTransitions(bool healthSmooth, bool heatSmooth, float healthSpeed = 5f, float heatSpeed = 8f)
	{
		smoothHealthBarTransition = healthSmooth;
		smoothHeatBarTransition = heatSmooth;
		healthBarTransitionSpeed = healthSpeed;
		heatBarTransitionSpeed = heatSpeed;
	}

	private void UpdateAnimationBools()
	{
		if (healthAnimator != null)
		{
			healthAnimator.SetBool("IsHealthFull", IsAtFullHealth());
		}
		if (heatAnimator != null)
		{
			heatAnimator.SetBool("HasAnyHeat", currentHeat > 0f);
		}
	}

	private void Die()
	{
		if (!isDead)
		{
			if (TrinketManager.Instance != null && TrinketManager.Instance.TryConsumeRevive(out var healthFraction))
			{
				currentHealth = Mathf.Max(1, Mathf.RoundToInt((float)maxHealth * healthFraction));
				SetHeat(0f);
				UpdateHealthBar();
			}
			else
			{
				isDead = true;
				UpdateHealthBar();
				OnPlayerDeath?.Invoke();
			}
		}
	}

	public void Revive(int healthAmount = -1)
	{
		if (isDead)
		{
			isDead = false;
			if (healthAmount < 0)
			{
				healthAmount = maxHealth / 2;
			}
			SetHealth(healthAmount);
			SetHeat(0f);
			externalMovementMultiplier = 1f;
			CheckMovementSpeedChanges();
		}
	}

	public void RestoreToFull()
	{
		SetHealth(maxHealth);
		SetHeat(0f);
	}

	public string GetHealthString()
	{
		return $"HP: {currentHealth}/{maxHealth} ({HealthPercentage:P0})";
	}

	public string GetHeatString()
	{
		string text = $"Heat: {currentHeat:F1}/{maxHeat}";
		if (IsInHeatOverflow)
		{
			text += $" (Overflow: {maxHeatOverflow}) OVERFLOW!";
		}
		return text + $" ({OverflowHeatPercentage:P0})";
	}

	public string GetStatusString()
	{
		string text = GetHealthString() + " | " + GetHeatString();
		if (IsAttackLocked())
		{
			text += " | ATTACK LOCKED & MOVEMENT SLOWED";
		}
		else if (IsOverheating)
		{
			text += " | OVERHEATING";
		}
		return text + $" | Speed: {MovementSpeedMultiplier:P0}";
	}

	public PlayerStatsData GetSaveData()
	{
		return new PlayerStatsData
		{
			currentHealth = currentHealth,
			maxHealth = maxHealth,
			currentHeat = currentHeat,
			maxHeat = maxHeat,
			maxHeatOverflow = maxHeatOverflow,
			baseWalkSpeed = baseWalkSpeed,
			baseCrouchSpeed = baseCrouchSpeed,
			baseDashForce = baseDashForce,
			jumpHeight = jumpHeight,
			dashDuration = dashDuration,
			dashCooldown = dashCooldown,
			bonusDashCharges = bonusDashCharges,
			overheatingThreshold = overheatingThreshold,
			criticalHeatThreshold = criticalHeatThreshold,
			healthRegenRate = healthRegenRate,
			heatCooldownRate = heatCooldownRate,
			externalMovementMultiplier = externalMovementMultiplier
		};
	}

	public void LoadSaveData(PlayerStatsData data)
	{
		maxHealth = data.maxHealth;
		currentHealth = data.currentHealth;
		maxHeat = data.maxHeat;
		currentHeat = data.currentHeat;
		maxHeatOverflow = data.maxHeatOverflow;
		baseWalkSpeed = data.baseWalkSpeed;
		baseCrouchSpeed = data.baseCrouchSpeed;
		baseDashForce = data.baseDashForce;
		jumpHeight = data.jumpHeight;
		dashDuration = data.dashDuration;
		dashCooldown = data.dashCooldown;
		bonusDashCharges = data.bonusDashCharges;
		overheatingThreshold = data.overheatingThreshold;
		criticalHeatThreshold = data.criticalHeatThreshold;
		healthRegenRate = data.healthRegenRate;
		heatCooldownRate = data.heatCooldownRate;
		heatOffsetsInitialized = false;
		CacheHeatOffsets();
		hasBeenOverheated = false;
		externalMovementMultiplier = ((data.externalMovementMultiplier > 0.01f) ? data.externalMovementMultiplier : 1f);
		targetHealthPercentage = HealthPercentage;
		targetHeatPercentage = OverflowHeatPercentage;
		OnHealthChanged?.Invoke(currentHealth, maxHealth);
		OnHeatChanged?.Invoke(currentHeat, maxHeat);
		OnHealthBarUpdated?.Invoke(HealthPercentage);
		OnHeatBarUpdated?.Invoke(HeatPercentage);
		UpdateHealthBarImmediate();
		UpdateHeatBarImmediate();
		CheckMovementSpeedChanges();
		if (currentHealth > 0)
		{
			isDead = false;
		}
	}

	public bool IsAtFullHealth()
	{
		if (Mathf.Abs(currentDisplayedHealth - (float)maxHealth) < 1f)
		{
			return currentHealth >= maxHealth;
		}
		return false;
	}

	public bool IsCriticallyInjured()
	{
		return HealthPercentage <= 0.25f;
	}

	public int GetRemainingHealth()
	{
		return maxHealth - currentHealth;
	}

	public void SetHealthRegenRate(float regenRate)
	{
		healthRegenRate = Mathf.Max(0f, regenRate);
	}

	public float GetHealthRegenRate()
	{
		return healthRegenRate;
	}

	public void SetHealthRegenDelay(float delay)
	{
		healthRegenDelay = Mathf.Max(0f, delay);
	}

	public void SetHealthRegenEnabled(bool enabled)
	{
		healthRegenRate = (enabled ? Mathf.Abs(healthRegenRate) : 0f);
	}

	public void SetHeatCooldownRate(float cooldownRate)
	{
		heatCooldownRate = Mathf.Max(1f, cooldownRate);
	}

	public float GetHeatCooldownRate()
	{
		return heatCooldownRate;
	}

	public void SetHeatCooldownDelay(float delay)
	{
		heatCooldownDelay = Mathf.Max(0f, delay);
	}

	public void SetHeatCooldownEnabled(bool enabled)
	{
		heatCooldownRate = (enabled ? Mathf.Abs(heatCooldownRate) : 0f);
	}

	public void SetHeatThresholds(float overheatingThreshold, float criticalThreshold, float maxOverflow = 150f)
	{
		this.overheatingThreshold = Mathf.Clamp(overheatingThreshold, 0f, maxHeat);
		criticalHeatThreshold = Mathf.Clamp(criticalThreshold, overheatingThreshold, maxHeat);
		maxHeatOverflow = Mathf.Max(maxHeat, maxOverflow);
		heatOffsetsInitialized = false;
		CacheHeatOffsets();
	}

	public void SetHeatOverflowSettings(bool allowOverflow, float maxOverflow, float movementPenalty)
	{
		allowHeatOverflow = allowOverflow;
		maxHeatOverflow = Mathf.Max(maxHeat, maxOverflow);
		overflowMovementPenalty = Mathf.Clamp01(movementPenalty);
	}

	public float GetCurrentMovementMultiplier()
	{
		return MovementSpeedMultiplier;
	}

	public bool CanAttackNow()
	{
		if (hasBeenOverheated)
		{
			return currentHeat <= 0f;
		}
		return true;
	}

	public float GetTimeUntilCanAttack()
	{
		if (CanAttackNow())
		{
			return 0f;
		}
		if (heatCooldownRate <= 0f)
		{
			return float.MaxValue;
		}
		return currentHeat / heatCooldownRate;
	}

	public bool IsAttackLocked()
	{
		if (hasBeenOverheated)
		{
			return currentHeat > 0f;
		}
		return false;
	}

	public string GetAttackRestrictionReason()
	{
		if (CanAttackNow())
		{
			return "";
		}
		if (hasBeenOverheated)
		{
			return $"Overheated! Cool to 0 to attack again ({GetTimeUntilCanAttack():F1}s)";
		}
		return "Cannot attack";
	}

	private void OnValidate()
	{
		if (Application.isPlaying)
		{
			if (currentHealth > maxHealth)
			{
				currentHealth = maxHealth;
			}
			if (currentHeat < 0f)
			{
				currentHeat = 0f;
			}
			OnMovementSpeedChanged?.Invoke(MovementSpeedMultiplier);
			lastMovementSpeeds = GetCurrentMovementSpeeds();
			if (healthBarFill != null || healthSlider != null)
			{
				UpdateHealthBarImmediate();
			}
			if (heatBarFill != null || heatSlider != null)
			{
				UpdateHeatBarImmediate();
			}
			OnHealthChanged?.Invoke(currentHealth, maxHealth);
			OnHeatChanged?.Invoke(currentHeat, maxHeat);
		}
	}
}
