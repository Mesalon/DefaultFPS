using UnityEngine;

public class GripAttachment : Attachment {
    [SerializeField] Transform grabPoint;
    [SerializeField] [Tooltip("Plus or minus, the effect of this attachment in percentage for each stat.")]
    RecoilSettings recoilMod;
    
    public override void Initalize(Firearm f) {
        f.IKLTarget = grabPoint;
        f.rMods.Add(recoilMod);
    }
}
