using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SECTR_Member))]
[AddComponentMenu("Procedural Worlds/SECTR/Stream/SECTR Hibernator")]
public class SECTR_Hibernator : MonoBehaviour
{
	public delegate void HibernateCallback();

	private bool hibernating;

	private SECTR_Member cachedMember;

	private Dictionary<SECTR_Chunk, SECTR_Chunk> chunks = new Dictionary<SECTR_Chunk, SECTR_Chunk>(4);

	private int numLoadedSectors;

	[SECTR_ToolTip("Hibernate components on children as well as ones on this game object.")]
	public bool HibernateChildren = true;

	[SECTR_ToolTip("Disable Behavior components during hibernation.")]
	public bool HibernateBehaviors = true;

	[SECTR_ToolTip("Disable Collder components during hibernation.")]
	public bool HibernateColliders = true;

	[SECTR_ToolTip("Disable RigidBody components during hibernation.")]
	public bool HibernateRigidBodies = true;

	[SECTR_ToolTip("Hide Render components during hibernation.")]
	public bool HibernateRenderers = true;

	[SECTR_ToolTip("Apply hibernation to an alternate entity.")]
	public GameObject HibernateTarget;

	public event HibernateCallback Awoke;

	public event HibernateCallback Hibernated;

	public event HibernateCallback HibernateUpdate;

	private void OnEnable()
	{
		cachedMember = GetComponent<SECTR_Member>();
		cachedMember.Changed += _MembershipChanged;
		chunks.Clear();
	}

	private void OnDisable()
	{
		cachedMember.Changed -= _MembershipChanged;
		cachedMember = null;
		chunks.Clear();
	}

	private void _ChunkChanged(SECTR_Chunk source, SECTR_Chunk.LoadState loadState)
	{
		switch (loadState)
		{
		case SECTR_Chunk.LoadState.Loaded:
			numLoadedSectors++;
			break;
		case SECTR_Chunk.LoadState.Unloaded:
			numLoadedSectors--;
			break;
		}
		_HibernationChanged();
	}

	private void _MembershipChanged(List<SECTR_Sector> left, List<SECTR_Sector> joined)
	{
		if (joined != null)
		{
			int count = joined.Count;
			for (int i = 0; i < count; i++)
			{
				SECTR_Sector sECTR_Sector = joined[i];
				if (!sECTR_Sector)
				{
					continue;
				}
				SECTR_Chunk component = sECTR_Sector.GetComponent<SECTR_Chunk>();
				if ((bool)component && !chunks.ContainsKey(component))
				{
					component.Changed += _ChunkChanged;
					chunks[component] = component;
					if (component.IsLoaded())
					{
						numLoadedSectors++;
					}
				}
			}
		}
		if (left != null)
		{
			int count2 = left.Count;
			for (int j = 0; j < count2; j++)
			{
				SECTR_Sector sECTR_Sector2 = left[j];
				if (!sECTR_Sector2)
				{
					continue;
				}
				SECTR_Chunk component2 = sECTR_Sector2.GetComponent<SECTR_Chunk>();
				if ((bool)component2 && chunks.ContainsKey(component2))
				{
					component2.Changed -= _ChunkChanged;
					chunks.Remove(component2);
					if (component2.IsLoaded())
					{
						numLoadedSectors--;
					}
				}
			}
		}
		_HibernationChanged();
	}

	private void _HibernationChanged()
	{
		if (numLoadedSectors == 0 && !hibernating)
		{
			_Hibernate();
		}
		else if (numLoadedSectors > 0 && hibernating)
		{
			_WakeUp();
		}
		if (hibernating && HibernateUpdate != null)
		{
			HibernateUpdate();
		}
	}

	private void _WakeUp()
	{
		if (hibernating)
		{
			hibernating = false;
			_UpdateComponents();
			if (Awoke != null)
			{
				Awoke();
			}
		}
	}

	private void _Hibernate()
	{
		if (!hibernating)
		{
			hibernating = true;
			_UpdateComponents();
			if (Hibernated != null)
			{
				Hibernated();
			}
		}
	}

	private void _UpdateComponents()
	{
		GameObject gameObject = (HibernateTarget ? HibernateTarget : base.gameObject);
		if (HibernateBehaviors)
		{
			Behaviour[] array = (HibernateChildren ? gameObject.GetComponentsInChildren<Behaviour>() : gameObject.GetComponents<Behaviour>());
			int num = array.Length;
			for (int i = 0; i < num; i++)
			{
				Behaviour behaviour = array[i];
				if (behaviour.GetType() != typeof(SECTR_Hibernator) && behaviour.GetType() != typeof(SECTR_Member))
				{
					behaviour.enabled = !hibernating;
				}
			}
		}
		if (HibernateRigidBodies)
		{
			Rigidbody[] array2 = (HibernateChildren ? gameObject.GetComponentsInChildren<Rigidbody>() : gameObject.GetComponents<Rigidbody>());
			int num2 = array2.Length;
			for (int j = 0; j < num2; j++)
			{
				Rigidbody rigidbody = array2[j];
				if (hibernating)
				{
					rigidbody.Sleep();
					rigidbody.isKinematic = true;
				}
				else
				{
					rigidbody.isKinematic = false;
					rigidbody.WakeUp();
				}
			}
		}
		if (HibernateColliders)
		{
			Collider[] array3 = (HibernateChildren ? gameObject.GetComponentsInChildren<Collider>() : gameObject.GetComponents<Collider>());
			int num3 = array3.Length;
			for (int k = 0; k < num3; k++)
			{
				array3[k].enabled = !hibernating;
			}
		}
		if (HibernateRenderers)
		{
			Renderer[] array4 = (HibernateChildren ? gameObject.GetComponentsInChildren<Renderer>() : gameObject.GetComponents<Renderer>());
			int num4 = array4.Length;
			for (int l = 0; l < num4; l++)
			{
				array4[l].enabled = !hibernating;
			}
		}
	}
}
