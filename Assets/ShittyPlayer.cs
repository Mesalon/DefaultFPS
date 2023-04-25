using System;
using Fusion;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[OrderBefore(typeof(NetworkTransform))]
[DisallowMultipleComponent]
// ReSharper disable once CheckNamespace
public class ShittyPlayer : NetworkTransform {
    [Networked] [HideInInspector] public Vector3 Velocity { get; set; }

    protected override Vector3 DefaultTeleportInterpolationVelocity => Velocity;
    private CharacterController cc;
    private Controls controls;
    
    protected override void Awake() {
        controls = new();
        base.Awake();
        cc = GetComponent<CharacterController>();
    }
    private void OnEnable() { controls.Enable(); }
    private void OnDisable() { controls.Disable(); }

    protected override void CopyFromBufferToEngine() {  
        base.CopyFromBufferToEngine();
        cc.enabled = true;
    }

    public override void Spawned() {
        Runner.GetComponent<NetworkEvents>().OnInput.AddListener(OnInput);
    }

    public virtual void Move(Vector3 direction) {
        var deltaTime = Runner.DeltaTime;
        var previousPos = transform.position;
        var moveVelocity = Velocity;

        direction = direction.normalized;

        var horizontalVel = default(Vector3);
        horizontalVel.x = moveVelocity.x;
        horizontalVel.z = moveVelocity.z;

        if (direction == default) { horizontalVel = Vector3.Lerp(horizontalVel, default, 10 * deltaTime); }
        else { horizontalVel = Vector3.ClampMagnitude(horizontalVel + direction * 10 * deltaTime, 5); }

        moveVelocity.x = horizontalVel.x;
        moveVelocity.z = horizontalVel.z;

        cc.Move(moveVelocity * deltaTime);

        Velocity = (transform.position - previousPos) * Runner.Simulation.Config.TickRate;
    }

    public override void FixedUpdateNetwork() {
        if (GetInput(out NetworkInputData input)) {
            Move(new(input.movement.x, 0, input.movement.y));
            print(Velocity);
        }
    }
    
    private void OnInput(NetworkRunner runner, NetworkInput input) {
        NetworkInputData data = new() {
            movement = controls.Player.Move.ReadValue<Vector2>(),
        };
        input.Set(data);
    }
}