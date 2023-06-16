using UnityEngine;

public class OpticAttachment : Attachment {
    [SerializeField] Transform aimPoint;
    [SerializeField] float zoom = 1.25f;

    public override void Initalize(Firearm f) {
        f.aimPoint = aimPoint;
        f.aimingZoom = zoom;
    }
}