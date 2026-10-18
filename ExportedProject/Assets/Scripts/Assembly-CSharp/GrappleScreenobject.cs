using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using PixelCrushers.GridController;
using UnityEngine;
using UnityEngine.UI;

public class GrappleScreenobject : MonoBehaviour
{
	[Header("Grapple Settings")]
	[SerializeField]
	private float heatIncreaseRate = 15f;

	[SerializeField]
	private float movementSpeedReduction = 0.67f;

	[SerializeField]
	private int maxGrappleCount = 3;

	[SerializeField]
	private bool threeEnemiesStopsMovement = true;

	[Header("UI Elements")]
	[SerializeField]
	private GameObject grappleUI;

	[SerializeField]
	private GameObject grappleImage;

	[SerializeField]
	private Text grappleText;

	[SerializeField]
	private Button struggleButton;

	[Header("Animation")]
	[Tooltip("The controller, state names and loop sounds come from the grappling enemy's ChargingEnemyAI.")]
	[SerializeField]
	private Animator animator;

	[Header("Visual Effects")]
	[SerializeField]
	private GameObject grappleEffect;

	[SerializeField]
	private AudioClip grappleStartSound;

	[SerializeField]
	private AudioClip grappleEndSound;

	[Header("Mouse Shake Escape")]
	[SerializeField]
	private int baseShakesToEscape = 4;

	[Tooltip("Flips per grappler required on a gamepad. Lower than the mouse's baseShakesToEscape: a stick has to physically travel between the thresholds against its spring, whereas the mouse path clears minShakeDelta (1.5 raw pixels) on almost every frame of a jiggle.")]
	[SerializeField]
	private int gamepadShakesToEscape = 2;

	[SerializeField]
	private float minShakeDelta = 1.5f;

	[Tooltip("Stick deflection that counts as a shake. Read from rightStick/x, which bypasses the stick's deadzone processor, so keep this well clear of resting drift.")]
	[SerializeField]
	private float stickShakeThreshold = 0.4f;

	[SerializeField]
	private Image shakeProgressFill;

	[Header("Trio Grab Overflow")]
	[Tooltip("When maxGrappleCount grapple enemies are attached AND the player reaches max heat, clear the grapple and escalate to a GrabScreen trio scene. The scene itself is configured on the enemy.")]
	[SerializeField]
	private bool enableTrioGrabOverflow = true;

	private bool isGrappling;

	private int grappleCount;

	private readonly List<GameObject> grapplingEnemies = new List<GameObject>();

	private PlayerStats playerStats;

	private AudioSource audioSource;

	private float originalMovementMultiplier;

	private bool isDashingOverride;

	private ChargingEnemyAI activeGrappleSource;

	private RuntimeAnimatorController activeGrappleController;

	private RuntimeAnimatorController originalController;

	[Header("Shake-Off Throw")]
	[Tooltip("Where a grappled enemy is stored relative to the player, and where a thrown one launches from.")]
	[SerializeField]
	private Vector3 storedEnemyOffset = Vector3.zero;

	[Tooltip("How far along the throw direction the enemy is placed when released. Stops its collider reactivating inside the player, where depenetration would fight the throw.")]
	[SerializeField]
	private float releaseClearance = 1.2f;

	private Camera throwCamera;

	private float shakeProgress;

	private int lastShakeDirection;

	private float accumulatedMouseX;

	public static GrappleScreenobject Instance { get; private set; }

	private int ShakesRequired => Mathf.Max(1, (InputDeviceTracker.UsingGamepad ? gamepadShakesToEscape : baseShakesToEscape) * grappleCount);

	public bool IsGrappling => isGrappling;

	public int GrappleCount => grappleCount;

	private void Awake()
	{
		Instance = this;
		GameObject gameObject = GameObject.FindGameObjectWithTag("Player");
		if (gameObject != null)
		{
			playerStats = gameObject.GetComponent<PlayerStats>();
		}
		throwCamera = Camera.main;
		audioSource = GetComponent<AudioSource>();
		if (audioSource == null)
		{
			audioSource = base.gameObject.AddComponent<AudioSource>();
		}
		if (grappleUI != null)
		{
			grappleUI.SetActive(value: false);
		}
		if (grappleImage != null)
		{
			grappleImage.SetActive(value: false);
		}
	}

	private void Update()
	{
		if (isGrappling)
		{
			UpdateGrapple();
			TrackShake();
		}
	}

	public bool StartGrapple(GameObject enemy)
	{
		if (playerStats == null || !playerStats.CanBeGrabbed)
		{
			return false;
		}
		if (grappleCount >= maxGrappleCount)
		{
			return false;
		}
		ChargingEnemyAI component = enemy.GetComponent<ChargingEnemyAI>();
		if (component == null || component.grappleAnimatorController == null)
		{
			return false;
		}
		if (isGrappling && activeGrappleController != component.grappleAnimatorController)
		{
			return false;
		}
		GridInventorySystem gridInventorySystem = Object.FindAnyObjectByType<GridInventorySystem>();
		if (gridInventorySystem != null)
		{
			int totalThornsDamage = gridInventorySystem.GetTotalThornsDamage();
			if (totalThornsDamage > 0)
			{
				enemy.GetComponent<IDamageable>()?.TakeDamage(totalThornsDamage);
			}
		}
		EnemyAI component2 = enemy.GetComponent<EnemyAI>();
		if (component.currentHealth <= 0 || (component2 != null && component2.currentHealth <= 0))
		{
			return false;
		}
		grappleCount++;
		grapplingEnemies.Add(enemy);
		LevelStatsTracker.RecordGrab();
		UnlockGalleryEntry(enemy);
		if (!isGrappling)
		{
			InitializeGrapple(component);
		}
		ForceStopDash();
		PlayGrappleAnimation();
		UpdateMovementSpeed();
		UpdateGrappleUI();
		UpdateGrappleLoopSound();
		if (grappleStartSound != null && audioSource != null)
		{
			audioSource.PlayOneShot(grappleStartSound);
		}
		if (grappleEffect != null && playerStats != null)
		{
			Object.Instantiate(grappleEffect, playerStats.transform.position, Quaternion.identity);
		}
		return true;
	}

	private void InitializeGrapple(ChargingEnemyAI source)
	{
		isGrappling = true;
		ResetShakeState();
		activeGrappleSource = source;
		activeGrappleController = source.grappleAnimatorController;
		if (animator != null)
		{
			originalController = animator.runtimeAnimatorController;
			if (originalController != activeGrappleController)
			{
				animator.runtimeAnimatorController = activeGrappleController;
			}
		}
		originalMovementMultiplier = playerStats.GetExternalMovementMultiplier();
		UpdateMovementSpeed();
		if (grappleUI != null)
		{
			grappleUI.SetActive(value: true);
		}
		if (grappleImage != null)
		{
			grappleImage.SetActive(value: true);
		}
		UpdateGrappleLoopSound();
	}

	private void PlayGrappleAnimation()
	{
		if (!(animator == null) && !(activeGrappleSource == null))
		{
			string text = grappleCount switch
			{
				1 => activeGrappleSource.grappleOneAnimation, 
				2 => activeGrappleSource.grappleTwoAnimation, 
				3 => activeGrappleSource.grappleThreeAnimation, 
				_ => null, 
			};
			if (!string.IsNullOrEmpty(text))
			{
				animator.Play(text);
			}
		}
	}

	private void UpdateGrappleLoopSound()
	{
		if (!(audioSource == null) && !(activeGrappleSource == null))
		{
			AudioClip audioClip = grappleCount switch
			{
				1 => activeGrappleSource.grappleLoopOne, 
				2 => activeGrappleSource.grappleLoopTwo, 
				3 => activeGrappleSource.grappleLoopThree, 
				_ => null, 
			};
			if (audioClip == null)
			{
				audioSource.Stop();
			}
			else if (audioSource.clip != audioClip || !audioSource.isPlaying)
			{
				audioSource.clip = audioClip;
				audioSource.loop = true;
				audioSource.Play();
			}
		}
	}

	private void UpdateMovementSpeed()
	{
		if (!(playerStats == null))
		{
			if (isDashingOverride)
			{
				playerStats.SetExternalMovementMultiplier(originalMovementMultiplier);
			}
			else if (grappleCount == 3)
			{
				playerStats.SetExternalMovementMultiplier(0f);
			}
			else if (grappleCount > 0)
			{
				playerStats.SetExternalMovementMultiplier(originalMovementMultiplier * movementSpeedReduction);
			}
			else
			{
				playerStats.SetExternalMovementMultiplier(originalMovementMultiplier);
			}
		}
	}

	private void UpdateGrappleUI()
	{
		if (grappleText != null)
		{
			if (grappleCount == 1)
			{
				grappleText.text = "Being Grappled! Shake mouse to escape!";
			}
			else if (grappleCount == 3)
			{
				grappleText.text = "IMMOBILIZED by 3 enemies! Shake mouse to escape!";
			}
			else
			{
				grappleText.text = $"Being Grappled by {grappleCount} enemies! Shake mouse to escape!";
			}
		}
	}

	private void UpdateGrapple()
	{
		if (isGrappling)
		{
			if (playerStats != null)
			{
				float amount = heatIncreaseRate * (float)grappleCount * Time.deltaTime;
				playerStats.GenerateHeat(amount);
			}
			ParkGrapplersOnPlayer();
			CheckTrioGrabOverflow();
		}
	}

	private void ParkGrapplersOnPlayer()
	{
		if (playerStats == null)
		{
			return;
		}
		Vector3 position = playerStats.transform.position + storedEnemyOffset;
		for (int i = 0; i < grapplingEnemies.Count; i++)
		{
			if (grapplingEnemies[i] != null)
			{
				grapplingEnemies[i].transform.position = position;
			}
		}
	}

	private void CheckTrioGrabOverflow()
	{
		if (enableTrioGrabOverflow && grappleCount >= maxGrappleCount && !(playerStats == null) && !(playerStats.CurrentHeat < playerStats.MaxHeat) && !(GrabScreen.Instance == null) && !GrabScreen.Instance.IsGrabbed && !(activeGrappleSource == null) && !(activeGrappleSource.trioGrabAnimatorController == null))
		{
			TriggerTrioGrabOverflow();
		}
	}

	private void TriggerTrioGrabOverflow()
	{
		List<GameObject> list = new List<GameObject>(grapplingEnemies);
		grapplingEnemies.Clear();
		GameObject gameObject = null;
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i] != null)
			{
				gameObject = list[i];
				break;
			}
		}
		if (gameObject == null)
		{
			return;
		}
		ChargingEnemyAI component = gameObject.GetComponent<ChargingEnemyAI>();
		if (component == null)
		{
			return;
		}
		RuntimeAnimatorController trioGrabAnimatorController = component.trioGrabAnimatorController;
		Vector3 trioGrabCameraPositionOffset = component.trioGrabCameraPositionOffset;
		Vector3 trioGrabCameraRotationOffset = component.trioGrabCameraRotationOffset;
		GrabAnimationData[] grabScreenAnimations = component.grabScreenAnimations;
		SilenceForGrab(gameObject);
		gameObject.SetActive(value: true);
		if (playerStats != null)
		{
			playerStats.SetExternalMovementMultiplier(originalMovementMultiplier);
		}
		if (audioSource != null)
		{
			audioSource.loop = false;
			audioSource.Stop();
		}
		GrabScreen.Instance.StartGrab(gameObject, trioGrabAnimatorController, trioGrabCameraPositionOffset, trioGrabCameraRotationOffset, keepPlayerRotation: false, grabScreenAnimations);
		if (!GrabScreen.Instance.IsGrabbed)
		{
			for (int j = 0; j < list.Count; j++)
			{
				if (list[j] != null)
				{
					Object.Destroy(list[j]);
				}
			}
			EndGrapple();
			return;
		}
		EndGrapple(restoreMovement: false, playEndSound: false);
		for (int k = 0; k < list.Count; k++)
		{
			if (list[k] != null && list[k] != gameObject)
			{
				Object.Destroy(list[k]);
			}
		}
		StartCoroutine(DestroyRepresentativeAfterGrab(gameObject));
	}

	private void SilenceForGrab(GameObject enemy)
	{
		if (!(enemy == null))
		{
			ChargingEnemyAI component = enemy.GetComponent<ChargingEnemyAI>();
			if (component != null)
			{
				component.enabled = false;
			}
			Collider[] componentsInChildren = enemy.GetComponentsInChildren<Collider>(includeInactive: true);
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].enabled = false;
			}
			Renderer[] componentsInChildren2 = enemy.GetComponentsInChildren<Renderer>(includeInactive: true);
			for (int i = 0; i < componentsInChildren2.Length; i++)
			{
				componentsInChildren2[i].enabled = false;
			}
			Rigidbody component2 = enemy.GetComponent<Rigidbody>();
			if (component2 != null)
			{
				component2.linearVelocity = Vector3.zero;
				component2.angularVelocity = Vector3.zero;
				component2.isKinematic = true;
			}
		}
	}

	private IEnumerator DestroyRepresentativeAfterGrab(GameObject representative)
	{
		yield return null;
		while (GrabScreen.Instance != null && GrabScreen.Instance.IsGrabbed)
		{
			yield return null;
		}
		if (representative != null)
		{
			Object.Destroy(representative);
		}
	}

	private void TrackShake()
	{
		if (InputManager.Grapple != null)
		{
			float num = InputManager.Grapple.ReadValue<float>();
			if (InputDeviceTracker.UsingGamepad)
			{
				TrackStickShake(num);
			}
			else
			{
				TrackMouseShake(num);
			}
		}
	}

	private void TrackMouseShake(float mouseX)
	{
		accumulatedMouseX += mouseX;
		if (!(Mathf.Abs(accumulatedMouseX) < minShakeDelta))
		{
			CommitShakeDirection((accumulatedMouseX > 0f) ? 1 : (-1));
			accumulatedMouseX = 0f;
		}
	}

	private void TrackStickShake(float stickX)
	{
		if (!(Mathf.Abs(stickX) < stickShakeThreshold))
		{
			CommitShakeDirection((stickX > 0f) ? 1 : (-1));
			accumulatedMouseX = 0f;
		}
	}

	private void CommitShakeDirection(int direction)
	{
		if (lastShakeDirection != 0 && direction != lastShakeDirection)
		{
			shakeProgress++;
			int shakesRequired = ShakesRequired;
			UpdateShakeProgressUI(shakesRequired);
			if (shakeProgress >= (float)shakesRequired)
			{
				RemoveOneGrappler(direction);
			}
		}
		lastShakeDirection = direction;
	}

	private void ReleaseGrappler(GameObject enemy, int shakeSign)
	{
		if (!(enemy == null))
		{
			Vector3 right = ((throwCamera != null) ? throwCamera.transform : ((playerStats != null) ? playerStats.transform : base.transform)).right;
			right.y = 0f;
			if (right.sqrMagnitude < 0.0001f)
			{
				right = Vector3.right;
			}
			Vector3 vector = right.normalized * ((shakeSign >= 0) ? 1f : (-1f));
			if (playerStats != null)
			{
				enemy.transform.position = playerStats.transform.position + storedEnemyOffset + vector * releaseClearance;
			}
			ChargingEnemyAI component = enemy.GetComponent<ChargingEnemyAI>();
			if (component != null)
			{
				component.ThrowAfterGrapple(vector * component.ThrowForce);
			}
			else
			{
				enemy.SetActive(value: true);
			}
		}
	}

	private void RemoveOneGrappler(int shakeSign)
	{
		grappleCount--;
		if (grapplingEnemies.Count > 0)
		{
			int index = grapplingEnemies.Count - 1;
			GameObject enemy = grapplingEnemies[index];
			grapplingEnemies.RemoveAt(index);
			ReleaseGrappler(enemy, shakeSign);
		}
		shakeProgress = 0f;
		accumulatedMouseX = 0f;
		lastShakeDirection = 0;
		if (grappleCount <= 0)
		{
			EndGrapple();
			return;
		}
		PlayGrappleAnimation();
		UpdateMovementSpeed();
		UpdateGrappleUI();
		UpdateGrappleLoopSound();
		UpdateShakeProgressUI(ShakesRequired);
	}

	private void UpdateShakeProgressUI(int shakesRequired)
	{
		if (shakeProgressFill != null)
		{
			shakeProgressFill.fillAmount = shakeProgress / (float)shakesRequired;
		}
	}

	private void ResetShakeState()
	{
		shakeProgress = 0f;
		accumulatedMouseX = 0f;
		lastShakeDirection = 0;
		if (shakeProgressFill != null)
		{
			shakeProgressFill.fillAmount = 0f;
		}
	}

	private void OnStruggleButtonPressed()
	{
	}

	public void EndGrapple(bool restoreMovement = true, bool playEndSound = true)
	{
		if (!isGrappling)
		{
			return;
		}
		isGrappling = false;
		grappleCount = 0;
		for (int i = 0; i < grapplingEnemies.Count; i++)
		{
			if (grapplingEnemies[i] != null)
			{
				Object.Destroy(grapplingEnemies[i]);
			}
		}
		grapplingEnemies.Clear();
		if (restoreMovement && playerStats != null)
		{
			playerStats.SetExternalMovementMultiplier(originalMovementMultiplier);
		}
		if (grappleUI != null)
		{
			grappleUI.SetActive(value: false);
		}
		if (grappleImage != null)
		{
			grappleImage.SetActive(value: false);
		}
		if (animator != null && originalController != null && originalController != activeGrappleController)
		{
			animator.runtimeAnimatorController = originalController;
		}
		activeGrappleSource = null;
		activeGrappleController = null;
		if (audioSource != null)
		{
			audioSource.loop = false;
			audioSource.Stop();
			if (playEndSound && grappleEndSound != null)
			{
				audioSource.PlayOneShot(grappleEndSound);
			}
		}
		if (playerStats != null)
		{
			StartCoroutine(GrabImmunity());
		}
	}

	private IEnumerator GrabImmunity()
	{
		playerStats.CanBeGrabbed = false;
		yield return new WaitForSeconds(1f);
		playerStats.CanBeGrabbed = true;
	}

	public void ForceEndGrapple()
	{
		EndGrapple();
	}

	public void EnableDashOverride()
	{
		isDashingOverride = true;
		UpdateMovementSpeed();
	}

	public void DisableDashOverride()
	{
		isDashingOverride = false;
		UpdateMovementSpeed();
	}

	public bool IsPlayerGrappled()
	{
		if (isGrappling)
		{
			return grappleCount > 0;
		}
		return false;
	}

	public void OnMeleeAttack()
	{
	}

	private void ForceStopDash()
	{
		if (playerStats == null)
		{
			return;
		}
		Rigidbody component = playerStats.GetComponent<Rigidbody>();
		if (component != null)
		{
			component.linearVelocity = Vector3.zero;
			component.angularVelocity = Vector3.zero;
		}
		MonoBehaviour[] components = playerStats.GetComponents<MonoBehaviour>();
		foreach (MonoBehaviour monoBehaviour in components)
		{
			MethodInfo method = monoBehaviour.GetType().GetMethod("StopDash");
			if (method != null)
			{
				method.Invoke(monoBehaviour, null);
				break;
			}
			MethodInfo method2 = monoBehaviour.GetType().GetMethod("CancelDash");
			if (method2 != null)
			{
				method2.Invoke(monoBehaviour, null);
				break;
			}
		}
	}

	public void SetHeatIncreaseRate(float newRate)
	{
		heatIncreaseRate = Mathf.Max(0f, newRate);
	}

	public void SetMovementSpeedReduction(float reductionMultiplier)
	{
		movementSpeedReduction = Mathf.Clamp01(reductionMultiplier);
		if (isGrappling && playerStats != null)
		{
			playerStats.SetExternalMovementMultiplier(originalMovementMultiplier * movementSpeedReduction);
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
			ChargingEnemyAI component2 = enemy.GetComponent<ChargingEnemyAI>();
			ProjectileEnemyAI component3 = enemy.GetComponent<ProjectileEnemyAI>();
			DragonEnemyAI component4 = enemy.GetComponent<DragonEnemyAI>();
			ProximityDragonEnemyAI component5 = enemy.GetComponent<ProximityDragonEnemyAI>();
			MimicEnemy component6 = enemy.GetComponent<MimicEnemy>();
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
		}
		if (!string.IsNullOrEmpty(text))
		{
			GalleryProgressManager.Instance?.UnlockEnemy(text);
		}
	}

	private void OnDestroy()
	{
		if (isGrappling && playerStats != null)
		{
			playerStats.SetExternalMovementMultiplier(originalMovementMultiplier);
		}
	}
}
