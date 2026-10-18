using UnityEngine;

[AddComponentMenu("Procedural Worlds/SECTR/Audio/SECTR Start Music")]
public class SECTR_StartMusic : MonoBehaviour
{
	[SECTR_ToolTip("The music to play on Start.")]
	public SECTR_AudioCue Cue;

	private void Start()
	{
		SECTR_AudioSystem.PlayMusic(Cue);
		Object.Destroy(this);
	}
}
