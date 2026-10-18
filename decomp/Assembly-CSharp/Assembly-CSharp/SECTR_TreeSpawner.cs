using UnityEngine;

public class SECTR_TreeSpawner : MonoBehaviour
{
	public GameObject treeToSpawn;

	public float spawnThreshold = 0.5f;

	public bool spawnEnabled = true;

	private void Start()
	{
	}

	private void Update()
	{
	}

	private void OnDrawGizmos()
	{
		if (!spawnEnabled)
		{
			return;
		}
		Terrain[] activeTerrains = Terrain.activeTerrains;
		foreach (Terrain terrain in activeTerrains)
		{
			if (terrain.terrainData.treePrototypes.Length != 0)
			{
				continue;
			}
			Debug.Log("Generating new trees for terrain " + terrain.name);
			TreePrototype treePrototype = new TreePrototype
			{
				prefab = treeToSpawn,
				bendFactor = 0f
			};
			TreePrototype[] treePrototypes = new TreePrototype[1] { treePrototype };
			terrain.terrainData.treePrototypes = treePrototypes;
			for (float num = 0f; num < terrain.terrainData.size.x; num++)
			{
				for (float num2 = 0f; num2 < terrain.terrainData.size.x; num2++)
				{
					if (Random.value >= spawnThreshold)
					{
						terrain.AddTreeInstance(new TreeInstance
						{
							position = new Vector3(num / terrain.terrainData.size.x, 0f, num2 / terrain.terrainData.size.z),
							prototypeIndex = 0,
							widthScale = 1f,
							heightScale = 1f,
							color = Color.white,
							lightmapColor = Color.white
						});
					}
				}
			}
		}
		spawnEnabled = false;
	}
}
