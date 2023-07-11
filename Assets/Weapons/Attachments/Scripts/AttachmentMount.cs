using UnityEngine;
using System;
using System.Collections.Generic;

public class AttachmentMount : MonoBehaviour {
    public Attachment preview;
    public AttachmentType acceptedAttachments;
    public List<GameObject> defaultVisuals;


    public void Preview(int toAttachID, Material mat = null) {
        Attachment toAttach = GameManager.GetAttachment(toAttachID);
        if (preview) {
            Destroy(preview.gameObject);
            if (preview.id == toAttachID) {
                foreach(GameObject g in defaultVisuals) { g.SetActive(true); }
                preview = null;
                return;
            }
        }

        preview = Instantiate(toAttach, transform);
        foreach(GameObject g in defaultVisuals) { g.SetActive(false); }
        if (mat) {
            foreach (var mr in preview.GetComponentsInChildren<MeshRenderer>()) {
                mr.gameObject.layer = LayerMask.NameToLayer("UI");
                Material[] temp = mr.materials;
                for (int i = 0; i < temp.Length; i++) { temp[i] = mat; }
                mr.materials = temp;
            }
        }
    }
}

[Flags] public enum AttachmentType {
    Optic = 1 << 0,
    Grip = 1 << 1,
    Muzzle = 1 << 2,
    Accessory = 1 << 3,
}