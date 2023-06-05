using UnityEngine;
using Fusion;

public class Handling : NetworkBehaviour {
    [Networked] private WeaponRole CurrentGunType { get; set; }
    [Networked] NetworkInputData LastInput { get; set; }
    [HideInInspector] public Firearm gun;
    [HideInInspector] public Character character;
    [HideInInspector] public Vector2 currentCamRecoil, currentPosRecoil, currentRotRecoil;
    [SerializeField] Firearm primaryGun, secondaryGun;
    [SerializeField] Transform gunAwayPose;
    [SerializeField] Transform riflePose, rifleSprintPose;
    [SerializeField] Transform pistolPose, pistolSprintPose;
    [SerializeField] TwoBoneIK armIKL, armIKR;
    [SerializeField] Camera cam;
    [SerializeField] float lookSway, moveSway;
    private Locomotion locomotion;
    private float startFOV, startBobSpeed, startPosBob, startRotBob;
    private Firearm newGun; // For switching weapons
    private Pose gunHandlePose, gunRecoilPose, gunBobPose;
    private float bobClock;
    public float bobSpeed, posBob, rotBob;
    public float sprintFOV, sprintBobSpeed, sprintPosBob, sprintRotBob;
    public float aimBobSpeed, aimPosBob, aimRotBob;
    private Vector3 velV1, velV2, velV3, velV4;
    private Quaternion velQ1, velQ2, velQ3, velQ4;
    private float vel1, vel2, vel3, vel4, vel5, vel6;

    private void Awake() {
        character = GetComponent<Character>();
        locomotion = GetComponent<Locomotion>();
    }

    public override void Spawned() {
        CurrentGunType = WeaponRole.Primary;
        newGun = gun = primaryGun;
        startFOV = cam.fieldOfView;
        startBobSpeed = bobSpeed;
        startPosBob = posBob;
        startRotBob = rotBob;
        gunHandlePose = new(gun.transform.localPosition, gun.transform.localRotation);
    }

    public override void FixedUpdateNetwork() {
        if (GetInput(out NetworkInputData input)) {
            if (input.buttons.IsSet(Buttons.Aim)) { locomotion.CurrentMoveSpeed = gun.walkSpeed * gun.aimingSpeedMult; }
            else if (input.buttons.IsSet(Buttons.Run)) { locomotion.CurrentMoveSpeed = gun.walkSpeed * gun.runningSpeedMult; }
            else { locomotion.CurrentMoveSpeed = gun.walkSpeed; }
            
            if (input.buttons.WasPressed(LastInput.buttons, Buttons.switchPrimary)) { CurrentGunType = WeaponRole.Primary; }
            if (input.buttons.WasPressed(LastInput.buttons, Buttons.switchSecondary)) { CurrentGunType = WeaponRole.Secondary; }
            if (input.buttons.WasPressed(LastInput.buttons, Buttons.Reload)) { gun.Reload(); }
            gun.TriggerState = input.buttons.IsSet(Buttons.Fire);
            LastInput = input;
        }
        newGun = CurrentGunType == WeaponRole.Primary ? primaryGun : secondaryGun;
    }

    public override void Render() {
        Pose gunPoseTarget;
        float poseTime;
        float fovTarget, bobSpeedTarget, posBobTarget, rotBobTarget;
        if (LastInput.buttons.IsSet(Buttons.Aim)) {
            gunPoseTarget = new(cam.transform.localPosition - gun.aimPoint.localPosition, Quaternion.identity);
            poseTime = gun.aimTime;
            fovTarget = startFOV / gun.aimingZoom;
            locomotion.currentSensitivity = locomotion.sensitivity / gun.aimingZoom;
            bobSpeedTarget = aimBobSpeed;
            posBobTarget = aimPosBob;
            rotBobTarget = aimRotBob;
        }
        else if (LastInput.buttons.IsSet(Buttons.Run)) {
            gunPoseTarget = gun.type == WeaponClass.Rifle ? new(rifleSprintPose.localPosition, rifleSprintPose.localRotation) : new(pistolSprintPose.localPosition, pistolSprintPose.localRotation);
            poseTime = gun.weight;
            fovTarget = sprintFOV;
            locomotion.currentSensitivity = locomotion.sensitivity;
            bobSpeedTarget = sprintBobSpeed;
            posBobTarget = sprintPosBob;
            rotBobTarget = sprintRotBob;
        }
        else {
            poseTime = gun.weight;
            gunPoseTarget = gun.type == WeaponClass.Rifle ? new(riflePose.localPosition, riflePose.localRotation) : new(pistolPose.localPosition, pistolPose.localRotation);
            fovTarget = startFOV;
            locomotion.currentSensitivity = locomotion.sensitivity;
            bobSpeedTarget = startBobSpeed;
            posBobTarget = startPosBob;
            rotBobTarget = startRotBob;
        }
        
        // Weapon switching
        if (gun != newGun) {
            gunPoseTarget = new(gunAwayPose.localPosition, gunAwayPose.localRotation);
            poseTime = newGun.weight;
            if ((gun.transform.position - gunAwayPose.position).magnitude < 0.05f) { // Done switching
                gunHandlePose = new(gunAwayPose.localPosition, gunAwayPose.localRotation);
                gun.gameObject.SetActive(false);
                gun = newGun;
            }
        }
        else if (!newGun.gameObject.activeSelf) {
            newGun.gameObject.SetActive(true);
            armIKL.target = newGun.IKLTarget;
            armIKR.target = newGun.IKRTarget;
            gunHandlePose = new(gunAwayPose.localPosition, gunAwayPose.localRotation);
        }
        
        cam.fieldOfView = Mathf.SmoothDamp(cam.fieldOfView, fovTarget, ref vel2, gun.weight);
        bobSpeed = Mathf.SmoothDamp(bobSpeed, bobSpeedTarget, ref vel4, gun.weight);
        posBob = Mathf.SmoothDamp(posBob, posBobTarget, ref vel5, gun.weight);
        rotBob = Mathf.SmoothDamp(rotBob, rotBobTarget, ref vel6, gun.weight);
        gunHandlePose.position = Vector3.SmoothDamp(gunHandlePose.position, gunPoseTarget.position, ref velV1, poseTime);
        gunHandlePose.rotation = QuaternionSmoothDamp(gunHandlePose.rotation, gunPoseTarget.rotation, ref velQ1, poseTime);
        // Weapon bob
        Vector3 bobPosTarget = Vector3.zero;
        Quaternion bobRotTarget = Quaternion.identity;
        Quaternion lookSwayRot = Quaternion.AngleAxis(-LastInput.lookDelta.y * lookSway, Vector3.left) * Quaternion.AngleAxis(LastInput.lookDelta.x * lookSway, Vector3.down);
        if (LastInput.movement == Vector2.zero) { bobClock = 0; }
        else {
            bobClock += Time.deltaTime * bobSpeed;
            bobPosTarget = new Vector3(Mathf.Sin(bobClock), -Mathf.Abs(Mathf.Cos(bobClock)), 0) * posBob; // Sin and Cos for circular motion, abs value to simulate bouncing.
            bobRotTarget = Quaternion.Euler(new Vector3(-Mathf.Abs(Mathf.Sin(bobClock)), Mathf.Cos(bobClock), 0) * rotBob);
        }

        gunBobPose.position = Vector3.SmoothDamp(gunBobPose.position, bobPosTarget, ref velV2, 0.1f);
        gunBobPose.rotation = QuaternionSmoothDamp(gunBobPose.rotation, bobRotTarget * lookSwayRot, ref velQ2, 0.1f);

        // Weapon Recoil
        Vector2 appliedPosRecoil = currentPosRecoil * gun.rs.posSpeed * Time.deltaTime;
        Vector2 appliedRotRecoil = currentRotRecoil * gun.rs.rotSpeed * Time.deltaTime;
        Vector2 appliedCamRecoil = currentCamRecoil * gun.rs.camSpeed * Time.deltaTime;
        gunRecoilPose.position += new Vector3(0, appliedPosRecoil.y, appliedPosRecoil.x);
        gunRecoilPose.rotation *= Quaternion.Euler(appliedRotRecoil.y, appliedRotRecoil.x, 0);
        gunRecoilPose.position = Vector3.Slerp(gunRecoilPose.position, Vector3.zero, gun.rs.posRecovery * Time.deltaTime);
        gunRecoilPose.rotation = Quaternion.Slerp(gunRecoilPose.rotation, Quaternion.identity, gun.rs.rotRecovery * Time.deltaTime);
        locomotion.localLook += appliedCamRecoil;
        currentPosRecoil -= appliedPosRecoil;
        currentRotRecoil -= appliedRotRecoil;
        currentCamRecoil -= appliedCamRecoil;
        
        gun.transform.localPosition = gunHandlePose.position + gunRecoilPose.position + gunBobPose.position;
        gun.transform.localRotation = gunHandlePose.rotation * gunRecoilPose.rotation * gunBobPose.rotation;

        // IK
        armIKL.InvertKinematics();
        armIKR.InvertKinematics();
    }
    
    private static Quaternion QuaternionSmoothDamp(Quaternion rot, Quaternion target, ref Quaternion velocity, float time) {
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
