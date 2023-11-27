using System;
using Fusion;
using Fusion.KCC;
using UnityEngine;

public enum CharacterPose { Walking, Crouching, Sprinting, Sliding }
[OrderBefore(typeof(KCC), typeof(Character), typeof(Handling), typeof(Firearm))]
public class Locomotion : NetworkKCCProcessor {
    [Networked] NetworkInputData LastInput { get; set; }
    [Networked] public CharacterPose Pose { get; set; }
    
    [HideInInspector] public KCC kcc;
    [HideInInspector] public float sensitivity;
    [HideInInspector] public float currentSensitivity;
    [HideInInspector] public Vector2 localLook;
    [SerializeField] GroundKCCProcessor groundProcessor;
    [SerializeField] SlideKCCP slideProcessor;
    [SerializeField] Transform abdomen, chest, head;
    [SerializeField] float jumpForce;
    [SerializeField] float sprintMoveSpeed;
    [SerializeField] float crouchMoveSpeed;
    private Controls controls;
    private Handling handling;
    private Quaternion startAbdomenRot, startChestRot, startHeadRot;
    private Vector3 v;
    
    [Header("Animations")]
    [SerializeField] LegLayer legLayer;

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
            void Jump() => kcc.Jump(Vector3.up * jumpForce);
            bool Pressed(Buttons button) => input.buttons.WasPressed(LastInput.buttons, button);
            
            float speed = handling.weaponWalkSpeed;
            switch (Pose) {
                case CharacterPose.Walking:
                    if (Pressed(Buttons.Sprint)) { Pose = CharacterPose.Sprinting; }
                    if (Pressed(Buttons.Crouch)) { Pose = CharacterPose.Crouching; }
                    if (Pressed(Buttons.Jump)) { Jump(); }
                    break;
                case CharacterPose.Crouching:
                    if (Pressed(Buttons.Sprint)) { Pose = CharacterPose.Sprinting; }
                    if (Pressed(Buttons.Crouch) || Pressed(Buttons.Jump)) { Pose = CharacterPose.Walking; }
                    break;
                case CharacterPose.Sprinting:
                    if (input.buttons.WasReleased(LastInput.buttons, Buttons.Sprint)) { Pose = CharacterPose.Walking; }
                    if (Pressed(Buttons.Jump)) { Jump(); }
                    if (Pressed(Buttons.Crouch)) {
                        Pose = CharacterPose.Sliding;
                        kcc.AddModifier(slideProcessor);
                    }
                    break;
                case CharacterPose.Sliding:
                    if(Pressed(Buttons.Crouch) || Pressed(Buttons.Sprint) || Pressed(Buttons.Jump)) { kcc.RemoveModifier(slideProcessor); }
                    if (Pressed(Buttons.Crouch)) { Pose = CharacterPose.Crouching; }
                    if (Pressed(Buttons.Sprint)) { Pose = CharacterPose.Sprinting; }
                    if (Pressed(Buttons.Jump)) { Pose = CharacterPose.Walking; }
                    break;
            }

            if (Pose == CharacterPose.Sprinting) { speed *= sprintMoveSpeed; }
            else if (Pose == CharacterPose.Crouching) {
                speed *= crouchMoveSpeed; 
                kcc.SetHeight(1.3f);
            }
            else { kcc.SetHeight(1.8f); }
            
            groundProcessor.KinematicSpeed = speed;
            kcc.SetInputDirection(kcc.Data.TransformRotation * new Vector3(input.movement.x, 0, input.movement.y));
            
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
    }

    private void UpdateLook(Vector2 lookDelta) {
        Vector2 look = kcc.FixedData.GetLookRotation(true, true);
        kcc.SetLookRotation(look + lookDelta);
        float pitch = kcc.RenderData.GetLookRotation(true, false).x;
        
        abdomen.localRotation = startAbdomenRot;
        chest.localRotation = startChestRot;
        head.localRotation = startHeadRot;
        abdomen.RotateAround(abdomen.position, transform.right, pitch * 0.3f);
        chest.RotateAround(chest.position, transform.right, pitch * 0.3f);
        head.RotateAround(head.position, transform.right, pitch * 0.4f);
    }
}