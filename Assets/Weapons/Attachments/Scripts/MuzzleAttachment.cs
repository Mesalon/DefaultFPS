using UnityEngine;

public class MuzzleAttachment : Attachment {
    [SerializeField] Transform exit;
    [SerializeField] [Tooltip("Plus or minus, the effect of this attachment in percentage for each stat.")]
    RecoilSettings recoilMod;
    
    public override void Initalize(Firearm f) {
        f.muzzlePoint = exit;
        f.rMods.Add(recoilMod);
    }
}
