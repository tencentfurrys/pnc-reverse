using UnityEngine;

namespace FronkonGames.Retro.NTSC
{
	public sealed class Rotate : MonoBehaviour
	{
		[SerializeField]
		private Vector3 angularSpeed = new Vector3(0f, 15f, 0f);

		private void Update()
		{
			base.transform.Rotate(angularSpeed * Time.deltaTime);
		}
	}
}
