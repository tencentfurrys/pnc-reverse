using System.Collections;
using UnityEngine;

public class SecretDoorBlock : MonoBehaviour
{
	[Header("Movement Settings")]
	[SerializeField]
	private Vector3 moveDirection = Vector3.right;

	[SerializeField]
	private float moveDistance = 5f;

	[SerializeField]
	private float moveSpeed = 2f;

	[SerializeField]
	private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

	[Header("Block State")]
	[SerializeField]
	private bool startOpen;

	[SerializeField]
	private bool allowToggle = true;

	[SerializeField]
	private bool oneTimeUse;

	[Header("Audio")]
	[SerializeField]
	private AudioSource audioSource;

	[SerializeField]
	private AudioClip moveSound;

	[SerializeField]
	private AudioClip stopSound;

	[Header("Effects")]
	[SerializeField]
	private GameObject moveEffect;

	[SerializeField]
	private bool createDustEffect = true;

	[Header("Debug")]
	[SerializeField]
	private bool debugMode = true;

	[SerializeField]
	private bool showGizmos = true;

	private Vector3 closedPosition;

	private Vector3 openPosition;

	private bool isOpen;

	private bool isMoving;

	private bool hasBeenUsed;

	public bool IsOpen => isOpen;

	public bool IsMoving => isMoving;

	public bool CanActivate
	{
		get
		{
			if (!isMoving)
			{
				if (oneTimeUse)
				{
					return !hasBeenUsed;
				}
				return true;
			}
			return false;
		}
	}

	private void Awake()
	{
		closedPosition = base.transform.localPosition;
		openPosition = closedPosition + moveDirection.normalized * moveDistance;
		isOpen = startOpen;
		if (startOpen)
		{
			base.transform.localPosition = openPosition;
		}
		if (audioSource == null)
		{
			audioSource = GetComponent<AudioSource>();
			if (audioSource == null)
			{
				audioSource = base.gameObject.AddComponent<AudioSource>();
			}
		}
		audioSource.playOnAwake = false;
	}

	public void ActivateBlock()
	{
		if (CanActivate)
		{
			if (oneTimeUse)
			{
				hasBeenUsed = true;
			}
			bool flag = !allowToggle || !isOpen;
			if (flag != isOpen)
			{
				StartCoroutine(MoveBlock(flag));
			}
		}
	}

	private IEnumerator MoveBlock(bool moveToOpenPosition)
	{
		isMoving = true;
		Vector3 startPos = base.transform.localPosition;
		Vector3 targetPos = (moveToOpenPosition ? openPosition : closedPosition);
		PlaySound(moveSound);
		if (moveEffect != null)
		{
			Object.Destroy(Object.Instantiate(moveEffect, base.transform.position, base.transform.rotation), 5f);
		}
		if (createDustEffect)
		{
			CreateDustEffect();
		}
		float elapsed = 0f;
		float duration = Vector3.Distance(startPos, targetPos) / moveSpeed;
		while (elapsed < duration)
		{
			elapsed += Time.deltaTime;
			float time = elapsed / duration;
			float t = moveCurve.Evaluate(time);
			base.transform.localPosition = Vector3.Lerp(startPos, targetPos, t);
			yield return null;
		}
		base.transform.localPosition = targetPos;
		isOpen = moveToOpenPosition;
		isMoving = false;
		PlaySound(stopSound);
	}

	private void CreateDustEffect()
	{
		for (int i = 0; i < Random.Range(10, 20); i++)
		{
			StartCoroutine(CreateDustParticle());
		}
	}

	private IEnumerator CreateDustParticle()
	{
		GameObject dust = GameObject.CreatePrimitive(PrimitiveType.Cube);
		dust.transform.localScale = Vector3.one * Random.Range(0.05f, 0.15f);
		dust.transform.position = base.transform.position + Random.insideUnitSphere * 0.5f;
		Renderer dustRenderer = dust.GetComponent<Renderer>();
		dustRenderer.material.color = new Color(0.6f, 0.4f, 0.2f, 0.8f);
		Object.Destroy(dust.GetComponent<Collider>());
		Vector3 velocity = Random.insideUnitSphere * 2f;
		velocity.y = Mathf.Abs(velocity.y);
		float lifetime = Random.Range(1f, 3f);
		float elapsed = 0f;
		while (elapsed < lifetime && dust != null)
		{
			elapsed += Time.deltaTime;
			dust.transform.position += velocity * Time.deltaTime;
			velocity.y -= 9.81f * Time.deltaTime;
			Color color = dustRenderer.material.color;
			color.a = 1f - elapsed / lifetime;
			dustRenderer.material.color = color;
			yield return null;
		}
		if (dust != null)
		{
			Object.Destroy(dust);
		}
	}

	private void PlaySound(AudioClip clip)
	{
		if (clip != null && audioSource != null)
		{
			audioSource.clip = clip;
			audioSource.Play();
		}
	}

	public void SetDoorState(bool open, bool instant = false)
	{
		if (!isMoving)
		{
			if (instant)
			{
				isOpen = open;
				base.transform.localPosition = (open ? openPosition : closedPosition);
			}
			else
			{
				StartCoroutine(MoveBlock(open));
			}
		}
	}

	public void ResetBlock()
	{
		StopAllCoroutines();
		isMoving = false;
		hasBeenUsed = false;
		SetDoorState(startOpen, instant: true);
	}

	private void OnDrawGizmosSelected()
	{
		if (showGizmos)
		{
			Vector3 obj = (Application.isPlaying ? closedPosition : base.transform.position);
			Vector3 vector = obj + moveDirection.normalized * moveDistance;
			Gizmos.color = Color.red;
			Gizmos.DrawWireCube(obj, base.transform.localScale);
			Gizmos.color = Color.green;
			Gizmos.DrawWireCube(vector, base.transform.localScale);
			Gizmos.color = Color.blue;
			Gizmos.DrawLine(obj, vector);
			if (Application.isPlaying)
			{
				Gizmos.color = (isOpen ? Color.green : Color.red);
				Gizmos.DrawSphere(base.transform.position, 0.2f);
			}
		}
	}

	[ContextMenu("Test Activate")]
	public void TestActivate()
	{
		ActivateBlock();
	}

	[ContextMenu("Reset Block")]
	public void TestReset()
	{
		ResetBlock();
	}
}
