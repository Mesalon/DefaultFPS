using System;
using Fusion;
using UnityEngine;

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

		int tick = Runner.Tick;
		for (int i = 0; i < projectiles.Length; i++) {
			Projectile p = projectiles[i];
			if (p.isActive) {
				p.UpdateProjectile(Runner, out bool destroyProjectile, tick);
				if (destroyProjectile) {
					print("kaboom");
					p.isActive = false;
				}
				projectiles.Set(i, p);
			}
		}
	}


	public override void Render() {
		float renderTime = Object.IsProxy == true ? Runner.InterpolationRenderTime : Runner.SimulationRenderTime;
		float floatTick = renderTime / Runner.DeltaTime;
		for (int i = 0; i < projectiles.Length-1; i++) {
			Projectile p = projectiles[i];
			if (p.isActive) { p.DrawProjectile(GetMovePosition(ref p, floatTick)); }
		}
	}

	public void CreateProjectile(Projectile projectile) {
		projectiles.Set(ProjectileIndex % projectiles.Length, projectile);
		ProjectileIndex++;
	}

	public Vector3 GetMovePosition(ref Projectile data, float currentTick) {
		float time = (currentTick - data.fireTick) * Runner.DeltaTime;
		if (time <= 0f) { return data.firePosition; }
		return data.firePosition + data.velocity * time + Physics.gravity * time * time;
	}
}