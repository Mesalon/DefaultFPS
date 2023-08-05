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
    [Networked] private Vector2 NetworkLook { get; set; }

    [HideInInspector] public KCC kcc;
    [HideInInspector] public float sensitivity;
    [HideInInspector] public float currentSensitivity;
    [HideInInspector] public Vector2 look;
    [SerializeField] Animator anim;
    [SerializeField] Transform abdomen, chest, head;
    [SerializeField] EventReference footsteps;
    [SerializeField] float jumpForce;
    [SerializeField] private float ADSSpeed, sprintSpeed;
    private Handling handling;
    private Controls controls;
    private EventInstance footstepInst;
    private Quaternion startAbdomenRot, startChestRot, startHeadRot;
    private float stepHeight;

    protected void Awake() {
        handling = GetComponent<Handling>();
        kcc = GetComponent<KCC>();
        stepHeight = kcc.Settings.StepHeight;
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
            if (input.buttons.IsSet(Buttons.Aim)) { CurrentMoveSpeed = handling.Gun.stats.walkSpeed * ADSSpeed; }
            else if (input.buttons.IsSet(Buttons.Run)) { CurrentMoveSpeed = handling.Gun.stats.walkSpeed * sprintSpeed; }
            else { CurrentMoveSpeed = handling.Gun.stats.walkSpeed; }
            
            NetworkLook = input.look;
            Vector3 inputDirection = transform.rotation * new Vector3(input.movement.x, 0, input.movement.y);
            kcc.SetInputDirection(inputDirection);
            kcc.FixedData.speed = CurrentMoveSpeed;
            if (input.buttons.WasPressed(LastInput.buttons, Buttons.Jump)) { kcc.Jump(Vector3.up * jumpForce); }
            LastInput = input;
        }

        if (!HasInputAuthority) {
            UpdateLook(NetworkLook);
        }
    }

    public override void Render() {
        // Look
        if (HasInputAuthority) {
            Vector2 input = controls.Player.Look.ReadValue<Vector2>();
            look = new Vector2(Mathf.Clamp(look.x - input.y * currentSensitivity, -90, 90), look.y + input.x * currentSensitivity);
            UpdateLook(look);
        }

        print(LastInput.movement);
        anim.SetFloat("MoveX", LastInput.movement.x, 0.1f, Time.deltaTime);
        anim.SetFloat("MoveZ", LastInput.movement.y, 0.1f, Time.deltaTime);
    }

    private void UpdateLook(Vector2 input) {
        abdomen.localRotation = startAbdomenRot;
        chest.localRotation = startChestRot;
        head.localRotation = startHeadRot;
        //print($"Object: {Object.InputAuthority}, Look: {input}");
        abdomen.RotateAround(abdomen.position, transform.right, input.x * 0.3f);
        chest.RotateAround(chest.position, transform.right, input.x * 0.3f);
        head.RotateAround(head.position, transform.right, input.x * 0.4f);
        kcc.SetLookRotation(0, input.y);
    }
}