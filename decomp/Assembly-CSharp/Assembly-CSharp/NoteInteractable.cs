using UnityEngine;

public class NoteInteractable : MonoBehaviour, IPlayerInteractable
{
	[Header("Note Content")]
	[SerializeField]
	private string noteTitle = "Note";

	[TextArea(3, 10)]
	[SerializeField]
	private string noteBody = "Enter note text here...";

	[Header("Collider")]
	[SerializeField]
	private Collider interactionCollider;

	[Header("Outline Effect")]
	[SerializeField]
	private SpriteRenderer spriteRenderer;

	[SerializeField]
	private float outlineAlphaInRange = 1f;

	[SerializeField]
	private float outlineAlphaOutOfRange;

	private bool isBeingLookedAt;

	public bool CanInteract
	{
		get
		{
			if (!(NoteViewer.Instance == null))
			{
				return !NoteViewer.Instance.IsOpen;
			}
			return true;
		}
	}

	public float InteractionRange => 0f;

	private void Awake()
	{
		if (spriteRenderer == null)
		{
			spriteRenderer = GetComponent<SpriteRenderer>();
		}
		if (spriteRenderer != null && spriteRenderer.material != null)
		{
			spriteRenderer.material.SetFloat("_OutlineAlpha", outlineAlphaOutOfRange);
		}
	}

	private void Update()
	{
		if (spriteRenderer != null && spriteRenderer.material != null)
		{
			float value = (isBeingLookedAt ? outlineAlphaInRange : outlineAlphaOutOfRange);
			spriteRenderer.material.SetFloat("_OutlineAlpha", value);
		}
	}

	public void OnLookedAt(GameObject player)
	{
		isBeingLookedAt = true;
	}

	public void OnLookedAway()
	{
		isBeingLookedAt = false;
	}

	public void OnInteracted()
	{
		if (NoteViewer.Instance != null)
		{
			NoteViewer.Instance.Show(noteTitle, noteBody);
		}
	}

	private void OnValidate()
	{
		if (spriteRenderer == null)
		{
			spriteRenderer = GetComponent<SpriteRenderer>();
		}
	}
}
