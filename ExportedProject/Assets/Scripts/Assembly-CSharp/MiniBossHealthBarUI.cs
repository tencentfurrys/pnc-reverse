using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class MiniBossHealthBarUI : MonoBehaviour
{
	private static readonly List<EnemyAI> pendingRegistrations = new List<EnemyAI>();

	[Header("Scene References")]
	[Tooltip("Animator on this object running the parameterless \"Miniboss Healthbar\" controller. Auto-found on this GameObject if left blank.")]
	[SerializeField]
	private Animator barAnimator;

	[Tooltip("The \"Health\" image under HealthContainer. MUST be Image Type = Filled, Fill Method = Horizontal, Fill Origin = Left.")]
	[SerializeField]
	private Image fillImage;

	[Tooltip("The \"Health Number\" label. Shows \"current / max\", e.g. 112 / 150.")]
	[SerializeField]
	private TMP_Text healthNumberText;

	[Tooltip("The \"Enemy Name\" label. Shows the boss's display name, swapped on stage 2.")]
	[SerializeField]
	private TMP_Text enemyNameText;

	[Header("Fill Animation")]
	[Tooltip("Exponential lerp speed the fill chases real health with during normal combat. Matches the EnemyHealthBar feel.")]
	[SerializeField]
	private float fillLerpSpeed = 8f;

	[Tooltip("Fill snaps to its target once within this distance, so it lands instead of asymptoting.")]
	[SerializeField]
	private float fillSnapThreshold = 0.005f;

	[Header("Stage 2 Transition")]
	[Tooltip("Seconds the bar takes to visually drain to empty when the boss transforms.")]
	[SerializeField]
	private float stageDrainDuration = 0.6f;

	[Tooltip("Seconds the bar sits empty (with the new name shown) between draining and refilling.")]
	[SerializeField]
	private float stageHoldDuration = 0.25f;

	[Tooltip("Seconds the bar takes to refill to the stage-2 max health.")]
	[SerializeField]
	private float stageRefillDuration = 0.9f;

	[Header("Death / Hide")]
	[Tooltip("Seconds the bar takes to drain to empty after the boss dies, before it fades out.")]
	[SerializeField]
	private float deathDrainDuration = 0.35f;

	[Tooltip("Extra seconds the empty bar lingers after a death drain before playing \"Hiding\".")]
	[SerializeField]
	private float hideDelay = 0.5f;

	[Header("Time")]
	[Tooltip("Use unscaled time for the lerps and force the animator to unscaled update, so the bar still animates while the game is time-scaled or paused.")]
	[SerializeField]
	private bool useUnscaledTime = true;

	private static readonly int ShowingHash = Animator.StringToHash("Showing");

	private static readonly int HidingHash = Animator.StringToHash("Hiding");

	private static readonly int HiddenHash = Animator.StringToHash("Hidden");

	private readonly List<EnemyAI> trackedBosses = new List<EnemyAI>();

	private EnemyAI currentBoss;

	private float displayedFill = 1f;

	private int displayedMaxHealth = 1;

	private int lastShownCurrent = -1;

	private int lastShownMax = -1;

	private bool isVisible;

	private Coroutine activeRoutine;

	private bool scriptedFill;

	public static MiniBossHealthBarUI Instance { get; private set; }

	private float Dt
	{
		get
		{
			if (!useUnscaledTime)
			{
				return Time.deltaTime;
			}
			return Time.unscaledDeltaTime;
		}
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStatics()
	{
		Instance = null;
		pendingRegistrations.Clear();
	}

	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Debug.LogWarning("[MiniBossHealthBarUI] Duplicate instance; disabling this one.", this);
			base.enabled = false;
			return;
		}
		Instance = this;
		if (barAnimator == null)
		{
			barAnimator = GetComponent<Animator>();
		}
		if (barAnimator != null)
		{
			barAnimator.updateMode = (useUnscaledTime ? AnimatorUpdateMode.UnscaledTime : AnimatorUpdateMode.Normal);
			barAnimator.Play(HiddenHash, 0, 0f);
			barAnimator.Update(0f);
		}
		isVisible = false;
		displayedFill = 1f;
		if (fillImage != null)
		{
			fillImage.fillAmount = 1f;
		}
		for (int i = 0; i < pendingRegistrations.Count; i++)
		{
			RegisterInternal(pendingRegistrations[i]);
		}
		pendingRegistrations.Clear();
	}

	private void OnDestroy()
	{
		if (Instance == this)
		{
			Instance = null;
		}
		trackedBosses.Clear();
		currentBoss = null;
	}

	public static void Register(EnemyAI boss)
	{
		if (!(boss == null))
		{
			if (Instance != null)
			{
				Instance.RegisterInternal(boss);
			}
			else if (!pendingRegistrations.Contains(boss))
			{
				pendingRegistrations.Add(boss);
			}
		}
	}

	public static void Unregister(EnemyAI boss)
	{
		if (!(boss == null))
		{
			pendingRegistrations.Remove(boss);
			if (Instance != null)
			{
				Instance.UnregisterInternal(boss);
			}
		}
	}

	public static void NotifyStageChanged(EnemyAI boss, int previousMaxHealth)
	{
		if (Instance != null)
		{
			Instance.HandleStageChanged(boss, previousMaxHealth);
		}
	}

	private void RegisterInternal(EnemyAI boss)
	{
		if (!(boss == null) && !trackedBosses.Contains(boss))
		{
			trackedBosses.Add(boss);
			StopActiveRoutine();
			AdoptBoss(boss, snapFill: true);
			Show();
		}
	}

	private void UnregisterInternal(EnemyAI boss)
	{
		if (boss == null)
		{
			return;
		}
		bool num = boss == currentBoss;
		trackedBosses.Remove(boss);
		if (!num)
		{
			return;
		}
		currentBoss = null;
		for (int num2 = trackedBosses.Count - 1; num2 >= 0; num2--)
		{
			if (trackedBosses[num2] == null)
			{
				trackedBosses.RemoveAt(num2);
			}
		}
		StopActiveRoutine();
		if (trackedBosses.Count > 0)
		{
			AdoptBoss(trackedBosses[trackedBosses.Count - 1], snapFill: true);
		}
		else
		{
			activeRoutine = StartCoroutine(DeathHideRoutine());
		}
	}

	private void AdoptBoss(EnemyAI boss, bool snapFill)
	{
		currentBoss = boss;
		displayedMaxHealth = Mathf.Max(1, boss.maxHealth);
		if (snapFill)
		{
			displayedFill = Mathf.Clamp01(boss.HealthPercentage);
		}
		ApplyFill();
		ApplyName(boss);
		SetNumberText(boss.currentHealth, boss.maxHealth);
	}

	public void Show()
	{
		if (!isVisible)
		{
			isVisible = true;
			if (barAnimator != null)
			{
				barAnimator.Play(ShowingHash, 0, 0f);
			}
		}
	}

	public void Hide()
	{
		if (isVisible)
		{
			isVisible = false;
			if (barAnimator != null)
			{
				barAnimator.Play(HidingHash, 0, 0f);
			}
		}
	}

	public void ForceHide()
	{
		StopActiveRoutine();
		trackedBosses.Clear();
		currentBoss = null;
		Hide();
	}

	private void LateUpdate()
	{
		if (scriptedFill || currentBoss == null)
		{
			return;
		}
		int num = Mathf.Max(0, currentBoss.currentHealth);
		int num2 = (displayedMaxHealth = Mathf.Max(1, currentBoss.maxHealth));
		float num3 = Mathf.Clamp01((float)num / (float)num2);
		if (!Mathf.Approximately(displayedFill, num3))
		{
			displayedFill = Mathf.Lerp(displayedFill, num3, Dt * fillLerpSpeed);
			if (Mathf.Abs(displayedFill - num3) < fillSnapThreshold)
			{
				displayedFill = num3;
			}
			ApplyFill();
		}
		SetNumberText(num, num2);
	}

	private void HandleStageChanged(EnemyAI boss, int previousMaxHealth)
	{
		if (!(boss == null) && !(boss != currentBoss))
		{
			int oldMax = ((previousMaxHealth > 0) ? previousMaxHealth : Mathf.Max(1, displayedMaxHealth));
			StopActiveRoutine();
			Show();
			activeRoutine = StartCoroutine(StageTransitionRoutine(boss, oldMax));
		}
	}

	private IEnumerator StageTransitionRoutine(EnemyAI boss, int oldMax)
	{
		scriptedFill = true;
		float startFill = displayedFill;
		float t = 0f;
		float drain = Mathf.Max(0.01f, stageDrainDuration);
		while (t < drain)
		{
			t += Dt;
			displayedFill = Mathf.Lerp(startFill, 0f, Mathf.Clamp01(t / drain));
			ApplyFill();
			SetNumberText(Mathf.CeilToInt(displayedFill * (float)oldMax), oldMax);
			yield return null;
		}
		displayedFill = 0f;
		ApplyFill();
		SetNumberText(0, oldMax);
		ApplyName(boss);
		float hold = 0f;
		while (hold < stageHoldDuration)
		{
			hold += Dt;
			yield return null;
		}
		if (boss == null)
		{
			activeRoutine = null;
			scriptedFill = false;
			yield break;
		}
		int newMax = (displayedMaxHealth = Mathf.Max(1, boss.maxHealth));
		float targetFill = Mathf.Clamp01(boss.HealthPercentage);
		t = 0f;
		float refill = Mathf.Max(0.01f, stageRefillDuration);
		while (t < refill)
		{
			t += Dt;
			displayedFill = Mathf.Lerp(0f, targetFill, Mathf.Clamp01(t / refill));
			ApplyFill();
			SetNumberText(Mathf.CeilToInt(displayedFill * (float)newMax), newMax);
			yield return null;
		}
		displayedFill = targetFill;
		ApplyFill();
		activeRoutine = null;
		scriptedFill = false;
	}

	private IEnumerator DeathHideRoutine()
	{
		scriptedFill = true;
		int max = Mathf.Max(1, displayedMaxHealth);
		float startFill = displayedFill;
		float t = 0f;
		float drain = Mathf.Max(0.01f, deathDrainDuration);
		while (t < drain)
		{
			t += Dt;
			displayedFill = Mathf.Lerp(startFill, 0f, Mathf.Clamp01(t / drain));
			ApplyFill();
			SetNumberText(Mathf.CeilToInt(displayedFill * (float)max), max);
			yield return null;
		}
		displayedFill = 0f;
		ApplyFill();
		SetNumberText(0, max);
		float wait = 0f;
		while (wait < hideDelay)
		{
			wait += Dt;
			yield return null;
		}
		Hide();
		activeRoutine = null;
		scriptedFill = false;
	}

	private void StopActiveRoutine()
	{
		if (activeRoutine != null)
		{
			StopCoroutine(activeRoutine);
		}
		activeRoutine = null;
		scriptedFill = false;
	}

	private void ApplyFill()
	{
		if (fillImage != null)
		{
			fillImage.fillAmount = Mathf.Clamp01(displayedFill);
		}
	}

	private void ApplyName(EnemyAI boss)
	{
		if (!(enemyNameText == null) && !(boss == null))
		{
			enemyNameText.text = boss.GetMiniBossDisplayName();
		}
	}

	private void SetNumberText(int current, int max)
	{
		if (!(healthNumberText == null))
		{
			current = Mathf.Max(0, current);
			if (current != lastShownCurrent || max != lastShownMax)
			{
				lastShownCurrent = current;
				lastShownMax = max;
				healthNumberText.text = current + " / " + max;
			}
		}
	}
}
