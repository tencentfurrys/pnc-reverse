using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class SECTR_FloatingPointFixParticleSystem : SECTR_FloatingPointFixMember
{
	protected new void OnEnable()
	{
		ParticleSystem component = GetComponent<ParticleSystem>();
		if ((bool)component && component.main.simulationSpace == ParticleSystemSimulationSpace.World)
		{
			SECTR_FloatingPointFix.Instance.AddWorldSpaceParticleSystem(component);
		}
	}

	protected new void OnDestroy()
	{
		if (SECTR_FloatingPointFix.IsActive)
		{
			ParticleSystem component = GetComponent<ParticleSystem>();
			if ((bool)component)
			{
				SECTR_FloatingPointFix.Instance.RemoveWorldSpaceParticleSystem(component);
			}
		}
	}

	private void Start()
	{
	}

	private void Update()
	{
	}
}
