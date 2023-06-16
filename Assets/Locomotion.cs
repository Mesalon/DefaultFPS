using Fusion;
using UnityEngine;

[OrderBefore(typeof(Character))]
public class Locomotion : NetworkTransform {
    [HideInInspector, Networked] public float CurrentMoveSpeed { get; set; }
    [Networked] Vector2 Look { get; set; }
    [Networked] float VelocityY { get; set; }
    [Networked] private bool IsGrounded { get; set; }
    [Networked] NetworkInputData LastInput { get; set; }

    [HideInInspector] public CharacterController cc;
    [HideInInspector] public float sensitivity;
    [HideInInspector] public float currentSensitivity;
    [HideInInspector] public Vector2 localLook;
    [SerializeField] Transform abdomen, chest, head;
    [SerializeField] Transform groundCheck;
    [SerializeField] LayerMask groundLayer;
    [SerializeField] float jumpForce;
    [SerializeField] float gravity;
    private Controls controls;
    private Quaternion startAbdomenRot, startChestRot, startHeadRot;

    protected void Awake() {
        cc = GetComponent<CharacterController>();
        startAbdomenRot = abdomen.localRotation;
        startChestRot = chest.localRotation;
        startHeadRot = head.localRotation;
        controls = new();
    }
    private void OnEnable() { controls.Enable(); }
    private void OnDisable() { controls.Disable(); }
    
    public override void FixedUpdateNetwork() {
        Vector3 moveVelocity = Vector3.zero;
        if (GetInput(out NetworkInputData input)) {
            Look = new(Look.x + input.lookDelta.x, Mathf.Clamp(Look.y + input.lookDelta.y, -90, 90));
            IsGrounded = Physics.CheckSphere(groundCheck.position, 0.15f, groundLayer);
            
            moveVelocity = transform.rotation * new Vector3(input.movement.x, 0, input.movement.y) * CurrentMoveSpeed;
            VelocityY += gravity * Runner.DeltaTime;
            if (IsGrounded) {
                if (VelocityY < 0) VelocityY = 0;
                if (input.buttons.WasPressed(LastInput.buttons, Buttons.Jump)) VelocityY += jumpForce;
            }
            cc.Move(new Vector3(moveVelocity.x, VelocityY, moveVelocity.z) * Runner.DeltaTime);

            //print($"{name} - Tick: {Runner.Tick}, vel: {VelocityY}, ground: {cc.isGrounded}");

            LastInput = input;
        }
        
        abdomen.localRotation = startAbdomenRot;
        chest.localRotation = startChestRot;
        head.localRotation = startHeadRot;
        abdomen.RotateAround(abdomen.position, transform.right, -Look.y * 0.3f);
        chest.RotateAround(chest.position, transform.right, -Look.y * 0.3f);
        head.RotateAround(head.position, transform.right, -Look.y * 0.4f);
        transform.rotation = Quaternion.Euler(0, Look.x, 0);
    }

    public override void Render() {
        // Look
        if (HasInputAuthority) {
            localLook += controls.Player.Look.ReadValue<Vector2>() * currentSensitivity;
            float finalLook = -Mathf.Clamp(Look.y + localLook.y, -90, 90);
            abdomen.localRotation = startAbdomenRot;
            chest.localRotation = startChestRot;
            head.localRotation = startHeadRot;
            abdomen.RotateAround(abdomen.position, transform.right, finalLook * 0.3f);
            chest.RotateAround(chest.position, transform.right, finalLook * 0.3f);
            head.RotateAround(head.position, transform.right, finalLook * 0.4f);
            transform.rotation = Quaternion.Euler(0, Look.x + localLook.x, 0);
        }
    }
    
    protected override void CopyFromBufferToEngine() {
        // Prevents Unity from doing funky shit when applying values. Required for CC function.
        cc.enabled = false;
        base.CopyFromBufferToEngine();
        cc.enabled = true;
    }
}
