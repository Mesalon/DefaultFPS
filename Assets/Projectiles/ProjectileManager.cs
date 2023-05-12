using System.Collections.Generic;
using UnityEngine.VFX;
using UnityEngine;
using Fusion;
using System.Linq;


public class ProjectileManager : NetworkBehaviour {
	public static ProjectileManager inst { get; set; }
	public ProjectileData[] projectileLibrary;
	[Networked, Capacity(256)] private NetworkArray<Projectile> projectiles { get; }
	[Networked] private int ProjectileIndex { get; set; }

	Queue<GameObject> impactPool = new();
	public GameObject ImpactEffect;
	public Vector3 impactOffset;

	private void Awake() {
		inst = this;
		projectileLibrary = Resources.LoadAll("ProjectileData").OfType<ProjectileData>().ToArray();
	}

	public override void FixedUpdateNetwork() {
		for (int i = 0; i < projectiles.Length; i++) {
			Projectile p = projectiles[i];
			if (p.isActive) {
				p.UpdateProjectile(out bool destroyProjectile);
				if (destroyProjectile) {
					p.isActive = false;
					Instantiate(ImpactEffect, p.hitPosition, Quaternion.identity).GetComponent<VisualEffect>().Play();
				}
				projectiles.Set(i, p);
			}
		}
	}


	public override void Render() {
		for (int i = 0; i < projectiles.Length - 1; i++) {
			Projectile p = projectiles[i];
			if (p.isActive) { p.DrawProjectile(); }

		}
	}

	public void CreateProjectile(Projectile projectile) {
		projectiles.Set(ProjectileIndex % projectiles.Length, projectile);
		ProjectileIndex++;
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