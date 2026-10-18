using UnityEngine;

public class ProximityDragonAttackDetector : MonoBehaviour
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
