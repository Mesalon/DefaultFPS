using UnityEngine;
using Fusion;
using Debug = UnityEngine.Debug;

public struct Projectile : INetworkStruct {
    public bool isActive;
    private int dataIndex;
    private PlayerRef owner;
    private Vector3 position;
    private Vector3 lastPosition;
    private Vector3 velocity;
    // todo: Projectile death timer

    public Projectile(int dataIndex, PlayerRef owner, Vector3 position, Vector3 direction) {
        isActive = true;
        this.dataIndex = dataIndex;
        this.position = position;
        this.owner = owner;
        lastPosition = position;
        velocity = direction * ProjectileManager.inst.projectileLibrary[dataIndex].speed;
    }

    // todo: Implement fragmentation. Final projectile impacts should choose between ricochet, penetration, and fragmentation.
    // todo: Implement drag and terminal velocity.
    /// <param name="destroyProjectile">Whether the projectile should be destroyed this tick</param>
    public void UpdateProjectile(NetworkRunner runner, out bool destroyProjectile) {
        ProjectileData data = ProjectileManager.inst.projectileLibrary[dataIndex];
        destroyProjectile = false;
        lastPosition = position;
        // Apply forces
        Vector3 forces = Physics.gravity;
        velocity += forces * runner.DeltaTime;
        position += velocity * runner.DeltaTime;
        
        // Tracers
        if (data.showDebugTracers) {
            float time = data.debugTracerTime == 0 ? Time.deltaTime : data.debugTracerTime;
            Debug.DrawRay(lastPosition, position - lastPosition, data.debugTracerColor, time);
        }

        if (runner.LagCompensation.Raycast(lastPosition, position - lastPosition, Vector3.Distance(position, lastPosition), owner, out LagCompensatedHit hit, options: HitOptions.IncludePhysX)) {
            Debug.Log($"Hit {(hit.GameObject != null ? hit.GameObject.name : "Nothing")}");
            if (hit.Hitbox && hit.Hitbox.TryGetComponent(out Player player)) {
                player.Health -= data.damage;
                destroyProjectile = true;
            }
            
            /*bool canRicochet = ProjectileManager.projectileLibrary[dataIndex].canRicochet &&
                velocity.magnitude > ProjectileManager.projectileLibrary[dataIndex].ricochetThreshold // Projectile speed is high enough to ricochet
                && Vector3.Angle(velocity, hit.normal) % 90 / 90 > ProjectileManager.projectileLibrary[dataIndex].maxRicochetAngle; // Projectile angle is suitable for ricochet
            Debug.Log($"Hit ange: {Vector3.Angle(velocity, hit.normal)}");

            // Ricochet debug
            if(!(velocity.magnitude >= ProjectileManager.projectileLibrary[dataIndex].ricochetThreshold)) { Debug.Log("Could not ricochet because velocity is not high enough"); }
            else if(!((Random.Range(0, 1000) * 0.001) > (Vector3.Angle(velocity, hit.normal) % 90 / 90))) { Debug.Log("Could not ricochet because angle is unsuitable for ricochet"); }

            else if (canRicochet) {
                velocity = ProjectileManager.projectileLibrary[dataIndex].ricochetDamping * Vector3.Reflect(velocity, hit.normal);
                position = hit.point;
            }*/

            else { // If it hit anything else
                destroyProjectile = true;
                Debug.Log($"Destroying projectile");
            }
        }
    }

    public void DrawProjectile() {
        ProjectileData data = ProjectileManager.inst.projectileLibrary[dataIndex];
        Graphics.DrawMesh(data.tracerMesh, Matrix4x4.TRS(position, Quaternion./*LookRotation(position - lastPosition)*/identity, Vector3.one * 5), data.tracerMat, 0);
    }
    
    private bool CanPenetrate(RaycastHit hit, out Vector3 exitPoint) {
        exitPoint = Vector3.zero;
        return false;
    }
}
