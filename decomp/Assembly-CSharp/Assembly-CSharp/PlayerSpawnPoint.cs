using System.Collections;
using CMF;
using UnityEngine;

public class PlayerSpawnPoint : MonoBehaviour
{
	[Header("Settings")]
	[SerializeField]
	private bool useTag = true;

	[SerializeField]
	private string playerTag = "Player";

	private GameObject playerObj;

	private Rigidbody playerRb;

	private AdvancedWalkerController walker;

	private void Start()
	{
		playerObj = (useTag ? GameObject.FindGameObjectWithTag(playerTag) : FindPlayerByComponent());
		if (!(playerObj == null))
		{
			playerRb = playerObj.GetComponent<Rigidbody>();
			walker = playerObj.GetComponent<AdvancedWalkerController>();
			TeleportAndFreeze();
			DunGenAStarIntegration dunGenAStarIntegration = Object.FindAnyObjectByType<DunGenAStarIntegration>();
			if (dunGenAStarIntegration != null && !dunGenAStarIntegration.IsFullyReady())
			{
				StartCoroutine(WaitForReadyThenUnfreeze(dunGenAStarIntegration));
			}
			else
			{
				StartCoroutine(UnfreezeNextFixedUpdate());
			}
		}
	}

	private IEnumerator WaitForReadyThenUnfreeze(DunGenAStarIntegration integration)
	{
		while (!integration.IsFullyReady())
		{
			yield return null;
		}
		yield return UnfreezeNextFixedUpdate();
	}

	private void TeleportAndFreeze()
	{
		if (playerRb != null)
		{
			playerRb.linearVelocity = Vector3.zero;
			playerRb.isKinematic = true;
		}
		if (walker != null)
		{
			walker.SetMomentum(Vector3.zero);
			walker.enabled = false;
		}
		playerObj.transform.SetPositionAndRotation(transform.position, transform.rotation);
	}

	private IEnumerator UnfreezeNextFixedUpdate()
	{
		if (playerObj != null)
		{
			playerObj.transform.SetPositionAndRotation(transform.position, transform.rotation);
		}
		Physics.SyncTransforms();
		yield return new WaitForFixedUpdate();
		if (playerRb != null)
		{
			playerRb.isKinematic = false;
		}
		if (walker != null)
		{
			walker.enabled = true;
		}
	}

	private GameObject FindPlayerByComponent()
	{
		FirstPersonController firstPersonController = Object.FindAnyObjectByType<FirstPersonController>();
		if (firstPersonController != null)
		{
			return firstPersonController.gameObject;
		}
		PlayerStats playerStats = Object.FindAnyObjectByType<PlayerStats>();
		if (playerStats != null)
		{
			return playerStats.gameObject;
		}
		return null;
	}

	private void OnDrawGizmos()
	{
		Gizmos.color = Color.green;
		Gizmos.DrawWireSphere(transform.position, 0.5f);
		Gizmos.DrawLine(transform.position, transform.position + transform.forward * 1.5f);
	}
}
