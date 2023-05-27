using UnityEngine;
using Fusion;
using Debug = UnityEngine.Debug;

public struct Projectile : INetworkStruct {
    private NetworkRunner Runner => ProjectileManager.inst.Runner;
    public bool isActive;
    public int dataIndex;
    private PlayerRef owner;
    public Vector3 firePosition { get; }
    //public Vector3 velocity;
    public Vector3 direction;

    public int fireTick;
    public int finishTick;

    public Vector3 hitPosition;

    public Projectile(int dataIndex, PlayerRef owner, Vector3 position, Vector3 direction, int fireTick, float lifespan, NetworkRunner runner) {
        isActive = true;
        this.dataIndex = dataIndex;
        firePosition = position;
        this.owner = owner;
        this.fireTick = fireTick;
        finishTick = runner.Tick + Mathf.RoundToInt(lifespan / runner.DeltaTime);
        //velocity = direction * ProjectileManager.inst.projectileLibrary[dataIndex].speed;
        this.direction = direction;
        hitPosition = Vector3.zero;
    }

    // todo: Implement fragmentation. Final projectile impacts should choose between ricochet, penetration, and fragmentation.
    // todo: Implement drag and terminal velocity.
    /// <param name="destroyProjectile">Whether the projectile should be destroyed this tick</param>
    public void UpdateProjectile(out bool destroyProjectile) {
        if (finishTick <= Runner.Tick) {
            destroyProjectile = true; 
            return;
        }
        
        destroyProjectile = false;
        // Apply forces
        var lastPosition = GetMovePosition(Runner.Tick - 1f);
        var Position = GetMovePosition(Runner.Tick);

        if (Runner.LagCompensation.Raycast(lastPosition, Position - lastPosition, Vector3.Distance(Position, lastPosition), owner, out LagCompensatedHit hit, options: HitOptions.IncludePhysX)) {
            hitPosition = hit.Point;
            if (hit.Hitbox) {
                if (hit.Hitbox.transform.root.TryGetComponent(out Character player)) {
                    player.Health -= ProjectileManager.inst.projectileLibrary[dataIndex].damage;
                    if (player.Health <= 0) { player.Kill(); }
                }
                destroyProjectile = true;
            }
            else { // If it hit anything else
                destroyProjectile = true;
            }
        }
    }

    public void DrawProjectile() {
        ProjectileData data = ProjectileManager.inst.projectileLibrary[dataIndex];
        Graphics.DrawMesh(data.tracerMesh, Matrix4x4.TRS(GetMovePosition(Runner.Tick), Quaternion.LookRotation(direction), Vector3.one * 5), data.tracerMat, 0);
        
        /*if (data.showDebugTracers) {
            float time = data.debugTracerTime == 0 ? Time.deltaTime : data.debugTracerTime;
            Debug.DrawRay(lastPosition, position - lastPosition, data.debugTracerColor, time);
        }*/
    }
    
    public Vector3 GetMovePosition(float tick) {
        float time = (tick - fireTick) * Runner.DeltaTime;
        if (time <= 0f) { return firePosition; }
        return firePosition + (direction * ProjectileManager.inst.projectileLibrary[dataIndex].speed + Physics.gravity * time) * time;
    }
}

// Banished to a faraway land

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
/*private bool CanPenetrate(RaycastHit hit, out Vector3 exitPoint) {
    exitPoint = Vector3.zero;
    return false;
}*/