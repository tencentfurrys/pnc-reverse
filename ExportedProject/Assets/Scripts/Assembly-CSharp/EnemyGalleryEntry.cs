using UnityEngine;

[CreateAssetMenu(fileName = "New Enemy Entry", menuName = "Gallery/Enemy Entry")]
public class EnemyGalleryEntry : ScriptableObject
{
	[Header("Enemy Information")]
	[Tooltip("Unique identifier for unlock tracking")]
	public string enemyID;

	[Tooltip("If true, this enemy is always visible in the gallery without needing to be encountered")]
	public bool alwaysUnlocked;

	[Tooltip("Display name in the gallery")]
	public string enemyName = "Unknown Enemy";

	[TextArea(3, 5)]
	[Tooltip("Description shown in the gallery")]
	public string enemyDescription = "A mysterious creature.";

	[Tooltip("Icon/thumbnail for the gallery display")]
	public Sprite enemyIcon;

	[Header("Animation")]
	[Tooltip("The animator controller for this enemy")]
	public RuntimeAnimatorController animatorController;

	[Tooltip("Available animation state names (e.g., Idle, Walk, Grab, Attack)")]
	public string[] availableAnimations = new string[4] { "Idle", "Walk", "Grab", "Attack" };

	[Header("Grab Scene")]
	[Tooltip("Whether this enemy has a grab scene")]
	public bool hasGrabScene;

	[Tooltip("Animator controller for grab scene display")]
	public RuntimeAnimatorController grabAnimatorController;

	[Tooltip("Grab animations with their associated sounds")]
	public GrabAnimationData[] grabAnimations;

	public bool HasGrabScene()
	{
		if (hasGrabScene)
		{
			return grabAnimatorController != null;
		}
		return false;
	}

	public GrabAnimationData GetGrabAnimationAt(int index)
	{
		if (grabAnimations == null || index < 0 || index >= grabAnimations.Length)
		{
			return null;
		}
		return grabAnimations[index];
	}

	public bool HasAnimation(string animationName)
	{
		if (availableAnimations == null)
		{
			return false;
		}
		string[] array = availableAnimations;
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i] == animationName)
			{
				return true;
			}
		}
		return false;
	}

	public string GetAnimationAt(int index)
	{
		if (availableAnimations == null || index < 0 || index >= availableAnimations.Length)
		{
			return null;
		}
		return availableAnimations[index];
	}
}
