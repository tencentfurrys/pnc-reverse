using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace PixelCrushers.GridController;

public class InventoryItemPickup : MonoBehaviour, IPoolResettable, IPlayerInteractable
{
	[Header("Item Data")]
	[SerializeField]
	private InventoryItem itemData;

	[Header("Pickup Settings")]
	[SerializeField]
	private float pickupRange = 2.5f;

	[SerializeField]
	private bool autoPickup;

	[SerializeField]
	private float autoPickupDelay = 0.5f;

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
	[SerializeField]
	private float spriteSize = 0.5f;

	[SerializeField]
	private Material spriteMaterial;

	[SerializeField]
	private bool rotateItem;

	[SerializeField]
	private float rotationSpeed = 45f;

	[Header("Visual Animation")]
	[SerializeField]
	private bool bobItem = true;

	[SerializeField]
	private float bobHeight = 0.15f;

	[SerializeField]
	private float bobSpeed = 3f;

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

	private static Shader _cachedUnlitTransparent;

	private static Mesh _sharedQuadMesh;

	private bool isBeingLookedAt;

	private GameObject player;

	private InventoryUI playerInventoryUI;

	private Vector3 startPosition;

	private float bobTimer;

	private bool wasPickedUp;

	private GameObject itemModel;

	private MeshFilter cachedMeshFilter;

	private MeshRenderer cachedMeshRenderer;

	private float currentAutoPickupDelay;

	private bool hasLanded;

	private Rigidbody itemRigidbody;

	private bool isHeldInChest;

	private static Shader UnlitTransparentShader
	{
		get
		{
			if (_cachedUnlitTransparent == null)
			{
				_cachedUnlitTransparent = Shader.Find("Unlit/Transparent");
			}
			return _cachedUnlitTransparent;
		}
	}

	private static Mesh SharedQuadMesh
	{
		get
		{
			if (_sharedQuadMesh == null)
			{
				_sharedQuadMesh = CreateQuadMeshStatic();
				_sharedQuadMesh.name = "SharedItemSpriteQuad";
			}
			return _sharedQuadMesh;
		}
	}

	public bool CanInteract => !wasPickedUp;

	public float InteractionRange => pickupRange;

	public bool IsPickedUp => wasPickedUp;

	private void Awake()
	{
		startPosition = transform.position;
		if (audioSource == null)
		{
			audioSource = GetComponent<AudioSource>();
		}
		if (pickupPrompt != null)
		{
			pickupPrompt.SetActive(value: false);
		}
		itemRigidbody = GetComponent<Rigidbody>();
		if (itemRigidbody != null)
		{
			ApplyPhysicsSettings();
		}
	}

	private void ApplyPhysicsSettings()
	{
		if (!(itemRigidbody == null))
		{
			itemRigidbody.linearDamping = drag;
			itemRigidbody.angularDamping = angularDrag;
			itemRigidbody.useGravity = useGravity;
			if (freezeRotation)
			{
				itemRigidbody.freezeRotation = true;
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
			transform.rotation = Quaternion.Euler(0f, Random.Range(0, 360), 0f);
		}
		if (itemData != null && itemModel == null)
		{
			Create2DSpriteModel();
		}
		currentAutoPickupDelay = autoPickupDelay;
	}

	private void Update()
	{
		if (wasPickedUp)
		{
			return;
		}
		if (autoPickup && isBeingLookedAt)
		{
			currentAutoPickupDelay -= Time.deltaTime;
			if (currentAutoPickupDelay <= 0f)
			{
				TryPickup();
			}
		}
		UpdateVisualEffects();
	}

	private void Create2DSpriteModel()
	{
		if (itemData?.itemIcon == null)
		{
			return;
		}
		if (itemModel != null && cachedMeshRenderer != null)
		{
			if (cachedMeshRenderer.material != null)
			{
				cachedMeshRenderer.material.mainTexture = itemData.itemIcon.texture;
				cachedMeshRenderer.material.SetColor("_Color", Color.white);
			}
			else
			{
				cachedMeshRenderer.material = CreateSpriteMaterial(itemData.itemIcon);
			}
			itemModel.SetActive(value: true);
			itemModel.transform.localScale = Vector3.one * spriteSize;
			return;
		}
		if (itemModel != null)
		{
			Object.Destroy(itemModel);
			itemModel = null;
		}
		GameObject gameObject = new GameObject("SpriteQuad");
		gameObject.transform.SetParent(transform);
		gameObject.transform.localPosition = Vector3.zero;
		cachedMeshFilter = gameObject.AddComponent<MeshFilter>();
		cachedMeshRenderer = gameObject.AddComponent<MeshRenderer>();
		cachedMeshFilter.sharedMesh = SharedQuadMesh;
		Material material = CreateSpriteMaterial(itemData.itemIcon);
		cachedMeshRenderer.material = material;
		gameObject.AddComponent<Billboard>().lockYAxis = true;
		itemModel = gameObject;
		gameObject.transform.localScale = Vector3.one * spriteSize;
		SetOutlineAlpha(0f);
	}

	private static Mesh CreateQuadMeshStatic()
	{
		Mesh mesh = new Mesh();
		mesh.name = "ItemSprite";
		Vector3[] vertices = new Vector3[4]
		{
			new Vector3(-0.5f, -0.5f, 0f),
			new Vector3(0.5f, -0.5f, 0f),
			new Vector3(-0.5f, 0.5f, 0f),
			new Vector3(0.5f, 0.5f, 0f)
		};
		Vector2[] uv = new Vector2[4]
		{
			new Vector2(0f, 0f),
			new Vector2(1f, 0f),
			new Vector2(0f, 1f),
			new Vector2(1f, 1f)
		};
		int[] triangles = new int[6] { 0, 2, 1, 2, 3, 1 };
		mesh.vertices = vertices;
		mesh.uv = uv;
		mesh.triangles = triangles;
		mesh.RecalculateNormals();
		return mesh;
	}

	private Material CreateSpriteMaterial(Sprite sprite)
	{
		Material material = ((!(spriteMaterial != null)) ? new Material(UnlitTransparentShader) : new Material(spriteMaterial));
		material.mainTexture = sprite.texture;
		material.SetColor("_Color", Color.white);
		return material;
	}

	private void UpdateVisualEffects()
	{
		if (itemModel == null || isHeldInChest)
		{
			return;
		}
		if (!hasLanded && itemRigidbody != null && itemRigidbody.linearVelocity.magnitude < 0.5f && itemRigidbody.angularVelocity.magnitude < 1f)
		{
			hasLanded = true;
			startPosition = transform.position;
		}
		if (hasLanded)
		{
			if (rotateItem)
			{
				transform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f);
			}
			if (bobItem)
			{
				bobTimer += Time.deltaTime * bobSpeed;
				Vector3 position = startPosition;
				position.y += Mathf.Sin(bobTimer) * bobHeight;
				transform.position = position;
			}
		}
	}

	public void OnLookedAt(GameObject playerObj)
	{
		if (!wasPickedUp)
		{
			player = playerObj;
			playerInventoryUI = playerObj.GetComponent<InventoryUI>();
			if (playerInventoryUI == null)
			{
				playerInventoryUI = playerObj.GetComponentInChildren<InventoryUI>();
			}
			if (playerInventoryUI == null)
			{
				playerInventoryUI = Object.FindAnyObjectByType<InventoryUI>();
			}
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
		playerInventoryUI = null;
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
		if (wasPickedUp || itemData == null || playerInventoryUI == null)
		{
			return;
		}
		if (isHeldInChest)
		{
			transform.SetParent(null);
			if (itemRigidbody != null)
			{
				itemRigidbody.isKinematic = false;
				itemRigidbody.useGravity = true;
			}
			isHeldInChest = false;
		}
		if (playerInventoryUI.TryAddItem(itemData))
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
		if (usePickupAnimation && itemModel != null)
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
		Transform modelTransform = itemModel.transform;
		Vector3 startScale = modelTransform.localScale;
		Vector3 targetScale = startScale * 1.5f;
		Vector3 startPos = modelTransform.localPosition;
		float elapsed = 0f;
		Material mat = itemModel.GetComponent<Renderer>()?.material;
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
			GameObject gameObject = new GameObject("TempAudio");
			gameObject.transform.position = transform.position;
			AudioSource audioSource = gameObject.AddComponent<AudioSource>();
			audioSource.clip = pickupSound;
			audioSource.outputAudioMixerGroup = sfxMixerGroup;
			audioSource.Play();
			Object.Destroy(gameObject, pickupSound.length + 0.1f);
		}
		if (pickupEffect != null)
		{
			Object.Destroy(Object.Instantiate(pickupEffect, transform.position, transform.rotation), 3f);
		}
		if (PickupPoolManager.Instance != null)
		{
			PickupPoolManager.Instance.ReturnItemPickup(base.gameObject);
		}
		else
		{
			Object.Destroy(base.gameObject);
		}
	}

	private void UpdatePromptText()
	{
		if (!dynamicPromptText || pickupPrompt == null || itemData == null)
		{
			return;
		}
		string text = itemData.rarity.ToString();
		string text2 = itemData.itemName + " (" + text + ")";
		Color rarityColor = itemData.GetRarityColor();
		TextMeshProUGUI textMeshProUGUI = pickupPrompt.GetComponent<TextMeshProUGUI>();
		if (textMeshProUGUI == null)
		{
			textMeshProUGUI = pickupPrompt.GetComponentInChildren<TextMeshProUGUI>();
		}
		if (textMeshProUGUI != null)
		{
			textMeshProUGUI.text = text2;
			textMeshProUGUI.color = rarityColor;
			return;
		}
		Text text3 = pickupPrompt.GetComponent<Text>();
		if (text3 == null)
		{
			text3 = pickupPrompt.GetComponentInChildren<Text>();
		}
		if (text3 != null)
		{
			text3.text = text2;
			text3.color = rarityColor;
		}
	}

	public void PoolReset()
	{
		StopAllCoroutines();
		wasPickedUp = false;
		isBeingLookedAt = false;
		player = null;
		playerInventoryUI = null;
		hasLanded = false;
		bobTimer = 0f;
		currentAutoPickupDelay = autoPickupDelay;
		isHeldInChest = false;
		itemData = null;
		if (itemModel != null)
		{
			itemModel.SetActive(value: false);
		}
		if (itemRigidbody != null)
		{
			itemRigidbody.linearVelocity = Vector3.zero;
			itemRigidbody.angularVelocity = Vector3.zero;
			itemRigidbody.isKinematic = false;
		}
		if (pickupPrompt != null)
		{
			pickupPrompt.SetActive(value: false);
		}
		SetOutlineAlpha(0f);
	}

	private void SetOutlineAlpha(float alpha)
	{
		if (cachedMeshRenderer != null && cachedMeshRenderer.material != null)
		{
			cachedMeshRenderer.material.SetFloat("_OutlineAlpha", alpha);
		}
	}

	public void SetItemData(InventoryItem item)
	{
		itemData = item;
		if (item != null && item.itemIcon != null)
		{
			Create2DSpriteModel();
			UpdatePromptText();
		}
	}

	public void SetSpriteMaterial(Material material)
	{
		spriteMaterial = material;
		if (itemData != null)
		{
			Create2DSpriteModel();
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

	public static GameObject CreatePickup(InventoryItem item, Vector3 position, GameObject pickupPrefab = null)
	{
		GameObject gameObject = null;
		if (PickupPoolManager.Instance != null && (pickupPrefab == null || pickupPrefab == PickupPoolManager.Instance.ItemPrefab))
		{
			gameObject = PickupPoolManager.Instance.GetItemPickup(position, Quaternion.identity);
		}
		if (gameObject == null)
		{
			GameObject gameObject2 = pickupPrefab;
			if (gameObject2 == null)
			{
				gameObject2 = new GameObject("Pickup_" + item.itemName);
				gameObject2.AddComponent<InventoryItemPickup>();
				SphereCollider sphereCollider = gameObject2.AddComponent<SphereCollider>();
				sphereCollider.isTrigger = true;
				sphereCollider.radius = 1f;
				gameObject2.AddComponent<Rigidbody>();
			}
			gameObject = Object.Instantiate(gameObject2, position, Quaternion.identity);
		}
		InventoryItemPickup component = gameObject.GetComponent<InventoryItemPickup>();
		if (component != null)
		{
			component.SetItemData(item);
			component.ApplyPhysicsSettings();
		}
		return gameObject;
	}

	public InventoryItem GetItemData()
	{
		return itemData;
	}

	public void SetHeldInChest(bool held)
	{
		isHeldInChest = held;
	}

	public string GetDebugInfo()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("=== Pickup Debug: " + (itemData?.itemName ?? "None") + " ===");
		stringBuilder.AppendLine($"Position: {transform.position}");
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
		if (itemRigidbody != null)
		{
			stringBuilder.AppendLine($"Rigidbody Mass: {itemRigidbody.mass} (Set manually)");
			stringBuilder.AppendLine($"Rigidbody Velocity: {itemRigidbody.linearVelocity.magnitude:F2}");
			stringBuilder.AppendLine($"Rigidbody Angular Velocity: {itemRigidbody.angularVelocity.magnitude:F2}");
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
				Gizmos.DrawWireSphere(transform.position, sphereCollider.radius);
			}
			else
			{
				Gizmos.color = Color.red;
				Gizmos.DrawWireSphere(transform.position, pickupRange);
			}
			_ = itemData != null;
		}
	}

	private void OnValidate()
	{
		if (spriteSize <= 0f)
		{
			spriteSize = 0.5f;
		}
		if (pickupRange <= 0f)
		{
			pickupRange = 2.5f;
		}
		if (Application.isPlaying && itemRigidbody != null)
		{
			ApplyPhysicsSettings();
		}
	}
}
