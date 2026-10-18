using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DunGen;
using DunGen.Generation;
using Pathfinding;
using UnityEngine;

public class DunGenAStarIntegration : MonoBehaviour
{
	[Header("References")]
	[SerializeField]
	private RuntimeDungeon runtimeDungeon;

	[SerializeField]
	private AstarPath astarPath;

	[Header("Settings")]
	[SerializeField]
	private bool autoScanOnGeneration = true;

	[SerializeField]
	private float scanDelay = 0.5f;

	[Header("Debug")]
	[SerializeField]
	private bool enableDebugLogs = true;

	private bool isDungeonGenerated;

	private bool isAStarScanComplete;

	private int generationRetryCount;

	public GenerationStatus GeneratorStatus
	{
		get
		{
			if (runtimeDungeon == null || runtimeDungeon.Generator == null)
			{
				return GenerationStatus.NotStarted;
			}
			return runtimeDungeon.Generator.Status;
		}
	}

	private void Awake()
	{
		if (runtimeDungeon == null)
		{
			runtimeDungeon = UnityEngine.Object.FindAnyObjectByType<RuntimeDungeon>();
		}
		if (!(runtimeDungeon == null))
		{
			runtimeDungeon.Generator.OnGenerationStatusChanged += OnGenerationStatusChanged;
			runtimeDungeon.Generator.Retrying += OnGenerationRetry;
			DungeonGenerator.OnGenerationFailureReportProduced += OnGenerationFailureReport;
		}
	}

	private void Start()
	{
		if ((UnityEngine.Object)(object)astarPath == null)
		{
			astarPath = AstarPath.active;
		}
		if (!(runtimeDungeon == null) && !((UnityEngine.Object)(object)astarPath == null) && !isDungeonGenerated && runtimeDungeon.Generator.Status == GenerationStatus.Complete)
		{
			OnDungeonGenerationComplete();
		}
	}

	private void OnDestroy()
	{
		if (runtimeDungeon != null && runtimeDungeon.Generator != null)
		{
			runtimeDungeon.Generator.OnGenerationStatusChanged -= OnGenerationStatusChanged;
			runtimeDungeon.Generator.Retrying -= OnGenerationRetry;
		}
		DungeonGenerator.OnGenerationFailureReportProduced -= OnGenerationFailureReport;
	}

	private void OnGenerationFailureReport(DungeonGenerator generator, GenerationFailureReport report)
	{
		if (report == null || report.TilePlacementResults == null)
		{
			return;
		}
		StringBuilder stringBuilder = new StringBuilder("DunGen failure detail: ");
		foreach (IGrouping<Type, TilePlacementResult> item in from r in report.TilePlacementResults
			where !(r is SuccessPlacementResult)
			group r by r.GetType() into g
			orderby g.Count() descending
			select g)
		{
			stringBuilder.Append($"{item.First().DisplayName} x{item.Count()}");
			List<string> list = (from p in item.Select(GetResultPrefab)
				where p != null
				group p by p.name into g
				orderby g.Count() descending
				select $"{g.Key} x{g.Count()}").ToList();
			if (list.Count > 0)
			{
				stringBuilder.Append(" (" + string.Join(", ", list) + ")");
			}
			stringBuilder.Append("; ");
		}
		Debug.LogError(stringBuilder.ToString());
	}

	private static GameObject GetResultPrefab(TilePlacementResult result)
	{
		if (!(result is NoMatchingDoorwayPlacementResult noMatchingDoorwayPlacementResult))
		{
			if (!(result is NoCandidateTilesResult noCandidateTilesResult))
			{
				if (result is TileTemplatePlacementResult tileTemplatePlacementResult)
				{
					return tileTemplatePlacementResult.TileTemplatePrefab;
				}
				return null;
			}
			return noCandidateTilesResult.FromTilePrefab;
		}
		return noMatchingDoorwayPlacementResult.FromTilePrefab;
	}

	private void OnGenerationRetry()
	{
		generationRetryCount++;
		if (generationRetryCount > 1 && generationRetryCount % 10 == 0)
		{
			Debug.LogWarning($"DunGen generation attempt #{generationRetryCount} - generation is failing repeatedly");
		}
	}

	private void OnGenerationStatusChanged(DungeonGenerator generator, GenerationStatus status)
	{
		switch (status)
		{
		case GenerationStatus.Complete:
			OnDungeonGenerationComplete();
			break;
		case GenerationStatus.Failed:
			OnDungeonGenerationFailed();
			break;
		}
	}

	private void OnDungeonGenerationComplete()
	{
		isDungeonGenerated = true;
		if (autoScanOnGeneration)
		{
			StartCoroutine(RescanAStarGraphDelayed());
		}
	}

	private void OnDungeonGenerationFailed()
	{
		isDungeonGenerated = false;
	}

	private IEnumerator RescanAStarGraphDelayed()
	{
		yield return new WaitForSeconds(scanDelay);
		RescanAStarGraph();
	}

	public void RescanAStarGraph()
	{
		if (!((UnityEngine.Object)(object)astarPath == null))
		{
			isAStarScanComplete = false;
			StartCoroutine(ScanGraphsAsync());
		}
	}

	private IEnumerator ScanGraphsAsync()
	{
		int steps = 0;
		foreach (Progress item in AstarPath.active.ScanAsync())
		{
			_ = item;
			steps++;
			if (steps % 10 == 0)
			{
				yield return null;
			}
		}
		isAStarScanComplete = true;
	}

	public void RescanSpecificGraphs(params int[] graphIndices)
	{
		if (!((UnityEngine.Object)(object)astarPath == null))
		{
			StartCoroutine(RescanSpecificGraphsAsync(graphIndices));
		}
	}

	private IEnumerator RescanSpecificGraphsAsync(int[] graphIndices)
	{
		foreach (int num in graphIndices)
		{
			if (num < 0 || num >= AstarPath.active.data.graphs.Length)
			{
				continue;
			}
			int steps = 0;
			foreach (Progress item in AstarPath.active.ScanAsync(AstarPath.active.data.graphs[num]))
			{
				_ = item;
				steps++;
				if (steps % 10 == 0)
				{
					yield return null;
				}
			}
		}
	}

	public void RegenerateDungeonAndRescan()
	{
		if (!(runtimeDungeon == null))
		{
			isDungeonGenerated = false;
			runtimeDungeon.Generate();
		}
	}

	public bool IsDungeonGenerated()
	{
		return isDungeonGenerated;
	}

	public bool IsAStarScanComplete()
	{
		return isAStarScanComplete;
	}

	public bool IsFullyReady()
	{
		if (isDungeonGenerated)
		{
			return isAStarScanComplete;
		}
		return false;
	}

	public Dungeon GetCurrentDungeon()
	{
		if (runtimeDungeon != null && isDungeonGenerated)
		{
			return runtimeDungeon.Generator.CurrentDungeon;
		}
		return null;
	}

	public IEnumerator WaitForDungeonGeneration(Action onComplete = null)
	{
		while (!isDungeonGenerated)
		{
			yield return null;
		}
		onComplete?.Invoke();
	}
}
