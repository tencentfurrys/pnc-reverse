using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PixelCrushers.GridController;

public class PickupPoolManager : MonoBehaviour
{
	[Header("Prefabs")]
	[SerializeField]
	private GameObject currencyPickupPrefab;

	[SerializeField]
	private GameObject itemPickupPrefab;

	[Header("Pool Sizes")]
	[SerializeField]
	private int currencyPoolSize = 30;

	[SerializeField]
	private int itemPoolSize = 15;

	private Queue<GameObject> currencyPool = new Queue<GameObject>();

	private Queue<GameObject> itemPool = new Queue<GameObject>();

	private Transform poolParent;

	public static PickupPoolManager Instance { get; private set; }

	[Header("Debug")]
	public GameObject CurrencyPrefab => currencyPickupPrefab;

	public GameObject ItemPrefab => itemPickupPrefab;

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
			Object.DontDestroyOnLoad(gameObject);
			CreatePoolParent();
		}
		else
		{
			Object.Destroy(gameObject);
		}
	}

	private void CreatePoolParent()
	{
		GameObject gameObject = new GameObject("PickupPool");
		gameObject.transform.SetParent(transform);
		gameObject.SetActive(value: false);
		poolParent = gameObject.transform;
	}

	public IEnumerator ResetForNewLevel()
	{
		int destroyed = 0;
		foreach (GameObject item in currencyPool)
		{
			if (item != null)
			{
				Object.Destroy(item);
				destroyed++;
				if (destroyed % 5 == 0)
				{
					yield return null;
				}
			}
		}
		foreach (GameObject item2 in itemPool)
		{
			if (item2 != null)
			{
				Object.Destroy(item2);
				destroyed++;
				if (destroyed % 5 == 0)
				{
					yield return null;
				}
			}
		}
		currencyPool.Clear();
		itemPool.Clear();
		if (poolParent == null)
		{
			CreatePoolParent();
		}
	}

	public IEnumerator PrewarmPools()
	{
		int spawned = 0;
		if (currencyPickupPrefab != null)
		{
			for (int i = 0; i < currencyPoolSize; i++)
			{
				GameObject gameObject = Object.Instantiate(currencyPickupPrefab, Vector3.zero, Quaternion.identity, poolParent);
				gameObject.SetActive(value: false);
				currencyPool.Enqueue(gameObject);
				spawned++;
				if (spawned % 3 == 0)
				{
					yield return null;
				}
			}
		}
		if (!(itemPickupPrefab != null))
		{
			yield break;
		}
		for (int i = 0; i < itemPoolSize; i++)
		{
			GameObject gameObject2 = Object.Instantiate(itemPickupPrefab, Vector3.zero, Quaternion.identity, poolParent);
			gameObject2.SetActive(value: false);
			itemPool.Enqueue(gameObject2);
			spawned++;
			if (spawned % 3 == 0)
			{
				yield return null;
			}
		}
	}

	public GameObject GetCurrencyPickup(Vector3 position, Quaternion rotation)
	{
		return Dequeue(currencyPool, currencyPickupPrefab, position, rotation);
	}

	public GameObject GetItemPickup(Vector3 position, Quaternion rotation)
	{
		return Dequeue(itemPool, itemPickupPrefab, position, rotation);
	}

	public void ReturnCurrencyPickup(GameObject obj)
	{
		ReturnToPool(obj, currencyPool);
	}

	public void ReturnItemPickup(GameObject obj)
	{
		ReturnToPool(obj, itemPool);
	}

	private GameObject Dequeue(Queue<GameObject> pool, GameObject prefab, Vector3 position, Quaternion rotation)
	{
		while (pool.Count > 0)
		{
			GameObject gameObject = pool.Dequeue();
			if (gameObject != null)
			{
				gameObject.transform.SetParent(null);
				gameObject.transform.position = position;
				gameObject.transform.rotation = rotation;
				gameObject.SetActive(value: true);
				return gameObject;
			}
		}
		if (prefab != null)
		{
			return Object.Instantiate(prefab, position, rotation);
		}
		return null;
	}

	private void ReturnToPool(GameObject obj, Queue<GameObject> pool)
	{
		if (!(obj == null))
		{
			obj.GetComponent<IPoolResettable>()?.PoolReset();
			obj.SetActive(value: false);
			if (poolParent == null)
			{
				CreatePoolParent();
			}
			obj.transform.SetParent(poolParent);
			pool.Enqueue(obj);
		}
	}
}
