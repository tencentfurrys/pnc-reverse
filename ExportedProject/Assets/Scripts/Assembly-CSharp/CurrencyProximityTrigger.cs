using UnityEngine;

public class CurrencyProximityTrigger : MonoBehaviour
{
	[Header("Debug")]
	[SerializeField]
	private bool drawGizmo = true;

	[SerializeField]
	private Color gizmoColor = new Color(1f, 0.85f, 0.2f, 0.25f);

	private CurrencyDisplayUI currencyDisplayUI;

	private GameObject currentPlayer;

	private bool counted;

	private void OnTriggerEnter(Collider other)
	{
		if (!counted && IsPlayer(other))
		{
			currentPlayer = other.gameObject;
			counted = true;
			if (currencyDisplayUI == null)
			{
				currencyDisplayUI = Object.FindAnyObjectByType<CurrencyDisplayUI>();
			}
			if (currencyDisplayUI != null)
			{
				currencyDisplayUI.ShowForShop();
			}
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (counted && !(other.gameObject != currentPlayer))
		{
			Release();
		}
	}

	private void OnDisable()
	{
		Release();
	}

	private void OnDestroy()
	{
		Release();
	}

	private void Release()
	{
		if (counted)
		{
			counted = false;
			currentPlayer = null;
			if (currencyDisplayUI != null)
			{
				currencyDisplayUI.HideForShop();
			}
		}
	}

	private bool IsPlayer(Collider other)
	{
		if (other.CompareTag("Player"))
		{
			return true;
		}
		if (other.GetComponent<FirstPersonController>() != null)
		{
			return true;
		}
		if (other.GetComponent<CharacterController>() != null)
		{
			return true;
		}
		return false;
	}

	private void OnDrawGizmosSelected()
	{
		if (drawGizmo)
		{
			Gizmos.color = gizmoColor;
			Gizmos.matrix = base.transform.localToWorldMatrix;
			BoxCollider component2;
			if (TryGetComponent<SphereCollider>(out var component))
			{
				Gizmos.DrawSphere(component.center, component.radius);
			}
			else if (TryGetComponent<BoxCollider>(out component2))
			{
				Gizmos.DrawCube(component2.center, component2.size);
			}
		}
	}
}
