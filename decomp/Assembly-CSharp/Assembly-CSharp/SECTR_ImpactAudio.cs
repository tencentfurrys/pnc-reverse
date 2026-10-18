using System;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[AddComponentMenu("Procedural Worlds/SECTR/Audio/SECTR Impact Audio")]
public class SECTR_ImpactAudio : MonoBehaviour
{
	[Serializable]
	public class ImpactSound
	{
		public PhysicsMaterial SurfaceMaterial;

		public SECTR_AudioCue ImpactCue;
	}

	private float nextImpactTime;

	private Dictionary<PhysicsMaterial, ImpactSound> surfaceTable;

	[SECTR_ToolTip("Default sound to play on impact.")]
	public ImpactSound DefaultSound;

	[SECTR_ToolTip("Surface specific impact sounds.")]
	public List<ImpactSound> SurfaceImpacts = new List<ImpactSound>();

	[SECTR_ToolTip("The minimum relative speed at the time of impact required to trigger this cue.")]
	public float MinImpactSpeed = 0.01f;

	[SECTR_ToolTip("The minimum amount of time between playback of this sound.")]
	public float MinImpactInterval = 0.5f;

	private void OnEnable()
	{
		int count = SurfaceImpacts.Count;
		for (int i = 0; i < count; i++)
		{
			ImpactSound impactSound = SurfaceImpacts[i];
			if (impactSound.SurfaceMaterial != null)
			{
				if (surfaceTable == null)
				{
					surfaceTable = new Dictionary<PhysicsMaterial, ImpactSound>();
				}
				surfaceTable[impactSound.SurfaceMaterial] = impactSound;
			}
		}
	}

	private void OnDisable()
	{
		surfaceTable = null;
	}

	private void OnCollisionStay(Collision collision)
	{
		if (Time.time >= nextImpactTime && collision != null && collision.contacts.Length != 0 && collision.relativeVelocity.sqrMagnitude >= MinImpactSpeed * MinImpactSpeed)
		{
			if (collision.collider.sharedMaterial == null || surfaceTable == null || !surfaceTable.TryGetValue(collision.collider.sharedMaterial, out var value))
			{
				value = DefaultSound;
			}
			SECTR_AudioSystem.Play(value.ImpactCue, collision.contacts[0].point, loop: false);
			nextImpactTime = Time.time + MinImpactInterval;
		}
	}
}
