using UnityEngine;
using Fusion;
using Debug = UnityEngine.Debug;

public struct Projectile : INetworkStruct {
    public bool isActive;
    public int dataIndex;
    private PlayerRef owner;
    public Vector3 firePosition { get; }
    //public Vector3 velocity;
    public Vector3 direction;

    public int fireTick;
    public int finishTick;

    public Vector3 hitPosition;
    // todo: Projectile death timer

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
    public void UpdateProjectile(NetworkRunner runner, out bool destroyProjectile, out Player hitPlayer, int tick) {
        hitPlayer = null;
        if (finishTick <= tick) { destroyProjectile = true; return; }
        ProjectileData data = ProjectileManager.inst.projectileLibrary[dataIndex];
        destroyProjectile = false;
        // Apply forces
        Vector3 forces = Physics.gravity;
        var lastPosition = ProjectileManager.inst.GetMovePosition(ref this, tick - 1f);
        var Position = ProjectileManager.inst.GetMovePosition(ref this, tick);

        if (runner.LagCompensation.Raycast(lastPosition, Position - lastPosition, Vector3.Distance(Position, lastPosition), owner, out LagCompensatedHit hit, options: HitOptions.IncludePhysX)) {
            if (hit.Hitbox) {
                hit.Hitbox.transform.root.TryGetComponent(out Player player);
                if (player) {
                    hitPlayer = player;
                }
                hitPosition = hit.Point;
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
                hitPosition = hit.Point;
                destroyProjectile = true;
                Debug.Log("sex");
            }
        }
    }

    public void DrawProjectile(Vector3 position) {
        ProjectileData data = ProjectileManager.inst.projectileLibrary[dataIndex];
        Graphics.DrawMesh(data.tracerMesh, Matrix4x4.TRS(position, Quaternion.LookRotation(direction), Vector3.one * 5), data.tracerMat, 0);
        
        /*if (data.showDebugTracers) {
            float time = data.debugTracerTime == 0 ? Time.deltaTime : data.debugTracerTime;
            Debug.DrawRay(lastPosition, position - lastPosition, data.debugTracerColor, time);
        }*/
    }
    
    private bool CanPenetrate(RaycastHit hit, out Vector3 exitPoint) {
        exitPoint = Vector3.zero;
        return false;
    }
}
