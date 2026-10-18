using UnityEngine;

public abstract class SECTR_Loader : MonoBehaviour
{
	protected bool locked;

	public abstract bool Loaded { get; }

	public override string ToString()
	{
		return "SECTR Loader";
	}

	protected void LockSelf(bool lockSelf)
	{
		if (lockSelf == locked)
		{
			return;
		}
		Rigidbody[] componentsInChildren = GetComponentsInChildren<Rigidbody>();
		int num = componentsInChildren.Length;
		for (int i = 0; i < num; i++)
		{
			Rigidbody rigidbody = componentsInChildren[i];
			if (lockSelf)
			{
				rigidbody.Sleep();
			}
			else
			{
				rigidbody.WakeUp();
			}
		}
		Collider[] componentsInChildren2 = GetComponentsInChildren<Collider>();
		int num2 = componentsInChildren2.Length;
		for (int j = 0; j < num2; j++)
		{
			componentsInChildren2[j].enabled = !lockSelf;
		}
		locked = lockSelf;
	}
}
