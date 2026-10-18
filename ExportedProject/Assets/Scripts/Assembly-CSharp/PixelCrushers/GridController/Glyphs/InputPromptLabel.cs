using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PixelCrushers.GridController.Glyphs
{
	public class InputPromptLabel : MonoBehaviour
	{
		[Tooltip("Text component the prompt is written into. Auto-found in children if left empty.")]
		[SerializeField]
		private TMP_Text label;

		[Tooltip("Action whose glyph is shown. Optional if UseInteractAction is ticked.")]
		[SerializeField]
		private InputActionReference action;

		[Tooltip("Ignore the action field and use InputManager.Interact (the default interaction).")]
		[SerializeField]
		private bool useInteractAction = true;

		[Tooltip("Format string; {0} is replaced by the glyph. e.g. \"Press {0} to view\".")]
		[SerializeField]
		private string template = "Press {0}";

		private void Awake()
		{
			if (label == null)
			{
				label = GetComponentInChildren<TMP_Text>(includeInactive: true);
			}
		}

		private void OnEnable()
		{
			InputGlyphs.OnActiveDeviceChanged += Refresh;
			Refresh();
		}

		private void OnDisable()
		{
			InputGlyphs.OnActiveDeviceChanged -= Refresh;
		}

		public void SetTemplate(string newTemplate)
		{
			template = newTemplate;
			if (base.isActiveAndEnabled)
			{
				Refresh();
			}
		}

		public void Refresh()
		{
			if (label == null)
			{
				return;
			}
			InputAction inputAction = ResolveAction();
			if (inputAction == null)
			{
				label.text = string.Format(template, string.Empty);
				return;
			}
			string actionMarkup = InputGlyphs.GetActionMarkup(inputAction, out var asset);
			if (asset != null)
			{
				label.spriteAsset = asset;
			}
			label.text = string.Format(template, actionMarkup);
		}

		private InputAction ResolveAction()
		{
			if (useInteractAction)
			{
				return InputManager.Interact;
			}
			if (!(action != null))
			{
				return null;
			}
			return action.action;
		}
	}
}
