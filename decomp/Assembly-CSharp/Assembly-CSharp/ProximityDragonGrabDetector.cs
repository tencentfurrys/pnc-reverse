using UnityEngine;

public class ProximityDragonGrabDetector : MonoBehaviour
{
	private ProximityDragonEnemyAI dragonAI;

	public void Initialize(ProximityDragonEnemyAI dragon)
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
