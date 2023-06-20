using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class AttachmentEditor : MonoBehaviour
{
    [SerializeField] TMP_Dropdown gripMenu;
    [SerializeField] TMP_Dropdown sightMenu;
    [SerializeField] TMP_Dropdown muzzleMenu;

    [SerializeField] List<string> gripOptions;
    [SerializeField] List<string> sightOptions;
    [SerializeField] List<string> muzzleOptions;

    public delegate void EquipWeapon(int attachmentIndex);
    public EquipWeapon EquipGripAttachments;
    public EquipWeapon EquipOpticAttachments;
    public EquipWeapon EquipMuzzleAttachments;
    Firearm primaryGun;
    Firearm secondaryGun;

    public TMP_Text selectedWeaponName;

    public bool isPrimary; //is the current weapon selected to be edited a primary

    public void specifyCurrentWeapon(bool primary) {
        isPrimary = primary;
    }

    public static void ClearPrimaryAttachments() {
        GameManager.inst.localPlayer.gun1Attachments.Clear();
    }

    public static void ClearSecondaryAttachments() {
        GameManager.inst.localPlayer.gun2Attachments.Clear();
    }

    private void Awake() {
        EquipGripAttachments += EquipGrip;
        EquipMuzzleAttachments += EquipMuzzle;
        EquipOpticAttachments += EquipOptic;
    }

    public void EquipGrip(int attachmentIndex) {
        if(attachmentIndex != -1) {
            if (isPrimary) {
                GameManager.inst.localPlayer.gun1Attachments.Add(primaryGun.compatibleGrips[attachmentIndex]);
            } else {
                GameManager.inst.localPlayer.gun2Attachments.Add(secondaryGun.compatibleGrips[attachmentIndex]);
            }
        } else {
            if (isPrimary) {
                foreach (Attachment attachment in GameManager.inst.localPlayer.gun1Attachments) {
                    if(attachment.GetType() == typeof(GripAttachment)) {
                        GameManager.inst.localPlayer.gun1Attachments.Remove(attachment);
                    }
                }
            } else {
                foreach (Attachment attachment in GameManager.inst.localPlayer.gun2Attachments) {
                    if (attachment.GetType() == typeof(GripAttachment)) {
                        GameManager.inst.localPlayer.gun2Attachments.Remove(attachment);
                    }
                }
            }

        }
    }

    public void EquipMuzzle(int attachmentIndex) {
        if (attachmentIndex != -1) {
            if (isPrimary) {
                GameManager.inst.localPlayer.gun1Attachments.Add(primaryGun.compatibleMuzzles[attachmentIndex]);
            } else {
                GameManager.inst.localPlayer.gun2Attachments.Add(secondaryGun.compatibleMuzzles[attachmentIndex]);
            }
        } else {
            if (isPrimary) {
                foreach (Attachment attachment in GameManager.inst.localPlayer.gun1Attachments) {
                    if (attachment.GetType() == typeof(MuzzleAttachment)) {
                        GameManager.inst.localPlayer.gun1Attachments.Remove(attachment);
                    }
                }
            } else {
                foreach (Attachment attachment in GameManager.inst.localPlayer.gun2Attachments) {
                    if (attachment.GetType() == typeof(MuzzleAttachment)) {
                        GameManager.inst.localPlayer.gun2Attachments.Remove(attachment);
                    }
                }
            }

        }
    }

    public void EquipOptic(int attachmentIndex) {
        if (attachmentIndex != -1) {
            if (isPrimary) {
                GameManager.inst.localPlayer.gun1Attachments.Add(primaryGun.compatibleOptics[attachmentIndex]);
            } else {
                GameManager.inst.localPlayer.gun2Attachments.Add(secondaryGun.compatibleOptics[attachmentIndex]);
            }
        } else {
            if (isPrimary) {
                foreach (Attachment attachment in GameManager.inst.localPlayer.gun1Attachments) {
                    if (attachment.GetType() == typeof(OpticAttachment)) {
                        GameManager.inst.localPlayer.gun1Attachments.Remove(attachment);
                    }
                }
            } else {
                foreach (Attachment attachment in GameManager.inst.localPlayer.gun2Attachments) {
                    if (attachment.GetType() == typeof(OpticAttachment)) {
                        GameManager.inst.localPlayer.gun2Attachments.Remove(attachment);
                    }
                }
            }

        }
    }

    private void OnEnable() {
        gripOptions.Clear();
        muzzleOptions.Clear();
        sightOptions.Clear();
        gripMenu.ClearOptions();
        sightMenu.ClearOptions();
        muzzleMenu.ClearOptions();
        gripOptions.Add("None");
        sightOptions.Add("None");
        muzzleOptions.Add("None");
        if (isPrimary) {
            GameManager.inst.localPlayer.gun1Attachments.Clear();
            primaryGun = GameManager.inst.gun1Library[GameManager.inst.localPlayer.gun1];
            selectedWeaponName.text = $"{primaryGun.name} Attachments:";
            foreach (GripAttachment grip in primaryGun.compatibleGrips) {
                gripOptions.Add(grip.name);
                gripMenu.onValueChanged.AddListener(index => EquipGripAttachments(index-1));
            }
            foreach (OpticAttachment optic in primaryGun.compatibleOptics) {
                sightOptions.Add(optic.name);
                sightMenu.onValueChanged.AddListener(index => EquipOpticAttachments(index - 1));
            }
            foreach (MuzzleAttachment muzzle in primaryGun.compatibleMuzzles) {
                muzzleOptions.Add(muzzle.name);
                muzzleMenu.onValueChanged.AddListener(index => EquipMuzzleAttachments(index - 1));
            }
        } 
        else {
            GameManager.inst.localPlayer.gun2Attachments.Clear();
            secondaryGun = GameManager.inst.gun2Library[GameManager.inst.localPlayer.gun2];
            selectedWeaponName.text = $"{secondaryGun.name} Attachments:";
            foreach (GripAttachment grip in secondaryGun.compatibleGrips) {
                gripOptions.Add(grip.name);
                gripMenu.onValueChanged.AddListener(index => EquipGripAttachments(index - 1));
            }
            foreach (OpticAttachment optic in secondaryGun.compatibleOptics) {
                sightOptions.Add(optic.name);
                sightMenu.onValueChanged.AddListener(index => EquipOpticAttachments(index - 1));
            }
            foreach (MuzzleAttachment muzzle in secondaryGun.compatibleMuzzles) {
                muzzleOptions.Add(muzzle.name);
                muzzleMenu.onValueChanged.AddListener(index => EquipMuzzleAttachments(index - 1));
            }
        }
        gripMenu.AddOptions(gripOptions);
        sightMenu.AddOptions(sightOptions);
        muzzleMenu.AddOptions(muzzleOptions);
    }
}
