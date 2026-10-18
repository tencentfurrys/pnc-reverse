using System;
using UnityEngine;

[Serializable]
public class GrabAnimationData
{
	[Tooltip("Animation state name")]
	public string animationName;

	[Tooltip("Sound to play when this animation is selected")]
	public AudioClip sound;
}
