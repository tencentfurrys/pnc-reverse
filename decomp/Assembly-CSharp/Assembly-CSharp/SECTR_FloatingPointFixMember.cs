using UnityEngine;

public class SECTR_FloatingPointFixMember : MonoBehaviour
{
	protected void OnEnable()
	{
		SECTR_FloatingPointFix.Instance.AddMember(this);
	}

	protected void OnDestroy()
	{
		if (SECTR_FloatingPointFix.IsActive)
		{
			SECTR_FloatingPointFix.Instance.RemoveMember(this);
		}
	}

	private void Start()
	{
	}

	private void Update()
	{
	}
}
