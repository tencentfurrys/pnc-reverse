using UnityEngine;

public class Billboard : MonoBehaviour
{
	[Header("Billboard Settings")]
	public bool lockYAxis;

	private Transform _viewPoint;

	private void Start()
	{
		Camera main = Camera.main;
		if (main != null)
		{
			_viewPoint = ((main.transform.parent != null) ? main.transform.parent : main.transform);
		}
	}

	private void LateUpdate()
	{
		if (_viewPoint == null)
		{
			return;
		}
		Vector3 vector = _viewPoint.position - base.transform.position;
		if (!(vector == Vector3.zero))
		{
			if (lockYAxis)
			{
				vector.y = 0f;
			}
			if (vector != Vector3.zero)
			{
				base.transform.forward = -vector;
			}
		}
	}
}
