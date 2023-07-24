using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class PlayerSetup : MonoBehaviour {
    [HideInInspector] public WeaponOption[] lastWeapons;
    [SerializeField] Button primariesButton;
    [SerializeField] Button secondariesButton;
    [SerializeField] private Transform weaponOptionsContainer;
    [SerializeField] Transform primariesContainer;
    [SerializeField] Transform secondariesContainer;
    [SerializeField] WeaponOption weaponOptionPF;
    [Header("Weapon Preview")] 
    [SerializeField] Material wireframe;
    [SerializeField] Transform previewHolder;
    [SerializeField] TextMeshProUGUI weaponName;
    [FormerlySerializedAs("stats")] [SerializeField] TextMeshProUGUI statText;
    [Header("Attachments")]
    [SerializeField] AttachmentMountBubble attachmentMountBubblePf;
    [SerializeField] AttachmentOption attachmentOptionPF;
    [SerializeField] Transform attachmentsContainer;
    private AttachmentMountBubble selectedMountBubble;
    private List<AttachmentOption> attachmentOptions = new();
    private List<GameObject> attachmentMountBubbles = new();
    private WeaponOption selectedOption;
    private Camera cam;
    private bool showingMounts;
    
    private void Start() {
        lastWeapons = new WeaponOption[2];
        cam = GetComponent<Camera>();
        for (int i = 0; i < GameManager.gunLibrary.Count; i++) {
            Firearm gun = GameManager.GetWeapon(i);

            // Create and frame weapon preview
            GameObject preview = Instantiate(gun.visuals, previewHolder);
            Bounds visualBounds = new Bounds(preview.transform.position, Vector3.zero);
            foreach (var mr in preview.GetComponentsInChildren<MeshRenderer>()) {
                mr.gameObject.layer = LayerMask.NameToLayer("UI");
                visualBounds.Encapsulate(mr.bounds);
                Material[] temp = mr.materials; // Change all mats to be wireframe
                for (int ii = 0; ii < temp.Length; ii++) { temp[ii] = wireframe; }
                mr.materials = temp;
            }
            Vector3 offset = preview.transform.position - visualBounds.center;
            preview.transform.position = previewHolder.transform.position + offset;
            preview.SetActive(false);
            
            // Initialize weapon option and attachments for it
            WeaponOption weaponOption = Instantiate(weaponOptionPF, gun.role == WeaponRole.Primary ? primariesContainer : secondariesContainer);
            weaponOption.Init(gun.name, i);
            weaponOption.preview = preview;
            weaponOption.button.onClick.AddListener(() => { SelectWeapon(weaponOption); });
            
            if (preview.TryGetComponent(out AttachmentController controller)) {
                foreach (AttachmentMount mount in controller.mounts) {
                    AttachmentMountBubble mountBubble = Instantiate(attachmentMountBubblePf, mount.transform);
                    mountBubble.mount = mount;
                    mountBubble.button.onClick.AddListener(() => { SelectMount(mountBubble); });
                    mountBubble.gameObject.SetActive(false);
                    attachmentMountBubbles.Add(mountBubble.gameObject);
                    weaponOption.mountOptions.Add(mountBubble);
                }
            }

            // Save the first primaries and secondaries to be added as the default one when you change tabs
            if (!lastWeapons[0] && gun.role == WeaponRole.Primary) { lastWeapons[0] = weaponOption; }
            if (!lastWeapons[1] && gun.role == WeaponRole.Secondary) { lastWeapons[1] = weaponOption; }
            if (i == 0) { SelectWeapon(weaponOption); }
        }

        for (int i = 0; i < GameManager.attachmentLibrary.Count; i++) {
            Attachment attachment = GameManager.GetAttachment(i);
            AttachmentOption attachmentOption = Instantiate(attachmentOptionPF, attachmentsContainer);
            attachmentOption.Init(attachment.name, i);
            attachmentOption.button.onClick.AddListener(() => { SelectAttachment(attachmentOption); });
            attachmentOptions.Add(attachmentOption);
        }
        
        primariesButton.onClick.AddListener(() => { SelectWeapon(lastWeapons[0]); });
        secondariesButton.onClick.AddListener(() => { SelectWeapon(lastWeapons[1]); });
    }

    private void LateUpdate() {
        foreach (AttachmentMountBubble option in selectedOption.mountOptions) {
            option.transform.position = cam.WorldToScreenPoint(option.mount.transform.position);
            option.transform.rotation = Quaternion.identity;
        }
    }

    private void SelectWeapon(WeaponOption option) {
        lastWeapons[GameManager.GetWeapon(option.id).role == WeaponRole.Primary ? 0 : 1] = option;
        if (selectedOption) { selectedOption.preview.SetActive(false); }
        option.preview.SetActive(true);
        selectedOption = option;
        UpdateStats();
    }

    private void SelectMount(AttachmentMountBubble bubble) {
        selectedMountBubble = bubble;
        foreach (AttachmentOption option in attachmentOptions) { // Filter attachments by accepted types
            option.gameObject.SetActive(bubble.mount.acceptedAttachments.HasFlag(GameManager.GetAttachment(option.id).type));
        }
    }

    private void SelectAttachment(AttachmentOption option) {
        AttachmentMount mount = selectedMountBubble.mount;
        mount.Preview(option.id, wireframe);
        selectedMountBubble.isOccupiedText.text = $"<color=#{(mount.preview ? "64ff64>[-]" : "ff6464>[+]")}</color>";
        UpdateStats();
    }

    private void UpdateStats() {
        Firearm gun = GameManager.GetWeapon(selectedOption.id);
        WeaponStats stats = gun.stats; 
        
        // todo: Use instances of Firearm instead of this dogshit.
        // Gotta make a phantom copy (Phantom forces reference) cause some of these are fucking refereces and attachments will modify the prefab if I don't
        ProjectileData oldProjectile = stats.projectile;
        stats.projectile = ScriptableObject.CreateInstance<ProjectileData>();
        stats.projectile.name = oldProjectile.name;
        stats.projectile.damage = oldProjectile.damage;
        stats.projectile.speed = oldProjectile.speed;
        stats.lHandTarget = null;
        stats.aimPoint = null;
        stats.recoilMods = new List<RecoilStats>(stats.recoilMods);

        weaponName.text = stats.name;
        foreach (AttachmentMountBubble mountBubble in selectedOption.mountOptions) {
            if (mountBubble.mount.preview) {mountBubble.mount.preview.Initalize(ref stats); }
        }
        RecoilStats recoil = gun.baseRecoil.Mod(stats.recoilMods);
        
        // TODO: LITERALLY FUCK THIS CODE
        bool[] modifiedStatChecks = {
            stats.classification != gun.stats.classification,
            stats.cyclicRate != gun.stats.cyclicRate,
            stats.isFullAuto != gun.stats.isFullAuto,
            stats.capacity != gun.stats.capacity,
            false,
            stats.weight != gun.stats.weight,
            stats.reloadTime != gun.stats.reloadTime,
            stats.aimingZoomX != gun.stats.aimingZoomX,
            false,
            stats.projectile.name != gun.stats.projectile.name,
            stats.projectile.damage != gun.stats.projectile.damage,
            stats.projectile.speed != gun.stats.projectile.speed,
            false,
            false,
            false,
            recoil.stability != gun.baseRecoil.stability,
            recoil.recoilY != gun.baseRecoil.recoilY,
            recoil.minRecoilX != gun.baseRecoil.minRecoilX,
            recoil.maxRecoilX != gun.baseRecoil.maxRecoilX,
            recoil.posRecoilMult != gun.baseRecoil.posRecoilMult,
            recoil.rotRecoilMult != gun.baseRecoil.rotRecoilMult,
            recoil.stability != gun.baseRecoil.stability,
            recoil.camSpeed != gun.baseRecoil.camSpeed,
            recoil.posSpeed != gun.baseRecoil.posSpeed,
            recoil.rotSpeed != gun.baseRecoil.rotSpeed,
            recoil.posRecovery != gun.baseRecoil.posRecovery,
            recoil.rotRecovery != gun.baseRecoil.rotRecovery,
        };

        string[] statText = {
            $"{Regex.Replace(stats.classification.ToString(), "(\\B[A-Z])", " $1")}", // Don't ask me what the fuck this does
            $"{stats.cyclicRate} RPM",
            $"{(stats.isFullAuto ? "Auto" : "Semi")}",
            $"{stats.capacity} / {stats.startAmmo}",
            $"\n",
            $"{stats.weight} kg",
            $"{stats.reloadTime}s",
            $"{stats.aimingZoomX}x",
            $"\n",
            $"{stats.projectile.name.Replace("_", ".")}",
            $"{stats.projectile.damage} HP",
            $"{stats.projectile.speed} m/s",
            $"501 N/I",
            $"501 N/I",
            $"\n",
            $"{recoil.stability * 100}%",
            $"{recoil.recoilY}",
            $"{recoil.minRecoilX}",
            $"{recoil.maxRecoilX}",
            $"{recoil.posRecoilMult * 100}",
            $"{recoil.rotRecoilMult}",
            $"{recoil.camSpeed}",
            $"{recoil.posSpeed}",
            $"{recoil.rotSpeed}",
            $"{recoil.posRecovery}",
            $"{recoil.rotRecovery}"
        };

        for (int i = 0; i < statText.Length; i++) {
            if (modifiedStatChecks[i]) { statText[i] = $"<color=yellow>{statText[i]}</color>"; }
        }

        this.statText.text = "\n" + string.Join("\n", statText);
    }
    
    public void ShowAttachments(bool explicitDisable) {
        if (explicitDisable) { showingMounts = false; }
        else { showingMounts = !showingMounts; }
        weaponOptionsContainer.gameObject.SetActive(!showingMounts);
        attachmentsContainer.gameObject.SetActive(showingMounts);
        foreach (GameObject option in attachmentMountBubbles) { option.SetActive(showingMounts); }
        foreach (AttachmentOption option in attachmentOptions) { option.gameObject.SetActive(false); }
        print($"Showing: {showingMounts}, explicit: {explicitDisable}");

    }
}
