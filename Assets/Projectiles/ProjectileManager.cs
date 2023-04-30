using System;
using Fusion;

public class ProjectileManager : NetworkBehaviour {
	public static ProjectileManager inst { get; set; }
	#if UNITY_EDITOR 
	[HardSerialize] 
	#endif 
	public ProjectileData[] projectileLibrary;
	[Networked, Capacity(256)] private NetworkArray<Projectile> projectiles { get; }
	[Networked] private int ProjectileIndex { get; set; }

	private void Awake() { inst = this; }
	
	public override void FixedUpdateNetwork() {
		for (int i = 0; i < projectiles.Length; i++) {
			Projectile p = projectiles[i];
			if (p.isActive) {
				p.UpdateProjectile(Runner, out bool destroyProjectile);
				if (destroyProjectile) {
					p.isActive = false;
				}
				projectiles.Set(i, p);
			}
		}
	}

	public override void Render() {
		foreach (var p in projectiles) {
			if (p.isActive) { p.DrawProjectile(); }
		}
	}

	public void CreateProjectile(Projectile projectile) {
		projectiles.Set(ProjectileIndex % projectiles.Length, projectile);
		ProjectileIndex++;
	}
}