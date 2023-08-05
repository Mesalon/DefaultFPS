using System.Collections;
using System.Collections.Generic;
using Fusion.KCC;
using UnityEngine;

public class SprintKCCProcessor : KCCProcessor {
    public override EKCCStages GetValidStages(KCC kcc, KCCData data) {
        // Only SetKinematicSpeed stage is used, rest are filtered out and corresponding method calls will be skipped.
        return EKCCStages.SetKinematicSpeed;
    }

    public override void SetKinematicSpeed(KCC kcc, KCCData data){
        data.KinematicSpeed = data.speed;
    }
}
