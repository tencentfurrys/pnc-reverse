using System;
using UnityEngine;

[CreateAssetMenu(fileName = "PatronList", menuName = "Game Data/Patron List")]
public class PatronListData : ScriptableObject
{
	public string[] dungeonMasters;

	public string[] depthsWalkers;

	public string[] GetCombinedNames()
	{
		int num = ((dungeonMasters != null) ? dungeonMasters.Length : 0);
		int num2 = ((depthsWalkers != null) ? depthsWalkers.Length : 0);
		string[] array = new string[num + num2];
		if (num > 0)
		{
			Array.Copy(dungeonMasters, 0, array, 0, num);
		}
		if (num2 > 0)
		{
			Array.Copy(depthsWalkers, 0, array, num, num2);
		}
		return array;
	}
}
