using UnityEngine;

public class EnemyGrabDetector : MonoBehaviour
{
	private EnemyAI enemyAI;

	public void Initialize(EnemyAI enemy)
	{
		enemyAI = enemy;
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other.CompareTag("Player") && enemyAI != null)
		{
			enemyAI.OnPlayerGrabDetected();
		}
	}

	private void OnTriggerStay(Collider other)
	{
		if (other.CompareTag("Player") && enemyAI != null && !enemyAI.HasGrabbedThisAttempt)
		{
			enemyAI.OnPlayerGrabDetected();
		}
	}
}
