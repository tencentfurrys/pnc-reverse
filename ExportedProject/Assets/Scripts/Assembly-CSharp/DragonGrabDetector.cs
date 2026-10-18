using UnityEngine;

public class DragonGrabDetector : MonoBehaviour
{
	private DragonEnemyAI dragonAI;

	public void Initialize(DragonEnemyAI dragon)
	{
		dragonAI = dragon;
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other.CompareTag("Player") && dragonAI != null)
		{
			dragonAI.OnPlayerGrabDetected();
		}
	}

	private void OnTriggerStay(Collider other)
	{
		if (other.CompareTag("Player") && dragonAI != null && !dragonAI.HasGrabbedThisAttempt)
		{
			dragonAI.OnPlayerGrabDetected();
		}
	}
}
