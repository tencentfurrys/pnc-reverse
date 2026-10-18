using UnityEngine;

public interface IPlayerInteractable
{
	bool CanInteract { get; }

	float InteractionRange { get; }

	void OnLookedAt(GameObject player);

	void OnLookedAway();

	void OnInteracted();
}
