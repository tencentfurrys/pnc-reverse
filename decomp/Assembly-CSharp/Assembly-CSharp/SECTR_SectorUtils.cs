using System;
using System.Collections.Generic;
using UnityEngine;

public class SECTR_SectorUtils : MonoBehaviour
{
	public static bool DoHaveSectors()
	{
		return FindObjectOfType<SECTR_Sector>() != null;
	}

	public new static T FindObjectOfType<T>() where T : UnityEngine.Object
	{
		return UnityEngine.Object.FindFirstObjectByType(typeof(T)) as T;
	}

	public new static T[] FindObjectsOfType<T>() where T : UnityEngine.Object
	{
		return UnityEngine.Object.FindObjectsByType(typeof(T), FindObjectsSortMode.None) as T[];
	}

	public static void SendObjectsIntoSectors(ref List<GameObject> parentsUndoList, List<GameObject> gameObjects, Vector3 parentLocation, SECTR_Constants.ReparentingMode localizeBy = SECTR_Constants.ReparentingMode.Bounds, bool mergeSpawns = true, bool doGlobalParenting = false)
	{
		SendObjectsIntoSectors(ref parentsUndoList, gameObjects, parentLocation, new string[0], localizeBy, mergeSpawns, doGlobalParenting);
	}

	public static void SendObjectsIntoSectors(ref List<GameObject> parentsUndoList, List<GameObject> gameObjects, Vector3 parentLocation, string[] hierarchy, SECTR_Constants.ReparentingMode localizeBy = SECTR_Constants.ReparentingMode.Bounds, bool mergeSpawns = true, bool doGlobalParenting = false)
	{
		if (!DoHaveSectors())
		{
			if (doGlobalParenting)
			{
				ParentObjectsGlobally(ref parentsUndoList, gameObjects, parentLocation, hierarchy, mergeSpawns);
			}
			return;
		}
		List<SECTR_SectorChildCandidate> sectorChildCandidates = new List<SECTR_SectorChildCandidate>();
		switch (localizeBy)
		{
		case SECTR_Constants.ReparentingMode.Bounds:
		{
			for (int j = 0; j < gameObjects.Count; j++)
			{
				AddObjToCandidateListByBounds(ref sectorChildCandidates, gameObjects[j].transform, hierarchy);
			}
			break;
		}
		case SECTR_Constants.ReparentingMode.Position:
		{
			for (int i = 0; i < gameObjects.Count; i++)
			{
				AddObjToCandidateListByPosition(ref sectorChildCandidates, gameObjects[i].transform, hierarchy);
			}
			break;
		}
		default:
			throw new NotImplementedException("Reparenting mode not recognized: " + localizeBy);
		}
		List<SECTR_Sector> topLevelSectors = GetTopLevelSectors();
		HashSet<Transform> hashSet = new HashSet<Transform>();
		for (int k = 0; k < topLevelSectors.Count; k++)
		{
			Transform transform = null;
			for (int num = sectorChildCandidates.Count - 1; num >= 0; num--)
			{
				if (sectorChildCandidates[num].transform != topLevelSectors[k].transform && SECTR_Geometry.BoundsContainsBounds(topLevelSectors[k].TotalBounds, sectorChildCandidates[num].bounds))
				{
					if (transform == null)
					{
						transform = GetParent(ref parentsUndoList, topLevelSectors[k].transform, parentLocation, sectorChildCandidates[num].ancestors, mergeSpawns);
					}
					sectorChildCandidates[num].transform.parent = transform;
					hashSet.Add(sectorChildCandidates[num].transform);
				}
			}
		}
		if (!doGlobalParenting)
		{
			return;
		}
		List<GameObject> list = new List<GameObject>();
		for (int num2 = sectorChildCandidates.Count - 1; num2 >= 0; num2--)
		{
			if (!hashSet.Contains(sectorChildCandidates[num2].transform))
			{
				list.Add(sectorChildCandidates[num2].transform.gameObject);
			}
		}
		ParentObjectsGlobally(ref parentsUndoList, list, parentLocation, hierarchy, mergeSpawns);
	}

	public static void AddObjToCandidateListByPosition(ref List<SECTR_SectorChildCandidate> sectorChildCandidates, Transform objectTransform)
	{
		AddObjToCandidateListByPosition(ref sectorChildCandidates, objectTransform, new string[0]);
	}

	public static void AddObjToCandidateListByPosition(ref List<SECTR_SectorChildCandidate> sectorChildCandidates, Transform objectTransform, string[] ancestors)
	{
		sectorChildCandidates.Add(new SECTR_SectorChildCandidate
		{
			ancestors = new List<string>(ancestors),
			transform = objectTransform,
			bounds = new Bounds(objectTransform.position, Vector3.zero)
		});
	}

	public static void AddObjToCandidateListByBounds(ref List<SECTR_SectorChildCandidate> sectorChildCandidates, Transform objectTransform)
	{
		AddObjToCandidateListByBounds(ref sectorChildCandidates, objectTransform, new string[0]);
	}

	public static void AddObjToCandidateListByBounds(ref List<SECTR_SectorChildCandidate> sectorChildCandidates, Transform objectTransform, string[] ancestors)
	{
		Bounds bounds = default;
		bool flag = false;
		Renderer[] componentsInChildren = objectTransform.GetComponentsInChildren<Renderer>();
		foreach (Renderer obj in componentsInChildren)
		{
			Bounds bounds2 = obj.bounds;
			if (obj.GetType() == typeof(ParticleSystemRenderer))
			{
				bounds2 = new Bounds(objectTransform.position, Vector3.one);
			}
			if (!flag)
			{
				bounds = bounds2;
				flag = true;
			}
			else
			{
				bounds.Encapsulate(bounds2);
			}
		}
		Light[] componentsInChildren2 = objectTransform.GetComponentsInChildren<Light>();
		foreach (Light light in componentsInChildren2)
		{
			if (!flag)
			{
				bounds = SECTR_Geometry.ComputeBounds(light);
				flag = true;
			}
			else
			{
				bounds.Encapsulate(SECTR_Geometry.ComputeBounds(light));
			}
		}
		if (flag)
		{
			sectorChildCandidates.Add(new SECTR_SectorChildCandidate
			{
				ancestors = new List<string>(ancestors),
				transform = objectTransform,
				bounds = bounds
			});
		}
	}

	public static List<SECTR_Sector> GetTopLevelSectors()
	{
		List<SECTR_Sector> list = new List<SECTR_Sector>();
		SECTR_Sector[] array = FindObjectsOfType<SECTR_Sector>();
		foreach (SECTR_Sector sECTR_Sector in array)
		{
			bool flag = true;
			Transform parent = sECTR_Sector.transform.parent;
			while (parent != null)
			{
				if (parent.GetComponent<SECTR_Sector>() != null)
				{
					flag = false;
					break;
				}
				parent = parent.parent;
			}
			if (flag)
			{
				list.Add(sECTR_Sector);
			}
		}
		return list;
	}

	public static void Encapsulate(List<SECTR_SectorChildCandidate> sectorChildCandidates, string undoString)
	{
		List<SECTR_Sector> topLevelSectors = GetTopLevelSectors();
		for (int i = 0; i < topLevelSectors.Count; i++)
		{
			Encapsulate(topLevelSectors[i], sectorChildCandidates, undoString);
		}
	}

	public static void Encapsulate(SECTR_Sector newSector, List<SECTR_SectorChildCandidate> sectorChildCandidates, string undoString)
	{
		for (int i = 0; i < sectorChildCandidates.Count; i++)
		{
			if (!(sectorChildCandidates[i].transform != newSector.transform) || !SECTR_Geometry.BoundsContainsBounds(newSector.TotalBounds, sectorChildCandidates[i].bounds))
			{
				continue;
			}
			Transform transform = newSector.transform;
			if (sectorChildCandidates[i].ancestors != null && sectorChildCandidates[i].ancestors.Count > 0)
			{
				for (int num = sectorChildCandidates[i].ancestors.Count - 1; num >= 1; num--)
				{
					bool flag = false;
					for (int j = 0; j < transform.childCount; j++)
					{
						if (transform.GetChild(j).name == sectorChildCandidates[i].ancestors[num])
						{
							transform = transform.GetChild(j);
							flag = true;
							break;
						}
					}
					if (!flag)
					{
						transform = UndoParent(transform, new GameObject(sectorChildCandidates[i].ancestors[num]).transform, undoString);
					}
				}
			}
			UndoParent(transform, sectorChildCandidates[i].transform, undoString);
		}
	}

	public static Transform UndoParent(Transform parent, Transform child, string undoString)
	{
		child.transform.parent = parent.transform;
		return child;
	}

	public static void UndoParent(GameObject parent, GameObject child, string undoString)
	{
		child.transform.parent = parent.transform;
	}

	public static double GetUnixTimeStamp()
	{
		return DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds;
	}

	private static Transform GetParent(ref List<GameObject> newParentList, Transform parentOfHierarchy, Vector3 parentLocation, List<string> hierarchy, bool mergeSpawns)
	{
		Transform transform = parentOfHierarchy;
		if (hierarchy != null && hierarchy.Count > 0)
		{
			Transform transform2 = null;
			for (int num = hierarchy.Count - 1; num >= 0; num--)
			{
				bool flag = true;
				for (int i = 0; i < transform.childCount; i++)
				{
					if (transform.GetChild(i).name == hierarchy[num])
					{
						if (mergeSpawns || num != 0)
						{
							transform = transform.GetChild(i);
							flag = false;
						}
						break;
					}
				}
				if (flag)
				{
					transform2 = new GameObject(hierarchy[num]).transform;
					transform2.position = parentLocation;
					transform2.parent = transform;
					transform = transform2;
				}
			}
			if (transform2 != null)
			{
				newParentList.Add(transform2.gameObject);
			}
		}
		return transform;
	}

	private static GameObject GetGlobalParent(ref List<GameObject> newParents, Vector3 parentLocation, string[] hierarchy, bool mergeSpawns)
	{
		GameObject gameObject = null;
		string text = hierarchy[^1];
		GameObject[] array = FindObjectsOfType<GameObject>();
		foreach (GameObject gameObject2 in array)
		{
			if (gameObject2.transform.parent == null && gameObject2.name == text)
			{
				gameObject = gameObject2;
				break;
			}
		}
		if (hierarchy.Length == 1)
		{
			if (gameObject == null || !mergeSpawns)
			{
				gameObject = new GameObject(text);
				gameObject.transform.position = parentLocation;
				newParents.Add(gameObject);
			}
		}
		else
		{
			List<string> list = new List<string>(hierarchy);
			list.RemoveAt(list.Count - 1);
			if (gameObject == null)
			{
				gameObject = new GameObject(text);
				gameObject.transform.position = parentLocation;
			}
			gameObject = GetParent(ref newParents, gameObject.transform, parentLocation, list, mergeSpawns).gameObject;
		}
		return gameObject;
	}

	private static void ParentObjectsGlobally(ref List<GameObject> newParents, List<GameObject> gameObjects, Vector3 parentLocation, string[] hierarchy, bool mergeSpawns)
	{
		if (gameObjects.Count >= 1 && hierarchy.Length >= 1)
		{
			GameObject globalParent = GetGlobalParent(ref newParents, parentLocation, hierarchy, mergeSpawns);
			for (int i = 0; i < gameObjects.Count; i++)
			{
				gameObjects[i].transform.parent = globalParent.transform;
			}
		}
	}
}
