using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
	[SerializeField]
	private Image fillImage;

	[SerializeField]
	private bool hideWhenFull = true;

	[SerializeField]
	private float fillLerpSpeed = 5f;

	private Transform _viewPoint;

	private CanvasGroup canvasGroup;

	private float targetFillAmount = 1f;

	private bool hasBeenDamaged;

	private void Start()
	{
		Camera main = Camera.main;
		if (main != null)
		{
			_viewPoint = ((main.transform.parent != null) ? main.transform.parent : main.transform);
		}
		canvasGroup = GetComponent<CanvasGroup>();
		if (canvasGroup == null)
		{
			canvasGroup = base.gameObject.AddComponent<CanvasGroup>();
		}
		if (hideWhenFull)
		{
			canvasGroup.alpha = 0f;
		}
		if (fillImage != null)
		{
			fillImage.fillAmount = 1f;
		}
	}

	public void UpdateHealth(float healthPercent)
	{
		targetFillAmount = Mathf.Clamp01(healthPercent);
		if (!hasBeenDamaged && healthPercent < 1f)
		{
			hasBeenDamaged = true;
		}
		if (hasBeenDamaged && canvasGroup != null)
		{
			canvasGroup.alpha = 1f;
		}
	}

	private void LateUpdate()
	{
		if (_viewPoint == null)
		{
			Camera main = Camera.main;
			if (main == null)
			{
				return;
			}
			_viewPoint = ((main.transform.parent != null) ? main.transform.parent : main.transform);
		}
		Vector3 vector = _viewPoint.position - base.transform.position;
		vector.y = 0f;
		if (vector != Vector3.zero)
		{
			base.transform.forward = -vector;
		}
		if (fillImage != null && fillImage.fillAmount != targetFillAmount)
		{
			fillImage.fillAmount = Mathf.Lerp(fillImage.fillAmount, targetFillAmount, Time.deltaTime * fillLerpSpeed);
			if (Mathf.Abs(fillImage.fillAmount - targetFillAmount) < 0.01f)
			{
				fillImage.fillAmount = targetFillAmount;
			}
		}
	}
}
