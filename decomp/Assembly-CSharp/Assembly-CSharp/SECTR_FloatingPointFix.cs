using System.Collections.Generic;
using UnityEngine;

public class SECTR_FloatingPointFix : MonoBehaviour
{
	private static SECTR_FloatingPointFix instance;

	private List<SECTR_FloatingPointFixMember> allMembers = new List<SECTR_FloatingPointFixMember>();

	private List<ParticleSystem> allWorldSpaceParticleSystems = new List<ParticleSystem>();

	private ParticleSystem.Particle[] currentParticles;

	public float threshold = 1000f;

	public Vector3 totalOffset = Vector3.zero;

	public static SECTR_FloatingPointFix Instance
	{
		get
		{
			if (instance == null)
			{
				instance = SECTR_SectorUtils.FindObjectOfType<SECTR_FloatingPointFix>();
			}
			if (instance == null && Application.isPlaying)
			{
				Debug.LogError("No Sectr Floating Point Fix Instance could be found, please add a SECTR Floating Point Fix component to the object that also contains the main sector loader.");
			}
			return instance;
		}
	}

	public static bool IsActive
	{
		get
		{
			if (instance == null)
			{
				instance = SECTR_SectorUtils.FindObjectOfType<SECTR_FloatingPointFix>();
			}
			return instance != null;
		}
	}

	private void OnEnable()
	{
		if (instance == null)
		{
			instance = this;
		}
		else if (instance != this)
		{
			Object.Destroy(gameObject);
			return;
		}
		SECTR_Sector[] array = SECTR_SectorUtils.FindObjectsOfType<SECTR_Sector>();
		foreach (SECTR_Sector obj in array)
		{
			obj.FloatingPointFix = true;
			obj.OverrideBounds = true;
			obj.BoundsUpdateMode = SECTR_Member.BoundsUpdateModes.Always;
		}
	}

	public void AddMember(SECTR_FloatingPointFixMember member)
	{
		if (!allMembers.Contains(member))
		{
			allMembers.Add(member);
		}
	}

	public void RemoveMember(SECTR_FloatingPointFixMember member)
	{
		if (allMembers.Contains(member))
		{
			allMembers.Remove(member);
		}
	}

	public void AddWorldSpaceParticleSystem(ParticleSystem ps)
	{
		if (!allWorldSpaceParticleSystems.Contains(ps))
		{
			allWorldSpaceParticleSystems.Add(ps);
		}
	}

	public void RemoveWorldSpaceParticleSystem(ParticleSystem ps)
	{
		if (allWorldSpaceParticleSystems.Contains(ps))
		{
			allWorldSpaceParticleSystems.Remove(ps);
		}
	}

	public Vector3 ConvertToOriginalSpace(Vector3 position)
	{
		return position += totalOffset;
	}

	private void LateUpdate()
	{
		Vector3 position = gameObject.transform.position;
		position.y = 0f;
		if (!(position.magnitude > threshold))
		{
			return;
		}
		totalOffset -= position;
		gameObject.transform.position -= position;
		foreach (SECTR_Sector item in SECTR_Sector.All)
		{
			item.BoundsOverride.center -= position;
			item.ForceUpdate(updateChildren: true);
		}
		foreach (SECTR_FloatingPointFixMember allMember in allMembers)
		{
			allMember.transform.position -= position;
		}
		foreach (ParticleSystem allWorldSpaceParticleSystem in allWorldSpaceParticleSystems)
		{
			bool isPaused = allWorldSpaceParticleSystem.isPaused;
			bool isPlaying = allWorldSpaceParticleSystem.isPlaying;
			if (!isPaused)
			{
				allWorldSpaceParticleSystem.Pause();
			}
			if (currentParticles == null || currentParticles.Length < allWorldSpaceParticleSystem.main.maxParticles)
			{
				currentParticles = new ParticleSystem.Particle[allWorldSpaceParticleSystem.main.maxParticles];
			}
			int particles = allWorldSpaceParticleSystem.GetParticles(currentParticles);
			for (int i = 0; i < particles; i++)
			{
				currentParticles[i].position -= position;
			}
			allWorldSpaceParticleSystem.SetParticles(currentParticles, particles);
			if (isPlaying)
			{
				allWorldSpaceParticleSystem.Play();
			}
		}
	}
}
