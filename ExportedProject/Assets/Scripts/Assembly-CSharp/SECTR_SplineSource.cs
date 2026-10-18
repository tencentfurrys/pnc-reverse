using System;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[AddComponentMenu("Procedural Worlds/SECTR/Audio/SECTR Spline Source")]
public class SECTR_SplineSource : SECTR_PointSource
{
	private class SplineNode
	{
		public Vector3 Point;

		public Quaternion Rot;

		public float T;

		public Vector2 EaseIO;

		public SplineNode(Vector3 p, Quaternion q, float t, Vector2 io)
		{
			Point = p;
			Rot = q;
			T = t;
			EaseIO = io;
		}

		public SplineNode(SplineNode o)
		{
			Point = o.Point;
			Rot = o.Rot;
			T = o.T;
			EaseIO = o.EaseIO;
		}
	}

	private List<SplineNode> nodes = new List<SplineNode>(8);

	[SECTR_ToolTip("Array of scene objects to use as control points for the spline")]
	public List<Transform> SplinePoints = new List<Transform>();

	[SECTR_ToolTip("Determines if the spline is open or closed (i.e. a loop).")]
	public bool Closed;

	[SECTR_ToolTip("Determines if the audio source is allowed to leave the spline during interpolation.")]
	public bool AllowShortcuts = true;

	[SECTR_ToolTip("The maximum speed at which to interpolate the sound in space.", 0f, -1f)]
	public float MaxInterpSpeed = 100f;

	public override void Play()
	{
		base.Play();
		if ((bool)instance)
		{
			instance.Position = _GetClosestPointOnSpline(SECTR_AudioSystem.Listener.position);
		}
	}

	private void Awake()
	{
		_SetupSpline();
	}

	private void OnEnable()
	{
	}

	protected override void OnDisable()
	{
		base.OnDisable();
	}

	private void Update()
	{
		if (!instance || nodes.Count <= 0)
		{
			return;
		}
		Vector3 vector = _GetClosestPointOnSpline(SECTR_AudioSystem.Listener.position);
		if (AllowShortcuts)
		{
			Vector3 vector2 = vector - instance.Position;
			float magnitude = vector2.magnitude;
			if (magnitude > Mathf.Epsilon)
			{
				vector2 /= magnitude;
				vector2 *= Mathf.Min(magnitude, MaxInterpSpeed * Time.deltaTime);
				vector = instance.Position + vector2;
				vector = base.transform.worldToLocalMatrix.MultiplyPoint3x4(vector);
				instance.LocalPosition = vector;
			}
		}
		else
		{
			instance.Position = vector;
		}
	}

	private void _SetupSpline()
	{
		nodes.Clear();
		int count = SplinePoints.Count;
		if (count < 2)
		{
			return;
		}
		float num = (Closed ? (1f / (float)count) : (1f / (float)(count - 1)));
		int i;
		for (i = 0; i < count; i++)
		{
			Transform transform = SplinePoints[i];
			if ((bool)transform)
			{
				nodes.Add(new SplineNode(transform.position, transform.rotation, num * (float)i, new Vector2(0f, 1f)));
			}
		}
		if (Closed && nodes.Count > 0)
		{
			float t = num * (float)i;
			nodes.Add(new SplineNode(nodes[0]));
			nodes[nodes.Count - 1].T = t;
			Vector3 normalized = (nodes[1].Point - nodes[0].Point).normalized;
			Vector3 normalized2 = (nodes[nodes.Count - 2].Point - nodes[nodes.Count - 1].Point).normalized;
			float magnitude = (nodes[1].Point - nodes[0].Point).magnitude;
			float magnitude2 = (nodes[nodes.Count - 2].Point - nodes[nodes.Count - 1].Point).magnitude;
			SplineNode splineNode = new SplineNode(nodes[0]);
			splineNode.Point = nodes[0].Point + normalized2 * magnitude;
			SplineNode splineNode2 = new SplineNode(nodes[nodes.Count - 1]);
			splineNode2.Point = nodes[0].Point + normalized * magnitude2;
			nodes.Insert(0, splineNode);
			nodes.Add(splineNode2);
		}
		int count2 = nodes.Count;
		for (int j = 1; j < count2; j++)
		{
			SplineNode splineNode3 = nodes[j];
			SplineNode splineNode4 = nodes[j - 1];
			if (Quaternion.Dot(splineNode3.Rot, splineNode4.Rot) < 0f)
			{
				splineNode3.Rot.x = 0f - splineNode3.Rot.x;
				splineNode3.Rot.y = 0f - splineNode3.Rot.y;
				splineNode3.Rot.z = 0f - splineNode3.Rot.z;
				splineNode3.Rot.w = 0f - splineNode3.Rot.w;
			}
		}
		if (count2 > 0 && !Closed)
		{
			nodes.Insert(0, nodes[0]);
			nodes.Add(nodes[nodes.Count - 1]);
		}
	}

	private Vector3 _GetClosestPointOnSpline(Vector3 point)
	{
		float num = float.MaxValue;
		float timeParam = 0f;
		int count = nodes.Count;
		int num2 = 1;
		int num3 = count;
		if (Closed)
		{
			num2 = 2;
			num3 = count - 1;
		}
		for (int i = num2; i < num3; i++)
		{
			Vector3 point2 = nodes[i - 1].Point;
			Vector3 vector = nodes[i].Point - point2;
			float magnitude = vector.magnitude;
			vector /= magnitude;
			float value = Vector3.Dot(point - point2, vector);
			value = Mathf.Clamp(value, 0f, magnitude);
			float num4 = Vector3.SqrMagnitude(point2 + vector * value - point);
			if (num4 < num)
			{
				num = num4;
				timeParam = Mathf.Lerp(nodes[i - 1].T, nodes[i].T, value / magnitude);
			}
		}
		return _GetHermiteAtT(timeParam);
	}

	private Vector3 _GetHermiteAtT(float timeParam)
	{
		int count = nodes.Count;
		if (timeParam >= nodes[count - 2].T)
		{
			return nodes[count - 2].Point;
		}
		int i;
		for (i = 1; i < count - 2 && !(nodes[i].T > timeParam); i++)
		{
		}
		int num = i - 1;
		float t = (timeParam - nodes[num].T) / (nodes[num + 1].T - nodes[num].T);
		t = _Ease(t, nodes[num].EaseIO.x, nodes[num].EaseIO.y);
		float num2 = t * t;
		float num3 = num2 * t;
		Vector3 point = nodes[num - 1].Point;
		Vector3 point2 = nodes[num].Point;
		Vector3 point3 = nodes[num + 1].Point;
		Vector3 point4 = nodes[num + 2].Point;
		Vector3 vector = 0.5f * (point3 - point);
		Vector3 vector2 = 0.5f * (point4 - point2);
		float num4 = 2f * num3 - 3f * num2 + 1f;
		float num5 = -2f * num3 + 3f * num2;
		float num6 = num3 - 2f * num2 + t;
		float num7 = num3 - num2;
		return num4 * point2 + num5 * point3 + num6 * vector + num7 * vector2;
	}

	private float _Ease(float t, float k1, float k2)
	{
		float num = k1 * 2f / MathF.PI + k2 - k1 + (1f - k2) * 2f / MathF.PI;
		float num2 = ((t < k1) ? (k1 * (2f / MathF.PI) * (Mathf.Sin(t / k1 * MathF.PI * 0.5f - MathF.PI / 2f) + 1f)) : ((!(t < k2)) ? (2f * k1 / MathF.PI + k2 - k1 + (1f - k2) * (2f / MathF.PI) * Mathf.Sin((t - k2) / (1f - k2) * MathF.PI / 2f)) : (2f * k1 / MathF.PI + t - k1)));
		return num2 / num;
	}
}
