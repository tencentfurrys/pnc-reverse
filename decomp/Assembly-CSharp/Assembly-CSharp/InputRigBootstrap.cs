using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public static class InputRigBootstrap
{
	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	private static void EnsureRig()
	{
		if (!(InputManager.I != null) || !(EventSystem.current != null))
		{
			GameObject gameObject = new GameObject("InputRig (auto)");
			gameObject.AddComponent<EventSystem>();
			gameObject.AddComponent<InputSystemUIInputModule>();
			gameObject.AddComponent<PersistentEventSystem>();
			gameObject.AddComponent<InputManager>();
			gameObject.AddComponent<InputDeviceTracker>();
			Debug.Log("[InputRigBootstrap] No EventSystem/InputManager in the loaded scene - created one. This is expected when entering Play Mode directly on a gameplay scene.");
		}
	}
}
