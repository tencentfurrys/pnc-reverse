using UnityEngine;

namespace PixelCrushers.GridController
{
	public class ChoiceItemPedestal : MonoBehaviour
	{
		[Header("Choice Items - Assign Directly")]
		[Tooltip("First item to choose from")]
		[SerializeField]
		private InventoryItem itemChoice1;

		[Tooltip("Second item to choose from")]
		[SerializeField]
		private InventoryItem itemChoice2;

		[Header("Prefab")]
		[SerializeField]
		private GameObject itemPickupPrefab;

		[Header("Item Holders")]
		[Tooltip("Empty GameObject where the first item will be placed")]
		[SerializeField]
		private Transform itemHolder1Position;

		[Tooltip("Empty GameObject where the second item will be placed")]
		[SerializeField]
		private Transform itemHolder2Position;

		[Header("Outline Effect (Optional)")]
		[SerializeField]
		private SpriteRenderer spriteRenderer;

		[SerializeField]
		private float outlineAlpha = 1f;

		[Header("Destruction Effect")]
		[Tooltip("Particle effect to play when the unchosen item is destroyed")]
		[SerializeField]
		private GameObject destructionEffect;

		[SerializeField]
		private float effectDuration = 3f;

		[Header("Animation (Optional)")]
		[SerializeField]
		private Animator pedestalAnimator;

		private bool hasSpawnedItems;

		private GameObject spawnedItem1;

		private GameObject spawnedItem2;

		private InventoryItemPickup pickup1;

		private InventoryItemPickup pickup2;

		private bool choiceMade;

		public bool HasSpawnedItems => hasSpawnedItems;

		public bool ChoiceMade => choiceMade;

		private void Start()
		{
			SpawnItemsOnPedestal();
			if (spriteRenderer != null && spriteRenderer.material != null)
			{
				spriteRenderer.material.SetFloat("_OutlineAlpha", outlineAlpha);
			}
		}

		private void Update()
		{
			if (hasSpawnedItems && !choiceMade)
			{
				if (IsItemTaken(spawnedItem1, pickup1))
				{
					DestroyOtherItem(2);
				}
				else if (IsItemTaken(spawnedItem2, pickup2))
				{
					DestroyOtherItem(1);
				}
			}
		}

		private void SpawnItemsOnPedestal()
		{
			if (!hasSpawnedItems && !(itemChoice1 == null) && !(itemChoice2 == null) && !(itemHolder1Position == null) && !(itemHolder2Position == null) && !(itemPickupPrefab == null))
			{
				spawnedItem1 = SpawnSingleItem(itemChoice1, itemHolder1Position);
				spawnedItem2 = SpawnSingleItem(itemChoice2, itemHolder2Position);
				if (spawnedItem1 != null)
				{
					pickup1 = spawnedItem1.GetComponent<InventoryItemPickup>();
				}
				if (spawnedItem2 != null)
				{
					pickup2 = spawnedItem2.GetComponent<InventoryItemPickup>();
				}
				hasSpawnedItems = true;
			}
		}

		private GameObject SpawnSingleItem(InventoryItem itemToSpawn, Transform holderPosition)
		{
			if (itemToSpawn == null || holderPosition == null)
			{
				return null;
			}
			Vector3 position = holderPosition.position;
			Quaternion rotation = holderPosition.rotation;
			GameObject obj = Object.Instantiate(itemPickupPrefab, position, rotation);
			InventoryItemPickup component = obj.GetComponent<InventoryItemPickup>();
			if (component != null)
			{
				component.SetItemData(itemToSpawn);
				component.SetHeldInChest(held: true);
			}
			Rigidbody component2 = obj.GetComponent<Rigidbody>();
			if (component2 != null)
			{
				component2.isKinematic = true;
				component2.useGravity = false;
			}
			obj.transform.SetParent(holderPosition);
			return obj;
		}

		private bool IsItemTaken(GameObject item, InventoryItemPickup pickup)
		{
			if (item == null)
			{
				return true;
			}
			if (!item.activeInHierarchy)
			{
				return true;
			}
			if (pickup != null && pickup.IsPickedUp)
			{
				return true;
			}
			return false;
		}

		private void DestroyOtherItem(int itemNumber)
		{
			if (choiceMade)
			{
				return;
			}
			choiceMade = true;
			PlayLaughAnimation();
			switch (itemNumber)
			{
			case 1:
				if (spawnedItem1 != null)
				{
					PlayDestructionEffect(spawnedItem1.transform.position);
					Object.Destroy(spawnedItem1);
					spawnedItem1 = null;
					pickup1 = null;
				}
				break;
			case 2:
				if (spawnedItem2 != null)
				{
					PlayDestructionEffect(spawnedItem2.transform.position);
					Object.Destroy(spawnedItem2);
					spawnedItem2 = null;
					pickup2 = null;
				}
				break;
			}
			if (itemNumber == 2 && spawnedItem2 != null && !spawnedItem2.activeInHierarchy)
			{
				spawnedItem2 = null;
				pickup2 = null;
			}
			else if (itemNumber == 1 && spawnedItem1 != null && !spawnedItem1.activeInHierarchy)
			{
				spawnedItem1 = null;
				pickup1 = null;
			}
		}

		private void PlayDestructionEffect(Vector3 position)
		{
			if (destructionEffect == null)
			{
				return;
			}
			GameObject gameObject = Object.Instantiate(destructionEffect, position, Quaternion.identity);
			Object.Destroy(gameObject, effectDuration);
			ParticleSystem component = gameObject.GetComponent<ParticleSystem>();
			if (component != null)
			{
				component.Play();
				if (component.main.duration > effectDuration)
				{
					Object.Destroy(gameObject, component.main.duration + component.main.startLifetime.constantMax);
				}
			}
		}

		public void ResetPedestal()
		{
			hasSpawnedItems = false;
			choiceMade = false;
			if (spawnedItem1 != null)
			{
				Object.Destroy(spawnedItem1);
				spawnedItem1 = null;
				pickup1 = null;
			}
			if (spawnedItem2 != null)
			{
				Object.Destroy(spawnedItem2);
				spawnedItem2 = null;
				pickup2 = null;
			}
		}

		public void SetChoiceItems(InventoryItem item1, InventoryItem item2)
		{
			itemChoice1 = item1;
			itemChoice2 = item2;
		}

		public void SpawnItems()
		{
			if (!hasSpawnedItems)
			{
				SpawnItemsOnPedestal();
			}
		}

		private void OnDrawGizmosSelected()
		{
			if (itemHolder1Position != null)
			{
				Gizmos.color = Color.cyan;
				Gizmos.DrawWireSphere(itemHolder1Position.position, 0.2f);
				Gizmos.color = Color.magenta;
				Gizmos.DrawLine(base.transform.position, itemHolder1Position.position);
				Gizmos.color = Color.red;
				Gizmos.DrawRay(itemHolder1Position.position, itemHolder1Position.right * 0.3f);
				Gizmos.color = Color.green;
				Gizmos.DrawRay(itemHolder1Position.position, itemHolder1Position.up * 0.3f);
				Gizmos.color = Color.blue;
				Gizmos.DrawRay(itemHolder1Position.position, itemHolder1Position.forward * 0.3f);
			}
			if (itemHolder2Position != null)
			{
				Gizmos.color = Color.yellow;
				Gizmos.DrawWireSphere(itemHolder2Position.position, 0.2f);
				Gizmos.color = Color.magenta;
				Gizmos.DrawLine(base.transform.position, itemHolder2Position.position);
				Gizmos.color = Color.red;
				Gizmos.DrawRay(itemHolder2Position.position, itemHolder2Position.right * 0.3f);
				Gizmos.color = Color.green;
				Gizmos.DrawRay(itemHolder2Position.position, itemHolder2Position.up * 0.3f);
				Gizmos.color = Color.blue;
				Gizmos.DrawRay(itemHolder2Position.position, itemHolder2Position.forward * 0.3f);
			}
			if (itemHolder1Position != null && itemHolder2Position != null)
			{
				Gizmos.color = Color.white;
				Gizmos.DrawLine(itemHolder1Position.position, itemHolder2Position.position);
			}
		}

		[ContextMenu("Test Spawn")]
		public void TestSpawn()
		{
			SpawnItems();
		}

		[ContextMenu("Test Reset")]
		public void TestReset()
		{
			ResetPedestal();
		}

		public void PlayLaughAnimation()
		{
			if (pedestalAnimator != null)
			{
				pedestalAnimator.Play("Laugh");
			}
		}

		[ContextMenu("Test Laugh Animation")]
		public void TestLaughAnimation()
		{
			PlayLaughAnimation();
		}
	}
}
