using UnityEngine;

namespace CMF
{
	public class CameraMouseInput : CameraInput
	{
		public string mouseHorizontalAxis = "Mouse X";

		public string mouseVerticalAxis = "Mouse Y";

		public bool invertHorizontalInput;

		public bool invertVerticalInput;

		public float mouseInputMultiplier = 0.01f;

		public override float GetHorizontalCameraInput()
		{
			float axisRaw = Input.GetAxisRaw(mouseHorizontalAxis);
			if (Time.timeScale > 0f && Time.deltaTime > 0f)
			{
				axisRaw /= Time.deltaTime;
				axisRaw *= Time.timeScale;
			}
			else
			{
				axisRaw = 0f;
			}
			axisRaw *= mouseInputMultiplier;
			if (invertHorizontalInput)
			{
				axisRaw *= -1f;
			}
			return axisRaw;
		}

		public override float GetVerticalCameraInput()
		{
			float num = 0f - Input.GetAxisRaw(mouseVerticalAxis);
			if (Time.timeScale > 0f && Time.deltaTime > 0f)
			{
				num /= Time.deltaTime;
				num *= Time.timeScale;
			}
			else
			{
				num = 0f;
			}
			num *= mouseInputMultiplier;
			if (invertVerticalInput)
			{
				num *= -1f;
			}
			return num;
		}
	}
}
