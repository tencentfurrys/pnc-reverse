using System.Collections.Generic;
using System.Linq;
using DunGen.Generation;
using UnityEngine;

namespace DunGen.Adapters;

[AddComponentMenu("DunGen/Culling/SECTR VIS Portal Culling Adapter")]
[HelpURL("https://dungen-docs.aegongames.com/integrations/sectr/")]
public class SECTRPortalCullingAdapter : CullingAdapter
{
	public Light DirectionalShadowCaster;

	public bool CullEachChild;

	public Vector3 ExtraBounds;

	protected readonly Dictionary<Tile, SECTR_Sector> tileSectors = new Dictionary<Tile, SECTR_Sector>();

	protected readonly Dictionary<Door, SECTR_Portal> doorPortals = new Dictionary<Door, SECTR_Portal>();

	protected readonly Dictionary<Vector2, Mesh> portalMeshes = new Dictionary<Vector2, Mesh>();

	protected virtual void OnDestroy()
	{
		foreach (KeyValuePair<Vector2, Mesh> portalMesh in portalMeshes)
		{
			if (portalMesh.Value != null)
			{
				Object.Destroy(portalMesh.Value);
			}
		}
		portalMeshes.Clear();
	}

	protected virtual void OnDoorStateChanged(Door door, bool isOpen)
	{
		if (doorPortals.TryGetValue(door, out var value))
		{
			value.SetFlag(SECTR_Portal.PortalFlags.Closed, door.ShouldCullBehind);
		}
		else
		{
			Debug.LogError("[DunGen] No SECTR_Portal component is assigned to this doorway (" + door.gameObject.name + "). This shouldn't happen.");
		}
	}

	protected virtual Mesh GetOrCreatePortalMesh(Vector2 size)
	{
		if (portalMeshes.TryGetValue(size, out var value))
		{
			return value;
		}
		value = new Mesh();
		float num = size.x * 0.5f;
		value.vertices = new Vector3[4]
		{
			new Vector3(0f - num, 0f, 0f),
			new Vector3(0f - num, size.y, 0f),
			new Vector3(num, size.y, 0f),
			new Vector3(num, 0f, 0f)
		};
		value.normals = new Vector3[4]
		{
			Vector3.forward,
			Vector3.forward,
			Vector3.forward,
			Vector3.forward
		};
		value.triangles = new int[6] { 0, 1, 2, 2, 3, 0 };
		portalMeshes.Add(size, value);
		return value;
	}

	protected override void PrepareForCulling(GenerationContext context)
	{
		DungeonAttachmentSettings attachmentSettings = context.Request.AttachmentSettings;
		Dungeon dungeon = context.Dungeon;
		if (attachmentSettings == null || attachmentSettings.TileProxy == null)
		{
			tileSectors.Clear();
			doorPortals.Clear();
		}
		foreach (DungeonGraphNode node in dungeon.ConnectionGraph.Nodes)
		{
			GameObject gameObject = node.Tile.gameObject;
			if (!gameObject.TryGetComponent<SECTR_Sector>(out var component))
			{
				component = gameObject.AddComponent<SECTR_Sector>();
			}
			component.BoundsUpdateMode = SECTR_Member.BoundsUpdateModes.Static;
			Bounds bounds = node.Tile.Bounds;
			if (ExtraBounds != Vector3.zero)
			{
				bounds.size += ExtraBounds;
			}
			component.OverrideBounds = true;
			component.BoundsOverride = bounds;
			tileSectors[node.Tile] = component;
			SetSectorProperties(component);
		}
		Dictionary<Doorway, SECTR_Sector> dictionary = GatherDoorwayInteriorSectors(dungeon);
		foreach (DungeonGraphConnection connection in dungeon.ConnectionGraph.Connections)
		{
			Doorway doorwayA = connection.DoorwayA;
			Vector2 size = doorwayA.Socket.Size;
			if (!doorwayA.gameObject.TryGetComponent<SECTR_Portal>(out var component2))
			{
				component2 = doorwayA.gameObject.AddComponent<SECTR_Portal>();
			}
			component2.HullMesh = GetOrCreatePortalMesh(size);
			component2.FrontSector = dictionary[connection.DoorwayB];
			component2.BackSector = dictionary[connection.DoorwayA];
			Door door = (doorwayA.HasDoorPrefabInstance ? doorwayA.UsedDoorPrefabInstance.GetComponent<Door>() : null);
			if (door != null)
			{
				doorPortals[door] = component2;
				component2.SetFlag(SECTR_Portal.PortalFlags.Closed, !door.IsOpen);
				if (door.gameObject.GetComponent<SECTR_Member>() == null)
				{
					door.gameObject.AddComponent<SECTR_Member>();
				}
				door.OnDoorStateChanged += OnDoorStateChanged;
			}
		}
		SECTR_CullingCamera[] array = UnityUtil.FindObjectsByType<SECTR_CullingCamera>();
		foreach (SECTR_CullingCamera sECTR_CullingCamera in array)
		{
			if (sECTR_CullingCamera.isActiveAndEnabled)
			{
				sECTR_CullingCamera.enabled = false;
				sECTR_CullingCamera.enabled = true;
			}
		}
	}

	protected virtual void SetSectorProperties(SECTR_Sector sector)
	{
		sector.DirShadowCaster = DirectionalShadowCaster;
		sector.ChildCulling = ((!CullEachChild) ? SECTR_Member.ChildCullModes.Group : SECTR_Member.ChildCullModes.Individual);
	}

	protected virtual Dictionary<Doorway, SECTR_Sector> GatherDoorwayInteriorSectors(Dungeon dungeon)
	{
		Dictionary<Doorway, SECTR_Sector> dictionary = new Dictionary<Doorway, SECTR_Sector>();
		Doorway[] array = dungeon.AllTiles.SelectMany((Tile x) => x.AllDoorways).Distinct().ToArray();
		foreach (Doorway doorway in array)
		{
			if (doorway.gameObject.TryGetComponent<SECTRDoorwayLink>(out var component))
			{
				SetSectorProperties(component.Sector);
				dictionary[doorway] = component.Sector;
			}
			else
			{
				dictionary[doorway] = tileSectors[doorway.Tile];
			}
		}
		return dictionary;
	}
}
