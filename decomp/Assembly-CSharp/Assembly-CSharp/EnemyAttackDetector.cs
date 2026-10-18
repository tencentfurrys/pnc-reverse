using UnityEngine;

public class EnemyAttackDetector : MonoBehaviour
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
			enemyAI.OnPlayerAttackDetected();
		}
	}

	private void OnTriggerStay(Collider other)
	{
		if (other.CompareTag("Player") && enemyAI != null && !enemyAI.HasDealtDamageThisAttack)
		{
			enemyAI.OnPlayerAttackDetected();
		}
	}
}
