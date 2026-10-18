using System;
using UnityEngine;

namespace CMF
{
	public class AnimationControl : MonoBehaviour
	{
		private Controller controller;

		private Animator animator;

		private Transform animatorTransform;

		private Transform tr;

		public bool useStrafeAnimations;

		public float landVelocityThreshold = 5f;

		private float smoothingFactor = 40f;

		private Vector3 oldMovementVelocity = Vector3.zero;

		private void Awake()
		{
			controller = GetComponent<Controller>();
			animator = GetComponentInChildren<Animator>();
			animatorTransform = animator.transform;
			tr = base.transform;
		}

		private void OnEnable()
		{
			Controller obj = controller;
			obj.OnLand = (Controller.VectorEvent)Delegate.Combine(obj.OnLand, new Controller.VectorEvent(OnLand));
			Controller obj2 = controller;
			obj2.OnJump = (Controller.VectorEvent)Delegate.Combine(obj2.OnJump, new Controller.VectorEvent(OnJump));
		}

		private void OnDisable()
		{
			Controller obj = controller;
			obj.OnLand = (Controller.VectorEvent)Delegate.Remove(obj.OnLand, new Controller.VectorEvent(OnLand));
			Controller obj2 = controller;
			obj2.OnJump = (Controller.VectorEvent)Delegate.Remove(obj2.OnJump, new Controller.VectorEvent(OnJump));
		}

		private void Update()
		{
			Vector3 velocity = controller.GetVelocity();
			Vector3 vector = VectorMath.RemoveDotVector(velocity, tr.up);
			Vector3 vector2 = velocity - vector;
			vector = (oldMovementVelocity = Vector3.Lerp(oldMovementVelocity, vector, smoothingFactor * Time.deltaTime));
			animator.SetFloat("VerticalSpeed", vector2.magnitude * VectorMath.GetDotProduct(vector2.normalized, tr.up));
			animator.SetFloat("HorizontalSpeed", vector.magnitude);
			if (useStrafeAnimations)
			{
				Vector3 vector3 = animatorTransform.InverseTransformVector(vector);
				animator.SetFloat("ForwardSpeed", vector3.z);
				animator.SetFloat("StrafeSpeed", vector3.x);
			}
			animator.SetBool("IsGrounded", controller.IsGrounded());
			animator.SetBool("IsStrafing", useStrafeAnimations);
		}

		private void OnLand(Vector3 _v)
		{
			if (!(VectorMath.GetDotProduct(_v, tr.up) > 0f - landVelocityThreshold))
			{
				animator.SetTrigger("OnLand");
			}
		}

		private void OnJump(Vector3 _v)
		{
		}
	}
}
