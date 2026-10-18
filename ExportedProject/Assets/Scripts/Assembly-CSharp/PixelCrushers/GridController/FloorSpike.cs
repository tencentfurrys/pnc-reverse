using UnityEngine;

namespace PixelCrushers.GridController
{
	public class FloorSpike : MonoBehaviour
	{
		[Header("Configuration")]
		[SerializeField]
		private int damage = 15;

		[SerializeField]
		private LayerMask targetLayerMask = 256;

		[SerializeField]
		private float damageRadius = 1f;

		[SerializeField]
		private Vector3 damageCenter = Vector3.zero;

		[SerializeField]
		private float maxLifetime = 3f;

		[Header("Components")]
		[SerializeField]
		private Collider spikeCollider;

		[SerializeField]
		private AudioSource audioSource;

		[SerializeField]
		private AudioClip emergeSound;

		[SerializeField]
		private AudioClip retractSound;

		[SerializeField]
		private Animator animator;

		private GameObject owner;

		private bool hasDealtDamage;

		private void Awake()
		{
			if (spikeCollider == null)
			{
				spikeCollider = GetComponent<Collider>();
			}
			if (audioSource == null)
			{
				audioSource = GetComponent<AudioSource>();
			}
			if (animator == null)
			{
				animator = GetComponent<Animator>();
			}
			if (spikeCollider != null)
			{
				spikeCollider.isTrigger = true;
				spikeCollider.enabled = false;
			}
		}

		public void Initialize(GameObject spikeOwner)
		{
			owner = spikeOwner;
			if (animator != null)
			{
				animator.Play("FlamerFloorSpike Animation");
			}
			Object.Destroy((base.transform.parent != null) ? base.transform.parent.gameObject : base.gameObject, maxLifetime);
		}

		public void OnSpikeEmerge()
		{
			if (emergeSound != null && audioSource != null)
			{
				audioSource.PlayOneShot(emergeSound);
			}
		}

		public void OnSpikeDamage()
		{
			if (hasDealtDamage)
			{
				return;
			}
			hasDealtDamage = true;
			Collider[] array = Physics.OverlapSphere(base.transform.position + damageCenter, damageRadius, (int)targetLayerMask | 0x40000);
			foreach (Collider collider in array)
			{
				if (!(collider.transform == owner?.transform))
				{
					collider.GetComponent<IDamageable>()?.TakeDamage(damage);
				}
			}
		}

		public void OnSpikeRetract()
		{
			if (retractSound != null && audioSource != null)
			{
				audioSource.PlayOneShot(retractSound);
			}
		}

		public void OnSpikeDestroy()
		{
			Object.Destroy((base.transform.parent != null) ? base.transform.parent.gameObject : base.gameObject);
		}

		public void SetDamage(int newDamage)
		{
			damage = newDamage;
		}

		public bool HasDealtDamage()
		{
			return hasDealtDamage;
		}

		private void OnDrawGizmosSelected()
		{
			Gizmos.color = (hasDealtDamage ? Color.red : Color.gray);
			Gizmos.DrawWireSphere(base.transform.position + damageCenter, damageRadius);
		}
	}
}
