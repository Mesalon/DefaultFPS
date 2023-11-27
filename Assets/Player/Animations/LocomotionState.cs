using System;
using System.Collections;
using System.Collections.Generic;
using Fusion.Animations;
using Fusion.KCC;
using UnityEngine;
using UnityEngine.Serialization;

public class LocomotionState : BlendTreeState {
    [SerializeField] private Locomotion locomotion;
    [SerializeField] private KCC kcc;
    
    protected override Vector2 GetBlendPosition(bool interpolated) => kcc.transform.InverseTransformDirection(kcc.Data.RealVelocity).XZ0().normalized * (locomotion.Pose == CharacterPose.Sprinting ? 2 : 1);

    /*public override float GetSpeedMultiplier() => 1;
    
    protected override int GetSetID() => locomotion.Pose switch {
        CharacterPose.Walking => 0,
        CharacterPose.Crouching => 1,
        CharacterPose.Sliding => 2,
        _ => 0
    };*/
}
