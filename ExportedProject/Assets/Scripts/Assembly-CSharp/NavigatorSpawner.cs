using System.Reflection;
using PixelCrushers.GridController;
using UnityEngine;

public class NavigatorSpawner : MonoBehaviour
{
	[Header("Navigator Settings")]
	[Tooltip("Prefab of the navigator to spawn")]
	[SerializeField]
	private GameObject navigatorPrefab;

	[Tooltip("Key to press to spawn the navigator")]
	[SerializeField]
	private KeyCode spawnKey = KeyCode.G;

	[Header("Spawn Settings")]
	[Tooltip("Offset from the spawn point")]
	[SerializeField]
	private Vector3 spawnOffset = Vector3.forward * 2f;

	[Tooltip("Destroy previous navigator when spawning a new one")]
	[SerializeField]
	private bool destroyPreviousNavigator = true;

	[Tooltip("Spawn effect (optional)")]
	[SerializeField]
	private GameObject spawnEffect;

	[Tooltip("Spawn sound (optional)")]
	[SerializeField]
	private AudioClip spawnSound;

	[Tooltip("Cooldown duration in seconds between navigator spawns")]
	[SerializeField]
	private float cooldownDuration = 5f;

	[Tooltip("Error sound when trying to spawn during cooldown")]
	[SerializeField]
	private AudioClip errorSound;

	[Header("Auto-Find Settings")]
	[Tooltip("Automatically find the DualWieldingSystem component")]
	[SerializeField]
	private bool autoFindDualWieldingSystem = true;

	[Header("References")]
	[Tooltip("Reference to the DualWieldingSystem (auto-found if not set)")]
	[SerializeField]
	private DualWieldingSystem dualWieldingSystem;

	private Transform bothHandsLaunchPoint;

	private GameObject currentNavigator;

	private AudioSource audioSource;

	private float lastSpawnTime = float.NegativeInfinity;

	private void Awake()
	{
		if (autoFindDualWieldingSystem && dualWieldingSystem == null)
		{
			dualWieldingSystem = GetComponent<DualWieldingSystem>();
		}
		audioSource = GetComponent<AudioSource>();
		if (audioSource == null && (spawnSound != null || errorSound != null))
		{
			audioSource = base.gameObject.AddComponent<AudioSource>();
		}
	}

	private void Start()
	{
		if (dualWieldingSystem != null)
		{
			bothHandsLaunchPoint = GetBothHandsLaunchPoint();
		}
	}

	private void Update()
	{
		if (InputManager.SpawnNavigator != null && InputManager.SpawnNavigator.WasPressedThisFrame())
		{
			if (Time.time >= lastSpawnTime + cooldownDuration)
			{
				SpawnNavigator();
			}
			else if (errorSound != null && audioSource != null)
			{
				audioSource.PlayOneShot(errorSound);
			}
		}
	}

	private void SpawnNavigator()
	{
		if (!(navigatorPrefab == null))
		{
			_ = bothHandsLaunchPoint == null;
			if (destroyPreviousNavigator && currentNavigator != null)
			{
				Object.Destroy(currentNavigator);
			}
			Vector3 position;
			Quaternion rotation;
			if (bothHandsLaunchPoint != null)
			{
				position = bothHandsLaunchPoint.position + bothHandsLaunchPoint.TransformDirection(spawnOffset);
				rotation = bothHandsLaunchPoint.rotation;
			}
			else
			{
				position = base.transform.position + base.transform.TransformDirection(spawnOffset);
				rotation = base.transform.rotation;
			}
			currentNavigator = Object.Instantiate(navigatorPrefab, position, rotation);
			if (spawnEffect != null)
			{
				Object.Destroy(Object.Instantiate(spawnEffect, position, rotation), 2f);
			}
			if (spawnSound != null && audioSource != null)
			{
				audioSource.PlayOneShot(spawnSound);
			}
			lastSpawnTime = Time.time;
		}
	}

	private Transform GetBothHandsLaunchPoint()
	{
		if (dualWieldingSystem == null)
		{
			return null;
		}
		FieldInfo field = typeof(DualWieldingSystem).GetField("bothHandsLaunchPoint", BindingFlags.Instance | BindingFlags.NonPublic);
		if (field != null)
		{
			return field.GetValue(dualWieldingSystem) as Transform;
		}
		return null;
	}

	public void SpawnNavigatorPublic()
	{
		SpawnNavigator();
	}

	public void SpawnNavigatorAt(Vector3 position, Quaternion rotation)
	{
		if (!(navigatorPrefab == null))
		{
			if (destroyPreviousNavigator && currentNavigator != null)
			{
				Object.Destroy(currentNavigator);
			}
			currentNavigator = Object.Instantiate(navigatorPrefab, position, rotation);
		}
	}
}
