using Fusion;
using Fusion.KCC;
using UnityEngine;

[OrderBefore(typeof(KCC), typeof(Character), typeof(Handling), typeof(Firearm))]
public class Locomotion : NetworkKCCProcessor {
    [HideInInspector, Networked] public float CurrentMoveSpeed { get; set; }
    [Networked] NetworkInputData LastInput { get; set; }

    public KCC kcc;
    [HideInInspector] public float sensitivity;
    [HideInInspector] public float currentSensitivity;
    [HideInInspector] public Vector2 localLook;
    public Animator anim;
    [SerializeField] Transform abdomen, chest, head;
    [SerializeField] private float ADSSpeed, sprintSpeed;
    [SerializeField] float jumpForce;
    private Controls controls;
    private Handling handling;
    private Quaternion startAbdomenRot, startChestRot, startHeadRot; 
    
    protected void Awake() {
        handling = GetComponent<Handling>();
        kcc = GetComponent<KCC>();
        startAbdomenRot = abdomen.localRotation;
        startChestRot = chest.localRotation;
        startHeadRot = head.localRotation;
        controls = new();
    }

    private void OnEnable() { controls.Enable(); }
    private void OnDisable() { controls.Disable(); }

    public override void FixedUpdateNetwork() {
        if (GetInput(out NetworkInputData input)) {
            if (input.buttons.IsSet(Buttons.Aim)) { CurrentMoveSpeed = handling.Gun.stats.walkSpeed * ADSSpeed; }
            else if (input.buttons.IsSet(Buttons.Run)) { CurrentMoveSpeed = handling.Gun.stats.walkSpeed * sprintSpeed; }
            else { CurrentMoveSpeed = handling.Gun.stats.walkSpeed; }
            kcc.FixedData.speed = CurrentMoveSpeed;
            Vector3 inputDirection = kcc.Data.TransformRotation * new Vector3(input.movement.x, 0, input.movement.y);
            kcc.SetInputDirection(inputDirection);
            if (input.buttons.WasPressed(LastInput.buttons, Buttons.Jump)) { kcc.Jump(Vector3.up * jumpForce); }
            
            LastInput = input;
        }
        
        UpdateLook(input.lookDelta);
    }

    public override void Render() {
        // Look
        if (HasInputAuthority) {
            Vector2 input = controls.Player.Look.ReadValue<Vector2>();
            localLook += new Vector2(-input.y, input.x) * currentSensitivity;
            UpdateLook(localLook);
        }
        
        anim.SetFloat("MoveX", LastInput.movement.x, 0.1f, Time.deltaTime);
        anim.SetFloat("MoveZ", LastInput.movement.y, 0.1f, Time.deltaTime);
    }

    private void UpdateLook(Vector2 lookDelta) {
        Vector2 look = kcc.FixedData.GetLookRotation(true, true);
        kcc.SetLookRotation(look + lookDelta);
        float pitch = kcc.RenderData.GetLookRotation(true, true).x;
        
        abdomen.localRotation = startAbdomenRot;
        chest.localRotation = startChestRot;
        head.localRotation = startHeadRot;
        abdomen.RotateAround(abdomen.position, transform.right, pitch * 0.3f);
        chest.RotateAround(chest.position, transform.right, pitch * 0.3f);
        head.RotateAround(head.position, transform.right, pitch * 0.4f);
    }
}