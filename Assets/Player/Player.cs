using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class Player : NetworkTransform {
    public static void OnHealthChanged(Changed<Player> changed) {
        changed.LoadOld(); float old = changed.Behaviour.Health; changed.LoadNew();
        if (changed.Behaviour.Health < old) {
            print($"{changed.Behaviour.name}: Damaged to {changed.Behaviour.Health}!");
        }
    }

    [Networked(OnChanged = nameof(OnHealthChanged))]
    public float Health { get; set; }
    [Networked] private NetworkInputData LastInput { get; set; }
    [Networked] private bool IsGrounded { get; set; }
    [Networked] private Vector3 Velocity { get; set; }
    [Networked] private Vector2 Look { get; set; }
    [Networked] private float CurrentMoveSpeed { get; set; }
    
    [Header("Character")]
    [SerializeField] private float sensitivity;
    [SerializeField] private float moveSpeed;
    private float currentSensitivity;
    [SerializeField] private float jumpForce;
    [SerializeField] private float gravity;
    [SerializeField] private List<GameObject> localDisabled;
    [SerializeField] private List<GameObject> remoteDisabled;
    private Vector2 localLook;
    [Header("References")]
    [SerializeField] private Transform head;
    [SerializeField] private Camera cam;
    [SerializeField] private Firearm weapon;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Transform gunHandle;
    [SerializeField] private Transform gunRecoilHandle;
    [SerializeField] private Transform gunBobHandle;
    [SerializeField] private Transform weaponSprintPose;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Animator anim;
    private CharacterController cc;
    [Header("Weapon Handling")]
    [HideInInspector] public Vector2 currentCamRecoil, currentPosRecoil, currentRotRecoil;
    [SerializeField] private float lookSway, moveSway;

    private float bobTime;

    [System.Serializable] private class WeaponInterpolations {
        [HideInInspector] public Pose startPose, sprintPose, aimPose;
        public float bobSpeed, posBob, rotBob;
        public float sprintFOV, sprintMoveSpeed, sprintBobSpeed, sprintPosBob, sprintRotBob;
        public float aimBobSpeed, aimPosBob, aimRotBob;
        [HideInInspector] public float startFOV, startBobSpeed, startPosBob, startRotBob;
    }
    [SerializeField] private WeaponInterpolations inter;
    
    // SmoothDamp variables
    private Vector3 posVelocity;
    private Quaternion rotVelocity;
    private Vector3 gunPosBobVelocity;
    private Quaternion gunRotBobVelocity;
    private float gunBobVelocity;
    private float aimVelocity;
    private float sensVelocity;
    private float bobSpeedVelocity;
    private float posBobVelocity;
    private float rotBobVelocity;

    private float gunBobProgress;
    
    private Controls controls;
    private new void OnEnable() { controls.Enable(); }
    private void OnDisable() { controls.Disable(); }

    private void Awake() {
        controls = new();
        cc = GetComponent<CharacterController>();
        GameObject mainCam = GameObject.Find("Main Camera");
        if(mainCam) mainCam.SetActive(false);
    }

    private void OnInput(NetworkRunner runner, NetworkInput input) {
        NetworkInputData data = new();
        data.buttons.Set(Buttons.Sprint, controls.Player.Sprint.ReadValue<float>() == 1);
        data.buttons.Set(Buttons.Jump, controls.Player.Jump.ReadValue<float>() == 1);
        data.buttons.Set(Buttons.Fire, controls.Player.Shoot.ReadValue<float>() == 1);
        data.buttons.Set(Buttons.Aim, controls.Player.Aim.ReadValue<float>() == 1);
        data.buttons.Set(Buttons.Reload, controls.Player.Reload.ReadValue<float>() == 1);
        data.movement = controls.Player.Move.ReadValue<Vector2>();
        data.lookDelta = localLook;
        localLook = Vector2.zero; // Consume that mother fucker
        input.Set(data);
    }
    
    public override void Spawned() {
        inter.startPose = new(gunHandle.localPosition, gunHandle.localRotation);
        inter.sprintPose = new(weaponSprintPose.localPosition, weaponSprintPose.localRotation);
        inter.aimPose = new(gunHandle.localPosition - (weapon.aimPoint.position - cam.transform.position), Quaternion.identity);
        inter.startFOV = cam.fieldOfView;
        inter.startBobSpeed = inter.bobSpeed;
        inter.startPosBob = inter.posBob;
        inter.startRotBob = inter.rotBob;
        
        if (Object.HasInputAuthority) {
            name = "Client";
            Cursor.lockState = CursorLockMode.Locked;
            Runner.GetComponent<NetworkEvents>().OnInput.AddListener(OnInput);
            foreach (GameObject go in localDisabled) { go.GetComponent<Renderer>().enabled = false; }
        }
        else {
            name = "Proxy";
            foreach (GameObject go in remoteDisabled) { go.GetComponent<Renderer>().enabled = false; }
            cam.gameObject.SetActive(false);
        }
    }

    public override void FixedUpdateNetwork() {
        if (GetInput(out NetworkInputData input)) {
            if (input.buttons.IsSet(Buttons.Aim)) {
                CurrentMoveSpeed = weapon.aimMoveSpeed;
            }
            else if (input.buttons.IsSet(Buttons.Sprint)) {
                CurrentMoveSpeed = inter.sprintMoveSpeed;
            }
            else {
                CurrentMoveSpeed = moveSpeed;
            }
            
            IsGrounded = Physics.CheckSphere(groundCheck.position, 0.15f, groundLayer);
            Look = new(Look.x + input.lookDelta.x, Mathf.Clamp(Look.y + input.lookDelta.y, -90, 90));
            
            Vector3 previousPos = transform.position;
            Vector3 moveVelocity = transform.rotation * new Vector3(input.movement.x, 0, input.movement.y) * CurrentMoveSpeed;
            moveVelocity.y = Velocity.y + gravity * Runner.DeltaTime;
            if (IsGrounded) {
                if(moveVelocity.y < 0) moveVelocity.y = 0f;
                if(input.buttons.WasPressed(LastInput.buttons, Buttons.Jump)) moveVelocity.y += jumpForce;
            }
            cc.Move(moveVelocity * Runner.DeltaTime);
            Velocity = (transform.position - previousPos) * Runner.Simulation.Config.TickRate;
            
            // Weapon operation
            weapon.TriggerState = input.buttons.IsSet(Buttons.Fire);
            if (input.buttons.WasPressed(LastInput.buttons, Buttons.Reload)) { weapon.Reload(); }

            LastInput = input;
        }
        transform.rotation = Quaternion.Euler(0, Look.x, 0);
        head.localRotation = Quaternion.Euler(-Look.y, 0, 0);
    }

    public override void Render() {
        if (Object.HasInputAuthority) {
            // Camera recoil
            Vector2 appliedRecoil = currentCamRecoil * weapon.rs.camSpeed * Time.deltaTime;
            currentCamRecoil -= appliedRecoil;

            // Look
            localLook += controls.Player.Look.ReadValue<Vector2>() * currentSensitivity + appliedRecoil;
            transform.rotation = Quaternion.Euler(0, Look.x + localLook.x, 0);
            head.localRotation = Quaternion.Euler(Mathf.Clamp(-Look.y - localLook.y, -90, 90), 0, 0);
        }
        else {
            /*anim.SetFloat("MoveX", LastInput.movement.x, 0.1f, Time.deltaTime);
            anim.SetFloat("MoveZ", LastInput.movement.y, 0.1f, Time.deltaTime);
            anim.SetFloat("Aim", LastInput.buttons.IsSet(Buttons.Aim) ? 1 : 0, 0.1f, Time.deltaTime);*/
            localLook = LastInput.lookDelta; // For weapon sway
        }

        // Weapon pose
        Pose gunPoseTarget;
        float poseTime;
        float fovTarget, sensTarget, bobSpeedTarget, posBobTarget, rotBobTarget;
        if (LastInput.buttons.IsSet(Buttons.Aim)) {
            poseTime = weapon.aimTime;
            gunPoseTarget = inter.aimPose;
            fovTarget = inter.startFOV / weapon.aimingZoom;
            sensTarget = sensitivity / weapon.aimingZoom;
            bobSpeedTarget = inter.aimBobSpeed;
            posBobTarget = inter.aimPosBob;
            rotBobTarget = inter.aimRotBob;
        }
        else if (LastInput.buttons.IsSet(Buttons.Sprint)) {
            poseTime = weapon.weight;
            gunPoseTarget = inter.sprintPose;
            fovTarget = inter.sprintFOV;
            sensTarget = sensitivity;
            bobSpeedTarget = inter.sprintBobSpeed;
            posBobTarget = inter.sprintPosBob;
            rotBobTarget = inter.sprintRotBob;
        }
        else {
            poseTime = weapon.weight;
            gunPoseTarget = inter.startPose;
            fovTarget = inter.startFOV;
            sensTarget = sensitivity;
            bobSpeedTarget = inter.startBobSpeed;
            posBobTarget = inter.startPosBob;
            rotBobTarget = inter.startRotBob;
        }
        gunHandle.localPosition = Vector3.SmoothDamp(gunHandle.localPosition, gunPoseTarget.position, ref posVelocity, poseTime);
        gunHandle.localRotation = SmoothDampRot(gunHandle.localRotation, gunPoseTarget.rotation, ref rotVelocity, poseTime);
        cam.fieldOfView = Mathf.SmoothDamp(cam.fieldOfView, fovTarget, ref aimVelocity, weapon.weight);
        currentSensitivity = Mathf.SmoothDamp(currentSensitivity, sensTarget, ref sensVelocity, weapon.weight);
        inter.bobSpeed = Mathf.SmoothDamp(inter.bobSpeed, bobSpeedTarget, ref bobSpeedVelocity, weapon.weight);
        inter.posBob = Mathf.SmoothDamp(inter.posBob, posBobTarget, ref posBobVelocity, weapon.weight);
        inter.rotBob = Mathf.SmoothDamp(inter.rotBob, rotBobTarget, ref rotBobVelocity, weapon.weight);

        // Weapon bob
        Vector3 bobPosTarget = Vector3.zero;
        Quaternion bobRotTarget = Quaternion.identity;
        Quaternion lookSwayRot = Quaternion.AngleAxis(-localLook.y * lookSway, Vector3.left) * Quaternion.AngleAxis(localLook.x * lookSway, Vector3.down);
        if (LastInput.movement == default) {
            bobTime = 0;
        }
        else {
            bobTime += Time.deltaTime * inter.bobSpeed;
            bobPosTarget = new Vector3(Mathf.Sin(bobTime), -Mathf.Abs(Mathf.Cos(bobTime)), 0) * inter.posBob; // Sin and Cos for circular motion, abs value to simulate bouncing.
            bobRotTarget = Quaternion.Euler(new Vector3(-Mathf.Abs(Mathf.Sin(bobTime)), Mathf.Cos(bobTime), 0) * inter.rotBob);
        }
        gunBobHandle.localPosition = Vector3.SmoothDamp(gunBobHandle.localPosition, bobPosTarget, ref gunPosBobVelocity, 0.1f);
        gunBobHandle.localRotation = SmoothDampRot(gunBobHandle.localRotation, bobRotTarget * lookSwayRot, ref gunRotBobVelocity, 0.1f);

        // Weapon Recoil
        Vector2 appliedPosRecoil = currentPosRecoil * weapon.rs.posSpeed * Time.deltaTime;
        Vector2 appliedRotRecoil = currentRotRecoil * weapon.rs.rotSpeed * Time.deltaTime;
        gunRecoilHandle.localPosition += new Vector3(0, appliedPosRecoil.y, appliedPosRecoil.x);
        gunRecoilHandle.localRotation *= Quaternion.Euler(appliedRotRecoil.y, appliedRotRecoil.x, 0);
        gunRecoilHandle.localPosition = Vector3.Slerp(gunRecoilHandle.localPosition, Vector3.zero, weapon.rs.posRecovery);
        gunRecoilHandle.localRotation = Quaternion.Slerp(gunRecoilHandle.localRotation, Quaternion.identity, weapon.rs.rotRecovery);
        currentPosRecoil -= appliedPosRecoil;
        currentRotRecoil -= appliedRotRecoil;

    }

    protected override void CopyFromBufferToEngine() { // Prevents Unity from doing funky shit when applying values. Required for CC function.
        cc.enabled = false;
        base.CopyFromBufferToEngine();
        cc.enabled = true;
    }
    
    private static Quaternion SmoothDampRot(Quaternion rot, Quaternion target, ref Quaternion velocity, float time) { // Stolen asf code
        if (Time.deltaTime < Mathf.Epsilon) return rot;
        // account for double-cover
        var Dot = Quaternion.Dot(rot, target);
        var Multi = Dot > 0f ? 1f : -1f;
        target.x *= Multi;
        target.y *= Multi;
        target.z *= Multi;
        target.w *= Multi;
        // smooth damp (nlerp approx)
        var Result = new Vector4(
            Mathf.SmoothDamp(rot.x, target.x, ref velocity.x, time),
            Mathf.SmoothDamp(rot.y, target.y, ref velocity.y, time),
            Mathf.SmoothDamp(rot.z, target.z, ref velocity.z, time),
            Mathf.SmoothDamp(rot.w, target.w, ref velocity.w, time)
        ).normalized;
		
        // ensure velocity is tangent
        var velocityError = Vector4.Project(new Vector4(velocity.x, velocity.y, velocity.z, velocity.w), Result);
        velocity.x -= velocityError.x;
        velocity.y -= velocityError.y;
        velocity.z -= velocityError.z;
        velocity.w -= velocityError.w;		
		
        return new Quaternion(Result.x, Result.y, Result.z, Result.w);
    }
}
