using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(SECTR_Sector))]
[AddComponentMenu("Procedural Worlds/SECTR/Stream/SECTR Chunk")]
public class SECTR_Chunk : MonoBehaviour
{
	public enum LoadState
	{
		Unloaded = 0,
		Loading = 1,
		Loaded = 2,
		Unloading = 3,
		Active = 4
	}

	public delegate void LoadCallback(SECTR_Chunk source, LoadState loadState);

	private AsyncOperation asyncLoadOp;

	private LoadState loadState;

	private int refCount;

	private GameObject chunkRoot;

	private GameObject chunkSector;

	private bool recenterChunk;

	private SECTR_Sector cachedSector;

	private GameObject proxy;

	private bool quitting;

	private static SECTR_Chunk chunkActivating = null;

	private static LinkedList<SECTR_Chunk> activationQueue = new LinkedList<SECTR_Chunk>();

	private static bool requestedDeferredUnload = false;

	[SECTR_ToolTip("The path of the scene to load")]
	public string ScenePath;

	[SECTR_ToolTip("The unique name of the root object in the exported Sector.")]
	public string NodeName;

	[SECTR_ToolTip("Exports the Chunk in a way that allows it to be shared by multiple Sectors, but may take more CPU to load.")]
	public bool ExportForReuse;

	[SECTR_ToolTip("A mesh to display when this Chunk is unloaded. Will be hidden when loaded.")]
	public Mesh ProxyMesh;

	[SECTR_ToolTip("The per-submesh materials for the proxy.")]
	public Material[] ProxyMaterials;

	public SECTR_Sector Sector => cachedSector;

	public event LoadCallback Changed;

	public event LoadCallback ReferenceChange;

	public void AddReference()
	{
		if (refCount == 0)
		{
			_Load();
		}
		refCount++;
		if (ReferenceChange != null)
		{
			ReferenceChange(this, LoadState.Loading);
		}
	}

	public void RemoveReference()
	{
		if (ReferenceChange != null)
		{
			ReferenceChange(this, LoadState.Unloading);
		}
		refCount--;
		if (refCount <= 0)
		{
			_Unload();
			refCount = 0;
		}
	}

	public bool IsLoaded()
	{
		return loadState == LoadState.Active;
	}

	public bool IsUnloaded()
	{
		return loadState == LoadState.Unloaded;
	}

	public float LoadProgress()
	{
		switch (loadState)
		{
		case LoadState.Loading:
			if (asyncLoadOp == null)
			{
				return 0.5f;
			}
			return asyncLoadOp.progress * 0.8f;
		case LoadState.Loaded:
			return 0.9f;
		case LoadState.Active:
			return 1f;
		default:
			return 0f;
		}
	}

	private void Awake()
	{
		SECTR_LightmapRef.InitRefCounts();
	}

	private void OnEnable()
	{
		cachedSector = GetComponent<SECTR_Sector>();
		if (cachedSector.Frozen)
		{
			_CreateProxy();
		}
	}

	private void OnDisable()
	{
		if (!quitting && asyncLoadOp != null && !asyncLoadOp.isDone)
		{
			Debug.LogError("Chunk unloaded with async operation active. Do not disable chunks until async operations are complete or Unity will likely crash.");
		}
		if (loadState != LoadState.Unloaded)
		{
			_FindChunkRoot();
			if ((bool)chunkRoot)
			{
				_DestroyChunk(createProxy: false, fromDisable: true);
			}
		}
		cachedSector = null;
	}

	private void OnApplicationQuit()
	{
		quitting = true;
	}

	private void FixedUpdate()
	{
		switch (loadState)
		{
		case LoadState.Loading:
			_TrySceneActivation();
			if (asyncLoadOp == null || asyncLoadOp.isDone)
			{
				if (asyncLoadOp != null)
				{
					chunkActivating = null;
					activationQueue.RemoveFirst();
					asyncLoadOp = null;
				}
				loadState = LoadState.Loaded;
				if (Changed != null)
				{
					Changed(this, loadState);
				}
				FixedUpdate();
			}
			break;
		case LoadState.Loaded:
			_SetupChunk();
			break;
		case LoadState.Unloading:
			_TrySceneActivation();
			_FindChunkRoot();
			if ((bool)chunkRoot)
			{
				_DestroyChunk(createProxy: true, fromDisable: false);
			}
			break;
		case LoadState.Active:
			break;
		}
	}

	private void _Load()
	{
		if (ScenePath == null || !base.enabled || (loadState != LoadState.Unloaded && loadState != LoadState.Unloading))
		{
			return;
		}
		if (loadState == LoadState.Unloaded)
		{
			if (!SECTR_Modules.HasPro())
			{
				SceneManager.LoadScene(ScenePath, LoadSceneMode.Additive);
				_SetupChunk();
			}
			else
			{
				asyncLoadOp = SceneManager.LoadSceneAsync(ScenePath, LoadSceneMode.Additive);
				activationQueue.AddLast(this);
			}
			chunkRoot = null;
			chunkSector = null;
			recenterChunk = false;
		}
		loadState = LoadState.Loading;
		if (Changed != null)
		{
			Changed(this, loadState);
		}
	}

	private void _Unload()
	{
		if (!base.enabled || loadState == LoadState.Unloaded)
		{
			return;
		}
		if ((bool)cachedSector)
		{
			cachedSector.Frozen = true;
		}
		if ((bool)chunkRoot)
		{
			_DestroyChunk(createProxy: true, fromDisable: false);
			return;
		}
		loadState = LoadState.Unloading;
		if (Changed != null)
		{
			Changed(this, loadState);
		}
	}

	private void _DestroyChunk(bool createProxy, bool fromDisable)
	{
		if ((bool)cachedSector && ((bool)cachedSector.TopTerrain || (bool)cachedSector.BottomTerrain || (bool)cachedSector.RightTerrain || (bool)cachedSector.LeftTerrain))
		{
			cachedSector.DisonnectTerrainNeighbors();
		}
		SceneManager.UnloadSceneAsync(ScenePath);
		chunkRoot = null;
		chunkSector = null;
		recenterChunk = false;
		if (asyncLoadOp != null)
		{
			if (chunkActivating == this)
			{
				chunkActivating = null;
			}
			activationQueue.Remove(this);
			asyncLoadOp = null;
		}
		if (fromDisable || quitting)
		{
			_UnloadResources();
		}
		else if (!requestedDeferredUnload)
		{
			requestedDeferredUnload = true;
			StartCoroutine("_DeferredUnload");
		}
		loadState = LoadState.Unloaded;
		if (Changed != null)
		{
			Changed(this, loadState);
		}
		if (createProxy && (bool)ProxyMesh)
		{
			_CreateProxy();
		}
	}

	private void _FindChunkRoot()
	{
		if (!(chunkRoot == null) || quitting)
		{
			return;
		}
		SECTR_ChunkRef sECTR_ChunkRef = SECTR_ChunkRef.FindChunkRef(NodeName);
		if ((bool)sECTR_ChunkRef && (bool)sECTR_ChunkRef.RealSector)
		{
			recenterChunk = sECTR_ChunkRef.Recentered;
			if (recenterChunk)
			{
				sECTR_ChunkRef.RealSector.parent = base.transform;
				chunkRoot = sECTR_ChunkRef.RealSector.gameObject;
				chunkSector = chunkRoot;
				Object.Destroy(sECTR_ChunkRef.gameObject);
			}
			else
			{
				chunkRoot = sECTR_ChunkRef.gameObject;
				chunkSector = sECTR_ChunkRef.RealSector.gameObject;
				Object.Destroy(sECTR_ChunkRef);
			}
		}
		else
		{
			chunkRoot = GameObject.Find(NodeName);
			chunkSector = chunkRoot;
			recenterChunk = false;
		}
	}

	private void _SetupChunk()
	{
		_FindChunkRoot();
		if (!chunkRoot)
		{
			return;
		}
		if (!chunkRoot.activeSelf)
		{
			chunkRoot.SetActive(value: true);
		}
		if (recenterChunk && !cachedSector.FloatingPointFix)
		{
			Transform obj = chunkRoot.transform;
			obj.localPosition = Vector3.zero;
			obj.localRotation = Quaternion.identity;
			obj.localScale = Vector3.one;
		}
		if (cachedSector.FloatingPointFix)
		{
			chunkRoot.transform.position = SECTR_FloatingPointFix.Instance.totalOffset;
			if (chunkRoot.transform.GetComponent<SECTR_FloatingPointFixMember>() == null)
			{
				chunkRoot.AddComponent<SECTR_FloatingPointFixMember>();
			}
		}
		SECTR_Member sECTR_Member = chunkSector.GetComponent<SECTR_Member>();
		if (!sECTR_Member)
		{
			sECTR_Member = chunkSector.gameObject.AddComponent<SECTR_Member>();
			sECTR_Member.BoundsUpdateMode = SECTR_Member.BoundsUpdateModes.Static;
			sECTR_Member.ForceUpdate(updateChildren: true);
		}
		else if (recenterChunk)
		{
			sECTR_Member.ForceUpdate(updateChildren: true);
		}
		cachedSector.ChildProxy = sECTR_Member;
		cachedSector.Frozen = false;
		if ((bool)cachedSector.TopTerrain || (bool)cachedSector.BottomTerrain || (bool)cachedSector.LeftTerrain || (bool)cachedSector.RightTerrain)
		{
			cachedSector.ConnectTerrainNeighbors();
			if ((bool)cachedSector.TopTerrain)
			{
				cachedSector.TopTerrain.ConnectTerrainNeighbors();
			}
			if ((bool)cachedSector.BottomTerrain)
			{
				cachedSector.BottomTerrain.ConnectTerrainNeighbors();
			}
			if ((bool)cachedSector.LeftTerrain)
			{
				cachedSector.LeftTerrain.ConnectTerrainNeighbors();
			}
			if ((bool)cachedSector.RightTerrain)
			{
				cachedSector.RightTerrain.ConnectTerrainNeighbors();
			}
		}
		if ((bool)proxy)
		{
			Object.Destroy(proxy);
		}
		loadState = LoadState.Active;
		if (Changed != null)
		{
			Changed(this, loadState);
		}
	}

	private void _CreateProxy()
	{
		if (proxy == null && (bool)ProxyMesh && !quitting)
		{
			proxy = new GameObject(base.name + " Proxy");
			proxy.AddComponent<MeshFilter>().sharedMesh = ProxyMesh;
			proxy.AddComponent<MeshRenderer>().sharedMaterials = ProxyMaterials;
			proxy.transform.position = base.transform.position;
			proxy.transform.rotation = base.transform.rotation;
			proxy.transform.localScale = base.transform.lossyScale;
		}
	}

	private void _TrySceneActivation()
	{
		if (chunkActivating == null && asyncLoadOp != null && !asyncLoadOp.allowSceneActivation && asyncLoadOp.progress >= 0.9f && activationQueue.Count > 0 && activationQueue.First.Value == this)
		{
			chunkActivating = this;
			asyncLoadOp.allowSceneActivation = true;
		}
	}

	private void _UnloadResources()
	{
		Resources.UnloadUnusedAssets();
		requestedDeferredUnload = false;
	}

	private IEnumerator _DeferredUnload()
	{
		yield return new WaitForEndOfFrame();
		_UnloadResources();
		yield return null;
	}

	private IEnumerator _UnloadScene(string scenePath)
	{
		yield return new WaitForEndOfFrame();
		SceneManager.UnloadSceneAsync(ScenePath);
	}
}
