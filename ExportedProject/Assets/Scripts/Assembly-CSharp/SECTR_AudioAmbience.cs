using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SECTR_AudioAmbience
{
	[SECTR_ToolTip("The looping 2D cue to play as long as this ambience is active.", null, false)]
	public SECTR_AudioCue BackgroundLoop;

	[SECTR_ToolTip("A list of one-shots that will play randomly around the listener.")]
	public List<SECTR_AudioCue> OneShots = new List<SECTR_AudioCue>();

	[SECTR_ToolTip("The min and max time between one-shot playback.", "OneShots")]
	public Vector2 OneShotInterval = new Vector2(30f, 60f);

	[SECTR_ToolTip("The a volume scalar for the Cues in this Ambience. Combines with the base Cue volume.")]
	public float Volume = 1f;

	[SECTR_ToolTip("When enabled the higher probability one shots will be more likely to play, gets the probability from the cue.")]
	public bool UseOneShotCuesProbability = true;

	[HideInInspector]
	public float TotalProbability;
}
