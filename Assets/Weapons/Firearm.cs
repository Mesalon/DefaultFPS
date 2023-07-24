using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using UnityEngine.Serialization;
using UnityEngine.VFX;

public enum WeaponRole { Primary, Secondary }
public enum WeaponClass { AssaultRifle, Carbine, SubmachineGun, Pistol, }
public enum WeaponLength { OneHand, TwoHand }

[OrderBefore(typeof(Handling))]
public class Firearm : NetworkBehaviour {
    public RecoilStats Recoil => baseRecoil.Mod(stats.recoilMods);
    public List<Attachment> Attachments {
        get {
            List<Attachment> attachments = new();
            for (int i = 0; i < attachmentCont.mounts.Count; i++) {
                if(Configuration.Attachments[i] != -1) { attachments.Add(GameManager.GetAttachment(Configuration.Attachments[i])); }
            }
            return attachments;
        }
    }

    [Networked, HideInInspector] public Character Owner { get; set; }
    [Networked, HideInInspector] public WeaponConfiguration Configuration { get; set; }
    [Networked, HideInInspector] public NetworkBool TriggerState { get; set; }
    [Networked, HideInInspector] public int Ammo { get; set; }
    [Networked, HideInInspector] public int ReserveAmmo { get; set; }
    [Networked(OnChanged = nameof(OnFire))] private TickTimer FireTimer { get; set; }
    [Networked(OnChanged = nameof(ReloadFX))] private TickTimer ReloadTimer { get; set; }
    [Networked] private NetworkBool DisconnectorState { get; set; }
    [Networked] private float RecoilYaw { get; set; }
    
    public WeaponStats stats;
    public Transform muzzlePoint;
    public VisualEffect muzzleFlash;
    public Transform rHandTarget;
    public GameObject visuals;
    public WeaponRole role;
    public WeaponLength length;

    [SerializeField] AudioClip fireSound;
    [SerializeField] AudioClip reloadSound;
    public RecoilStats baseRecoil;
    private AttachmentController attachmentCont;
    private int projectileIndex = -1;
    private new AudioSource audio;

    void Awake() {
        attachmentCont = visuals.GetComponent<AttachmentController>();
        audio = GetComponent<AudioSource>();
    }

    private void Start() {
        ProjectileData[] library = ProjectileManager.inst.projectileLibrary;
        for (int i = 0; i < library.Length; i++) {
            if (library[i].name == stats.projectile.name) {
                projectileIndex = i;
                break;
            }
        }
    }

    public override void Spawned() {
        Ammo = stats.capacity;
        ReserveAmmo = stats.startAmmo;
        RecoilYaw = (Recoil.minRecoilX + Recoil.maxRecoilX) / 2;
        attachmentCont.Finalize(ref stats, Configuration.Attachments.ToArray());
    }

    public override void FixedUpdateNetwork() {
        if (GetInput(out NetworkInputData input)) {
            if (TriggerState) {
                if (FireTimer.ExpiredOrNotRunning(Runner) && ReloadTimer.ExpiredOrNotRunning(Runner) && !DisconnectorState && Ammo > 0) { // Fire
                    Ammo--;
                    if (!stats.isFullAuto) { DisconnectorState = true; }
                    UnityEngine.Random.InitState(Runner.Tick);
                    RecoilYaw = Mathf.Clamp(Mathf.Lerp(UnityEngine.Random.Range(Recoil.minRecoilX, Recoil.maxRecoilX), RecoilYaw, Recoil.stability), Recoil.minRecoilX, Recoil.maxRecoilX);
                    FireTimer = TickTimer.CreateFromSeconds(Runner, 1 / (stats.cyclicRate / 60));
                    ProjectileManager.inst.CreateProjectile(new(projectileIndex, Object.InputAuthority, input.muzzlePos, input.muzzleDir, Runner.Tick, 4, Runner));
                }
            }
            else { DisconnectorState = false; }
        }
    }

    public void Reload() {
        if (ReloadTimer.ExpiredOrNotRunning(Runner) && Ammo < stats.capacity && ReserveAmmo > 0) {
            ReloadTimer = TickTimer.CreateFromSeconds(Runner, stats.reloadTime);
            int toLoad = Mathf.Clamp(ReserveAmmo, 0, stats.capacity - Ammo);
            ReserveAmmo -= toLoad;
            Ammo += toLoad;
        }
    }
    
    public static void OnFire(Changed<Firearm> changed) {
        Firearm f = changed.Behaviour;
        f.audio.PlayOneShot(changed.Behaviour.fireSound);
        f.muzzleFlash.pause = false;
        f.muzzleFlash.playRate = 0.01f;
        f.muzzleFlash.Play();
        
        Vector2 finalRecoil = new(-f.Recoil.recoilY, f.RecoilYaw);
        f.Owner.handling.currentCamRecoil += finalRecoil;
        f.Owner.handling.currentPosRecoil += finalRecoil * f.Recoil.posRecoilMult;
        f.Owner.handling.currentRotRecoil += finalRecoil * f.Recoil.rotRecoilMult;
    }
    
    public static void ReloadFX(Changed<Firearm> changed) {
        changed.Behaviour.audio.PlayOneShot(changed.Behaviour.reloadSound);
    }
}

[Serializable] public struct WeaponStats {
    public string name;
    public ProjectileData projectile;
    public Transform lHandTarget;
    public Transform aimPoint;
    public List<RecoilStats> recoilMods;
    public WeaponClass classification;
    public float walkSpeed;
    public bool isFullAuto;
    public float cyclicRate;
    public float reloadTime;
    public int capacity;
    public int startAmmo;
    public float aimingZoomX;
    public float weight; // Affects handling speed
}

[Serializable] public struct RecoilStats {
    public float camSpeed, posSpeed, rotSpeed;
    public float posRecovery, rotRecovery;
    public float recoilY, minRecoilX, maxRecoilX;
    public float stability;
    public float posRecoilMult, rotRecoilMult;

    /// <returns>A modifies list of stats using a collection of modifiers</returns>
    public RecoilStats Mod(List<RecoilStats> modifiers) {
        RecoilStats final = this;
        foreach (RecoilStats m in modifiers) {
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