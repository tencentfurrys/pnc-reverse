using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PixelCrushers.GridController
{
	public class VFXPoolManager : MonoBehaviour
	{
		private readonly Dictionary<GameObject, Queue<GameObject>> pools = new Dictionary<GameObject, Queue<GameObject>>();

		private readonly Dictionary<GameObject, GameObject> instanceToPrefab = new Dictionary<GameObject, GameObject>();

		private Transform poolParent;

		public static VFXPoolManager Instance { get; private set; }

		private void Awake()
		{
			if (Instance != null && Instance != this)
			{
				Object.Destroy(base.gameObject);
				return;
			}
			Instance = this;
			Object.DontDestroyOnLoad(base.gameObject);
			CreatePoolParent();
		}

		private void OnDestroy()
		{
			if (Instance == this)
			{
				Instance = null;
			}
		}

		private void CreatePoolParent()
		{
			GameObject gameObject = new GameObject("VFXPool");
			gameObject.transform.SetParent(base.transform);
			gameObject.SetActive(value: false);
			poolParent = gameObject.transform;
		}

		public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
		{
			if (prefab == null)
			{
				return null;
			}
			if (!pools.TryGetValue(prefab, out var value))
			{
				value = new Queue<GameObject>();
				pools[prefab] = value;
			}
			GameObject gameObject = null;
			while (value.Count > 0)
			{
				gameObject = value.Dequeue();
				if (gameObject != null)
				{
					break;
				}
				gameObject = null;
			}
			if (gameObject == null)
			{
				gameObject = Object.Instantiate(prefab, position, rotation);
				instanceToPrefab[gameObject] = prefab;
			}
			else
			{
				gameObject.transform.SetParent(null);
				gameObject.transform.position = position;
				gameObject.transform.rotation = rotation;
				gameObject.SetActive(value: true);
			}
			return gameObject;
		}

		public GameObject Get(GameObject prefab, Transform parent)
		{
			if (prefab == null)
			{
				return null;
			}
			GameObject gameObject = Get(prefab, parent.position, parent.rotation);
			if (gameObject != null)
			{
				gameObject.transform.SetParent(parent, worldPositionStays: false);
			}
			return gameObject;
		}

		public void Return(GameObject instance)
		{
			if (instance == null)
			{
				return;
			}
			if (!instanceToPrefab.TryGetValue(instance, out var value))
			{
				Object.Destroy(instance);
				return;
			}
			instance.SetActive(value: false);
			if (poolParent == null)
			{
				CreatePoolParent();
			}
			instance.transform.SetParent(poolParent);
			if (!pools.TryGetValue(value, out var value2))
			{
				value2 = new Queue<GameObject>();
				pools[value] = value2;
			}
			value2.Enqueue(instance);
		}

		public void ReturnAfterDelay(GameObject instance, float delay)
		{
			if (!(instance == null))
			{
				StartCoroutine(ReturnAfterDelayCoroutine(instance, delay));
			}
		}

		private IEnumerator ReturnAfterDelayCoroutine(GameObject instance, float delay)
		{
			yield return new WaitForSeconds(delay);
			Return(instance);
		}

		public IEnumerator Prewarm(GameObject prefab, int count)
		{
			if (prefab == null)
			{
				yield break;
			}
			if (!pools.TryGetValue(prefab, out var pool))
			{
				pool = new Queue<GameObject>();
				pools[prefab] = pool;
			}
			if (poolParent == null)
			{
				CreatePoolParent();
			}
			for (int i = 0; i < count; i++)
			{
				GameObject gameObject = Object.Instantiate(prefab, Vector3.zero, Quaternion.identity, poolParent);
				gameObject.SetActive(value: false);
				instanceToPrefab[gameObject] = prefab;
				pool.Enqueue(gameObject);
				if (i % 3 == 2)
				{
					yield return null;
				}
			}
		}

		public IEnumerator ResetForNewLevel()
		{
			int destroyed = 0;
			foreach (KeyValuePair<GameObject, Queue<GameObject>> pool in pools)
			{
				foreach (GameObject item in pool.Value)
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
			}
			pools.Clear();
			instanceToPrefab.Clear();
			if (poolParent == null)
			{
				CreatePoolParent();
			}
		}
	}
}
