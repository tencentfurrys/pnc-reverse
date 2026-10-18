using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

[ExecuteInEditMode]
[AddComponentMenu("Procedural Worlds/SECTR/Core/SECTR Member")]
public class SECTR_Member : MonoBehaviour
{
	[Serializable]
	public class Child
	{
		public GameObject gameObject;

		public int gameObjectHash;

		public SECTR_Member member;

		public Renderer renderer;

		public int renderHash;

		public Light light;

		public int lightHash;

		public Terrain terrain;

		public int terrainHash;

		public Bounds rendererBounds;

		public Bounds lightBounds;

		public Bounds terrainBounds;

		public bool shadowLight;

		public bool rendererCastsShadows;

		public bool terrainCastsShadows;

		public LayerMask layer;

		public Vector3 shadowLightPosition;

		public float shadowLightRange;

		public LightType shadowLightType;

		public int shadowCullingMask;

		public bool processed;

		public bool renderCulled;

		public bool lightCulled;

		public bool terrainCulled;

		public void Init(GameObject gameObject, Renderer renderer, Light light, Terrain terrain, SECTR_Member member, bool dirShadowCaster, Vector3 shadowVec)
		{
			this.gameObject = gameObject;
			gameObjectHash = this.gameObject.GetInstanceID();
			this.member = member;
			this.renderer = (((bool)renderer && (renderCulled || renderer.enabled)) ? renderer : null);
			this.light = (((bool)light && (lightCulled || light.enabled) && (light.type == LightType.Point || light.type == LightType.Spot)) ? light : null);
			this.terrain = (((bool)terrain && (terrainCulled || terrain.enabled)) ? terrain : null);
			rendererBounds = (this.renderer ? this.renderer.bounds : default(Bounds));
			lightBounds = (this.light ? SECTR_Geometry.ComputeBounds(this.light) : default(Bounds));
			terrainBounds = (this.terrain ? SECTR_Geometry.ComputeBounds(this.terrain) : default(Bounds));
			layer = gameObject.layer;
			if (SECTR_Modules.VIS)
			{
				renderHash = (this.renderer ? this.renderer.GetInstanceID() : 0);
				lightHash = (this.light ? this.light.GetInstanceID() : 0);
				terrainHash = (this.terrain ? this.terrain.GetInstanceID() : 0);
				bool flag = true;
				shadowLight = (bool)this.light && light.shadows != LightShadows.None && (!light.bakingOutput.isBaked | flag);
				rendererCastsShadows = (bool)this.renderer && renderer.shadowCastingMode != ShadowCastingMode.Off && ((renderer.lightmapIndex == -1) | flag);
				terrainCastsShadows = (bool)this.terrain && terrain.shadowCastingMode == ShadowCastingMode.On && ((terrain.lightmapIndex == -1) | flag);
				if (dirShadowCaster)
				{
					if (rendererCastsShadows)
					{
						rendererBounds = SECTR_Geometry.ProjectBounds(rendererBounds, shadowVec);
					}
					if (terrainCastsShadows)
					{
						terrainBounds = SECTR_Geometry.ProjectBounds(terrainBounds, shadowVec);
					}
				}
				if (shadowLight)
				{
					shadowLightPosition = light.transform.position;
					shadowLightRange = light.range;
					shadowLightType = light.type;
					shadowCullingMask = light.cullingMask;
				}
				else
				{
					shadowLightPosition = Vector3.zero;
					shadowLightRange = 0f;
					shadowLightType = LightType.Rectangle;
					shadowCullingMask = 0;
				}
			}
			else
			{
				renderHash = 0;
				lightHash = 0;
				terrainHash = 0;
				shadowLight = false;
				rendererCastsShadows = false;
				terrainCastsShadows = false;
				shadowLightPosition = Vector3.zero;
				shadowLightRange = 0f;
				shadowLightType = LightType.Rectangle;
				shadowCullingMask = 0;
			}
			processed = true;
		}

		public override bool Equals(object obj)
		{
			if (obj is Child)
			{
				return this == (Child)obj;
			}
			return false;
		}

		public override int GetHashCode()
		{
			return gameObjectHash;
		}

		public static bool operator ==(Child x, Child y)
		{
			if ((object)x == null && (object)y == null)
			{
				return true;
			}
			if ((object)x == null || (object)y == null)
			{
				return false;
			}
			return x.gameObjectHash == y.gameObjectHash;
		}

		public static bool operator !=(Child x, Child y)
		{
			return !(x == y);
		}
	}

	public enum BoundsUpdateModes
	{
		Start = 0,
		Movement = 1,
		Always = 2,
		Static = 3,
		SelfOnly = 4
	}

	public enum ChildCullModes
	{
		Default = 0,
		Group = 1,
		Individual = 2
	}

	public delegate void MembershipChanged(List<SECTR_Sector> left, List<SECTR_Sector> joined);

	[SerializeField]
	private List<Transform> ignoreTransforms = new List<Transform>();

	[SerializeField]
	[HideInInspector]
	private List<Child> children = new List<Child>(16);

	[SerializeField]
	[HideInInspector]
	private List<Child> renderers = new List<Child>(16);

	[SerializeField]
	[HideInInspector]
	private List<Child> lights = new List<Child>(16);

	[SerializeField]
	[HideInInspector]
	private List<Child> terrains = new List<Child>(2);

	[SerializeField]
	[HideInInspector]
	private List<Child> shadowLights = (SECTR_Modules.VIS ? new List<Child>(16) : null);

	[SerializeField]
	[HideInInspector]
	private List<Child> shadowCasters = (SECTR_Modules.VIS ? new List<Child>(16) : null);

	[SerializeField]
	[HideInInspector]
	private Bounds totalBounds;

	[SerializeField]
	[HideInInspector]
	private Bounds renderBounds;

	[SerializeField]
	[HideInInspector]
	private Bounds lightBounds;

	[SerializeField]
	[HideInInspector]
	private bool hasRenderBounds;

	[SerializeField]
	[HideInInspector]
	private bool hasLightBounds;

	[SerializeField]
	[HideInInspector]
	private bool shadowCaster;

	[SerializeField]
	[HideInInspector]
	private bool shadowLight;

	[SerializeField]
	[HideInInspector]
	private bool frozen;

	[SerializeField]
	[HideInInspector]
	private bool neverJoin;

	[SerializeField]
	[HideInInspector]
	protected List<Light> bakedOnlyLights = (SECTR_Modules.VIS ? new List<Light>(8) : null);

	protected bool isSector;

	protected SECTR_Member childProxy;

	protected bool hasChildProxy;

	private bool started;

	private bool usedStartSector;

	private List<SECTR_Sector> sectors = new List<SECTR_Sector>(4);

	private List<SECTR_Sector> newSectors = new List<SECTR_Sector>(4);

	private List<SECTR_Sector> leftSectors = new List<SECTR_Sector>(4);

	private Dictionary<Transform, Child> childTable = new Dictionary<Transform, Child>(8);

	private Dictionary<Light, Light> bakedOnlyTable;

	private Vector3 lastPosition = Vector3.zero;

	private Stack<Child> childPool = new Stack<Child>(32);

	private static List<SECTR_Member> allMembers = new List<SECTR_Member>(256);

	private static Dictionary<Transform, SECTR_Member> allMemberTable = new Dictionary<Transform, SECTR_Member>(256);

	[SECTR_ToolTip("Set to true if Sector membership should only change when crossing a portal.")]
	public bool PortalDetermined;

	[SECTR_ToolTip("If set, forces the initial Sector to be the specified Sector.", "PortalDetermined")]
	public SECTR_Sector ForceStartSector;

	[SECTR_ToolTip("Global Setting. Determines how often the Sector Members are re-evaluated. More frequent updates requires more CPU. Switch to delayed or Save & Export only if experiencing Editor Slowdowns with many Sectors in the scene.")]
	public static SECTR_MemberUpdateMode MemberUpdateMode = SECTR_MemberUpdateMode.Realtime;

	[SECTR_ToolTip("Global Setting. Delay in Milliseconds until Sector Members are re-evaluated in delayed mode.")]
	public static int MemberUpdateDelay = 3000;

	[SECTR_ToolTip("Determines how often the bounds are recomputed. More frequent updates requires more CPU.")]
	public BoundsUpdateModes BoundsUpdateMode = BoundsUpdateModes.Always;

	[SECTR_ToolTip("Adds a buffer on bounding box to compensate for minor imprecisions.")]
	public float ExtraBounds = 0.01f;

	[SECTR_ToolTip("Override computed bounds with the user specified bounds. Advanced users only.")]
	public bool OverrideBounds;

	[SECTR_ToolTip("User specified override bounds. Auto-populated with the current bounds when override is inactive.", "OverrideBounds")]
	public Bounds BoundsOverride;

	[SECTR_ToolTip("Optional shadow casting directional light to use in membership calculations. Bounds will be extruded away from light, if set.")]
	public Light DirShadowCaster;

	[SECTR_ToolTip("Distance by which to extend the bounds away from the shadow casting light.", "DirShadowCaster")]
	public float DirShadowDistance = 100f;

	[SECTR_ToolTip("Determines if this SectorCuller should cull individual children, or cull all children based on the aggregate bounds.")]
	public ChildCullModes ChildCulling;

	[NonSerialized]
	[HideInInspector]
	public int LastVisibleFrameNumber;

	public bool FloatingPointFix;

	public static List<SECTR_Member> All => allMembers;

	public bool CullEachChild
	{
		get
		{
			if (ChildCulling != ChildCullModes.Individual)
			{
				if (ChildCulling == ChildCullModes.Default)
				{
					return isSector;
				}
				return false;
			}
			return true;
		}
	}

	public List<SECTR_Sector> Sectors => sectors;

	public List<Child> Children
	{
		get
		{
			if (!hasChildProxy)
			{
				return children;
			}
			return childProxy.children;
		}
	}

	public List<Child> Renderers
	{
		get
		{
			if (!hasChildProxy)
			{
				return renderers;
			}
			return childProxy.renderers;
		}
	}

	public bool ShadowCaster
	{
		get
		{
			if (!hasChildProxy)
			{
				return shadowCaster;
			}
			return childProxy.shadowCaster;
		}
	}

	public List<Child> ShadowCasters
	{
		get
		{
			if (!hasChildProxy)
			{
				return shadowCasters;
			}
			return childProxy.shadowCasters;
		}
	}

	public List<Child> Lights
	{
		get
		{
			if (!hasChildProxy)
			{
				return lights;
			}
			return childProxy.lights;
		}
	}

	public bool ShadowLight
	{
		get
		{
			if (!hasChildProxy)
			{
				return shadowLight;
			}
			return childProxy.shadowLight;
		}
	}

	public List<Child> ShadowLights
	{
		get
		{
			if (!hasChildProxy)
			{
				return shadowLights;
			}
			return childProxy.shadowLights;
		}
	}

	public List<Child> Terrains
	{
		get
		{
			if (!hasChildProxy)
			{
				return terrains;
			}
			return childProxy.terrains;
		}
	}

	public Bounds TotalBounds => totalBounds;

	public Bounds RenderBounds
	{
		get
		{
			if (!hasChildProxy)
			{
				return renderBounds;
			}
			return childProxy.renderBounds;
		}
	}

	public bool HasRenderBounds
	{
		get
		{
			if (!hasChildProxy)
			{
				return hasRenderBounds;
			}
			return childProxy.hasRenderBounds;
		}
	}

	public Bounds LightBounds
	{
		get
		{
			if (!hasChildProxy)
			{
				return lightBounds;
			}
			return childProxy.lightBounds;
		}
	}

	public bool HasLightBounds
	{
		get
		{
			if (!hasChildProxy)
			{
				return hasLightBounds;
			}
			return childProxy.hasLightBounds;
		}
	}

	public bool Frozen
	{
		get
		{
			return frozen;
		}
		set
		{
			if (isSector)
			{
				frozen = value;
			}
		}
	}

	public SECTR_Member ChildProxy
	{
		set
		{
			childProxy = value;
			hasChildProxy = childProxy != null;
		}
	}

	public bool NeverJoin
	{
		set
		{
			neverJoin = true;
		}
	}

	public bool IsSector => isSector;

	public event MembershipChanged Changed;

	public bool IsVisibleThisFrame()
	{
		return LastVisibleFrameNumber == Time.frameCount;
	}

	public bool WasVisibleLastFrame()
	{
		return LastVisibleFrameNumber == Time.frameCount - 1;
	}

	public void ForceUpdate(bool updateChildren)
	{
		if (updateChildren)
		{
			_UpdateChildren();
		}
		lastPosition = base.transform.position;
		if (!isSector && !neverJoin)
		{
			_UpdateSectorMembership();
		}
	}

	public void SectorDisabled(SECTR_Sector sector)
	{
		if ((bool)sector)
		{
			sectors.Remove(sector);
			if (Changed != null)
			{
				leftSectors.Clear();
				leftSectors.Add(sector);
				Changed(leftSectors, null);
			}
		}
	}

	private void Start()
	{
		started = true;
		ForceUpdate(children.Count == 0);
	}

	protected virtual void OnEnable()
	{
		allMembers.Add(this);
		allMemberTable.Add(base.transform, this);
		if (bakedOnlyLights != null)
		{
			int count = bakedOnlyLights.Count;
			bakedOnlyTable = new Dictionary<Light, Light>(count);
			for (int i = 0; i < count; i++)
			{
				Light light = bakedOnlyLights[i];
				if ((bool)light)
				{
					bakedOnlyTable[light] = light;
				}
			}
		}
		if (started)
		{
			ForceUpdate(updateChildren: true);
		}
	}

	private void OnSceneSaving(Scene scene, string path)
	{
		if (MemberUpdateMode == SECTR_MemberUpdateMode.OnSaveOnly && base.gameObject.scene == scene)
		{
			UpdateMembers();
		}
	}

	protected virtual void OnDisable()
	{
		if (Changed != null && sectors.Count > 0)
		{
			Changed(sectors, null);
		}
		if (!isSector && !neverJoin)
		{
			int count = sectors.Count;
			for (int i = 0; i < count; i++)
			{
				SECTR_Sector sECTR_Sector = sectors[i];
				if ((bool)sECTR_Sector)
				{
					sECTR_Sector.Deregister(this);
				}
			}
			sectors.Clear();
		}
		int count2 = children.Count;
		for (int j = 0; j < count2; j++)
		{
			Child child = children[j];
			child.processed = false;
			childPool.Push(child);
		}
		children.Clear();
		childTable.Clear();
		renderers.Clear();
		lights.Clear();
		terrains.Clear();
		if (SECTR_Modules.VIS)
		{
			shadowLights.Clear();
			shadowCasters.Clear();
		}
		bakedOnlyTable = null;
		allMembers.Remove(this);
		allMemberTable.Remove(base.transform);
	}

	protected virtual void OnDestroy()
	{
	}

	private void LateUpdate()
	{
		UpdateMembers();
	}

	public void UpdateMembers()
	{
		if (BoundsUpdateMode != BoundsUpdateModes.Static && (BoundsUpdateMode == BoundsUpdateModes.Always || base.transform.hasChanged))
		{
			_UpdateChildren();
			if (!isSector && !neverJoin)
			{
				_UpdateSectorMembership();
			}
			lastPosition = base.transform.position;
			base.transform.hasChanged = false;
		}
	}

	public void UpdatePrefSettings()
	{
	}

	private void _UpdateChildren()
	{
		if (FloatingPointFix)
		{
			totalBounds = BoundsOverride;
		}
		else
		{
			if (frozen || (bool)childProxy)
			{
				return;
			}
			bool flag = SECTR_Modules.VIS && (bool)DirShadowCaster && DirShadowCaster.type == LightType.Directional && DirShadowCaster.shadows != LightShadows.None;
			Vector3 shadowVec = (flag ? (DirShadowCaster.transform.forward * DirShadowDistance) : Vector3.zero);
			int num = children.Count;
			int num2 = 0;
			hasLightBounds = false;
			hasRenderBounds = false;
			shadowCaster = false;
			shadowLight = false;
			renderers.Clear();
			lights.Clear();
			terrains.Clear();
			if (SECTR_Modules.VIS)
			{
				shadowCasters.Clear();
				shadowLights.Clear();
			}
			if ((BoundsUpdateMode == BoundsUpdateModes.Start || BoundsUpdateMode == BoundsUpdateModes.SelfOnly) && num > 0)
			{
				while (num2 < num)
				{
					Child child = children[num2];
					if (!child.gameObject)
					{
						children.RemoveAt(num2);
						num--;
						continue;
					}
					child.Init(child.gameObject, child.renderer, child.light, child.terrain, child.member, flag, shadowVec);
					if ((bool)child.renderer)
					{
						if (!hasRenderBounds)
						{
							renderBounds = child.rendererBounds;
							hasRenderBounds = true;
						}
						else
						{
							renderBounds.Encapsulate(child.rendererBounds);
						}
						renderers.Add(child);
					}
					if ((bool)child.terrain)
					{
						if (!hasRenderBounds)
						{
							renderBounds = child.terrainBounds;
							hasRenderBounds = true;
						}
						else
						{
							renderBounds.Encapsulate(child.terrainBounds);
						}
						terrains.Add(child);
					}
					if ((bool)child.light)
					{
						if (SECTR_Modules.VIS && child.shadowLight)
						{
							shadowLights.Add(child);
							shadowLight = true;
						}
						if (!hasLightBounds)
						{
							lightBounds = child.lightBounds;
							hasLightBounds = true;
						}
						else
						{
							lightBounds.Encapsulate(child.lightBounds);
						}
						lights.Add(child);
					}
					if (SECTR_Modules.VIS && (child.terrainCastsShadows || child.rendererCastsShadows))
					{
						shadowCasters.Add(child);
						shadowCaster = true;
					}
					num2++;
				}
			}
			else
			{
				for (num2 = 0; num2 < num; num2++)
				{
					children[num2].processed = false;
				}
				_AddChildren(base.transform, flag, shadowVec);
				num2 = 0;
				num = children.Count;
				while (num2 < num)
				{
					Child child2 = children[num2];
					if (!child2.processed)
					{
						childPool.Push(child2);
						children.RemoveAt(num2);
						num--;
					}
					else
					{
						num2++;
					}
				}
			}
			Bounds bounds = new Bounds(base.transform.position, Vector3.zero);
			if (hasRenderBounds && (isSector || neverJoin))
			{
				totalBounds = renderBounds;
			}
			else if (hasRenderBounds && hasLightBounds)
			{
				totalBounds = renderBounds;
				totalBounds.Encapsulate(lightBounds);
			}
			else if (hasRenderBounds)
			{
				totalBounds = renderBounds;
				lightBounds = bounds;
			}
			else if (hasLightBounds)
			{
				totalBounds = lightBounds;
				renderBounds = bounds;
			}
			else
			{
				totalBounds = bounds;
				lightBounds = bounds;
				renderBounds = bounds;
			}
			totalBounds.Expand(ExtraBounds);
			if (OverrideBounds)
			{
				totalBounds = BoundsOverride;
			}
		}
	}

	private void _AddChildren(Transform childTransform, bool dirShadowCaster, Vector3 shadowVec)
	{
		if (!childTransform.gameObject.activeSelf || (!(childTransform == base.transform) && allMemberTable.ContainsKey(childTransform)) || (ignoreTransforms != null && ignoreTransforms.Contains(childTransform)))
		{
			return;
		}
		Child value = null;
		childTable.TryGetValue(childTransform, out value);
		Light light = ((value != null) ? value.light : childTransform.GetComponent<Light>());
		Renderer renderer = ((value != null) ? value.renderer : childTransform.GetComponent<Renderer>());
		Terrain terrain = null;
		if (isSector || neverJoin)
		{
			terrain = ((value != null) ? value.terrain : childTransform.GetComponent<Terrain>());
		}
		if (bakedOnlyLights != null && (bool)light && light.bakingOutput.isBaked && LightmapSettings.lightmaps.Length != 0 && bakedOnlyTable != null && bakedOnlyTable.ContainsKey(light))
		{
			light = null;
		}
		Child child = value;
		if (child == null)
		{
			child = ((childPool.Count <= 0) ? new Child() : childPool.Pop());
			childTable[childTransform] = child;
			children.Add(child);
		}
		child.Init(childTransform.gameObject, renderer, light, terrain, this, dirShadowCaster, shadowVec);
		if ((bool)child.renderer)
		{
			bool flag = true;
			if (isSector && renderer.GetType() == typeof(ParticleSystemRenderer))
			{
				flag = false;
			}
			if (flag)
			{
				if (!hasRenderBounds)
				{
					renderBounds = child.rendererBounds;
					hasRenderBounds = true;
				}
				else
				{
					renderBounds.Encapsulate(child.rendererBounds);
				}
			}
			renderers.Add(child);
		}
		if ((bool)child.light)
		{
			if (SECTR_Modules.VIS && child.shadowLight)
			{
				shadowLights.Add(child);
				shadowLight = true;
			}
			if (!hasLightBounds)
			{
				lightBounds = child.lightBounds;
				hasLightBounds = true;
			}
			else
			{
				lightBounds.Encapsulate(child.lightBounds);
			}
			lights.Add(child);
		}
		if ((bool)child.terrain)
		{
			if (!hasRenderBounds)
			{
				renderBounds = child.terrainBounds;
				hasRenderBounds = true;
			}
			else
			{
				renderBounds.Encapsulate(child.terrainBounds);
			}
			terrains.Add(child);
		}
		if (SECTR_Modules.VIS && (child.terrainCastsShadows || child.rendererCastsShadows))
		{
			shadowCasters.Add(child);
			shadowCaster = true;
		}
		if (BoundsUpdateMode != BoundsUpdateModes.SelfOnly)
		{
			int childCount = childTransform.transform.childCount;
			for (int i = 0; i < childCount; i++)
			{
				_AddChildren(childTransform.GetChild(i), dirShadowCaster, shadowVec);
			}
		}
	}

	private void _UpdateSectorMembership()
	{
		if (frozen || isSector || neverJoin)
		{
			return;
		}
		newSectors.Clear();
		leftSectors.Clear();
		if (PortalDetermined && sectors.Count > 0)
		{
			int count = sectors.Count;
			for (int i = 0; i < count; i++)
			{
				SECTR_Sector sECTR_Sector = sectors[i];
				SECTR_Portal sECTR_Portal = _CrossedPortal(sECTR_Sector);
				if ((bool)sECTR_Portal)
				{
					SECTR_Sector item = ((sECTR_Portal.FrontSector == sECTR_Sector) ? sECTR_Portal.BackSector : sECTR_Portal.FrontSector);
					if (!newSectors.Contains(item))
					{
						newSectors.Add(item);
					}
					leftSectors.Add(sECTR_Sector);
				}
			}
			count = newSectors.Count;
			for (int j = 0; j < count; j++)
			{
				SECTR_Sector sECTR_Sector2 = newSectors[j];
				sECTR_Sector2.Register(this);
				sectors.Add(sECTR_Sector2);
			}
			count = leftSectors.Count;
			for (int k = 0; k < count; k++)
			{
				SECTR_Sector sECTR_Sector3 = leftSectors[k];
				sECTR_Sector3.Deregister(this);
				sectors.Remove(sECTR_Sector3);
			}
		}
		else if (PortalDetermined && (bool)ForceStartSector && !usedStartSector)
		{
			ForceStartSector.Register(this);
			sectors.Add(ForceStartSector);
			newSectors.Add(ForceStartSector);
			usedStartSector = true;
		}
		else
		{
			SECTR_Sector.GetContaining(ref newSectors, TotalBounds);
			int num = 0;
			int num2 = sectors.Count;
			while (num < num2)
			{
				SECTR_Sector sECTR_Sector4 = sectors[num];
				if (newSectors.Contains(sECTR_Sector4))
				{
					newSectors.Remove(sECTR_Sector4);
					num++;
					continue;
				}
				sECTR_Sector4.Deregister(this);
				leftSectors.Add(sECTR_Sector4);
				sectors.RemoveAt(num);
				num2--;
			}
			num2 = newSectors.Count;
			if (num2 > 0)
			{
				for (num = 0; num < num2; num++)
				{
					SECTR_Sector sECTR_Sector5 = newSectors[num];
					sECTR_Sector5.Register(this);
					sectors.Add(sECTR_Sector5);
				}
			}
		}
		if (Changed != null && (leftSectors.Count > 0 || newSectors.Count > 0))
		{
			Changed(leftSectors, newSectors);
		}
	}

	private SECTR_Portal _CrossedPortal(SECTR_Sector sector)
	{
		if ((bool)sector)
		{
			Vector3 lhs = base.transform.position - lastPosition;
			int count = sector.Portals.Count;
			for (int i = 0; i < count; i++)
			{
				SECTR_Portal sECTR_Portal = sector.Portals[i];
				if ((bool)sECTR_Portal)
				{
					bool num = sECTR_Portal.FrontSector == sector;
					Plane plane = (num ? sECTR_Portal.HullPlane : sECTR_Portal.ReverseHullPlane);
					if ((bool)(num ? sECTR_Portal.BackSector : sECTR_Portal.FrontSector) && Vector3.Dot(lhs, plane.normal) < 0f && plane.GetSide(base.transform.position) != plane.GetSide(lastPosition) && sECTR_Portal.IsPointInHull(base.transform.position, lhs.magnitude))
					{
						return sECTR_Portal;
					}
				}
			}
		}
		return null;
	}
}
