using UnityEngine;

public class SECTR_CompleteDemoUI : SECTR_DemoUI
{
	private string originalDemoMessage;

	[Multiline]
	public string Unity4PerfMessage;

	private void Start()
	{
		if ((bool)PipController && PipController.GetComponent<SECTR_CullingCamera>() == null && (bool)GetComponent<SECTR_CullingCamera>() && (bool)GetComponent<Camera>())
		{
			PipController.gameObject.AddComponent<SECTR_CullingCamera>().cullingProxy = GetComponent<Camera>();
		}
	}

	protected override void OnEnable()
	{
		originalDemoMessage = DemoMessage;
		base.OnEnable();
		SECTR_StartLoader component = GetComponent<SECTR_StartLoader>();
		if ((bool)component)
		{
			component.Paused = true;
		}
	}

	protected override void OnGUI()
	{
		if (Application.isEditor && Application.isPlaying && !string.IsNullOrEmpty(Unity4PerfMessage))
		{
			DemoMessage = originalDemoMessage;
			DemoMessage += "\n\n";
			DemoMessage += Unity4PerfMessage;
		}
		base.OnGUI();
		SECTR_StartLoader component = GetComponent<SECTR_StartLoader>();
		if (passedIntro && (bool)component && component.Paused)
		{
			component.Paused = false;
		}
	}
}
