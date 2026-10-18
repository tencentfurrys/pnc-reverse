using UnityEngine;

public class EnemySpawnerTriggerHandler : MonoBehaviour
{
	private EnemySpawner spawner;

	public void Initialize(EnemySpawner enemySpawner)
	{
		spawner = enemySpawner;
	}

	private void OnTriggerEnter(Collider other)
	{
		if (!(spawner == null) && IsPlayer(other))
		{
			spawner.OnPlayerEnterTrigger(GetComponent<Collider>());
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (!(spawner == null) && IsPlayer(other))
		{
			spawner.OnPlayerExitTrigger(GetComponent<Collider>());
		}
	}

	private bool IsPlayer(Collider other)
	{
		if (other.CompareTag("Player"))
		{
			return true;
		}
		if (other.GetComponent<CharacterController>() != null)
		{
			return true;
		}
		if (other.GetComponent<Rigidbody>() != null && spawner != null && spawner.player == other.transform)
		{
			return true;
		}
		return false;
	}
}
