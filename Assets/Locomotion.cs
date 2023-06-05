using Fusion;
using UnityEngine;

[OrderBefore(typeof(Character))]
public class Locomotion : NetworkTransform {
    [HideInInspector, Networked] public float CurrentMoveSpeed { get; set; }
    [Networked] Vector2 Look { get; set; }
    [Networked] Vector3 Velocity { get; set; }
    [Networked] NetworkInputData LastInput { get; set; }
    [Networked] bool IsGrounded { get; set; }

    [HideInInspector] public float sensitivity;
    [HideInInspector] public float currentSensitivity;
    [HideInInspector] public Vector2 localLook;
    [SerializeField] Character character;
    [SerializeField] CharacterController cc;
    [SerializeField] Transform abdomen, chest, head;
    [SerializeField] Transform groundCheck;
    [SerializeField] LayerMask groundLayer;
    [SerializeField] float jumpForce;
    [SerializeField] float gravity;
    private Controls controls;
    private Quaternion startAbdomenRot, startChestRot, startHeadRot;
    private Handling handling;

    protected void Awake() {
        handling = GetComponent<Handling>();
        startAbdomenRot = abdomen.localRotation;
        startChestRot = chest.localRotation;
        startHeadRot = head.localRotation;
        controls = new();
    }
    private new void OnEnable() { controls.Enable(); }
    private void OnDisable() { controls.Disable(); }
    
    public override void FixedUpdateNetwork() {
        if (GetInput(out NetworkInputData input)) {
            IsGrounded = Physics.CheckSphere(groundCheck.position, 0.15f, groundLayer);
            Look = new(Look.x + input.lookDelta.x, Mathf.Clamp(Look.y + input.lookDelta.y, -90, 90));

            Vector3 previousPos = transform.position;
            Vector3 moveVelocity = transform.rotation * new Vector3(input.movement.x, 0, input.movement.y) * CurrentMoveSpeed;
            moveVelocity.y = Velocity.y + gravity * Runner.DeltaTime;
            if (IsGrounded) {
                if (moveVelocity.y < 0) moveVelocity.y = 0f;
                if (input.buttons.WasPressed(LastInput.buttons, Buttons.Jump)) moveVelocity.y += jumpForce;
            }
            cc.Move(moveVelocity * Runner.DeltaTime);
            Velocity = (transform.position - previousPos) * Runner.Simulation.Config.TickRate;
            
            LastInput = input;
        }
        abdomen.localRotation = startAbdomenRot;
        chest.localRotation = startChestRot;
        head.localRotation = startHeadRot;
        abdomen.RotateAround(abdomen.position, transform.right, -Look.y * 0.4f);
        chest.RotateAround(chest.position, transform.right, -Look.y * 0.4f);
        head.RotateAround(head.position, transform.right, -Look.y * 0.2f);
        transform.rotation = Quaternion.Euler(0, Look.x, 0);
    }

    public override void Render() {
        // Look
        localLook += controls.Player.Look.ReadValue<Vector2>() * currentSensitivity;
        float finalLook = -Mathf.Clamp(Look.y + localLook.y, -90, 90);
        abdomen.localRotation = startAbdomenRot;
        chest.localRotation = startChestRot;
        head.localRotation = startHeadRot;
        abdomen.RotateAround(abdomen.position, transform.right, finalLook * 0.4f);
        chest.RotateAround(chest.position, transform.right, finalLook * 0.4f);
        head.RotateAround(head.position, transform.right, finalLook * 0.2f);
        transform.rotation = Quaternion.Euler(0, Look.x + localLook.x, 0);
    }
    
    protected override void CopyFromBufferToEngine() {
        // Prevents Unity from doing funky shit when applying values. Required for CC function.
        cc.enabled = false;
        base.CopyFromBufferToEngine();
        cc.enabled = true;
    }
}
