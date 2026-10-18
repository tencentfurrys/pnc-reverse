using UnityEngine;

namespace CMF
{
	public class FPSCounter : MonoBehaviour
	{
		public float checkInterval = 1f;

		private int currentPassedFrames;

		private float currentPassedTime;

		public float currentFrameRate;

		private string currentFrameRateString = "";

		private void Update()
		{
			currentPassedFrames++;
			currentPassedTime += Time.deltaTime;
			if (currentPassedTime >= checkInterval)
			{
				currentFrameRate = (float)currentPassedFrames / currentPassedTime;
				currentPassedTime = 0f;
				currentPassedFrames = 0;
				currentFrameRate *= 100f;
				currentFrameRate = (int)currentFrameRate;
				currentFrameRate /= 100f;
				currentFrameRateString = currentFrameRate.ToString();
			}
		}

		private void OnGUI()
		{
			GUI.contentColor = Color.black;
			float num = 40f;
			float num2 = 2f;
			GUI.Label(new Rect((float)Screen.width - num + num2, num2, num, 30f), currentFrameRateString);
			GUI.contentColor = Color.white;
			GUI.Label(new Rect((float)Screen.width - num, 0f, num, 30f), currentFrameRateString);
		}
	}
}
