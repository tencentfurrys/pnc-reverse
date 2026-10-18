using System.Collections.Generic;
using PixelCrushers.GridController.Glyphs;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class RebindMenu : MonoBehaviour, IDynamicFirstSelectable
{
	private readonly struct CuratedAction
	{
		public readonly string ActionName;

		public readonly string DisplayName;

		public readonly string PartFilter;

		public readonly bool ShowOnKbm;

		public readonly bool ShowOnGamepad;

		public CuratedAction(string actionName, string displayName, string partFilter = null, bool showOnKbm = true, bool showOnGamepad = true)
		{
			ActionName = actionName;
			DisplayName = displayName;
			PartFilter = partFilter;
			ShowOnKbm = showOnKbm;
			ShowOnGamepad = showOnGamepad;
		}
	}

	public const string PrefsKey = "InputBindings";

	private const string PlayerMapName = "Player";

	[Header("Layout")]
	[SerializeField]
	private RectTransform entryContainer;

	[SerializeField]
	private RebindEntry entryPrefab;

	[SerializeField]
	private RectTransform sectionHeaderPrefab;

	[SerializeField]
	private ScrollRect scrollRect;

	[Header("Section Labels")]
	[SerializeField]
	private string kbmSectionLabel = "Keyboard & Mouse";

	[SerializeField]
	private string gamepadSectionLabel = "Gamepad";

	[Header("Optional")]
	[SerializeField]
	private Button resetAllButton;

	[SerializeField]
	private GameObject rebindOverlay;

	[SerializeField]
	private TMP_Text overlayLabel;

	[Header("Input Asset")]
	[Tooltip("Drag Assets/InputSystem_Actions.inputactions here (same asset assigned to InputManager).")]
	[SerializeField]
	private InputActionAsset actionAsset;

	[Header("Navigation")]
	[Tooltip("Selectable focus moves to when pressing Up from the first rebind entry (e.g., the Audio tab button).")]
	[SerializeField]
	private Selectable selectOnUpFromFirstEntry;

	[Tooltip("Selectable focus moves to when pressing Down from the last rebind entry (e.g., the Back button).")]
	[SerializeField]
	private Selectable selectOnDownFromLastEntry;

	private readonly List<RebindEntry> entries = new List<RebindEntry>();

	private static readonly CuratedAction[] CuratedActions = new CuratedAction[16]
	{
		new CuratedAction("Move", "Move Up", "up", showOnKbm: true, showOnGamepad: false),
		new CuratedAction("Move", "Move Down", "down", showOnKbm: true, showOnGamepad: false),
		new CuratedAction("Move", "Move Left", "left", showOnKbm: true, showOnGamepad: false),
		new CuratedAction("Move", "Move Right", "right", showOnKbm: true, showOnGamepad: false),
		new CuratedAction("Attack", "Attack"),
		new CuratedAction("AttackSecondary", "Secondary Attack"),
		new CuratedAction("Dash", "Dash"),
		new CuratedAction("Jump", "Jump"),
		new CuratedAction("Crouch", "Crouch"),
		new CuratedAction("Interact", "Interact"),
		new CuratedAction("ToggleInventory", "Inventory"),
		new CuratedAction("DropItem", "Drop Item"),
		new CuratedAction("Previous", "Previous Item"),
		new CuratedAction("Next", "Next Item"),
		new CuratedAction("Pause", "Pause"),
		new CuratedAction("Restart", "Restart Run")
	};

	public Selectable FirstSelectable
	{
		get
		{
			if (entries.Count <= 0)
			{
				return null;
			}
			return entries[0].RebindButton;
		}
	}

	private void Start()
	{
		BuildEntries();
	}

	private void OnEnable()
	{
		InputGlyphs.OnActiveDeviceChanged += RefreshAllEntries;
	}

	private void OnDisable()
	{
		InputGlyphs.OnActiveDeviceChanged -= RefreshAllEntries;
	}

	private void RefreshAllEntries()
	{
		for (int i = 0; i < entries.Count; i++)
		{
			entries[i].RefreshLabels();
		}
	}

	public void BuildEntries()
	{
		if (entries.Count > 0)
		{
			return;
		}
		if (actionAsset == null && InputManager.Move != null)
		{
			actionAsset = InputManager.Move.actionMap.asset;
		}
		if (actionAsset == null)
		{
			Debug.LogError("[RebindMenu] No InputActionAsset assigned and InputManager is not initialized.");
			return;
		}
		if (entryPrefab == null || entryContainer == null)
		{
			Debug.LogError("[RebindMenu] entryPrefab or entryContainer is not assigned.");
			return;
		}
		HideOverlay();
		InputActionMap inputActionMap = actionAsset.FindActionMap("Player");
		if (inputActionMap == null)
		{
			Debug.LogError("[RebindMenu] Action map 'Player' not found in " + actionAsset.name + ".");
			return;
		}
		BuildSection(inputActionMap, kbmSectionLabel, "Keyboard&Mouse");
		BuildSection(inputActionMap, gamepadSectionLabel, "Gamepad");
		WireEntryNavigation();
		if (resetAllButton != null)
		{
			resetAllButton.onClick.AddListener(ResetAll);
		}
	}

	private void WireEntryNavigation()
	{
		for (int i = 0; i < entries.Count; i++)
		{
			Button rebindButton = entries[i].RebindButton;
			Button resetButton = entries[i].ResetButton;
			Selectable selectOnUp = ((i > 0) ? entries[i - 1].RebindButton : selectOnUpFromFirstEntry);
			Selectable selectOnDown = ((i < entries.Count - 1) ? entries[i + 1].RebindButton : selectOnDownFromLastEntry);
			Selectable selectOnUp2 = ((i > 0) ? entries[i - 1].ResetButton : selectOnUpFromFirstEntry);
			Selectable selectOnDown2 = ((i < entries.Count - 1) ? entries[i + 1].ResetButton : selectOnDownFromLastEntry);
			if (rebindButton != null)
			{
				Navigation navigation = rebindButton.navigation;
				navigation.mode = Navigation.Mode.Explicit;
				navigation.selectOnUp = selectOnUp;
				navigation.selectOnDown = selectOnDown;
				navigation.selectOnLeft = null;
				navigation.selectOnRight = resetButton;
				rebindButton.navigation = navigation;
			}
			if (resetButton != null)
			{
				Navigation navigation2 = resetButton.navigation;
				navigation2.mode = Navigation.Mode.Explicit;
				navigation2.selectOnUp = selectOnUp2;
				navigation2.selectOnDown = selectOnDown2;
				navigation2.selectOnLeft = rebindButton;
				navigation2.selectOnRight = null;
				resetButton.navigation = navigation2;
			}
		}
	}

	public void EnsureBuilt()
	{
		BuildEntries();
	}

	public void FocusFirst()
	{
		if (!(EventSystem.current == null) && entries.Count != 0)
		{
			Button rebindButton = entries[0].RebindButton;
			if (rebindButton != null)
			{
				EventSystem.current.SetSelectedGameObject(rebindButton.gameObject);
			}
		}
	}

	private void BuildSection(InputActionMap playerMap, string label, string bindingGroup)
	{
		SpawnSectionHeader(label);
		bool flag = bindingGroup == "Keyboard&Mouse";
		for (int i = 0; i < CuratedActions.Length; i++)
		{
			CuratedAction curatedAction = CuratedActions[i];
			if (!(flag ? (!curatedAction.ShowOnKbm) : (!curatedAction.ShowOnGamepad)))
			{
				InputAction inputAction = playerMap.FindAction(curatedAction.ActionName);
				if (inputAction == null)
				{
					Debug.LogWarning("[RebindMenu] Action '" + curatedAction.ActionName + "' not found in 'Player' map; skipping.");
					continue;
				}
				RebindEntry rebindEntry = Object.Instantiate(entryPrefab, entryContainer);
				rebindEntry.Initialize(inputAction, curatedAction.DisplayName, bindingGroup, this, curatedAction.PartFilter);
				entries.Add(rebindEntry);
			}
		}
	}

	private void SpawnSectionHeader(string label)
	{
		if (!(sectionHeaderPrefab == null))
		{
			TMP_Text componentInChildren = Object.Instantiate(sectionHeaderPrefab, entryContainer).GetComponentInChildren<TMP_Text>(includeInactive: true);
			if (componentInChildren != null)
			{
				componentInChildren.text = label;
			}
		}
	}

	private void OnDestroy()
	{
		if (resetAllButton != null)
		{
			resetAllButton.onClick.RemoveListener(ResetAll);
		}
	}

	public void SaveOverrides()
	{
		if (!(actionAsset == null))
		{
			string text = actionAsset.SaveBindingOverridesAsJson();
			PlayerPrefs.SetString("InputBindings", text ?? string.Empty);
			PlayerPrefs.Save();
		}
	}

	public void ResetAll()
	{
		if (!(actionAsset == null))
		{
			actionAsset.RemoveAllBindingOverrides();
			PlayerPrefs.DeleteKey("InputBindings");
			PlayerPrefs.Save();
			for (int i = 0; i < entries.Count; i++)
			{
				entries[i].RefreshLabels();
			}
		}
	}

	public void ShowOverlay(string label)
	{
		if (rebindOverlay != null)
		{
			rebindOverlay.SetActive(value: true);
		}
		if (overlayLabel != null)
		{
			overlayLabel.text = label;
		}
	}

	public void HideOverlay()
	{
		if (rebindOverlay != null)
		{
			rebindOverlay.SetActive(value: false);
		}
	}
}
