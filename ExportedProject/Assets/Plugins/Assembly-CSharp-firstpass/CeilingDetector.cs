using UnityEngine;

public class CeilingDetector : MonoBehaviour
{
	public enum CeilingDetectionMethod
	{
		OnlyCheckFirstContact = 0,
		CheckAllContacts = 1,
		CheckAverageOfAllContacts = 2
	}

	private bool ceilingWasHit;

	public float ceilingAngleLimit = 10f;

	public CeilingDetectionMethod ceilingDetectionMethod;

	public bool isInDebugMode;

	private float debugDrawDuration = 2f;

	private Transform tr;

	private void Awake()
	{
		tr = base.transform;
	}

	private void OnCollisionEnter(Collision _collision)
	{
		CheckCollisionAngles(_collision);
	}

	private void OnCollisionStay(Collision _collision)
	{
		CheckCollisionAngles(_collision);
	}

	private void CheckCollisionAngles(Collision _collision)
	{
		float num = 0f;
		if (ceilingDetectionMethod == CeilingDetectionMethod.OnlyCheckFirstContact)
		{
			num = Vector3.Angle(-tr.up, _collision.contacts[0].normal);
			if (num < ceilingAngleLimit)
			{
				ceilingWasHit = true;
			}
			if (isInDebugMode)
			{
				Debug.DrawRay(_collision.contacts[0].point, _collision.contacts[0].normal, Color.red, debugDrawDuration);
			}
		}
		if (ceilingDetectionMethod == CeilingDetectionMethod.CheckAllContacts)
		{
			for (int i = 0; i < _collision.contacts.Length; i++)
			{
				num = Vector3.Angle(-tr.up, _collision.contacts[i].normal);
				if (num < ceilingAngleLimit)
				{
					ceilingWasHit = true;
				}
				if (isInDebugMode)
				{
					Debug.DrawRay(_collision.contacts[i].point, _collision.contacts[i].normal, Color.red, debugDrawDuration);
				}
			}
		}
		if (ceilingDetectionMethod != CeilingDetectionMethod.CheckAverageOfAllContacts)
		{
			return;
		}
		for (int j = 0; j < _collision.contacts.Length; j++)
		{
			num += Vector3.Angle(-tr.up, _collision.contacts[j].normal);
			if (isInDebugMode)
			{
				Debug.DrawRay(_collision.contacts[j].point, _collision.contacts[j].normal, Color.red, debugDrawDuration);
			}
		}
		if (num / (float)_collision.contacts.Length < ceilingAngleLimit)
		{
			ceilingWasHit = true;
		}
	}

	public bool HitCeiling()
	{
		return ceilingWasHit;
	}

	public void ResetFlags()
	{
		ceilingWasHit = false;
	}
}
