using System.Collections.Generic;
using UnityEngine;

public class AttachmentController : MonoBehaviour {
    public List<AttachmentMount> mounts;

    public void Finalize(ref WeaponStats s, int[] attachmentIDs) {
        for (int i = 0; i < mounts.Count; i++) {
            if (attachmentIDs[i] != -1) {
                foreach(GameObject g in mounts[i].defaultVisuals) { g.SetActive(false); }
                Attachment attachment = Instantiate(GameManager.GetAttachment(attachmentIDs[i]), mounts[i].transform);
                attachment.Initalize(ref s);
            }
        }
    }
}