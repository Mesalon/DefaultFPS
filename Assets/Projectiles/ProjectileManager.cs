using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class ProjectileManager : NetworkBehaviour {
	[SerializeField] private ProjectileData[] projectiles;
	public static ProjectileData[] serializedProjectiles;
	private static List<Projectile> activeProjectiles = new();
	private static Queue<GameObject> tracers = new();

	private void Awake() {
		serializedProjectiles = projectiles;
	}

	public override void FixedUpdateNetwork() {
		for(int i = activeProjectiles.Count - 1; i >= 0; i--) { // Count backwards as to not make the gods angry
			activeProjectiles[i].UpdateProjectile(out bool destroyProjectile);
			if (destroyProjectile) {
				GameObject go = activeProjectiles[i].tracer;
				go.SetActive(false);
				tracers.Enqueue(go);
				activeProjectiles.RemoveAt(i);
			}
		}
	}

	public static void CreateProjectile(Projectile projectile) {
		activeProjectiles.Add(projectile);
		if (tracers.TryPeek(out GameObject _)) {
			var trac = tracers.Dequeue();
			projectile.tracer = trac;
			projectile.tracer.SetActive(true);
		}
		else {
			Debug.LogWarning($"Tracer for projectile \"{projectile.data.name}\" could not be found in pool and will be instantiated instead.");
			projectile.tracer = Instantiate(projectile.data.tracerPF);
		}
	}

	/// <summary>This method is run by the class responsible for creating the projectile (i.e. the gun, magazine in VR, etc).</summary>
	public static void PoolTracers(GameObject tracerPF, int amount) {
		for (int i = 0; i < amount; i++) {
			GameObject go = Instantiate(tracerPF);
			go.SetActive(false);
			tracers.Enqueue(go);
		}
    }

	public static void DisposeTracers() {
		
	}
}