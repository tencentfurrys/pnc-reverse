using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

[DefaultExecutionOrder(-1000)]
public class InputManager : MonoBehaviour
{
	[Tooltip("Drag Assets/InputSystem_Actions.inputactions here.")]
	[SerializeField]
	private InputActionAsset actionAsset;

	private InputActionMap playerMap;

	private InputActionMap uiMap;

	public static InputManager I { get; private set; }

	public static InputAction Move { get; private set; }

	public static InputAction Look { get; private set; }

	public static InputAction Attack { get; private set; }

	public static InputAction AttackSecondary { get; private set; }

	public static InputAction Interact { get; private set; }

	public static InputAction Crouch { get; private set; }

	public static InputAction Dash { get; private set; }

	public static InputAction Jump { get; private set; }

	public static InputAction Previous { get; private set; }

	public static InputAction Next { get; private set; }

	public static InputAction ToggleInventory { get; private set; }

	public static InputAction DropItem { get; private set; }

	public static InputAction InventoryPickUp { get; private set; }

	public static InputAction Pause { get; private set; }

	public static InputAction Grapple { get; private set; }

	public static InputAction SpawnNavigator { get; private set; }

	public static InputAction DebugUnlock { get; private set; }

	public static InputAction NoteMove { get; private set; }

	public static InputAction AnyContinue { get; private set; }

	public static InputAction Restart { get; private set; }

	private void Awake()
	{
		if (I != null && I != this)
		{
			UnityEngine.Object.Destroy(gameObject);
			return;
		}
		I = this;
		UnityEngine.Object.DontDestroyOnLoad(gameObject);
		if (actionAsset == null)
		{
			actionAsset = InputSystem.actions;
		}
		if (actionAsset == null)
		{
			Debug.LogError("[InputManager] No InputActionAsset assigned. Drag Assets/InputSystem_Actions.inputactions onto the actionAsset field.");
			return;
		}
		playerMap = actionAsset.FindActionMap("Player", throwIfNotFound: true);
		uiMap = actionAsset.FindActionMap("UI", throwIfNotFound: true);
		Move = playerMap.FindAction("Move", throwIfNotFound: true);
		Look = playerMap.FindAction("Look", throwIfNotFound: true);
		Attack = playerMap.FindAction("Attack", throwIfNotFound: true);
		AttackSecondary = playerMap.FindAction("AttackSecondary", throwIfNotFound: true);
		Interact = playerMap.FindAction("Interact", throwIfNotFound: true);
		Crouch = playerMap.FindAction("Crouch", throwIfNotFound: true);
		Dash = playerMap.FindAction("Dash", throwIfNotFound: true);
		Jump = playerMap.FindAction("Jump", throwIfNotFound: true);
		Previous = playerMap.FindAction("Previous", throwIfNotFound: true);
		Next = playerMap.FindAction("Next", throwIfNotFound: true);
		ToggleInventory = playerMap.FindAction("ToggleInventory", throwIfNotFound: true);
		DropItem = playerMap.FindAction("DropItem", throwIfNotFound: true);
		InventoryPickUp = playerMap.FindAction("InventoryPickUp");
		if (InventoryPickUp == null)
		{
			Debug.LogWarning("[InputManager] Player/InventoryPickUp action missing from the action asset; controller pick-up/place in the inventory is disabled.");
		}
		Pause = playerMap.FindAction("Pause", throwIfNotFound: true);
		Grapple = playerMap.FindAction("Grapple", throwIfNotFound: true);
		SpawnNavigator = playerMap.FindAction("SpawnNavigator", throwIfNotFound: true);
		DebugUnlock = playerMap.FindAction("DebugUnlock", throwIfNotFound: true);
		NoteMove = playerMap.FindAction("NoteMove", throwIfNotFound: true);
		AnyContinue = playerMap.FindAction("AnyContinue", throwIfNotFound: true);
		Restart = playerMap.FindAction("Restart");
		if (Restart == null)
		{
			Debug.LogWarning("[InputManager] Player/Restart action missing from the action asset; hold-to-restart is disabled.");
		}
		string text = PlayerPrefs.GetString("InputBindings", string.Empty);
		if (!string.IsNullOrEmpty(text))
		{
			try
			{
				actionAsset.LoadBindingOverridesFromJson(text);
				Look.RemoveAllBindingOverrides();
				Grapple.RemoveAllBindingOverrides();
				PlayerPrefs.SetString("InputBindings", actionAsset.SaveBindingOverridesAsJson() ?? string.Empty);
				PlayerPrefs.Save();
			}
			catch (Exception ex)
			{
				Debug.LogWarning("[InputManager] Failed to apply saved bindings, clearing them: " + ex.Message);
				PlayerPrefs.DeleteKey("InputBindings");
			}
		}
		ConfigureUIModule();
	}

	private void ConfigureUIModule()
	{
		InputSystemUIInputModule component = GetComponent<InputSystemUIInputModule>();
		if (!(component == null))
		{
			component.actionsAsset = actionAsset;
			component.move = InputActionReference.Create(uiMap.FindAction("Navigate", throwIfNotFound: true));
			component.submit = InputActionReference.Create(uiMap.FindAction("Submit", throwIfNotFound: true));
			component.cancel = InputActionReference.Create(uiMap.FindAction("Cancel", throwIfNotFound: true));
			component.point = InputActionReference.Create(uiMap.FindAction("Point", throwIfNotFound: true));
			component.leftClick = InputActionReference.Create(uiMap.FindAction("Click", throwIfNotFound: true));
			component.rightClick = InputActionReference.Create(uiMap.FindAction("RightClick", throwIfNotFound: true));
			component.middleClick = InputActionReference.Create(uiMap.FindAction("MiddleClick", throwIfNotFound: true));
			component.scrollWheel = InputActionReference.Create(uiMap.FindAction("ScrollWheel", throwIfNotFound: true));
		}
	}

	private void OnEnable()
	{
		playerMap?.Enable();
		uiMap?.Enable();
	}

	private void OnDisable()
	{
		playerMap?.Disable();
		uiMap?.Disable();
	}

	public static void SwitchToUI(bool keepPauseAction = false)
	{
		if (!(I == null))
		{
			I.playerMap?.Disable();
			I.uiMap?.Enable();
			if (keepPauseAction)
			{
				Pause?.Enable();
			}
		}
	}

	public static void SwitchToGameplay()
	{
		if (!(I == null))
		{
			I.playerMap?.Enable();
			I.uiMap?.Enable();
		}
	}

	public static void EnableAll()
	{
		if (!(I == null))
		{
			I.playerMap?.Enable();
			I.uiMap?.Enable();
		}
	}
}
