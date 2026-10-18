using UnityEngine;

[AddComponentMenu("Procedural Worlds/SECTR/Audio/SECTR Door Audio")]
public class SECTR_DoorAudio : MonoBehaviour
{
	private SECTR_AudioCueInstance instance;

	[SECTR_ToolTip("Sound to play while door is in Open state.", null, false)]
	public SECTR_AudioCue OpenLoopCue;

	[SECTR_ToolTip("Sound to play while door is in Closed state.", null, false)]
	public SECTR_AudioCue ClosedLoopCue;

	[SECTR_ToolTip("Sound to play when door starts to open.", null, false)]
	public SECTR_AudioCue OpeningCue;

	[SECTR_ToolTip("Sound to play while door starts to close.", null, false)]
	public SECTR_AudioCue ClosingCue;

	[SECTR_ToolTip("Sound to play while waiting for the door to start opening.", null, false)]
	public SECTR_AudioCue WaitingCue;

	private void OnDisable()
	{
		_Stop(stopImmediately: true);
	}

	private void OnOpen()
	{
		_Stop(stopImmediately: false);
		instance = SECTR_AudioSystem.Play(OpenLoopCue, base.transform, Vector3.zero, loop: true);
	}

	private void OnOpening()
	{
		_Stop(stopImmediately: false);
		instance = SECTR_AudioSystem.Play(OpeningCue, base.transform, Vector3.zero, loop: false);
	}

	private void OnClose()
	{
		_Stop(stopImmediately: false);
		instance = SECTR_AudioSystem.Play(ClosedLoopCue, base.transform, Vector3.zero, loop: true);
	}

	private void OnClosing()
	{
		_Stop(stopImmediately: false);
		instance = SECTR_AudioSystem.Play(ClosingCue, base.transform, Vector3.zero, loop: false);
	}

	private void OnWaiting()
	{
		_Stop(stopImmediately: false);
		instance = SECTR_AudioSystem.Play(WaitingCue, base.transform, Vector3.zero, loop: true);
	}

	private void _Stop(bool stopImmediately)
	{
		instance.Stop(stopImmediately);
	}
}
