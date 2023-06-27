using UnityEngine;
using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine.VFX;

public enum WeaponClass { Rifle, Pistol, }
public class Firearm : NetworkBehaviour {
    public RecoilSettings Recoil => rs.Mod(rMods);
    [Networked, HideInInspector] public NetworkBool TriggerState { get; set; }
    [Networked, HideInInspector] public int Ammo { get; set; }
    [Networked, HideInInspector] public int ReserveAmmo { get; set; }
    [Networked(OnChanged = nameof(OnFire))] TickTimer FireTimer { get; set; }
    [Networked(OnChanged = nameof(ReloadFX))] TickTimer ReloadTimer { get; set; }
    [Networked] NetworkBool DisconnectorState { get; set; }
    [Networked] float RecoilYaw { get; set; }

    [HideInInspector] public Transform IKLTarget;
    [HideInInspector] public Transform aimPoint;
    [HideInInspector] public Transform muzzlePoint;
    [HideInInspector] public Character owner;
    [HideInInspector] public List<RecoilSettings> rMods;
    [HideInInspector] public float aimingZoom;
    public Transform IKRTarget;
    public WeaponClass type;
    public float aimTime;
    public float walkSpeed;
    public float aimingSpeedMult = 0.75f;
    public float runningSpeedMult = 1.25f;
    public float weight; // Affects handling speed
    public VisualEffect muzzleFlash;
    public List<AttachmentMount> attachmentMounts;
    public List<GripAttachment> compatibleGrips;
    public List<OpticAttachment> compatibleOptics;
    public List<MuzzleAttachment> compatibleMuzzles;
    [SerializeField] ProjectileData projectile;
    [SerializeField] AudioClip fireSound;
    [SerializeField] AudioClip reloadSound;
    [SerializeField] RecoilSettings rs;
    [SerializeField] bool isFullAuto;
    [SerializeField] float cyclicRate;
    [SerializeField] float reloadTime;
    [SerializeField] int capacity;
    [SerializeField] int startAmmo;
    private int projectileIndex = -1;
    private new AudioSource audio;

    void Awake() {
        audio = GetComponent<AudioSource>();
        if(type == WeaponClass.Rifle) {
            foreach(Attachment attachment in Runner.GetPlayerObject(Object.InputAuthority).GetComponent<Player>().gun1Attachments) {
                if(attachment.GetType() == typeof(GripAttachment)) {
                    attachmentMounts[2].attachment = attachment;
                }
                if (attachment.GetType() == typeof(OpticAttachment)) {
                    attachmentMounts[0].attachment = attachment;
                }
                if (attachment.GetType() == typeof(MuzzleAttachment)) {
                    attachmentMounts[1].attachment = attachment;
                }
            }
        } else {
            foreach (Attachment attachment in Runner.GetPlayerObject(Object.InputAuthority).GetComponent<Player>().gun2Attachments) {
                if (attachment.GetType() == typeof(GripAttachment)) {
                    attachmentMounts[2].attachment = attachment;
                }
                if (attachment.GetType() == typeof(OpticAttachment)) {
                    attachmentMounts[0].attachment = attachment;
                }
                if (attachment.GetType() == typeof(MuzzleAttachment)) {
                    attachmentMounts[1].attachment = attachment;
                }
            }
        }
        foreach (AttachmentMount mount in attachmentMounts) {
            if (mount.attachment) {
                Instantiate(mount.attachment, mount.transform).Initalize(this); 
                if(mount.defaultAttachment) { mount.defaultAttachment.gameObject.SetActive(false); }
            }
            else if(mount.defaultAttachment) { mount.defaultAttachment.Initalize(this);}
        }
    }

    private void Start() {
        ProjectileData[] library = ProjectileManager.inst.projectileLibrary;
        for (int i = 0; i < library.Length; i++) {
            if (library[i].name == projectile.name) {
                projectileIndex = i;
                break;
            }
        }
    }

    public override void Spawned() {
        Ammo = capacity;
        ReserveAmmo = startAmmo;
        RecoilYaw = (Recoil.minRecoilX + Recoil.maxRecoilX) / 2;
    }

    public override void FixedUpdateNetwork() {
        if (GetInput(out NetworkInputData input)) {
            if (TriggerState) { 
                if (FireTimer.ExpiredOrNotRunning(Runner) && ReloadTimer.ExpiredOrNotRunning(Runner) && !DisconnectorState && Ammo > 0) { // Fire
                    Ammo--;
                    if (!isFullAuto) { DisconnectorState = true; }
                    UnityEngine.Random.InitState(Runner.Tick);
                    RecoilYaw = Mathf.Clamp(Mathf.Lerp(UnityEngine.Random.Range(Recoil.minRecoilX, Recoil.maxRecoilX), RecoilYaw, Recoil.stability), Recoil.minRecoilX, Recoil.maxRecoilX);
                    FireTimer = TickTimer.CreateFromSeconds(Runner, 1 / (cyclicRate / 60));
                    ProjectileManager.inst.CreateProjectile(new(projectileIndex, Object.InputAuthority, input.muzzlePos, input.muzzleDir, Runner.Tick, 4, Runner));
                }
            }
            else { DisconnectorState = false; }
        }
    }

    public void Reload() {
        if (ReloadTimer.ExpiredOrNotRunning(Runner) && Ammo < capacity && ReserveAmmo > 0) {
            ReloadTimer = TickTimer.CreateFromSeconds(Runner, reloadTime);
            int toLoad = Mathf.Clamp(ReserveAmmo, 0, capacity - Ammo);
            ReserveAmmo -= toLoad;
            Ammo += toLoad;
        }
    }
    
    public static void FireFX(Changed<Firearm> changed) {
        changed.Behaviour.audio.PlayOneShot(changed.Behaviour.fireSound);
        VisualEffect fx = changed.Behaviour.muzzleFlash;
        Vector2 finalRecoil = new(-f.Recoil.recoilY, f.RecoilYaw);
        f.owner.handling.currentCamRecoil += finalRecoil;
        f.owner.handling.currentPosRecoil += finalRecoil * f.Recoil.posRecoilMult;
        f.owner.handling.currentRotRecoil += finalRecoil * f.Recoil.rotRecoilMult;
        fx.pause = false;
        fx.playRate = 0.01f;
        fx.Play();
    }
    
    public static void ReloadFX(Changed<Firearm> changed) {
        changed.Behaviour.audio.PlayOneShot(changed.Behaviour.reloadSound);
    }
}

[Serializable] public struct RecoilSettings {
    public float camSpeed, posSpeed, rotSpeed;
    public float posRecovery, rotRecovery;
    public float recoilY, minRecoilX, maxRecoilX;
    public float stability;
    public float posRecoilMult, rotRecoilMult;
    
    /// <returns>A modified recoil using a collection of modifiers</returns>
    public RecoilSettings Mod(List<RecoilSettings> modifiers) {
        RecoilSettings final = this;
        foreach (RecoilSettings m in modifiers) {
            final.camSpeed += final.camSpeed * m.camSpeed;
            final.posSpeed += final.posSpeed * m.posSpeed;
            final.rotSpeed += final.rotSpeed * m.rotSpeed;
            final.recoilY += final.recoilY * m.recoilY;
            final.minRecoilX += final.minRecoilX * m.minRecoilX;
            final.maxRecoilX += final.maxRecoilX * m.maxRecoilX;
            final.stability += final.stability * m.stability;
            final.posRecoilMult += final.posRecoilMult * m.posRecoilMult;
            final.rotRecoilMult += final.rotRecoilMult * m.rotRecoilMult;
        }
        return final;
    }
}