using Fusion;
using UnityEngine;

[OrderBefore(typeof(Character))]
public class Locomotion : NetworkBehaviour {
    [HideInInspector, Networked] public float CurrentMoveSpeed { get; set; }
    [Networked] Vector2 Look { get; set; }
    [Networked] float VelocityY { get; set; }
    [Networked] private bool IsGrounded { get; set; }
    [Networked] NetworkInputData LastInput { get; set; }

    [HideInInspector] public float sensitivity;
    [HideInInspector] public float currentSensitivity;
    [HideInInspector] public Vector2 localLook;
    [SerializeField] Transform abdomen, chest, head;
    [SerializeField] Transform groundCheck;
    [SerializeField] LayerMask groundLayer;
    [SerializeField] float jumpForce;
    [SerializeField] float gravity;
    private Character character;
    private CharacterController cc;
    private Controls controls;
    private Quaternion startAbdomenRot, startChestRot, startHeadRot;
    private Handling handling;

    protected void Awake() {
        character = GetComponent<Character>();
        cc = GetComponent<CharacterController>();
        handling = GetComponent<Handling>();
        startAbdomenRot = abdomen.localRotation;
        startChestRot = chest.localRotation;
        startHeadRot = head.localRotation;
        controls = new();
    }
    private void OnEnable() { controls.Enable(); }
    private void OnDisable() { controls.Disable(); }
    
    public override void FixedUpdateNetwork() {
        if (GetInput(out NetworkInputData input)) {
            Look = new(Look.x + input.lookDelta.x, Mathf.Clamp(Look.y + input.lookDelta.y, -90, 90));
            IsGrounded = Physics.CheckSphere(groundCheck.position, 0.15f, groundLayer);
            
            Vector3 moveVelocity = transform.rotation * new Vector3(input.movement.x, 0, input.movement.y) * CurrentMoveSpeed;
            VelocityY += gravity * Runner.DeltaTime;
            if (cc.isGrounded) {
                if (VelocityY < 0) VelocityY = 0;
                if (input.buttons.WasPressed(LastInput.buttons, Buttons.Jump)) VelocityY += jumpForce;
            }
            print($"{name} - Tick: {Runner.Tick}, vel: {VelocityY}, ground: {cc.isGrounded}");
            cc.Move(new Vector3(moveVelocity.x, VelocityY, moveVelocity.z) * Runner.DeltaTime);
            
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
        if (HasInputAuthority) {
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
    }
    
    /*protected override void CopyFromBufferToEngine() {
        // Prevents Unity from doing funky shit when applying values. Required for CC function.
        cc.enabled = false;
        base.CopyFromBufferToEngine();
        cc.enabled = true;
    }*/
}
