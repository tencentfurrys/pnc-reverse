using System;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("Procedural Worlds/SECTR/Audio/SECTR Character Audio")]
public class SECTR_CharacterAudio : MonoBehaviour
{
	[Serializable]
	public class SurfaceSound
	{
		[SECTR_ToolTip("The material that this set applies to.")]
		public PhysicsMaterial SurfaceMaterial;

		[SECTR_ToolTip("Default footstep sound. Used if no material specific sound exists.")]
		public SECTR_AudioCue FootstepCue;

		[SECTR_ToolTip("Default footstep sound. Used if no material specific sound exists.")]
		public SECTR_AudioCue JumpCue;

		[SECTR_ToolTip("Default landing sound. Used if no material specific sound exists.")]
		public SECTR_AudioCue LandCue;
	}

	private Dictionary<PhysicsMaterial, SurfaceSound> surfaceTable;

	[SECTR_ToolTip("Default sounds to play if there is no material specific sound.")]
	public SurfaceSound DefaultSounds = new SurfaceSound();

	[SECTR_ToolTip("List of surface specific sounds.")]
	public List<SurfaceSound> SurfaceSounds = new List<SurfaceSound>();

	private void OnEnable()
	{
		int count = SurfaceSounds.Count;
		for (int i = 0; i < count; i++)
		{
			SurfaceSound surfaceSound = SurfaceSounds[i];
			if (surfaceSound.SurfaceMaterial != null)
			{
				if (surfaceTable == null)
				{
					surfaceTable = new Dictionary<PhysicsMaterial, SurfaceSound>();
				}
				surfaceTable[surfaceSound.SurfaceMaterial] = surfaceSound;
			}
		}
	}

	private void OnDisable()
	{
		surfaceTable = null;
	}

	private void OnFootstep(PhysicsMaterial currentMaterial)
	{
		SECTR_AudioSystem.Play(_GetCurrentSurface(currentMaterial).FootstepCue, transform.position, loop: false);
	}

	private void OnJump(PhysicsMaterial currentMaterial)
	{
		SECTR_AudioSystem.Play(_GetCurrentSurface(currentMaterial).JumpCue, transform.position, loop: false);
	}

	private void OnLand(PhysicsMaterial currentMaterial)
	{
		SECTR_AudioSystem.Play(_GetCurrentSurface(currentMaterial).LandCue, transform.position, loop: false);
	}

	private SurfaceSound _GetCurrentSurface(PhysicsMaterial currentMaterial)
	{
		if (currentMaterial != null && surfaceTable != null && surfaceTable.TryGetValue(currentMaterial, out var value))
		{
			return value;
		}
		return DefaultSounds;
	}
}
