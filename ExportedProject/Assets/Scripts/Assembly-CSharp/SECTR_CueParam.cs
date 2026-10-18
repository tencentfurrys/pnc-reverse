using System;
using UnityEngine;

[Serializable]
public class SECTR_CueParam
{
	public enum TargetType
	{
		Volume = 0,
		Pitch = 1,
		Attribute = 2
	}

	[Serializable]
	public class AttributeData
	{
		private Type componentType;

		[SerializeField]
		private string componentTypeString;

		public string attributeName;

		public bool fieldAttribute;

		public Type ComponentType
		{
			get
			{
				if (componentType == null)
				{
					if (componentTypeString == null)
					{
						return null;
					}
					componentType = Type.GetType(componentTypeString);
				}
				return componentType;
			}
			set
			{
				componentType = value;
				if (componentType != null)
				{
					componentTypeString = componentType.ToString();
				}
			}
		}
	}

	public string name;

	public TargetType affects;

	public float defaultValue;

	public AnimationCurve curve;

	public AttributeData attributeData;

	public bool toggle;

	public SECTR_CueParam()
	{
		name = "distance";
		affects = TargetType.Volume;
		defaultValue = 0f;
		Keyframe[] keys = new Keyframe[2]
		{
			new Keyframe(0f, 1f),
			new Keyframe(1f, 1f)
		};
		curve = new AnimationCurve(keys);
		toggle = true;
	}
}
