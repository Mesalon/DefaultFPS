using System;
using Fusion;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.VFX;


public class ProjectileManager : NetworkBehaviour {


	public static ProjectileManager inst { get; set; }
#if UNITY_EDITOR
	[HardSerialize]
#endif
	public ProjectileData[] projectileLibrary;
	[Networked, Capacity(256)] private NetworkArray<Projectile> projectiles { get; }
	[Networked] private int ProjectileIndex { get; set; }

	Queue<GameObject> impactPool = new();
	public GameObject ImpactEffect;
	public Vector3 impactOffset;

	private void Awake() { inst = this; }

	public override void FixedUpdateNetwork() {

		int tick = Runner.Tick;
		for (int i = 0; i < projectiles.Length; i++) {
			Projectile p = projectiles[i];
			if (p.isActive) {
				p.UpdateProjectile(Runner, out bool destroyProjectile, out Player player, tick);
				if (destroyProjectile) {
					if (player && Object.HasStateAuthority) {
						player.Health -= projectileLibrary[p.dataIndex].damage;
						if (player.Health <= 0) { player.Respawn(); }
					}
					p.isActive = false;
					var impact = Instantiate(ImpactEffect, p.hitPosition, Quaternion.identity);
					impact.GetComponent<VisualEffect>().Play();
				}
				projectiles.Set(i, p);
			}
		}
	}


	public override void Render() {
		for (int i = 0; i < projectiles.Length - 1; i++) {
			Projectile p = projectiles[i];
			if (p.isActive) { p.DrawProjectile(GetMovePosition(ref p, Runner.Tick)); }

		}
	}

	public void CreateProjectile(Projectile projectile) {
		projectiles.Set(ProjectileIndex % projectiles.Length, projectile);
		ProjectileIndex++;
	}

	public Vector3 GetMovePosition(ref Projectile data, float currentTick) {
		float time = (currentTick - data.fireTick) * Runner.DeltaTime;
		if (time <= 0f) { return data.firePosition; }
		return data.firePosition + data.direction * projectileLibrary[data.dataIndex].speed * time + Physics.gravity * time * time;
	}

	/*private void SetImpactActive(Projectile p) {
		
		var effect = impactPool.Dequeue();
		effect.SetActive(true);
		effect.transform.position = p.hitPosition + impactOffset;
		effect.GetComponent<VisualEffect>().Play();

	}
	private void SetImpactInactive(Projectile p) {
		var effect = impactPool.Dequeue();
		effect.SetActive(active);
		if (active) {
			effect.transform.position = p.hitPosition + impactOffset;
			effect.GetComponent<VisualEffect>().Play();
		} else {
			impactPool.Enqueue(effect);

	}

	public void PoolImpacts(int amount) {
		for (int i = 0; i < amount; i++) {
			GameObject impact = Instantiate(ImpactEffect);
			impact.SetActive(false);
			impactPool.Enqueue(impact);
		}
	}*/
}