using Fusion;
using UnityEngine;

public class ShitPissController : NetworkBehaviour {
    private NetworkCharacterControllerPrototype nccp;
    private Controls controls;
    
    private void Awake() {
        controls = new();
        nccp = GetComponent<NetworkCharacterControllerPrototype>();
    }
    private void OnEnable() { controls.Enable(); }
    private void OnDisable() { controls.Disable(); }

    public override void Spawned() {
        if (HasInputAuthority) {
            Runner.GetComponent<NetworkEvents>().OnInput.AddListener(OnInput);
        }
    }

    public override void FixedUpdateNetwork() {
        if (GetInput(out NetworkInputData input)) {
            nccp.Move(new(input.movement.x, 0, input.movement.y));
            if (input.buttons.IsSet(Buttons.Jump)) nccp.Jump();
        }
    }
    
    private void OnInput(NetworkRunner runner, NetworkInput input) {
        NetworkInputData data = new();
        data.buttons.Set(Buttons.Jump, controls.Player.Jump.ReadValue<float>() == 1);
        data.movement = controls.Player.Move.ReadValue<Vector2>();
        input.Set(data);
    }
}
