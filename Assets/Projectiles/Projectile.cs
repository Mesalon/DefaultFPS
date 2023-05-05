using UnityEngine;
using Fusion;

public struct Projectile : INetworkStruct {
    private NetworkRunner Runner => ProjectileManager.inst.Runner;
    
    public bool isActive;
    private int dataIndex;
    private PlayerRef owner;
    private int fireTick; 
    private Vector3 firePosition;
    private Vector3 velocity;
    // todo: Projectile death timer

    public Projectile(int dataIndex, Vector3 position, Vector3 direction, PlayerRef owner) {
        isActive = true;
        firePosition = position;
        this.dataIndex = dataIndex;
        this.owner = owner;
        velocity = direction * ProjectileManager.inst.projectileLibrary[dataIndex].speed;
        fireTick = ProjectileManager.inst.Runner.Tick;
    }

    // todo: Implement fragmentation. Final projectile impacts should choose between ricochet, penetration, and fragmentation.
    // todo: Implement drag and terminal velocity.
    /// <param name="destroyProjectile">Whether the projectile should be destroyed this tick</param>
    public void UpdateProjectile(out bool destroyProjectile) {
        ProjectileData data = ProjectileManager.inst.projectileLibrary[dataIndex];
        destroyProjectile = false;
        var lastPosition = GetMovePosition(Runner.Tick - 1f); 
        var position = GetMovePosition(Runner.Tick); 
        
        if (data.showDebugTracers) {
            float time = data.debugTracerTime == 0 ? Runner.DeltaTime : data.debugTracerTime;
            Debug.DrawLine(lastPosition, position, data.debugTracerColor, time);
        }
        
        if (Runner.LagCompensation.Raycast(position, position - lastPosition, Vector3.Distance(lastPosition, position), owner, out LagCompensatedHit hit, options: HitOptions.IncludePhysX)) {
            if (hit.Hitbox && hit.Hitbox.TryGetComponent(out Player player)) {
                player.Health -= data.damage;
                destroyProjectile = true;
            }
            else { // If it hit anything else
                destroyProjectile = true;
            }
        }
    }

    public void DrawProjectile(bool isProxy) {
        ProjectileData data = ProjectileManager.inst.projectileLibrary[dataIndex];
        float renderTime = isProxy ? Runner.InterpolationRenderTime : Runner.SimulationRenderTime; 
        float tick = renderTime / Runner.DeltaTime; 
        Graphics.DrawMesh(data.tracerMesh, Matrix4x4.TRS(GetMovePosition(tick), Quaternion.LookRotation(velocity), Vector3.one * 5), data.tracerMat, 0);
    }

    // Will fail for complex projectile simulations. For implementation of such projectile like homing missiles see Mk.III advanced projectiles documentation.
    public Vector3 GetMovePosition(float tick) {
        float elapsedTime = (tick - fireTick) * Runner.DeltaTime;
        return elapsedTime <= 0f ? firePosition : firePosition + (velocity + Physics.gravity * elapsedTime) * elapsedTime;
    } 
}

// banished to a faraway land
/*
     private bool CanPenetrate(RaycastHit hit, out Vector3 exitPoint) {
        exitPoint = Vector3.zero;
        return false;
    }
    
 bool canRicochet = ProjectileManager.projectileLibrary[dataIndex].canRicochet &&
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