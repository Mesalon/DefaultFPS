using UnityEngine;

public class MuzzleAttachment : Attachment {
    public override void Initalize(ref WeaponStats s) {
        base.Initalize(ref s);
        s.recoilMods.Add(recoilMod);
    }
}
