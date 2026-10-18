using UnityEngine;

[CreateAssetMenu(fileName = "New Enemy Data", menuName = "Combat/Enemy Data")]
public class EnemyData : ScriptableObject
{
	[Header("Enemy Info")]
	public string enemyName = "Enemy";

	[Header("Prefab Variants")]
	[Tooltip("All available prefab variants for this enemy (base + alts). One is chosen randomly at spawn time.")]
	public GameObject[] prefabVariants;

	public GameObject GetRandomPrefab()
	{
		if (prefabVariants == null || prefabVariants.Length == 0)
		{
			return null;
		}
		return prefabVariants[Random.Range(0, prefabVariants.Length)];
	}
}
