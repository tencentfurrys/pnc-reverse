using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace PixelCrushers.GridController
{
	public class CurrencyPickup : MonoBehaviour, IPoolResettable, IPlayerInteractable
	{
		[Header("Currency Settings")]
		[SerializeField]
		private int currencyValue = 10;

		[Tooltip("Random value range added to base value (0 to disable)")]
		[SerializeField]
		private int randomValueRange;

		[Header("Pickup Settings")]
		[SerializeField]
		private float pickupRange = 2.5f;

		[SerializeField]
		private bool autoPickup = true;

		[SerializeField]
		private float autoPickupDelay = 0.3f;

		[Header("Magnet & Proximity")]
		[SerializeField]
		private float magnetRadius = 3f;

		[SerializeField]
		private float magnetSpeed = 8f;

		[SerializeField]
		private float pickupDistance = 0.6f;

		[SerializeField]
		private float proximitySoundRadius = 4f;

		[SerializeField]
		private AudioClip proximitySound;

		[SerializeField]
		[Range(0f, 1f)]
		private float proximityVolume = 1f;

		[Header("Physics Settings")]
		[SerializeField]
		private float drag = 0.5f;

		[SerializeField]
		private float angularDrag = 0.05f;

		[SerializeField]
		private bool useGravity = true;

		[SerializeField]
		private bool freezeRotation;

		[Header("2D Sprite Settings - Manual Configuration")]
		[Tooltip("Assign an existing SpriteRenderer, or leave blank to auto-create from sprite")]
		[SerializeField]
		private SpriteRenderer spriteRenderer;

		[SerializeField]
		private Sprite currencySprite;

		[SerializeField]
		private float spriteSize = 0.4f;

		[SerializeField]
		private Material spriteMaterial;

		[SerializeField]
		private Color coinColor = Color.yellow;

		[SerializeField]
		private bool rotateItem = true;

		[SerializeField]
		private float rotationSpeed = 180f;

		[Header("Visual Animation")]
		[SerializeField]
		private bool bobItem = true;

		[SerializeField]
		private float bobHeight = 0.1f;

		[SerializeField]
		private float bobSpeed = 4f;

		[Header("Audio")]
		[SerializeField]
		private AudioClip pickupSound;

		[SerializeField]
		private AudioSource audioSource;

		[SerializeField]
		private AudioMixerGroup sfxMixerGroup;

		[Header("Effects")]
		[SerializeField]
		private GameObject pickupEffect;

		[SerializeField]
		private bool usePickupAnimation = true;

		[SerializeField]
		private float pickupAnimationDuration = 0.3f;

		[Header("UI")]
		[SerializeField]
		private GameObject pickupPrompt;

		[SerializeField]
		private bool dynamicPromptText = true;

		[Header("Debug")]
		[SerializeField]
		private bool showPickupRange = true;

		private bool isBeingLookedAt;

		private GameObject player;

		private Vector3 startPosition;

		private float bobTimer;

		private bool wasPickedUp;

		private GameObject currencyModel;

		private float currentAutoPickupDelay;

		private bool hasLanded;

		private Rigidbody currencyRigidbody;

		private int actualCurrencyValue;

		private bool hasPlayedProximitySound;

		public bool CanInteract => !wasPickedUp;

		public float InteractionRange => pickupRange;

		public bool IsPickedUp => wasPickedUp;

		private void Awake()
		{
			startPosition = base.transform.position;
			actualCurrencyValue = currencyValue;
			if (randomValueRange > 0)
			{
				actualCurrencyValue += Random.Range(0, randomValueRange + 1);
			}
			if (audioSource == null)
			{
				audioSource = GetComponent<AudioSource>();
			}
			if (pickupPrompt != null)
			{
				pickupPrompt.SetActive(value: false);
			}
			currencyRigidbody = GetComponent<Rigidbody>();
			if (currencyRigidbody != null)
			{
				ApplyPhysicsSettings();
			}
		}

		private void ApplyPhysicsSettings()
		{
			if (!(currencyRigidbody == null))
			{
				currencyRigidbody.linearDamping = drag;
				currencyRigidbody.angularDamping = angularDrag;
				currencyRigidbody.useGravity = useGravity;
				if (freezeRotation)
				{
					currencyRigidbody.freezeRotation = true;
				}
			}
		}

		[ContextMenu("Apply Physics Settings")]
		public void UpdatePhysicsSettings()
		{
			ApplyPhysicsSettings();
		}

		private void Start()
		{
			if (rotateItem)
			{
				base.transform.rotation = Quaternion.Euler(0f, Random.Range(0, 360), 0f);
			}
			if (spriteRenderer != null)
			{
				currencyModel = spriteRenderer.gameObject;
				if (currencySprite != null)
				{
					spriteRenderer.sprite = currencySprite;
				}
				spriteRenderer.color = coinColor;
				if (spriteMaterial != null)
				{
					spriteRenderer.material = spriteMaterial;
				}
				spriteRenderer.transform.localScale = Vector3.one * spriteSize;
			}
			else
			{
				spriteRenderer = GetComponentInChildren<SpriteRenderer>();
				if (spriteRenderer == null)
				{
					GameObject gameObject = new GameObject("CurrencySprite");
					gameObject.transform.SetParent(base.transform);
					gameObject.transform.localPosition = Vector3.zero;
					spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
					currencyModel = gameObject;
				}
				else
				{
					currencyModel = spriteRenderer.gameObject;
				}
				if (currencySprite != null)
				{
					spriteRenderer.sprite = currencySprite;
				}
				spriteRenderer.color = coinColor;
				if (spriteMaterial != null)
				{
					spriteRenderer.material = spriteMaterial;
				}
				spriteRenderer.transform.localScale = Vector3.one * spriteSize;
			}
			currentAutoPickupDelay = autoPickupDelay;
			SetOutlineAlpha(0f);
		}

		private void Update()
		{
			if (wasPickedUp)
			{
				return;
			}
			EnsurePlayerCached();
			if (player != null)
			{
				float sqrMagnitude = (player.transform.position - base.transform.position).sqrMagnitude;
				if (!hasPlayedProximitySound && sqrMagnitude <= proximitySoundRadius * proximitySoundRadius)
				{
					PlayProximitySound();
					hasPlayedProximitySound = true;
				}
				if (sqrMagnitude <= magnetRadius * magnetRadius)
				{
					MagnetizeTowards(player.transform.position);
					if (sqrMagnitude <= pickupDistance * pickupDistance)
					{
						TryPickup();
					}
					return;
				}
			}
			UpdateVisualEffects();
		}

		private void EnsurePlayerCached()
		{
			if (!(player != null))
			{
				player = GameObject.FindGameObjectWithTag("Player");
			}
		}

		private void MagnetizeTowards(Vector3 target)
		{
			if (currencyRigidbody != null && !currencyRigidbody.isKinematic)
			{
				currencyRigidbody.linearVelocity = Vector3.zero;
				currencyRigidbody.angularVelocity = Vector3.zero;
				currencyRigidbody.isKinematic = true;
			}
			base.transform.position = Vector3.MoveTowards(base.transform.position, target, magnetSpeed * Time.deltaTime);
		}

		private void PlayProximitySound()
		{
			if (!(proximitySound == null))
			{
				if (AudioPool.Instance != null)
				{
					AudioPool.Instance.PlayAtPoint(proximitySound, base.transform.position, proximityVolume);
					return;
				}
				GameObject obj = new GameObject("CurrencyProximitySFX");
				obj.transform.position = base.transform.position;
				AudioSource obj2 = obj.AddComponent<AudioSource>();
				obj2.clip = proximitySound;
				obj2.volume = proximityVolume;
				obj2.outputAudioMixerGroup = sfxMixerGroup;
				obj2.Play();
				Object.Destroy(obj, proximitySound.length + 0.1f);
			}
		}

		private void UpdateVisualEffects()
		{
			if (currencyModel == null)
			{
				return;
			}
			if (!hasLanded && currencyRigidbody != null && currencyRigidbody.linearVelocity.magnitude < 0.5f && currencyRigidbody.angularVelocity.magnitude < 1f)
			{
				hasLanded = true;
				startPosition = base.transform.position;
			}
			if (hasLanded)
			{
				if (rotateItem)
				{
					base.transform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f);
				}
				if (bobItem)
				{
					bobTimer += Time.deltaTime * bobSpeed;
					Vector3 position = startPosition;
					position.y += Mathf.Sin(bobTimer) * bobHeight;
					base.transform.position = position;
				}
			}
		}

		public void OnLookedAt(GameObject playerObj)
		{
			if (!wasPickedUp)
			{
				player = playerObj;
				isBeingLookedAt = true;
				if (pickupPrompt != null)
				{
					pickupPrompt.SetActive(value: true);
					UpdatePromptText();
				}
				SetOutlineAlpha(1f);
				currentAutoPickupDelay = autoPickupDelay;
			}
		}

		public void OnLookedAway()
		{
			isBeingLookedAt = false;
			player = null;
			if (pickupPrompt != null)
			{
				pickupPrompt.SetActive(value: false);
			}
			SetOutlineAlpha(0f);
		}

		public void OnInteracted()
		{
			TryPickup();
		}

		private void TryPickup()
		{
			if (!wasPickedUp && !(CurrencyManager.Instance == null))
			{
				PerformPickup();
			}
		}

		private void PerformPickup()
		{
			wasPickedUp = true;
			if (pickupPrompt != null)
			{
				pickupPrompt.SetActive(value: false);
			}
			CurrencyManager.Instance.AddCurrency(actualCurrencyValue);
			if (usePickupAnimation && currencyModel != null)
			{
				StartCoroutine(PickupAnimation());
			}
			else
			{
				FinishPickup();
			}
		}

		private IEnumerator PickupAnimation()
		{
			Transform modelTransform = currencyModel.transform;
			Vector3 startScale = modelTransform.localScale;
			Vector3 targetScale = startScale * 1.5f;
			Vector3 startPos = modelTransform.localPosition;
			float elapsed = 0f;
			Material mat = currencyModel.GetComponent<Renderer>()?.material;
			Color originalColor = ((mat != null) ? mat.color : Color.white);
			while (elapsed < pickupAnimationDuration)
			{
				elapsed += Time.deltaTime;
				float num = elapsed / pickupAnimationDuration;
				modelTransform.localScale = Vector3.Lerp(startScale, targetScale, num);
				Vector3 localPosition = startPos;
				localPosition.y += num * 0.5f;
				modelTransform.localPosition = localPosition;
				if (mat != null)
				{
					Color color = originalColor;
					color.a = 1f - num;
					mat.color = color;
				}
				yield return null;
			}
			FinishPickup();
		}

		private void FinishPickup()
		{
			if (pickupSound != null)
			{
				GameObject obj = new GameObject("TempAudio");
				obj.transform.position = base.transform.position;
				AudioSource obj2 = obj.AddComponent<AudioSource>();
				obj2.clip = pickupSound;
				obj2.outputAudioMixerGroup = sfxMixerGroup;
				obj2.Play();
				Object.Destroy(obj, pickupSound.length + 0.1f);
			}
			if (pickupEffect != null)
			{
				Object.Destroy(Object.Instantiate(pickupEffect, base.transform.position, base.transform.rotation), 3f);
			}
			if (PickupPoolManager.Instance != null)
			{
				PickupPoolManager.Instance.ReturnCurrencyPickup(base.gameObject);
			}
			else
			{
				Object.Destroy(base.gameObject);
			}
		}

		private void UpdatePromptText()
		{
			if (!dynamicPromptText || pickupPrompt == null)
			{
				return;
			}
			string text = $"+{actualCurrencyValue} Gold";
			TextMeshProUGUI textMeshProUGUI = pickupPrompt.GetComponent<TextMeshProUGUI>();
			if (textMeshProUGUI == null)
			{
				textMeshProUGUI = pickupPrompt.GetComponentInChildren<TextMeshProUGUI>();
			}
			if (textMeshProUGUI != null)
			{
				textMeshProUGUI.text = text;
				textMeshProUGUI.color = coinColor;
				return;
			}
			Text text2 = pickupPrompt.GetComponent<Text>();
			if (text2 == null)
			{
				text2 = pickupPrompt.GetComponentInChildren<Text>();
			}
			if (text2 != null)
			{
				text2.text = text;
				text2.color = coinColor;
			}
		}

		public void PoolReset()
		{
			StopAllCoroutines();
			wasPickedUp = false;
			isBeingLookedAt = false;
			player = null;
			hasLanded = false;
			bobTimer = 0f;
			currentAutoPickupDelay = autoPickupDelay;
			if (currencyRigidbody != null)
			{
				currencyRigidbody.linearVelocity = Vector3.zero;
				currencyRigidbody.angularVelocity = Vector3.zero;
				currencyRigidbody.isKinematic = false;
			}
			hasPlayedProximitySound = false;
			if (spriteRenderer != null)
			{
				spriteRenderer.color = coinColor;
				spriteRenderer.transform.localScale = Vector3.one * spriteSize;
				spriteRenderer.transform.localPosition = Vector3.zero;
			}
			if (pickupPrompt != null)
			{
				pickupPrompt.SetActive(value: false);
			}
			SetOutlineAlpha(0f);
		}

		private void SetOutlineAlpha(float alpha)
		{
			if (spriteRenderer != null && spriteRenderer.material != null)
			{
				spriteRenderer.material.SetFloat("_OutlineAlpha", alpha);
			}
		}

		public void SetCurrencyValue(int value)
		{
			currencyValue = value;
			actualCurrencyValue = value;
			UpdatePromptText();
		}

		public void SetCurrencySprite(Sprite sprite)
		{
			currencySprite = sprite;
			if (spriteRenderer != null && sprite != null)
			{
				spriteRenderer.sprite = sprite;
			}
		}

		public void SetSpriteMaterial(Material material)
		{
			spriteMaterial = material;
			if (spriteRenderer != null && material != null)
			{
				spriteRenderer.material = material;
			}
		}

		public void SetPhysicsProperties(float newDrag = -1f, float newAngularDrag = -1f, bool? newUseGravity = null)
		{
			if (newDrag >= 0f)
			{
				drag = newDrag;
			}
			if (newAngularDrag >= 0f)
			{
				angularDrag = newAngularDrag;
			}
			if (newUseGravity.HasValue)
			{
				useGravity = newUseGravity.Value;
			}
			ApplyPhysicsSettings();
		}

		public string GetPhysicsInfo()
		{
			return $"Drag: {drag}, AngularDrag: {angularDrag}, UseGravity: {useGravity}";
		}

		public static GameObject CreateCurrencyPickup(Vector3 position, int value, GameObject pickupPrefab = null, Sprite sprite = null)
		{
			GameObject gameObject = null;
			if (PickupPoolManager.Instance != null && (pickupPrefab == null || pickupPrefab == PickupPoolManager.Instance.CurrencyPrefab))
			{
				gameObject = PickupPoolManager.Instance.GetCurrencyPickup(position, Quaternion.identity);
			}
			if (gameObject == null)
			{
				GameObject gameObject2 = pickupPrefab;
				if (gameObject2 == null)
				{
					gameObject2 = new GameObject($"CurrencyPickup_{value}");
					gameObject2.AddComponent<CurrencyPickup>();
					SphereCollider sphereCollider = gameObject2.AddComponent<SphereCollider>();
					sphereCollider.isTrigger = true;
					sphereCollider.radius = 1f;
					gameObject2.AddComponent<Rigidbody>().mass = 0.1f;
				}
				gameObject = Object.Instantiate(gameObject2, position, Quaternion.identity);
			}
			CurrencyPickup component = gameObject.GetComponent<CurrencyPickup>();
			if (component != null)
			{
				component.SetCurrencyValue(value);
				if (sprite != null)
				{
					component.SetCurrencySprite(sprite);
				}
				component.ApplyPhysicsSettings();
			}
			return gameObject;
		}

		public int GetCurrencyValue()
		{
			return actualCurrencyValue;
		}

		public string GetDebugInfo()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("=== Currency Pickup Debug ===");
			stringBuilder.AppendLine($"Value: {actualCurrencyValue}");
			stringBuilder.AppendLine($"Position: {base.transform.position}");
			stringBuilder.AppendLine($"Player In Range: {isBeingLookedAt}");
			stringBuilder.AppendLine($"Has Landed: {hasLanded}");
			stringBuilder.AppendLine($"Sprite Size: {spriteSize}");
			stringBuilder.AppendLine($"Pickup Range: {pickupRange}");
			stringBuilder.AppendLine($"Was Picked Up: {wasPickedUp}");
			stringBuilder.AppendLine("Physics: " + GetPhysicsInfo());
			Collider component = GetComponent<Collider>();
			if (component != null)
			{
				stringBuilder.AppendLine($"Collider: {component.GetType().Name} (Trigger: {component.isTrigger})");
				if (component is SphereCollider sphereCollider)
				{
					stringBuilder.AppendLine($"Collider Radius: {sphereCollider.radius}");
				}
			}
			else
			{
				stringBuilder.AppendLine("Collider: None (WARNING!)");
			}
			if (currencyRigidbody != null)
			{
				stringBuilder.AppendLine($"Rigidbody Mass: {currencyRigidbody.mass}");
				stringBuilder.AppendLine($"Rigidbody Velocity: {currencyRigidbody.linearVelocity.magnitude:F2}");
				stringBuilder.AppendLine($"Rigidbody Angular Velocity: {currencyRigidbody.angularVelocity.magnitude:F2}");
			}
			return stringBuilder.ToString();
		}

		private void OnDrawGizmosSelected()
		{
			if (showPickupRange)
			{
				Collider component = GetComponent<Collider>();
				if (component != null && component is SphereCollider sphereCollider)
				{
					Gizmos.color = Color.yellow;
					Gizmos.DrawWireSphere(base.transform.position, sphereCollider.radius);
				}
				else
				{
					Gizmos.color = Color.red;
					Gizmos.DrawWireSphere(base.transform.position, pickupRange);
				}
			}
		}

		private void OnValidate()
		{
			if (currencyValue < 0)
			{
				currencyValue = 0;
			}
			if (randomValueRange < 0)
			{
				randomValueRange = 0;
			}
			if (spriteSize <= 0f)
			{
				spriteSize = 0.4f;
			}
			if (pickupRange <= 0f)
			{
				pickupRange = 2.5f;
			}
			if (Application.isPlaying && currencyRigidbody != null)
			{
				ApplyPhysicsSettings();
			}
		}

		[ContextMenu("Test Pickup (Play Mode Only)")]
		private void TestPickup()
		{
			if (Application.isPlaying)
			{
				PerformPickup();
			}
		}
	}
}
