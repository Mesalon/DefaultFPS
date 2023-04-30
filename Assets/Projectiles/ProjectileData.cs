using UnityEngine;

[CreateAssetMenu(fileName = "New Projectile", menuName = "Items/New Projectile")]
public class ProjectileData : ScriptableObject {
	public Mesh tracerMesh;
	public Material tracerMat;
	public float speed;
	public float damage;
	public bool canRicochet;
	public float maxRicochetAngle;
	public float ricochetThreshold;
	public float ricochetDamping;
	public float penetration;

	[Header("Debug Settings")]
	public bool showDebugTracers;
	public float debugTracerTime; // Time it shows up. 0 for one frame.
	public Color debugTracerColor = Color.white;
}