using UnityEngine;

public class Attachment : MonoBehaviour {
    public AttachmentType type;
    public float weight;
    [HideInInspector] public int id;
    [SerializeField] [Tooltip("Plus or minus, the effect of this attachment in percentage for each stat.")]
    public RecoilStats recoilMod;

    public virtual void Initalize(ref WeaponStats s) {
        s.weight += weight;
    }
}
