using System.Text;
using PixelCrushers.GridController;
using UnityEngine;

public class ItemDropper : MonoBehaviour
{
	[Header("Drop Settings")]
	[SerializeField]
	private GameObject itemPickupPrefab;

	[SerializeField]
	private Transform dropPoint;

	[SerializeField]
	private float dropForce = 3f;

	[SerializeField]
	private float dropHeight = 1f;

	[SerializeField]
	private float dropSpread = 1f;

	[SerializeField]
	private int maxDropsPerFrame = 5;

	[Header("Manual Configuration Note")]
	[SerializeField]
	private bool showConfigurationNote = true;

	[Tooltip("Sprite size, collider sizes, and rigidbody mass are now configured manually on the pickup prefab - not set by ItemDropper")]
	[SerializeField]
	private string configurationNote = "Configure sprite size, collider sizes, and mass on the pickup prefab directly";

	[Header("Physics Settings")]
	[SerializeField]
	private float itemDrag = 0.5f;

	[SerializeField]
	private float itemAngularDrag = 2f;

	[SerializeField]
	private LayerMask groundLayer = 1;

	[SerializeField]
	private bool debugPhysics = true;

	[Header("Audio")]
	[SerializeField]
	private AudioClip dropSound;

	[SerializeField]
	private AudioClip[] dropSounds;

	[SerializeField]
	private AudioSource audioSource;

	[SerializeField]
	private bool randomizePitch = true;

	[SerializeField]
	private float minPitch = 0.8f;

	[SerializeField]
	private float maxPitch = 1.2f;

	[Header("Effects")]
	[SerializeField]
	private GameObject dropEffect;

	[SerializeField]
	private bool enableDropEffect = true;

	[Header("Debug")]
	[SerializeField]
	private bool showDropGizmos = true;

	private void Awake()
	{
		if (audioSource == null)
		{
			audioSource = GetComponent<AudioSource>();
		}
		if (audioSource == null)
		{
			audioSource = gameObject.AddComponent<AudioSource>();
		}
		if (dropPoint == null)
		{
			dropPoint = transform;
		}
		ValidateSetup();
	}

	private void ValidateSetup()
	{
	}

	public void DropItem(InventoryItem item)
	{
		if (!(item == null) && !(itemPickupPrefab == null))
		{
			Vector3 position = CalculateDropPosition();
			GameObject gameObject = CreatePickupObject(item, position);
			if (gameObject != null)
			{
				ApplyDropPhysics(gameObject);
				PlayDropEffects(position);
			}
		}
	}

	public void DropItemAtPosition(InventoryItem item, Vector3 position)
	{
		if (!(item == null) && !(itemPickupPrefab == null))
		{
			GameObject gameObject = CreatePickupObject(item, position);
			if (gameObject != null)
			{
				ApplyMinimalPhysics(gameObject);
				PlayDropEffects(position);
			}
		}
	}

	public void DropItems(InventoryItem[] items)
	{
		if (items == null || items.Length == 0)
		{
			return;
		}
		int num = 0;
		foreach (InventoryItem inventoryItem in items)
		{
			if (inventoryItem != null)
			{
				DropItem(inventoryItem);
				num++;
				if (num >= maxDropsPerFrame)
				{
					break;
				}
			}
		}
	}

	public void DropItemWithForce(InventoryItem item, Vector3 direction, float force)
	{
		if (item == null || itemPickupPrefab == null)
		{
			return;
		}
		Vector3 position = CalculateDropPosition();
		GameObject gameObject = CreatePickupObject(item, position);
		if (gameObject != null)
		{
			Rigidbody component = gameObject.GetComponent<Rigidbody>();
			if (component != null)
			{
				component.AddForce(direction.normalized * force, ForceMode.Impulse);
				component.AddTorque(Random.insideUnitSphere * force * 0.5f, ForceMode.Impulse);
			}
			PlayDropEffects(position);
		}
	}

	private Vector3 CalculateDropPosition()
	{
		Vector3 position = dropPoint.position + Vector3.up * dropHeight;
		position += dropPoint.forward * 1.2f;
		Vector3 vector = new Vector3(Random.Range(0f - dropSpread, dropSpread), Random.Range(0f, 0.3f), Random.Range(0f - dropSpread, dropSpread));
		position += vector;
		return EnsureValidDropHeight(position);
	}

	private Vector3 EnsureValidDropHeight(Vector3 position)
	{
		if (Physics.Raycast(new Vector3(position.x, position.y + 5f, position.z), Vector3.down, out var hitInfo, 20f, groundLayer))
		{
			float y = hitInfo.point.y + dropHeight + 0.5f;
			position.y = y;
		}
		else
		{
			position.y += 2f;
		}
		return position;
	}

	private GameObject CreatePickupObject(InventoryItem item, Vector3 position)
	{
		GameObject gameObject = ((!(PickupPoolManager.Instance != null)) ? Object.Instantiate(itemPickupPrefab, position, Quaternion.identity) : PickupPoolManager.Instance.GetItemPickup(position, Quaternion.identity));
		InventoryItemPickup component = gameObject.GetComponent<InventoryItemPickup>();
		if (component != null)
		{
			component.SetItemData(item);
			ConfigurePickupPhysics(gameObject);
			return gameObject;
		}
		Object.Destroy(gameObject);
		return null;
	}

	private void ConfigurePickupPhysics(GameObject pickup)
	{
		Rigidbody rigidbody = pickup.GetComponent<Rigidbody>();
		if (rigidbody == null)
		{
			rigidbody = pickup.AddComponent<Rigidbody>();
		}
		rigidbody.linearDamping = itemDrag;
		rigidbody.angularDamping = itemAngularDrag;
		rigidbody.useGravity = true;
		rigidbody.isKinematic = false;
		rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
		Collider[] components = pickup.GetComponents<Collider>();
		bool flag = false;
		Collider[] array = components;
		for (int i = 0; i < array.Length; i++)
		{
			if (!array[i].isTrigger)
			{
				flag = true;
			}
		}
		if (!flag)
		{
			SphereCollider sphereCollider = pickup.AddComponent<SphereCollider>();
			sphereCollider.radius = 0.1f;
			sphereCollider.isTrigger = false;
			sphereCollider.material = CreateBouncyPhysicsMaterial();
		}
	}

	private PhysicsMaterial CreateBouncyPhysicsMaterial()
	{
		return new PhysicsMaterial("ItemPhysics")
		{
			bounciness = 0.3f,
			dynamicFriction = 0.4f,
			staticFriction = 0.4f,
			frictionCombine = PhysicsMaterialCombine.Average,
			bounceCombine = PhysicsMaterialCombine.Average
		};
	}

	private void ApplyDropPhysics(GameObject pickup)
	{
		Rigidbody component = pickup.GetComponent<Rigidbody>();
		if (!(component == null))
		{
			component.useGravity = true;
			component.isKinematic = false;
			Vector3 vector = dropPoint.forward + Random.insideUnitSphere * 0.3f;
			vector.y = Mathf.Abs(vector.y) * 0.3f;
			Vector3 force = vector.normalized * dropForce;
			force.y = Mathf.Min(force.y, dropForce * 0.4f);
			component.AddForce(force, ForceMode.Impulse);
			Vector3 torque = Random.insideUnitSphere * (dropForce * 0.3f);
			component.AddTorque(torque, ForceMode.Impulse);
		}
	}

	private void ApplyMinimalPhysics(GameObject pickup)
	{
		Rigidbody component = pickup.GetComponent<Rigidbody>();
		if (!(component == null))
		{
			component.AddForce(Vector3.up * (dropForce * 0.2f), ForceMode.Impulse);
		}
	}

	private void PlayDropEffects(Vector3 position)
	{
		PlayDropSound();
		if (enableDropEffect && dropEffect != null)
		{
			Object.Destroy(Object.Instantiate(dropEffect, position, Quaternion.identity), 3f);
		}
	}

	private void PlayDropSound()
	{
		if (audioSource == null)
		{
			return;
		}
		AudioClip audioClip = null;
		if (dropSounds != null && dropSounds.Length != 0)
		{
			audioClip = dropSounds[Random.Range(0, dropSounds.Length)];
		}
		else if (dropSound != null)
		{
			audioClip = dropSound;
		}
		if (audioClip != null)
		{
			if (randomizePitch)
			{
				audioSource.pitch = Random.Range(minPitch, maxPitch);
			}
			else
			{
				audioSource.pitch = 1f;
			}
			audioSource.PlayOneShot(audioClip);
		}
	}

	public void SetPickupPrefab(GameObject prefab)
	{
		itemPickupPrefab = prefab;
		ValidateSetup();
	}

	public void SetDropPhysics(float force, float spread, float height)
	{
		dropForce = force;
		dropSpread = spread;
		dropHeight = height;
	}

	public Vector3 GetDropPoint()
	{
		if (!(dropPoint != null))
		{
			return transform.position;
		}
		return dropPoint.position;
	}

	public bool IsConfigured()
	{
		if (itemPickupPrefab != null)
		{
			return dropPoint != null;
		}
		return false;
	}

	public string GetDebugInfo()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("=== ItemDropper Debug Info ===");
		stringBuilder.AppendLine("Pickup Prefab: " + ((itemPickupPrefab != null) ? itemPickupPrefab.name : "None"));
		stringBuilder.AppendLine("Drop Point: " + ((dropPoint != null) ? dropPoint.name : "None"));
		stringBuilder.AppendLine($"Drop Force: {dropForce}");
		stringBuilder.AppendLine($"Drop Spread: {dropSpread}");
		stringBuilder.AppendLine($"Drop Height: {dropHeight}");
		stringBuilder.AppendLine("Audio Source: " + ((audioSource != null) ? "Present" : "Missing"));
		stringBuilder.AppendLine("Note: Sprite size, collider sizes, and mass configured on prefab");
		if (itemPickupPrefab != null)
		{
			InventoryItemPickup component = itemPickupPrefab.GetComponent<InventoryItemPickup>();
			Collider[] components = itemPickupPrefab.GetComponents<Collider>();
			Rigidbody component2 = itemPickupPrefab.GetComponent<Rigidbody>();
			stringBuilder.AppendLine($"Prefab has InventoryItemPickup: {component != null}");
			stringBuilder.AppendLine($"Prefab has Rigidbody: {component2 != null}");
			if (component2 != null)
			{
				stringBuilder.AppendLine($"Prefab Rigidbody Mass: {component2.mass}");
			}
			stringBuilder.AppendLine($"Prefab colliders: {components.Length}");
			Collider[] array = components;
			foreach (Collider collider in array)
			{
				stringBuilder.AppendLine($"  - {collider.GetType().Name} (Trigger: {collider.isTrigger})");
			}
		}
		return stringBuilder.ToString();
	}

	private void OnDrawGizmosSelected()
	{
		if (showDropGizmos && dropPoint != null)
		{
			Gizmos.color = Color.green;
			Gizmos.DrawWireSphere(dropPoint.position, 0.2f);
			Vector3 center = dropPoint.position + Vector3.up * dropHeight + dropPoint.forward * 1.2f;
			Gizmos.color = Color.yellow;
			Gizmos.DrawWireCube(center, Vector3.one * dropSpread * 2f);
			Gizmos.color = Color.blue;
			Gizmos.DrawRay(dropPoint.position, dropPoint.forward * 2f);
		}
	}

	[ContextMenu("Test Drop")]
	public void TestDrop()
	{
		_ = Application.isPlaying;
	}
}
