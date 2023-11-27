using UnityEngine;
using Fusion;
using Fusion.KCC;

public sealed class SlideKCCP : NetworkKCCProcessor {
    [Networked] float RemainingTime { set; get; }
    public override float Priority => 2001;
    [SerializeField] Locomotion locomotion;
    [SerializeField] GroundKCCProcessor groundP;
    [SerializeField] private float inputResponsivity = 1;
    [SerializeField] private float slideSpeed;
    [SerializeField] private float friction;
    [SerializeField] float slideDuration = 1;
    private float startProportionalKinematicFriction;
    private float startInputResponsivity;
    private float startSpeed;

    public override void OnEnter(KCC kcc, KCCData data) {
        if (!kcc.IsInFixedUpdate) { return; }
        RemainingTime = slideDuration;
        startProportionalKinematicFriction = groundP.proportionalKinematicFriction;
        startInputResponsivity = groundP.inputResponsivity;
        startSpeed = groundP.KinematicSpeed;
        kcc.AddExternalForce(kcc.transform.forward * slideSpeed);
        
        locomotion.Pose = CharacterPose.Sliding;
    }

    public override void SetKinematicDirection(KCC kcc, KCCData data) {
        groundP.inputResponsivity = inputResponsivity;
    }

    public override void SetKinematicVelocity(KCC kcc, KCCData data) {
        groundP.proportionalKinematicFriction = friction;
    }

    public override void OnStay(KCC kcc, KCCData data) {
        if (kcc.IsInFixedUpdate) {
            RemainingTime -= data.DeltaTime;
            if (RemainingTime <= 0.0f) { kcc.RemoveModifier(this); }
        }
    }

    public override void OnExit(KCC kcc, KCCData data) {
        groundP.KinematicSpeed = startSpeed;
        groundP.inputResponsivity = startInputResponsivity;
        groundP.proportionalKinematicFriction = startProportionalKinematicFriction;
        locomotion.Pose = CharacterPose.Crouching;
    }
}