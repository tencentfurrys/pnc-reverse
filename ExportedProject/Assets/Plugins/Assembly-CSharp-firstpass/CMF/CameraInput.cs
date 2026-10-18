using UnityEngine;

namespace CMF
{
	public abstract class CameraInput : MonoBehaviour
	{
		public abstract float GetHorizontalCameraInput();

		public abstract float GetVerticalCameraInput();
	}
}
