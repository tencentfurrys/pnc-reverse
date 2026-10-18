using UnityEngine;

namespace CMF
{
	public class DisableShadows : MonoBehaviour
	{
		private bool shadowsAreActive = true;

		public Light sceneLight;

		private void Start()
		{
			sceneLight = GetComponent<Light>();
		}

		public void SetShadows(bool _isActivated)
		{
			shadowsAreActive = _isActivated;
			if (!shadowsAreActive)
			{
				sceneLight.shadows = LightShadows.None;
			}
			else
			{
				sceneLight.shadows = LightShadows.Hard;
			}
		}
	}
}
