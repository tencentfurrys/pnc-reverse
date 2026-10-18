using UnityEngine;

public class TriggerHandler : MonoBehaviour
{
	private AudioTrigger parentTrigger;

	public void Initialize(AudioTrigger parent)
	{
		parentTrigger = parent;
	}

	private void OnTriggerEnter(Collider other)
	{
		parentTrigger?.HandleTriggerEnter(other);
	}

	private void OnTriggerExit(Collider other)
	{
		parentTrigger?.HandleTriggerExit(other);
	}
}
