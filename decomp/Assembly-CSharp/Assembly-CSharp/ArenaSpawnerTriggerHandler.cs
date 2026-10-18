using UnityEngine;

public class ArenaSpawnerTriggerHandler : MonoBehaviour
{
	private ArenaEnemySpawner spawner;

	public void Initialize(ArenaEnemySpawner arenaSpawner)
	{
		spawner = arenaSpawner;
	}

	private void OnTriggerEnter(Collider other)
	{
		if (!(spawner == null) && IsPlayer(other))
		{
			spawner.OnPlayerEnterTrigger(GetComponent<Collider>());
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
		return false;
	}
}
