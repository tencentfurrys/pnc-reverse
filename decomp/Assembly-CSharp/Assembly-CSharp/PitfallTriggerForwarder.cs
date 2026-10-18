using UnityEngine;

public class PitfallTriggerForwarder : MonoBehaviour
{
	private PitfallTeleporter owner;

	public void Initialize(PitfallTeleporter teleporter)
	{
		owner = teleporter;
	}

	private void OnTriggerEnter(Collider other)
	{
		owner?.ForwardTrigger(other);
	}

	private void OnTriggerStay(Collider other)
	{
		owner?.ForwardTrigger(other);
	}
}
