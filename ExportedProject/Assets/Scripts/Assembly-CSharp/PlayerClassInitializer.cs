using System.Collections;
using UnityEngine;

public class PlayerClassInitializer : MonoBehaviour
{
	[Header("Auto-Apply Settings")]
	[SerializeField]
	private bool applyClassOnStart = true;

	[SerializeField]
	private float delayBeforeApplying = 0.1f;

	[Header("Fallback Class")]
	[Tooltip("Class to use if no class was selected in main menu")]
	[SerializeField]
	private PlayerClass fallbackClass;

	private IEnumerator Start()
	{
		yield return null;
		if (applyClassOnStart)
		{
			ApplySelectedClass();
		}
	}

	public void ApplySelectedClass()
	{
		if (PlayerClassManager.Instance == null)
		{
			new GameObject("PlayerClassManager").AddComponent<PlayerClassManager>();
		}
		if (PlayerStateSaver.Instance != null && !PlayerStateSaver.Instance.IsFirstFloor)
		{
			PlayerStateSaver.Instance.LoadPlayerState();
			return;
		}
		if (PlayerClassManager.Instance.GetSelectedClass() == null && fallbackClass != null)
		{
			PlayerClassManager.Instance.SelectClass(fallbackClass);
		}
		PlayerClassManager.Instance.ApplyClassToPlayer();
	}

	[ContextMenu("Apply Class Now")]
	public void ApplyClassNow()
	{
		ApplySelectedClass();
	}
}
