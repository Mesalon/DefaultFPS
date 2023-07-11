using UnityEngine;

public class OpticAttachment : Attachment {
    [SerializeField] Transform aimPoint;
    [SerializeField] float zoom = 1.25f;

    public override void Initalize(ref WeaponStats s) {
        base.Initalize(ref s);
        s.aimPoint = aimPoint;
        s.aimingZoomX = zoom;
    }
}