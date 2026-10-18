using UnityEngine;

public class DragonAttackDetector : MonoBehaviour
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
			dragonAI.OnPlayerAttackDetected();
		}
	}

	private void OnTriggerStay(Collider other)
	{
		if (other.CompareTag("Player") && dragonAI != null && !dragonAI.HasDealtDamageThisAttack)
		{
			dragonAI.OnPlayerAttackDetected();
		}
	}
}
