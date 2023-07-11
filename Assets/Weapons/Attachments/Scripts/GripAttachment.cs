using UnityEngine;

public class GripAttachment : Attachment {
    [SerializeField] Transform grabPoint;

    public override void Initalize(ref WeaponStats s) {
        base.Initalize(ref s);
        s.lHandTarget = grabPoint;
        s.recoilMods.Add(recoilMod);
    }
}
