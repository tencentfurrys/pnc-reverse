using UnityEngine;

namespace PixelCrushers.GridController;

[CreateAssetMenu(fileName = "New Projectile", menuName = "Combat/Projectile Data")]
public class ProjectileData : ScriptableObject
{
	[Header("Basic Info")]
	public string projectileName = "Projectile";

	public GameObject projectilePrefab;

	[Header("Movement")]
	public float speed = 10f;

	public float maxLifetime = 5f;

	public float maxRange = 50f;

	public bool useGravity;

	public float gravityMultiplier = 1f;

	[Header("Homing (Optional)")]
	public bool isHoming;

	public float homingStrength = 5f;

	public float homingRange = 15f;

	public LayerMask homingTargetMask = 256;

	[Header("Impact")]
	public int damage = 10;

	public float impactRadius;

	public LayerMask impactLayerMask = 786688;

	public bool piercing;

	public int maxPierceTargets = 3;

	[Header("Effects")]
	public GameObject impactEffect;

	public GameObject trailEffect;

	public AudioClip launchSound;

	public AudioClip impactSound;

	[Header("Visual")]
	public bool faceMovementDirection = true;

	public Vector3 rotationOffset = Vector3.zero;

	[Header("Special Properties")]
	public bool bounces;

	public int maxBounces = 3;

	public float bounceSpeedMultiplier = 0.8f;

	public bool explodeOnImpact;

	public float explosionRadius = 3f;

	public int explosionDamage = 20;

	[Header("Scale Growth")]
	public bool scalesOverTime;

	public Vector3 startScale = Vector3.one;

	public Vector3 endScale = Vector3.one * 3f;

	public AnimationCurve scaleCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
}
