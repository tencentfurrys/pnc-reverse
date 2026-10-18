using TMPro;
using UnityEngine;

public class DamageNumber : MonoBehaviour
{
	[Header("Animation Settings")]
	[SerializeField]
	private float lifetime = 1f;

	[SerializeField]
	private float floatDistance = 1.5f;

	[SerializeField]
	private AnimationCurve scaleCurve;

	[SerializeField]
	private AnimationCurve alphaCurve;

	private TextMeshProUGUI damageText;

	private CanvasGroup canvasGroup;

	private Camera mainCamera;

	private Vector3 startPosition;

	private float elapsedTime;

	private bool isActive;

	private float scaleMultiplier = 1f;

	private void Awake()
	{
		damageText = GetComponentInChildren<TextMeshProUGUI>();
		canvasGroup = GetComponent<CanvasGroup>();
		if (canvasGroup == null)
		{
			canvasGroup = base.gameObject.AddComponent<CanvasGroup>();
		}
		if (scaleCurve == null || scaleCurve.keys.Length == 0)
		{
			scaleCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.1f, 1.3f), new Keyframe(0.2f, 1f), new Keyframe(1f, 1f));
		}
		if (alphaCurve == null || alphaCurve.keys.Length == 0)
		{
			alphaCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.6f, 1f), new Keyframe(1f, 0f));
		}
	}

	private void Start()
	{
		mainCamera = Camera.main;
	}

	public void Initialize(int damage, Color color, bool isCritical = false)
	{
		string text = (isCritical ? (damage + "!") : damage.ToString());
		Initialize(text, color, isCritical ? 1.5f : 1f);
	}

	public void Initialize(string text, Color color, float scale = 1f)
	{
		if (damageText == null)
		{
			damageText = GetComponentInChildren<TextMeshProUGUI>();
		}
		if (damageText != null)
		{
			damageText.text = text;
			damageText.color = color;
		}
		scaleMultiplier = scale;
		startPosition = base.transform.position;
		elapsedTime = 0f;
		isActive = true;
		base.transform.localScale = Vector3.zero;
		if (canvasGroup != null)
		{
			canvasGroup.alpha = 1f;
		}
	}

	private void LateUpdate()
	{
		if (!isActive)
		{
			return;
		}
		if (mainCamera == null)
		{
			mainCamera = Camera.main;
			if (mainCamera == null)
			{
				return;
			}
		}
		base.transform.rotation = mainCamera.transform.rotation;
		elapsedTime += Time.deltaTime;
		float num = Mathf.Clamp01(elapsedTime / lifetime);
		base.transform.position = startPosition + Vector3.up * (floatDistance * num);
		float num2 = scaleCurve.Evaluate(num);
		base.transform.localScale = Vector3.one * num2 * scaleMultiplier;
		if (canvasGroup != null)
		{
			canvasGroup.alpha = alphaCurve.Evaluate(num);
		}
		if (elapsedTime >= lifetime)
		{
			ReturnToPool();
		}
	}

	private void ReturnToPool()
	{
		isActive = false;
		if (DamageNumberManager.Instance != null)
		{
			DamageNumberManager.Instance.ReturnToPool(this);
		}
		else
		{
			base.gameObject.SetActive(value: false);
		}
	}

	public void ResetState()
	{
		isActive = false;
		elapsedTime = 0f;
		scaleMultiplier = 1f;
		base.transform.localScale = Vector3.zero;
		if (canvasGroup != null)
		{
			canvasGroup.alpha = 0f;
		}
	}
}
