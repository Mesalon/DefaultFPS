using System;
using UnityEngine;
using Fusion.KCC;
using Fusion;

[OrderAfter(typeof(Character))]
public class Handling : NetworkBehaviour {
    public Firearm Gun => CurrentGunRole == WeaponRole.Primary ? Gun1 : Gun2;
    // I don't like having two OnChanged methods but I don't know how to derive the weapon being set in the OnChanged method
    [Networked(OnChanged = nameof(InitPrimary)), HideInInspector] public Firearm Gun1 { get; set; }
    [Networked(OnChanged = nameof(InitSecondary)), HideInInspector] public Firearm Gun2 { get; set; }
    [Networked] WeaponRole CurrentGunRole { get; set; }
    [Networked] WeaponRole NewGunRole { get; set; } // For switching weapons
    [Networked] NetworkInputData LastInput { get; set; }
    
    [HideInInspector] public Character character;
    [HideInInspector] public Vector2 currentCamRecoil, currentPosRecoil, currentRotRecoil;
    public TwoBoneIK armIKL, armIKR;
    public Transform awayPose;
    public float weaponWalkSpeed;
    [SerializeField] Transform riflePose, rifleSprintPose;
    [SerializeField] Transform pistolPose, pistolSprintPose;
    [SerializeField] Transform head;
    [SerializeField] Camera cam;
    [SerializeField] WeightMap poseMap;
    [SerializeField] WeightMap bobSpeedMap;
    [SerializeField] WeightMap moveSwayMap;
    [SerializeField] float posBob, rotBob;
    [SerializeField] float ADSMoveSpeed;
    [SerializeField] float moveSwayRecoveryBaseX = 1;
    [SerializeField] float sprintFOVX = 1;
    [SerializeField] float sprintBobSpeedX = 1;
    [SerializeField] float sprintPosBobX = 1;
    [SerializeField] float sprintRotBobX = 1;
    [SerializeField] float aimBobSpeedX = 1;
    [SerializeField] float aimPosBobX = 1;
    [SerializeField] float aimRotBobX = 1;
    [SerializeField] float aimMoveSwayX = 1;
    [SerializeField] float aimMoveSwayRecoveryX = 1;
    [SerializeField] float lookSwayFactor;
    [SerializeField] float lookSwayRecovery;
    private KCC kcc;
    private Locomotion locomotion;
    private Vector3 lastLook;
    private Vector3 gunMoveSway;
    private Vector3 moveSwayDebt, lastMove;
    private Pose gunHandlePose, gunRecoilPose, gunBobPose;
    private Pose gunLookSway;
    private float startFOV, bobSpeed, startPosBob, startRotBob;
    private float bobClock;
    private Vector3 velV1, velV2, velV3, velV4, velV5, velV6;
    private Quaternion velQ1, velQ2, velQ3, velQ4, velQ5;
    private float vel1, vel2, vel3, vel4, vel5, vel6;
    private float targetSwayFactor;

    private void Awake() {
        character = GetComponent<Character>();
        locomotion = GetComponent<Locomotion>();
        kcc = GetComponent<KCC>();
        startFOV = cam.fieldOfView;
        startPosBob = posBob;
        startRotBob = rotBob;
    }

    public override void Spawned() {
        Gun2.gameObject.SetActive(false);
        armIKL.target = Gun.stats.lHandTarget;
        armIKR.target = Gun.rHandTarget;
        gunHandlePose = new(awayPose.localPosition, awayPose.localRotation);
    }

    public override void FixedUpdateNetwork() {
        if (GetInput(out NetworkInputData input)) {
            if (input.buttons.WasPressed(LastInput.buttons, Buttons.Weapon1)) { NewGunRole = WeaponRole.Primary; }
            if (input.buttons.WasPressed(LastInput.buttons, Buttons.Weapon2)) { NewGunRole = WeaponRole.Secondary; }
            weaponWalkSpeed = input.buttons.IsSet(Buttons.Aim) ? ADSMoveSpeed * Gun.stats.walkSpeed : Gun.stats.walkSpeed; 
            
            if (Gun.Object.IsValid) {
                if (input.buttons.WasPressed(LastInput.buttons, Buttons.Reload)) { Gun.Reload(); }
                Gun.TriggerState = input.buttons.IsSet(Buttons.Fire);
            }

            LastInput = input;
        }
    }

    public override void Render() {
        Pose gunPoseTarget = Gun.length == WeaponLength.TwoHand ? new(riflePose.localPosition, riflePose.localRotation) : new(pistolPose.localPosition, pistolPose.localRotation);
        locomotion.currentSensitivity = locomotion.sensitivity;
        float poseSpeed = Remap(Gun.stats.weight, poseMap);
        float bobSpeedTarget = Remap(Gun.stats.weight, bobSpeedMap);
        float fovTarget = startFOV;
        float posBobTarget = startPosBob;
        float rotBobTarget = startRotBob;
        float moveSway = Remap(Gun.stats.weight, moveSwayMap);
        float moveSwayRecovery = moveSwayRecoveryBaseX;

        if (LastInput.buttons.IsSet(Buttons.Aim)) {
            gunPoseTarget = new(cam.transform.localPosition - Gun.transform.InverseTransformPoint(Gun.stats.aimPoint.position), Quaternion.identity);
            locomotion.currentSensitivity = locomotion.sensitivity / Gun.stats.aimingZoomX;
            fovTarget /= Gun.stats.aimingZoomX;
            bobSpeedTarget *= aimBobSpeedX;
            posBobTarget *= aimPosBobX;
            rotBobTarget *= aimRotBobX;
            moveSway *= aimMoveSwayX;
            moveSwayRecovery *= aimMoveSwayRecoveryX;
        }
        else if (locomotion.Pose == CharacterPose.Sprinting) {
            gunPoseTarget = Gun.length == WeaponLength.TwoHand ? new(rifleSprintPose.localPosition, rifleSprintPose.localRotation) : new(pistolSprintPose.localPosition, pistolSprintPose.localRotation);
            fovTarget *= sprintFOVX;
            bobSpeedTarget *= sprintBobSpeedX;
            posBobTarget *= sprintPosBobX;
            rotBobTarget *= sprintRotBobX;
        }
        
        if (CurrentGunRole != NewGunRole) {
            Firearm newGun = NewGunRole == WeaponRole.Primary ? Gun1 : Gun2;
            gunPoseTarget = new(awayPose.localPosition, awayPose.localRotation);
            poseSpeed = Remap(newGun.stats.weight, poseMap);
            if ((Gun.transform.position - awayPose.position).magnitude < 0.05f) { // Done switching
                gunHandlePose = new(awayPose.localPosition, awayPose.localRotation);
                Gun.gameObject.SetActive(false);
                CurrentGunRole = NewGunRole;
                
                newGun.gameObject.SetActive(true);
                armIKL.target = newGun.stats.lHandTarget;
                armIKR.target = newGun.rHandTarget;
                gunHandlePose = new(awayPose.localPosition, awayPose.localRotation);
            }
        }
        
        // Weapon bob
        bobSpeed = Mathf.SmoothDamp(bobSpeed, bobSpeedTarget, ref vel4, poseSpeed);
        posBob = Mathf.SmoothDamp(posBob, posBobTarget, ref vel5, poseSpeed);
        rotBob = Mathf.SmoothDamp(rotBob, rotBobTarget, ref vel6, poseSpeed);
        Vector3 bobPosTarget = Vector3.zero;
        Quaternion bobRotTarget = Quaternion.identity;
        if (LastInput.movement == Vector2.zero) { bobClock = 0; }
        else {
            bobClock += Time.deltaTime * bobSpeed;
            bobPosTarget = new Vector3(Mathf.Sin(bobClock), -Mathf.Abs(Mathf.Cos(bobClock)), 0) * posBob; // Sin and Cos for circular motion, abs value to simulate bouncing.
            bobRotTarget = Quaternion.Euler(new Vector3(-Mathf.Abs(Mathf.Sin(bobClock)), Mathf.Cos(bobClock), 0) * rotBob);
        }
        gunBobPose.position = Vector3.SmoothDamp(gunBobPose.position, bobPosTarget, ref velV2, 0.1f);
        gunBobPose.rotation = RotationSmoothDamp(gunBobPose.rotation, bobRotTarget, ref velQ2, 0.1f);
        
        // Move Sway
        Vector3 moveDelta = lastMove - new Vector3(LastInput.movement.x, kcc.FixedData.RealVelocity.y, LastInput.movement.y);
        moveSwayDebt += moveSway * new Vector3(-moveDelta.z - moveDelta.y * 0.5f, moveDelta.x, 0);
        moveSwayDebt = Vector3.MoveTowards(moveSwayDebt, Vector3.zero, Time.deltaTime * moveSwayRecovery);
        gunMoveSway = Vector3.SmoothDamp(gunMoveSway, moveSwayDebt, ref velV4, 0.15f);
        
        // Look Sway
        Vector3 lookSwayTarget = lookSwayFactor * -(head.rotation.eulerAngles - lastLook); 
        gunLookSway.rotation = RotationSmoothDamp(gunLookSway.rotation, Quaternion.Euler(lookSwayTarget), ref velQ3, 1 / lookSwayRecovery);
        

        // Weapon Recoil 
        Vector2 appliedPosRecoil = currentPosRecoil * Gun.Recoil.posSpeed * Time.deltaTime;
        Vector2 appliedRotRecoil = currentRotRecoil * Gun.Recoil.rotSpeed * Time.deltaTime;
        Vector2 appliedCamRecoil = currentCamRecoil * Gun.Recoil.camSpeed * Time.deltaTime;
        gunRecoilPose.position += new Vector3(0, 0, appliedPosRecoil.x);
        gunRecoilPose.rotation *= Quaternion.Euler(appliedRotRecoil.x, appliedRotRecoil.y, 0);
        locomotion.localLook += appliedCamRecoil;
        gunRecoilPose.position = Vector3.Slerp(gunRecoilPose.position, Vector3.zero, Gun.Recoil.posRecovery * Time.deltaTime);
        gunRecoilPose.rotation = Quaternion.Slerp(gunRecoilPose.rotation, Quaternion.identity, Gun.Recoil.rotRecovery * Time.deltaTime);
        currentPosRecoil -= appliedPosRecoil;
        currentRotRecoil -= appliedRotRecoil;
        currentCamRecoil -= appliedCamRecoil;

        cam.fieldOfView = Mathf.SmoothDamp(cam.fieldOfView, fovTarget, ref vel2, poseSpeed);
        gunHandlePose.position = Vector3.SmoothDamp(gunHandlePose.position, gunPoseTarget.position, ref velV1, poseSpeed);
        gunHandlePose.rotation = RotationSmoothDamp(gunHandlePose.rotation, gunPoseTarget.rotation, ref velQ1, poseSpeed);

        Gun.transform.localPosition = gunHandlePose.position + gunBobPose.position + gunRecoilPose.position;
        Gun.transform.localRotation = gunHandlePose.rotation * gunBobPose.rotation * Quaternion.Euler(gunMoveSway) * gunLookSway.rotation * gunRecoilPose.rotation;
        
        lastMove = new(LastInput.movement.x, kcc.FixedData.RealVelocity.y, LastInput.movement.y);
        lastLook = head.rotation.eulerAngles;
        
        armIKL.UpdateIK();
        armIKR.UpdateIK();
    }
    
    private static Quaternion RotationSmoothDamp(Quaternion rot, Quaternion target, ref Quaternion velocity, float time) {
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
    
    public float Remap(float n, WeightMap map) => Mathf.Lerp(map.newMin, map.newMax, Mathf.InverseLerp(map.min, map.max, n));
    [Serializable] public struct WeightMap {
        public float min, max;
        public float newMin, newMax;
    }
    
    private static void InitPrimary(Changed<Handling> changed) {
        Transform t = changed.Behaviour.Gun1.transform;
        Handling c = changed.Behaviour;
        t.SetParent(c.head);
        t.SetPositionAndRotation(c.awayPose.position, c.awayPose.rotation);
    }
    
    private static void InitSecondary(Changed<Handling> changed) {
        Transform t = changed.Behaviour.Gun2.transform;
        Handling c = changed.Behaviour;
        t.SetParent(c.head);
        t.SetPositionAndRotation(c.awayPose.position, c.awayPose.rotation);
    }
}