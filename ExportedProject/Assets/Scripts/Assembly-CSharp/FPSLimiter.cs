using UnityEngine;

public class FPSLimiter : MonoBehaviour
{
	private void Awake()
	{
		OptionsMenu.ApplyFramerateSettings();
	}
}
