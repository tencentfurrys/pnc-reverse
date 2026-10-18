using System.Collections;
using TMPro;
using UnityEngine;

public class NoteViewer : MonoBehaviour
{
	[Header("UI References")]
	[SerializeField]
	private GameObject notePanel;

	[SerializeField]
	private TextMeshProUGUI titleText;

	[SerializeField]
	private TextMeshProUGUI bodyText;

	[SerializeField]
	private Animator animator;

	[Header("Animation States")]
	[SerializeField]
	private string showAnimation = "Note Panel Show";

	[SerializeField]
	private string hideAnimation = "Note Panel Hide";

	private float openedTime;

	private CursorLockMode previousCursorLockState;

	private bool previousCursorVisible;

	private float closedTime;

	public static NoteViewer Instance { get; private set; }

	public bool IsOpen { get; private set; }

	private void Awake()
	{
		Instance = this;
		if (notePanel != null)
		{
			notePanel.SetActive(value: false);
		}
		if (animator != null)
		{
			animator.updateMode = AnimatorUpdateMode.UnscaledTime;
		}
	}

	private void Update()
	{
		if (IsOpen && ((InputManager.Pause != null && InputManager.Pause.WasPressedThisFrame()) || (InputManager.Move != null && InputManager.Move.WasPressedThisFrame())))
		{
			Hide();
		}
	}

	public bool WasRecentlyClosed()
	{
		if (!IsOpen)
		{
			return Time.unscaledTime - closedTime < 0.2f;
		}
		return false;
	}

	public void Show(string title, string body)
	{
		if (!IsOpen)
		{
			IsOpen = true;
			openedTime = Time.unscaledTime;
			if (titleText != null)
			{
				titleText.text = title;
			}
			if (bodyText != null)
			{
				bodyText.text = body;
			}
			if (notePanel != null)
			{
				notePanel.SetActive(value: true);
			}
			if (animator != null)
			{
				animator.Play(showAnimation);
			}
			Time.timeScale = 0f;
			previousCursorLockState = Cursor.lockState;
			previousCursorVisible = Cursor.visible;
			Cursor.lockState = CursorLockMode.None;
			Cursor.visible = true;
		}
	}

	public void Hide()
	{
		if (IsOpen)
		{
			IsOpen = false;
			closedTime = Time.unscaledTime;
			if (animator != null)
			{
				animator.Play(hideAnimation);
			}
			Time.timeScale = 1f;
			StartCoroutine(RestoreCursorDelayed());
		}
	}

	private IEnumerator RestoreCursorDelayed()
	{
		yield return null;
		Cursor.lockState = previousCursorLockState;
		Cursor.visible = previousCursorVisible;
	}
}
