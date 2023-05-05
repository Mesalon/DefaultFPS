using Fusion;

public class ProjectileManager : NetworkBehaviour {
	public static ProjectileManager inst { get; set; }
	#if UNITY_EDITOR 
	[HardSerialize] 
	#endif 
	public ProjectileData[] projectileLibrary;
	[Networked, Capacity(256)] private NetworkArray<Projectile> projectiles { get; }
	// todo: ComplexProjectile implementation
	[Networked] private int fireCount { set; get; }

	private void Awake() { inst = this; }
	
	public override void FixedUpdateNetwork() {
		for (int i = 0; i < projectiles.Length; i++) {
			Projectile p = projectiles[i];
			if (p.isActive) {
				p.UpdateProjectile(out bool destroyProjectile);
				if (destroyProjectile) {
					print("kaboom");
					p.isActive = false;
				}
				projectiles.Set(i, p);
			}
		}
	}


	public override void Render() {
		foreach (Projectile p in projectiles) {
			if (p.isActive) { p.DrawProjectile(Object.IsProxy); }
		}
	}

	public void CreateProjectile(Projectile projectile) {
		projectiles.Set(fireCount % projectiles.Length, projectile);
		fireCount++;
	}
}