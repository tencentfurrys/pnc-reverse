using PixelCrushers.GridController;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class GalleryUnlockTrigger : MonoBehaviour
{
	[Header("Unlock")]
	[Tooltip("Gallery ID to unlock when the player enters this trigger")]
	[SerializeField]
	private string galleryID;

	[Header("Detection")]
	[SerializeField]
	private string playerTag = "Player";

	[Header("Audio")]
	[Tooltip("Played only when this trigger actually unlocks a new gallery entry. Silent if already unlocked.")]
	[SerializeField]
	private AudioClip unlockSound;

	[SerializeField]
	[Range(0f, 1f)]
	private float unlockVolume = 1f;

	private bool hasFired;

	private void OnTriggerEnter(Collider other)
	{
		if (hasFired || !other.CompareTag(playerTag))
		{
			return;
		}
		if (string.IsNullOrEmpty(galleryID))
		{
			Debug.LogWarning("GalleryUnlockTrigger: galleryID is empty.", this);
			return;
		}
		if (GalleryProgressManager.Instance == null)
		{
			Debug.LogWarning("GalleryUnlockTrigger: GalleryProgressManager.Instance is null.", this);
			return;
		}
		bool num = GalleryProgressManager.Instance.UnlockEnemy(galleryID);
		hasFired = true;
		if (num)
		{
			PlayUnlockSound();
		}
	}

	private void PlayUnlockSound()
	{
		if (!(unlockSound == null))
		{
			if (AudioPool.Instance != null)
			{
				AudioPool.Instance.PlayAtPoint(unlockSound, base.transform.position, unlockVolume, 0f);
			}
			else
			{
				AudioSource.PlayClipAtPoint(unlockSound, base.transform.position, unlockVolume);
			}
		}
	}

	private void OnDrawGizmos()
	{
		Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.4f);
		if (GetComponent<Collider>() is BoxCollider boxCollider)
		{
			Matrix4x4 matrix = Gizmos.matrix;
			Gizmos.matrix = base.transform.localToWorldMatrix;
			Gizmos.DrawCube(boxCollider.center, boxCollider.size);
			Gizmos.matrix = matrix;
		}
		else
		{
			Gizmos.DrawWireSphere(base.transform.position, 0.5f);
		}
	}
}
