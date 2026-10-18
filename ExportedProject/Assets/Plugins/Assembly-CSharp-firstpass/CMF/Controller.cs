using UnityEngine;

namespace CMF
{
	public abstract class Controller : MonoBehaviour
	{
		public delegate void VectorEvent(Vector3 v);

		public VectorEvent OnJump;

		public VectorEvent OnLand;

		public abstract Vector3 GetVelocity();

		public abstract Vector3 GetMovementVelocity();

		public abstract bool IsGrounded();
	}
}
