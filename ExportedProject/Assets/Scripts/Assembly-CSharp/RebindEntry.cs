using System;
using System.Collections.Generic;
using PixelCrushers.GridController.Glyphs;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.UI;

public class RebindEntry : MonoBehaviour
{
	public const string KbmGroup = "Keyboard&Mouse";

	public const string GamepadGroup = "Gamepad";

	private const string ListeningPlaceholder = "…";

	[Header("Labels")]
	[SerializeField]
	private TMP_Text actionLabel;

	[SerializeField]
	private TMP_Text bindingLabel;

	[Header("Buttons")]
	[SerializeField]
	private Button rebindButton;

	[SerializeField]
	private Button resetButton;

	private InputAction action;

	private string displayName;

	private string bindingGroup;

	private string partFilter;

	private RebindMenu owner;

	private InputActionRebindingExtensions.RebindingOperation activeOperation;

	private bool wasEnabledBeforeRebind;

	public Button RebindButton => rebindButton;

	public Button ResetButton => resetButton;

	private DeviceFamily Family
	{
		get
		{
			if (!(bindingGroup == "Keyboard&Mouse"))
			{
				return InputGlyphs.CurrentGamepadFamily;
			}
			return DeviceFamily.KeyboardMouse;
		}
	}

	public void Initialize(InputAction inputAction, string display, string group, RebindMenu rebindMenu, string compositePartFilter = null)
	{
		action = inputAction;
		displayName = display;
		bindingGroup = group;
		partFilter = compositePartFilter;
		owner = rebindMenu;
		if (actionLabel != null)
		{
			actionLabel.text = displayName;
		}
		if (rebindButton != null)
		{
			rebindButton.onClick.RemoveAllListeners();
			rebindButton.onClick.AddListener(StartRebind);
		}
		if (resetButton != null)
		{
			resetButton.onClick.RemoveAllListeners();
			resetButton.onClick.AddListener(ResetBinding);
		}
		RefreshLabels();
	}

	public void RefreshLabels()
	{
		if (action != null && !(bindingLabel == null))
		{
			bindingLabel.text = BuildDisplayString();
		}
	}

	private string BuildDisplayString()
	{
		List<int> bindingIndices = GetBindingIndices();
		if (bindingIndices.Count == 0)
		{
			return "—";
		}
		DeviceFamily family = Family;
		List<string> list = new List<string>(bindingIndices.Count);
		TMP_SpriteAsset tMP_SpriteAsset = null;
		for (int i = 0; i < bindingIndices.Count; i++)
		{
			string bindingMarkup = InputGlyphs.GetBindingMarkup(action, bindingIndices[i], family, out var asset);
			if (asset != null)
			{
				tMP_SpriteAsset = asset;
			}
			if (!string.IsNullOrEmpty(bindingMarkup))
			{
				list.Add(bindingMarkup);
			}
		}
		if (tMP_SpriteAsset != null && bindingLabel != null)
		{
			bindingLabel.spriteAsset = tMP_SpriteAsset;
		}
		if (list.Count <= 0)
		{
			return "—";
		}
		return string.Join(" ", list);
	}

	private List<int> GetBindingIndices()
	{
		List<int> list = new List<int>();
		if (action == null)
		{
			return list;
		}
		ReadOnlyArray<InputBinding> bindings = action.bindings;
		for (int i = 0; i < bindings.Count; i++)
		{
			InputBinding binding = bindings[i];
			if (binding.isComposite)
			{
				List<int> list2 = new List<int>();
				for (int j = i + 1; j < bindings.Count && bindings[j].isPartOfComposite; j++)
				{
					if (HasGroup(bindings[j], bindingGroup) && (string.IsNullOrEmpty(partFilter) || string.Equals(bindings[j].name, partFilter, StringComparison.OrdinalIgnoreCase)))
					{
						list2.Add(j);
					}
				}
				if (list2.Count > 0)
				{
					list.AddRange(list2);
					return list;
				}
			}
			else if (!binding.isPartOfComposite && HasGroup(binding, bindingGroup))
			{
				list.Add(i);
				return list;
			}
		}
		return list;
	}

	private static bool HasGroup(InputBinding binding, string group)
	{
		if (string.IsNullOrEmpty(binding.groups))
		{
			return false;
		}
		string[] array = binding.groups.Split(';');
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i] == group)
			{
				return true;
			}
		}
		return false;
	}

	private void StartRebind()
	{
		if (activeOperation != null || action == null)
		{
			return;
		}
		List<int> bindingIndices = GetBindingIndices();
		if (bindingIndices.Count != 0)
		{
			wasEnabledBeforeRebind = action.enabled;
			if (wasEnabledBeforeRebind)
			{
				action.Disable();
			}
			if (bindingLabel != null)
			{
				bindingLabel.text = "…";
			}
			RebindNext(bindingIndices, 0);
		}
	}

	private void RebindNext(List<int> indices, int step)
	{
		if (step >= indices.Count)
		{
			FinishRebind();
			return;
		}
		bool flag = bindingGroup == "Keyboard&Mouse";
		int bindingIndex = indices[step];
		string partName = GetPartName(bindingIndex);
		string text = (flag ? "Press a key" : "Press a button");
		string label = (string.IsNullOrEmpty(partName) ? (text + " for " + displayName + "…") : (text + " for " + displayName + " (" + Capitalize(partName) + ")…"));
		owner?.ShowOverlay(label);
		InputActionRebindingExtensions.RebindingOperation rebindingOperation = action.PerformInteractiveRebinding(bindingIndex).WithBindingGroup(bindingGroup).WithCancelingThrough("<Keyboard>/escape");
		if (flag)
		{
			rebindingOperation.WithControlsExcluding("<Mouse>/position").WithControlsExcluding("<Mouse>/delta").WithControlsExcluding("<Mouse>/scroll");
		}
		rebindingOperation.OnCancel((InputActionRebindingExtensions.RebindingOperation op) =>
		{
			op.Dispose();
			activeOperation = null;
			FinishRebind();
		}).OnComplete((InputActionRebindingExtensions.RebindingOperation op) =>
		{
			op.Dispose();
			activeOperation = null;
			RebindNext(indices, step + 1);
		});
		activeOperation = rebindingOperation;
		rebindingOperation.Start();
	}

	private string GetPartName(int bindingIndex)
	{
		InputBinding inputBinding = action.bindings[bindingIndex];
		if (!inputBinding.isPartOfComposite)
		{
			return null;
		}
		return inputBinding.name;
	}

	private static string Capitalize(string s)
	{
		if (string.IsNullOrEmpty(s))
		{
			return s;
		}
		return char.ToUpper(s[0]) + s.Substring(1);
	}

	private void ResetBinding()
	{
		if (action != null)
		{
			List<int> bindingIndices = GetBindingIndices();
			for (int i = 0; i < bindingIndices.Count; i++)
			{
				action.RemoveBindingOverride(bindingIndices[i]);
			}
			owner?.SaveOverrides();
			RefreshLabels();
		}
	}

	private void FinishRebind()
	{
		if (wasEnabledBeforeRebind && action != null && !action.enabled)
		{
			action.Enable();
		}
		owner?.HideOverlay();
		owner?.SaveOverrides();
		RefreshLabels();
	}

	private void OnDisable()
	{
		if (activeOperation != null)
		{
			activeOperation.Dispose();
			activeOperation = null;
			if (wasEnabledBeforeRebind && action != null && !action.enabled)
			{
				action.Enable();
			}
		}
	}
}
