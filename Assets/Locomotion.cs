using System;
using FMODUnity;
using Fusion;
using Fusion.KCC;
using UnityEngine;
using FMOD.Studio;

[OrderBefore(typeof(KCC), typeof(Character), typeof(Handling), typeof(Firearm))]
public class Locomotion : NetworkKCCProcessor {
    [HideInInspector, Networked] public float CurrentMoveSpeed { get; set; }
    [Networked] NetworkInputData LastInput { get; set; }
    [Networked] private Vector2 Look { get; set; }

    public KCC kcc;
    [HideInInspector] public float sensitivity;
    [HideInInspector] public float currentSensitivity;
    [HideInInspector] public Vector2 look;
    [SerializeField] EventReference footsteps;
    [SerializeField] Transform abdomen, chest, head;
    [SerializeField] float jumpForce;
    private Controls controls;
    private EventInstance footstepInst;
    private Quaternion startAbdomenRot, startChestRot, startHeadRot;
    
    protected void Awake() {
        kcc = GetComponent<KCC>();
        startAbdomenRot = abdomen.localRotation;
        startChestRot = chest.localRotation;
        startHeadRot = head.localRotation;
        controls = new();
        footstepInst = AudioManager.inst.CreateInstance(footsteps, transform);
    }

    private void OnEnable() { controls.Enable(); }
    private void OnDisable() { controls.Disable(); }

    public override void FixedUpdateNetwork() {
        if (GetInput(out NetworkInputData input)) {
            Vector3 inputDirection = transform.rotation * new Vector3(input.movement.x, 0, input.movement.y);
            kcc.Data.KinematicSpeed = CurrentMoveSpeed; 
            kcc.SetInputDirection(inputDirection);
            if (input.buttons.WasPressed(LastInput.buttons, Buttons.Jump)) { kcc.Jump(Vector3.up * jumpForce); }
            Look = input.look;
            LastInput = input;
        }

        if (!HasInputAuthority) {
            UpdateLook(look);
        }
    }

    public override void Render() {
        // Look
        if (HasInputAuthority) {
            Vector2 input = controls.Player.Look.ReadValue<Vector2>();
            look = new Vector2(Mathf.Clamp(look.x - input.y * currentSensitivity, -90, 90), look.y + input.x * currentSensitivity);
            UpdateLook(look);
        }
    }

    private void UpdateLook(Vector2 input) {
        look = input;
        abdomen.localRotation = startAbdomenRot;
        chest.localRotation = startChestRot;
        head.localRotation = startHeadRot;
        abdomen.RotateAround(abdomen.position, transform.right, look.x * 0.3f);
        chest.RotateAround(chest.position, transform.right, look.x * 0.3f);
        head.RotateAround(head.position, transform.right, look.x * 0.4f);
        transform.rotation = Quaternion.Euler(0, input.y, 0);
        kcc.SetLookRotation(0, input.y);
    }
}