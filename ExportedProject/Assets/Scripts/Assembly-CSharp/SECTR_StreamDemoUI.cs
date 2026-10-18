using UnityEngine;

public class SECTR_StreamDemoUI : SECTR_DemoUI
{
	[Multiline]
	public string NoExportMessage;

	protected override void OnEnable()
	{
		base.OnEnable();
		SECTR_StartLoader component = GetComponent<SECTR_StartLoader>();
		if ((bool)component)
		{
			component.Paused = true;
		}
	}

	protected override void OnGUI()
	{
		bool flag = false;
		int count = SECTR_Sector.All.Count;
		for (int i = 0; i < count; i++)
		{
			if (SECTR_Sector.All[i].Frozen)
			{
				flag = true;
				break;
			}
		}
		if (!flag && !string.IsNullOrEmpty(NoExportMessage))
		{
			DemoMessage = NoExportMessage;
		}
		base.OnGUI();
		SECTR_StartLoader component = GetComponent<SECTR_StartLoader>();
		if (passedIntro && (bool)component && component.Paused)
		{
			component.Paused = false;
		}
	}
}
