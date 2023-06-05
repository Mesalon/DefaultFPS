using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using Fusion;
using TMPro;

public class Character : NetworkTransform {
    [Networked(OnChanged = nameof(OnHealthChanged))]
    float Health { get; set; }

    [Networked] NetworkInputData LastInput { get; set; }
    [Networked] Vector3 Velocity { get; set; }
    [Networked] Vector2 Look { get; set; }
    [Networked] private WeaponRole CurrentGunType { get; set; }
    [Networked] bool IsGrounded { get; set; }
    [Networked] float CurrentMoveSpeed { get; set; }
    public Player player;
    public Player attacker; // Most recent damage source
    [HideInInspector] public Vector2 currentCamRecoil, currentPosRecoil, currentRotRecoil;
    [SerializeField] List<GameObject> localInvisible;
    [SerializeField] List<GameObject> remoteInvisible;
    [SerializeField] Firearm currentGun, oldGun, primaryGun, secondaryGun; // oldGun used for weapon switching
    [SerializeField] Transform groundCheck;
    [SerializeField] Transform gunAwayPose;
    [SerializeField] Transform riflePose, rifleSprintPose;
    [SerializeField] Transform pistolPose, pistolSprintPose;
    [SerializeField] LayerMask groundLayer;
    [SerializeField] float maxHealth;
    [SerializeField] float sensitivity;
    [SerializeField] float moveSpeed;
    [SerializeField] float jumpForce;
    [SerializeField] float gravity;
    [SerializeField] float lookSway, moveSway;
    [SerializeField] int FPSCap = -1;
    [Header("UI")] 
    [SerializeField] Camera cam;
    [SerializeField] HealthBar healthBar;
    [SerializeField] TMP_Text ammoCounter;
    [SerializeField] TMP_Text killIndicator;
    [SerializeField] float showNametagAngle, hideNametagAngle;
    [SerializeField] TMP_Text nametagText;
    [SerializeField] Transform nametagPosition;
    [SerializeField] Transform nametagAimPoint;
    [SerializeField] TMP_Text redTeamKills;
    [SerializeField] TMP_Text blueTeamKills;
    [SerializeField] float fpsAverageDepth;
    [Header("Kinematics")] 
    [SerializeField] Transform abdomen;
    [SerializeField] Transform chest, head;
    [SerializeField] TwoBoneIK armIKL, armIKR;
    private Queue<float> deltaTimes = new();
    private CharacterController cc;
    private Quaternion startAbdomenRot, startChestRot, startHeadRot;
    private Pose gunHandlePose, gunRecoilPose, gunBobPose;
    private Vector2 localLook;
    private float currentSensitivity;
    private float bobClock;

    [Serializable]
    private class WeaponInterpolations {
        public float bobSpeed, posBob, rotBob;
        public float sprintFOV, sprintMoveSpeed, sprintBobSpeed, sprintPosBob, sprintRotBob;
        public float aimBobSpeed, aimPosBob, aimRotBob;
        [HideInInspector] public float startFOV, startBobSpeed, startPosBob, startRotBob;
    }
    [SerializeField] private WeaponInterpolations lerp; // SmoothDamp variables
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
    private Vector3 damp1, damp2;
    private Quaternion damp3, damp4;
    private Controls controls;

    public static void OnHealthChanged(Changed<Character> changed) {
        Character c = changed.Behaviour;
        if (c.Health <= 0) {
            // Die
            Character atk = c.attacker.character;
            IEnumerator IndicateKill() {
                atk.killIndicator.text = $"KILL - {c.player.Name}";
                atk.killIndicator.gameObject.SetActive(true);
                yield return new WaitForSeconds(1.5f);
                atk.killIndicator.gameObject.SetActive(false);
            }
            atk.StartCoroutine(IndicateKill());
            if (atk.player.team == Team.Red) { GameManager.inst.redTeamKills++; }
            else { GameManager.inst.blueTeamKills++; }
            atk.player.Kills++;
            c.Kill();
        }
        else { changed.Behaviour.healthBar.SetHealthSlider(c.Health); }
    }

    private void Awake() {
        controls = new();
        cc = GetComponent<CharacterController>();
    }

    private new void OnEnable() { controls.Enable(); }
    private void OnDisable() { controls.Disable(); }

    private void OnInput(NetworkRunner runner, NetworkInput input) {
        NetworkInputData data = new();
        data.buttons.Set(Buttons.Run, controls.Player.Sprint.ReadValue<float>() == 1);
        data.buttons.Set(Buttons.Jump, controls.Player.Jump.ReadValue<float>() == 1);
        data.buttons.Set(Buttons.Fire, controls.Player.Shoot.ReadValue<float>() == 1);
        data.buttons.Set(Buttons.Aim, controls.Player.Aim.ReadValue<float>() == 1);
        data.buttons.Set(Buttons.Reload, controls.Player.Reload.ReadValue<float>() == 1);
        data.buttons.Set(Buttons.switchPrimary, controls.Player.Primary.ReadValue<float>() == 1);
        data.buttons.Set(Buttons.switchSecondary, controls.Player.Secondary.ReadValue<float>() == 1);
        data.movement = controls.Player.Move.ReadValue<Vector2>();
        data.lookDelta = localLook;
        data.muzzlePos = currentGun.muzzle.position;
        data.muzzleDir = currentGun.muzzle.forward;
        localLook = Vector2.zero; // Consume that mother fucker
        input.Set(data);
    }

    public override void Spawned() {
        player = GameManager.GetPlayer(Runner, Object.InputAuthority);
        player.character = this;
        Health = maxHealth;
        CurrentGunType = WeaponRole.Primary;
        currentGun = oldGun = primaryGun;
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
            foreach (GameObject go in localInvisible) {
                go.GetComponent<Renderer>().enabled = false;
            }

            nametagText.gameObject.SetActive(false);
            gunHandlePose = new(currentGun.transform.localPosition, currentGun.transform.localRotation);
        }
        else {
            name = "Proxy";
            foreach (GameObject go in remoteInvisible) {
                go.GetComponent<Renderer>().enabled = false;
            }

            cam.gameObject.SetActive(false);
        }

        healthBar.SetMaxHealthSlider(Health);
        healthBar.SetHealthSlider(Health);
    }

    public override void FixedUpdateNetwork() {
        if (GetInput(out NetworkInputData input)) {
            if (input.buttons.IsSet(Buttons.Aim)) { CurrentMoveSpeed = currentGun.aimMoveSpeed; }
            else if (input.buttons.IsSet(Buttons.Run)) { CurrentMoveSpeed = lerp.sprintMoveSpeed; }
            else { CurrentMoveSpeed = moveSpeed; }

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
            
            // Weapon operation
            if (input.buttons.WasPressed(LastInput.buttons, Buttons.switchPrimary)) { CurrentGunType = WeaponRole.Primary; }
            if (input.buttons.WasPressed(LastInput.buttons, Buttons.switchSecondary)) { CurrentGunType = WeaponRole.Secondary; }
            if (input.buttons.WasPressed(LastInput.buttons, Buttons.Reload)) { currentGun.Reload(); }
            currentGun.TriggerState = input.buttons.IsSet(Buttons.Fire);
        }
        currentGun = CurrentGunType == WeaponRole.Primary ? primaryGun : secondaryGun;

        abdomen.localRotation = startAbdomenRot;
        chest.localRotation = startChestRot;
        head.localRotation = startHeadRot;
        abdomen.RotateAround(abdomen.position, transform.right, -Look.y * 0.4f);
        chest.RotateAround(chest.position, transform.right, -Look.y * 0.4f);
        head.RotateAround(head.position, transform.right, -Look.y * 0.2f);
        transform.rotation = Quaternion.Euler(0, Look.x, 0);

        if (transform.position.y <= -50) { Kill(); }

        LastInput = input;
    }

    public override void Render() {
        if (Object.HasInputAuthority) {
            Application.targetFrameRate = FPSCap;

            ammoCounter.text = $"{currentGun.Ammo} / {currentGun.ReserveAmmo}";

            // Camera recoil
            Vector2 appliedRecoil = currentCamRecoil * currentGun.rs.camSpeed * Time.deltaTime;
            currentCamRecoil -= appliedRecoil;

            // Look
            localLook += controls.Player.Look.ReadValue<Vector2>() * currentSensitivity + appliedRecoil;
            float finalLook = -Mathf.Clamp(Look.y + localLook.y, -90, 90);
            abdomen.localRotation = startAbdomenRot;
            chest.localRotation = startChestRot;
            head.localRotation = startHeadRot;
            abdomen.RotateAround(abdomen.position, transform.right, finalLook * 0.4f);
            chest.RotateAround(chest.position, transform.right, finalLook * 0.4f);
            head.RotateAround(head.position, transform.right, finalLook * 0.2f);
            transform.rotation = Quaternion.Euler(0, Look.x + localLook.x, 0);
        }
        else {
            // Nametags
            nametagText.text = player.Name.ToString();
            Transform activeCam = GameManager.inst.activeCamera.transform;
            float angle = Vector3.Angle(activeCam.forward, (nametagAimPoint.position - activeCam.position).normalized);
            Color col = nametagText.color;
            col.a = Mathf.InverseLerp(hideNametagAngle, showNametagAngle, angle);
            nametagText.color = col;
            Vector3 point = GameManager.inst.activeCamera.WorldToScreenPoint(nametagPosition.position);
            nametagText.transform.position = point;
        }

        Pose gunPoseTarget;
        float poseTime;
        float fovTarget, bobSpeedTarget, posBobTarget, rotBobTarget;
        if (LastInput.buttons.IsSet(Buttons.Aim)) {
            gunPoseTarget = new(cam.transform.localPosition - oldGun.aimPoint.localPosition, Quaternion.identity);
            poseTime = oldGun.aimTime;
            fovTarget = lerp.startFOV / oldGun.aimingZoom;
            currentSensitivity = sensitivity / oldGun.aimingZoom;
            bobSpeedTarget = lerp.aimBobSpeed;
            posBobTarget = lerp.aimPosBob;
            rotBobTarget = lerp.aimRotBob;
        }
        else if (LastInput.buttons.IsSet(Buttons.Run)) {
            gunPoseTarget = oldGun.type == WeaponClass.Rifle ? new(rifleSprintPose.localPosition, rifleSprintPose.localRotation) : new(pistolSprintPose.localPosition, pistolSprintPose.localRotation);
            poseTime = oldGun.weight;
            fovTarget = lerp.sprintFOV;
            currentSensitivity = sensitivity;
            bobSpeedTarget = lerp.sprintBobSpeed;
            posBobTarget = lerp.sprintPosBob;
            rotBobTarget = lerp.sprintRotBob;
        }
        else {
            poseTime = oldGun.weight;
            gunPoseTarget = oldGun.type == WeaponClass.Rifle ? new(riflePose.localPosition, riflePose.localRotation) : new(pistolPose.localPosition, pistolPose.localRotation);
            fovTarget = lerp.startFOV;
            currentSensitivity = sensitivity;
            bobSpeedTarget = lerp.startBobSpeed;
            posBobTarget = lerp.startPosBob;
            rotBobTarget = lerp.startRotBob;
        }
        
        // Weapon switching
        if (oldGun != currentGun) {
            gunPoseTarget = new(gunAwayPose.localPosition, gunAwayPose.localRotation);
            poseTime = currentGun.weight;
            if ((oldGun.transform.position - gunAwayPose.position).magnitude < 0.05f) { // Done switching
                gunHandlePose = new(gunAwayPose.localPosition, gunAwayPose.localRotation);
                oldGun.gameObject.SetActive(false);
                oldGun = currentGun;
            }
        }
        else if (!currentGun.gameObject.activeSelf) {
            currentGun.gameObject.SetActive(true);
            armIKL.target = currentGun.IKLTarget;
            armIKR.target = currentGun.IKRTarget;
            gunHandlePose = new(gunAwayPose.localPosition, gunAwayPose.localRotation);
        }
        
        cam.fieldOfView = Mathf.SmoothDamp(cam.fieldOfView, fovTarget, ref aimVelocity, currentGun.weight);
        lerp.bobSpeed = Mathf.SmoothDamp(lerp.bobSpeed, bobSpeedTarget, ref bobSpeedVelocity, currentGun.weight);
        lerp.posBob = Mathf.SmoothDamp(lerp.posBob, posBobTarget, ref posBobVelocity, currentGun.weight);
        lerp.rotBob = Mathf.SmoothDamp(lerp.rotBob, rotBobTarget, ref rotBobVelocity, currentGun.weight);
        gunHandlePose.position = Vector3.SmoothDamp(gunHandlePose.position, gunPoseTarget.position, ref posVelocity, poseTime);
        gunHandlePose.rotation = SmoothDampRot(gunHandlePose.rotation, gunPoseTarget.rotation, ref rotVelocity, poseTime);
        // Weapon bob
        Vector3 bobPosTarget = Vector3.zero;
        Quaternion bobRotTarget = Quaternion.identity;
        Quaternion lookSwayRot = Quaternion.AngleAxis(-LastInput.lookDelta.y * lookSway, Vector3.left) * Quaternion.AngleAxis(LastInput.lookDelta.x * lookSway, Vector3.down);
        if (LastInput.movement == Vector2.zero) {
            bobClock = 0;
        }
        else {
            bobClock += Time.deltaTime * lerp.bobSpeed;
            bobPosTarget = new Vector3(Mathf.Sin(bobClock), -Mathf.Abs(Mathf.Cos(bobClock)), 0) * lerp.posBob; // Sin and Cos for circular motion, abs value to simulate bouncing.
            bobRotTarget = Quaternion.Euler(new Vector3(-Mathf.Abs(Mathf.Sin(bobClock)), Mathf.Cos(bobClock), 0) * lerp.rotBob);
        }

        gunBobPose.position = Vector3.SmoothDamp(gunBobPose.position, bobPosTarget, ref gunPosBobVelocity, 0.1f);
        gunBobPose.rotation = SmoothDampRot(gunBobPose.rotation, bobRotTarget * lookSwayRot, ref gunRotBobVelocity, 0.1f);

        // Weapon Recoil
        Vector2 appliedPosRecoil = currentPosRecoil * currentGun.rs.posSpeed * Time.deltaTime;
        Vector2 appliedRotRecoil = currentRotRecoil * currentGun.rs.rotSpeed * Time.deltaTime;
        gunRecoilPose.position += new Vector3(0, appliedPosRecoil.y, appliedPosRecoil.x);
        gunRecoilPose.rotation *= Quaternion.Euler(appliedRotRecoil.y, appliedRotRecoil.x, 0);
        gunRecoilPose.position = Vector3.Slerp(gunRecoilPose.position, Vector3.zero, currentGun.rs.posRecovery * Time.deltaTime);
        gunRecoilPose.rotation = Quaternion.Slerp(gunRecoilPose.rotation, Quaternion.identity, currentGun.rs.rotRecovery * Time.deltaTime);
        currentPosRecoil -= appliedPosRecoil;
        currentRotRecoil -= appliedRotRecoil;

        oldGun.transform.localPosition = gunHandlePose.position + gunRecoilPose.position + gunBobPose.position;
        oldGun.transform.localRotation = gunHandlePose.rotation * gunRecoilPose.rotation * gunBobPose.rotation;

        // IK
        armIKL.InvertKinematics();
        armIKR.InvertKinematics();

        //UI
        redTeamKills.text = GameManager.inst.redTeamKills.ToString();
        blueTeamKills.text = GameManager.inst.blueTeamKills.ToString();
    }

    private void OnGUI() {
        deltaTimes.Enqueue(Time.unscaledDeltaTime);
        if (deltaTimes.Count > fpsAverageDepth) {
            deltaTimes.Dequeue();
        }

        float avg = 0;
        foreach (float time in deltaTimes) {
            avg += time;
        }

        GUI.Label(new Rect(5, 5, 100, 25), "FPS: " + Math.Round(1 / (avg / fpsAverageDepth), 1));
    }

    public void Kill() {
        if (Object.HasInputAuthority) {
            GameManager.inst.SwitchCamera(GameManager.inst.mainCamera);
            Cursor.lockState = CursorLockMode.None;
        }

        player.Deaths++;
        Runner.Despawn(Object);
    }

    public void Damage(Player source, float amount) {
        // todo: Implement DamageSource struct with more detailed information like weapon, distance, etc
        attacker = source;
        Health = Mathf.Clamp(Health - amount, 0, maxHealth);
    }

    protected override void CopyFromBufferToEngine() {
        // Prevents Unity from doing funky shit when applying values. Required for CC function.
        cc.enabled = false;
        base.CopyFromBufferToEngine();
        cc.enabled = true;
    }

    private static Quaternion SmoothDampRot(Quaternion rot, Quaternion target, ref Quaternion velocity, float time) {
        // Stolen asf code
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

public enum WeaponRole { Primary, Secondary }
/*anim.SetFloat("MoveX", LastInput.movement.x, 0.1f, Time.deltaTime);
anim.SetFloat("MoveZ", LastInput.movement.y, 0.1f, Time.deltaTime);
anim.SetFloat("Aim", LastInput.buttons.IsSet(Buttons.Aim) ? 1 : 0, 0.1f, Time.deltaTime);*/
// todo: Troll Unity disc on alt - Ask to help model a sine wave in Unity, pretending to not know what a sine wave is called and show a video of a stream of my piss moving up and down as an example