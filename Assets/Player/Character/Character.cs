using System;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using TMPro;

public class Character : NetworkTransform {
    [Networked(OnChanged = nameof(OnHealthChanged))] public float Health { get; set; }
    [Networked] NetworkInputData LastInput { get; set; }
    [Networked] bool IsGrounded { get; set; }
    [Networked] Vector3 Velocity { get; set; }
    [Networked] Vector2 Look { get; set; }
    [Networked] float CurrentMoveSpeed { get; set; }
    
    public Player player;
    [SerializeField] float showNametagAngle, hideNametagAngle;
    [HideInInspector] public Vector2 currentCamRecoil, currentPosRecoil, currentRotRecoil;
    [SerializeField] List<GameObject> localInvisible;
    [SerializeField] List<GameObject> remoteInvisible;
    [SerializeField] Camera cam;
    [SerializeField] Firearm weapon;
    [SerializeField] private Transform armature;
    [SerializeField] Transform groundCheck;
    [SerializeField] Transform gunHandle;
    [SerializeField] Transform weaponSprintPose;
    [SerializeField] TMP_Text killIndicator;
    [SerializeField] TMP_Text nametagText;
    [SerializeField] Transform nametagPosition;
    [SerializeField] Transform nametagAimPoint;
    [SerializeField] LayerMask groundLayer;
    [SerializeField] int FPSCap = -1;
    [SerializeField] float sensitivity;
    [SerializeField] float moveSpeed;
    [SerializeField] float jumpForce;
    [SerializeField] float gravity;
    [SerializeField] float lookSway, moveSway;
    [SerializeField] private float fpsAverageDepth;
    [SerializeField] private HealthBar healthBar;
    [Header("Kinematics")]
    [SerializeField] Transform abdomen;
    [SerializeField] Transform chest, head;
    [SerializeField] TwoBoneIK armIKL, armIKR;
    private Queue<float> deltaTimes = new();
    private CharacterController cc;
    private Vector2 localLook;
    private float gunBobProgress;
    private float bobTime;
    private float currentSensitivity;
    private Quaternion startAbdomenRot, startChestRot, startHeadRot;
    private Pose gunHandlePose, gunRecoilPose, gunBobPose;

    [Serializable] private class WeaponInterpolations {
        [HideInInspector] public Pose startPose, sprintPose, aimPose;
        public float bobSpeed, posBob, rotBob;
        public float sprintFOV, sprintMoveSpeed, sprintBobSpeed, sprintPosBob, sprintRotBob;
        public float aimBobSpeed, aimPosBob, aimRotBob;
        [HideInInspector] public float startFOV, startBobSpeed, startPosBob, startRotBob;
    }
    [SerializeField] private WeaponInterpolations lerp;

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
    
    private Controls controls;

    private void Awake() {
        controls = new();
        cc = GetComponent<CharacterController>();
    }
    private new void OnEnable() { controls.Enable(); }
    private void OnDisable() { controls.Disable(); }

    public static void OnHealthChanged(Changed<Character> changed) {
        changed.Behaviour.healthBar.SetHealthSlider(changed.Behaviour.Health);
    }

    private void OnInput(NetworkRunner runner, NetworkInput input) {
        NetworkInputData data = new();
        data.buttons.Set(Buttons.Run, controls.Player.Sprint.ReadValue<float>() == 1);
        data.buttons.Set(Buttons.Jump, controls.Player.Jump.ReadValue<float>() == 1);
        data.buttons.Set(Buttons.Fire, controls.Player.Shoot.ReadValue<float>() == 1);
        data.buttons.Set(Buttons.Aim, controls.Player.Aim.ReadValue<float>() == 1);
        data.buttons.Set(Buttons.Reload, controls.Player.Reload.ReadValue<float>() == 1);
        data.movement = controls.Player.Move.ReadValue<Vector2>();
        data.lookDelta = localLook;
        data.muzzlePos = weapon.muzzle.position;
        data.muzzleDir = weapon.muzzle.forward;
        localLook = Vector2.zero; // Consume that mother fucker
        input.Set(data);
    }
    
    public override void Spawned() {
        player = GameManager.inst.LocalPlayer;

        print($"Spawned character for player {player.Name}");
        
        lerp.startPose = new(gunHandle.localPosition, gunHandle.localRotation);
        lerp.sprintPose = new(weaponSprintPose.localPosition, weaponSprintPose.localRotation);
        lerp.aimPose = new(gunHandle.localPosition - (weapon.aimPoint.position - cam.transform.position), Quaternion.identity);
        lerp.startFOV = cam.fieldOfView;
        lerp.startBobSpeed = lerp.bobSpeed;
        lerp.startPosBob = lerp.posBob;
        lerp.startRotBob = lerp.rotBob;
        
        startAbdomenRot = abdomen.localRotation;
        startChestRot = chest.localRotation;
        startHeadRot = head.localRotation;

        if (Object.HasInputAuthority) {
            name = "Client";
            Cursor.lockState = CursorLockMode.Locked;
            Runner.GetComponent<NetworkEvents>().OnInput.AddListener(OnInput);
            GameManager.inst.SwitchCamera(cam);
            foreach (GameObject go in localInvisible) { go.GetComponent<Renderer>().enabled = false; }
            nametagText.gameObject.SetActive(false);
            gunHandlePose = new(gunHandle.localPosition, gunHandle.localRotation);
        }
        else {
            name = "Proxy";
            foreach (GameObject go in remoteInvisible) { go.GetComponent<Renderer>().enabled = false; }
            cam.gameObject.SetActive(false);
        }

        healthBar.SetMaxHealthSlider(Health);
        healthBar.SetHealthSlider(Health);
    }

    public override void FixedUpdateNetwork() {
        if (Health <= 0) {
            print("Calling kill");
            Kill();
        }
        
        if (GetInput(out NetworkInputData input)) {
            if (input.buttons.IsSet(Buttons.Aim)) { CurrentMoveSpeed = weapon.aimMoveSpeed; }
            else if (input.buttons.IsSet(Buttons.Run)) { CurrentMoveSpeed = lerp.sprintMoveSpeed; }
            else { CurrentMoveSpeed = moveSpeed; }

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
        }
        
        float finalLook = -Look.y / 3;
        abdomen.localRotation = startAbdomenRot;
        chest.localRotation = startChestRot;
        head.localRotation = startHeadRot;
        abdomen.RotateAround(abdomen.position, transform.right, finalLook);
        chest.RotateAround(chest.position, transform.right, finalLook);
        head.RotateAround(head.position, transform.right, finalLook);
        transform.rotation = Quaternion.Euler(0, Look.x, 0);
        
        if(transform.position.y <= -50) { Kill(); }

        LastInput = input;
    }

    public override void Render() {
        if (Object.HasInputAuthority) {
            Application.targetFrameRate = FPSCap;
            
            // Camera recoil
            Vector2 appliedRecoil = currentCamRecoil * weapon.rs.camSpeed * Time.deltaTime;
            currentCamRecoil -= appliedRecoil;

            // Look
            localLook += controls.Player.Look.ReadValue<Vector2>() * currentSensitivity + appliedRecoil;

            float finalLook = -Mathf.Clamp(Look.y + localLook.y, -90, 90) / 3;
            abdomen.localRotation = startAbdomenRot;
            chest.localRotation = startChestRot;
            head.localRotation = startHeadRot;
            abdomen.RotateAround(abdomen.position, transform.right, finalLook);
            chest.RotateAround(chest.position, transform.right, finalLook);
            head.RotateAround(head.position, transform.right, finalLook);
            transform.rotation = Quaternion.Euler(0, Look.x + localLook.x, 0);
            
        }
        else {
            // Nametags
            nametagText.text = player.Name.ToString();
            print($"Activecam: {GameManager.inst.activeCamera}");
            Transform activeCam = GameManager.inst.activeCamera.transform;
            float angle = Vector3.Angle(activeCam.forward, (nametagAimPoint.position - activeCam.position).normalized);
            Color col = nametagText.color; 
            col.a = Mathf.InverseLerp(hideNametagAngle, showNametagAngle, angle);   
            nametagText.color = col;
            Vector3 point = GameManager.inst.activeCamera.WorldToScreenPoint(nametagPosition.position);
            nametagText.transform.position = point;
        }

        Pose gunTargetPose;
        float poseTime;
        float fovTarget, sensTarget, bobSpeedTarget, posBobTarget, rotBobTarget;
        if (LastInput.buttons.IsSet(Buttons.Aim)) {
            poseTime = weapon.aimTime;
            gunTargetPose = lerp.aimPose;
            fovTarget = lerp.startFOV / weapon.aimingZoom;
            sensTarget = sensitivity / weapon.aimingZoom;
            bobSpeedTarget = lerp.aimBobSpeed;
            posBobTarget = lerp.aimPosBob;
            rotBobTarget = lerp.aimRotBob;
        }
        else if (LastInput.buttons.IsSet(Buttons.Run)) {
            poseTime = weapon.weight;
            gunTargetPose = lerp.sprintPose;
            fovTarget = lerp.sprintFOV;
            sensTarget = sensitivity;
            bobSpeedTarget = lerp.sprintBobSpeed;
            posBobTarget = lerp.sprintPosBob;
            rotBobTarget = lerp.sprintRotBob;
        }
        else {
            poseTime = weapon.weight;
            gunTargetPose = lerp.startPose;
            fovTarget = lerp.startFOV;
            sensTarget = sensitivity;
            bobSpeedTarget = lerp.startBobSpeed;
            posBobTarget = lerp.startPosBob;
            rotBobTarget = lerp.startRotBob;
        }
        
        currentSensitivity = Mathf.SmoothDamp(currentSensitivity, sensTarget, ref sensVelocity, weapon.weight);
        cam.fieldOfView = Mathf.SmoothDamp(cam.fieldOfView, fovTarget, ref aimVelocity, weapon.weight);
        lerp.bobSpeed = Mathf.SmoothDamp(lerp.bobSpeed, bobSpeedTarget, ref bobSpeedVelocity, weapon.weight);
        lerp.posBob = Mathf.SmoothDamp(lerp.posBob, posBobTarget, ref posBobVelocity, weapon.weight);
        lerp.rotBob = Mathf.SmoothDamp(lerp.rotBob, rotBobTarget, ref rotBobVelocity, weapon.weight);
        gunHandlePose.position = Vector3.SmoothDamp(gunHandlePose.position, gunTargetPose.position, ref posVelocity, poseTime);
        gunHandlePose.rotation = SmoothDampRot(gunHandlePose.rotation, gunTargetPose.rotation, ref rotVelocity, poseTime);
        // Weapon bob
        Vector3 bobPosTarget = Vector3.zero;
        Quaternion bobRotTarget = Quaternion.identity;
        Quaternion lookSwayRot = Quaternion.AngleAxis(-LastInput.lookDelta.y * lookSway, Vector3.left) * Quaternion.AngleAxis(LastInput.lookDelta.x * lookSway, Vector3.down);
        if (LastInput.movement == Vector2.zero) { bobTime = 0; }
        else {
            bobTime += Time.deltaTime * lerp.bobSpeed;
            bobPosTarget = new Vector3(Mathf.Sin(bobTime), -Mathf.Abs(Mathf.Cos(bobTime)), 0) * lerp.posBob; // Sin and Cos for circular motion, abs value to simulate bouncing.
            bobRotTarget = Quaternion.Euler(new Vector3(-Mathf.Abs(Mathf.Sin(bobTime)), Mathf.Cos(bobTime), 0) * lerp.rotBob);
        }
        
        gunBobPose.position = Vector3.SmoothDamp(gunBobPose.position, bobPosTarget, ref gunPosBobVelocity, 0.1f);
        gunBobPose.rotation = SmoothDampRot(gunBobPose.rotation, bobRotTarget * lookSwayRot, ref gunRotBobVelocity, 0.1f);

        // Weapon Recoil
        Vector2 appliedPosRecoil = currentPosRecoil * weapon.rs.posSpeed * Time.deltaTime;
        Vector2 appliedRotRecoil = currentRotRecoil * weapon.rs.rotSpeed * Time.deltaTime;
        gunRecoilPose.position += new Vector3(0, appliedPosRecoil.y, appliedPosRecoil.x);
        gunRecoilPose.rotation *= Quaternion.Euler(appliedRotRecoil.y, appliedRotRecoil.x, 0);
        gunRecoilPose.position = Vector3.Slerp(gunRecoilPose.position, Vector3.zero, weapon.rs.posRecovery * Time.deltaTime);
        gunRecoilPose.rotation = Quaternion.Slerp(gunRecoilPose.rotation, Quaternion.identity, weapon.rs.rotRecovery * Time.deltaTime);
        currentPosRecoil -= appliedPosRecoil;
        currentRotRecoil -= appliedRotRecoil;

        gunHandle.localPosition = gunHandlePose.position + gunRecoilPose.position + gunBobPose.position;
        gunHandle.localRotation = gunHandlePose.rotation * gunRecoilPose.rotation * gunBobPose.rotation;
        
        // IK
        armIKL.InvertKinematics();
        armIKR.InvertKinematics();
    }

    private void OnGUI() {
        deltaTimes.Enqueue(Time.unscaledDeltaTime);
        if (deltaTimes.Count > fpsAverageDepth) { deltaTimes.Dequeue(); }
        float avg = 0;
        foreach (float time in deltaTimes) { avg += time; }
        GUI.Label(new Rect(5, 5, 100, 25), "FPS: " + Math.Round(1 / (avg / fpsAverageDepth), 1));
    }

    public void Kill() {
        print($"Killed player {Object.InputAuthority}");
        if (Object.HasInputAuthority) {
            print("Switching cam");
            GameManager.inst.SwitchCamera(GameManager.inst.mainCamera);
            Cursor.lockState = CursorLockMode.None;
        }
        Runner.Despawn(Object);
    }

    public void EnemyKilled(Character player) {
        killIndicator.text = $"Killed {player}";
        killIndicator.gameObject.SetActive(true);
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

/*anim.SetFloat("MoveX", LastInput.movement.x, 0.1f, Time.deltaTime);
anim.SetFloat("MoveZ", LastInput.movement.y, 0.1f, Time.deltaTime);
anim.SetFloat("Aim", LastInput.buttons.IsSet(Buttons.Aim) ? 1 : 0, 0.1f, Time.deltaTime);*/