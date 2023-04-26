using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine.UIElements;

public class Projectile {
    public NetworkRunner runner;
    public PlayerRef owner;
    public GameObject tracer;
    public readonly ProjectileData data;

    private Vector3 position;
    private Vector3 lastPosition;
    private Vector3 velocity;

    private float deathTimer;

    /// <param name="data">Settings for the projectile</param>
    /// <param name="position">Position to spawn the projectile at</param>
    /// <param name="direction">The direction the projectile starts in</param>
    /// <param name="owner">The player who is responsible for the projectile</param>
    public Projectile(NetworkRunner runner, ProjectileData data, Vector3 position, Vector3 direction) {
        this.runner = runner;
        this.data = data;
        this.position = position;
        velocity = direction * data.speed;
    }

    // todo: Implement fragmentation. Final projectile impacts should choose between ricochet, penetration, and fragmentation.
    // todo: Implement drag and terminal velocity.
    // todo: KYS
    /// <param name="destroyProjectile">Whether the projectile should be destroyed this frame</param>
    public void UpdateProjectile(out bool destroyProjectile) {


        destroyProjectile = false;

        if(deathTimer > 10) {
            destroyProjectile = true;
        }

        deathTimer += 0.02f;

        lastPosition = position;

        // Apply forces
        Vector3 forces = Physics.gravity;
        velocity += forces * runner.DeltaTime;
        position += velocity * runner.DeltaTime;

        // Tracers
        tracer.transform.rotation = Quaternion.LookRotation(velocity);
        if (data.showDebugTracers) {
            float time = data.debugTracerTime == 0 ? Time.deltaTime : data.debugTracerTime;
            Color col = Physics.Linecast(lastPosition, position, out _) ? Color.red : data.debugTracerColor;
            Debug.DrawLine(lastPosition, position, col, time);
        }
        
        
        if (runner.LagCompensation.Raycast(lastPosition, position - lastPosition, Vector3.Distance(position, lastPosition), owner, out LagCompensatedHit hit, options: HitOptions.IncludePhysX)) {
            Debug.Log($"Hit {(hit.GameObject != null ? hit.GameObject : "Nothing")}");
            if (hit.Hitbox && hit.Hitbox.TryGetComponent(out Player player)) {
                player.Health -= data.damage;
            }
            
            /*bool canRicochet = data.canRicochet &&
                velocity.magnitude > data.ricochetThreshold // Projectile speed is high enough to ricochet
                && Vector3.Angle(velocity, hit.normal) % 90 / 90 > data.maxRicochetAngle; // Projectile angle is suitable for ricochet
            Debug.Log($"Hit ange: {Vector3.Angle(velocity, hit.normal)}");

            // Ricochet debug
            if(!(velocity.magnitude >= data.ricochetThreshold)) { Debug.Log("Could not ricochet because velocity is not high enough"); }
            else if(!((Random.Range(0, 1000) * 0.001) > (Vector3.Angle(velocity, hit.normal) % 90 / 90))) { Debug.Log("Could not ricochet because angle is unsuitable for ricochet"); }

            else if (canRicochet) {
                velocity = data.ricochetDamping * Vector3.Reflect(velocity, hit.normal);
                position = hit.point;
            }*/

            else { // If it hit anything else
                destroyProjectile = true;
            }
        }

        tracer.transform.position = position;
        tracer.transform.rotation.SetLookRotation((position - lastPosition).normalized);
    }

    private bool CanPenetrate(RaycastHit hit, out Vector3 exitPoint) {
        exitPoint = Vector3.zero;
        return false;
    }
}
