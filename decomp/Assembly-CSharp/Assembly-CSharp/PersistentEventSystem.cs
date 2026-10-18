using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(EventSystem))]
public class PersistentEventSystem : MonoBehaviour
{
	private static PersistentEventSystem instance;

	private void Awake()
	{
		if (instance != null && instance != this)
		{
			Object.Destroy(gameObject);
			return;
		}
		instance = this;
		Object.DontDestroyOnLoad(gameObject);
	}
}
