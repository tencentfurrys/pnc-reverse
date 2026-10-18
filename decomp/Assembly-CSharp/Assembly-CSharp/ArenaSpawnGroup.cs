using UnityEngine;

[CreateAssetMenu(fileName = "New Arena Spawn Group", menuName = "Combat/Arena Spawn Group")]
public class ArenaSpawnGroup : ScriptableObject
{
	[Tooltip("Display name for logs and gizmos. Leave blank to use the asset name.")]
	public string groupName;

	public ArenaSpawnEntry[] entries;

	[Header("Audio")]
	[Tooltip("Looping combat track for this encounter. Leave empty to use the arena's own Arena Music.")]
	public AudioClip arenaMusic;

	[Tooltip("One-shot played when the room is cleared. Leave empty to use the arena's own Room Clear Sound.")]
	public AudioClip exitSting;

	public string DisplayName
	{
		get
		{
			if (!string.IsNullOrWhiteSpace(groupName))
			{
				return groupName;
			}
			return name;
		}
	}

	public bool HasValidEntry
	{
		get
		{
			if (entries == null)
			{
				return false;
			}
			for (int i = 0; i < entries.Length; i++)
			{
				if (entries[i] != null && entries[i].IsValid)
				{
					return true;
				}
			}
			return false;
		}
	}
}
