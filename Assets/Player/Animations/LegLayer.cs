using Fusion.Animations;
using Fusion.KCC;
using UnityEngine;
using AnimationState = Fusion.Animations.AnimationState;

public class LegLayer : AnimationLayer {
    [SerializeField] KCC kcc;
    [SerializeField] float fallThreshold;

    public AnimationState moveState;
    public AnimationState jumpState;
    public AnimationState fallState;

    protected override void OnSpawned() {
        moveState.Activate(0);
    }

    protected override void OnFixedUpdate() {
        if (kcc.Data.HasJumped) {
            jumpState.Activate(0); 
            print("Jump on");
        }

        if (GetActiveState() == jumpState) {
            if (kcc.Data.IsGrounded) {
                moveState.Activate(0);
            }
        }
        
    }
}
