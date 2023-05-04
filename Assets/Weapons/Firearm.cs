using UnityEngine;
using System;
using Fusion;
using TMPro;

public class Firearm : NetworkBehaviour {
    [Networked(OnChanged = nameof(FireFX))]
    private TickTimer FireTimer { get; set; }

    [Networked(OnChanged = nameof(ReloadFX))]
    private TickTimer ReloadTimer { get; set; }

    [Networked(OnChanged = nameof(AmmoChange))] int Ammo { get; set; }

    [Networked, HideInInspector] public NetworkBool TriggerState { get; set; }
    [Networked] private int ReserveAmmo { get; set; }
    [Networked] private NetworkBool DisconnectorState { get; set; }

    [SerializeField] private Transform muzzle;
    [SerializeField] private AudioClip fireSound;
    [SerializeField] private AudioClip reloadSound;

    [SerializeField] private int projectileDataKey;
    
    [Header("Settings")]
    public Transform aimPoint;
    public float aimTime;
    public float aimingZoom;
    public float aimMoveSpeed;
    public float weight; // Affects handling time, smaller making the gun more snappy in it's movements.
    [Serializable] public class RecoilSettings {
        public float camSpeed, posSpeed, rotSpeed;
        public float posRecovery, rotRecovery;
        public Vector2 camRecoil, posRecoil, rotRecoil;
    }
    public RecoilSettings rs;
    [SerializeField] private bool isFullAuto;
    [SerializeField] private float reloadTime;
    [SerializeField] private float cyclicTime;
    [SerializeField] private int capacity;
    [SerializeField] private int startAmmo;
    [SerializeField] private TMP_Text ammoCount;
    private Player owner;
    private new AudioSource audio;

    public static void FireFX(Changed<Firearm> changed) {
        changed.Behaviour.audio.PlayOneShot(changed.Behaviour.fireSound);
    }
    public static void ReloadFX(Changed<Firearm> changed) {
        changed.Behaviour.audio.PlayOneShot(changed.Behaviour.reloadSound);
    }

    public static void AmmoChange(Changed<Firearm> changed) {
        changed.Behaviour.ammoCount.text = $"{changed.Behaviour.Ammo} / {changed.Behaviour.ReserveAmmo}";
    }

    void Awake() {
        owner = transform.root.GetComponent<Player>();
        audio = GetComponent<AudioSource>();
    }

    public override void Spawned() {
        Ammo = capacity;
        ReserveAmmo = startAmmo;
    }
    
    public override void FixedUpdateNetwork() {
        if (TriggerState) { 
            if (FireTimer.ExpiredOrNotRunning(Runner) && ReloadTimer.ExpiredOrNotRunning(Runner) && !DisconnectorState && Ammo > 0) { // Fire
                Ammo--;
                FireTimer = TickTimer.CreateFromSeconds(Runner, cyclicTime);
                ProjectileManager.inst.CreateProjectile(new(projectileDataKey, new(), muzzle.position, muzzle.forward, Runner.Tick, 4, Runner));
                if (!isFullAuto) { DisconnectorState = true; }
                if (Object.HasInputAuthority && Runner.IsForward) {
                    owner.currentCamRecoil += rs.camRecoil;
                    owner.currentPosRecoil += rs.posRecoil;
                    owner.currentRotRecoil += rs.rotRecoil;
                    print("Bang!");
                }
            }
        }
        else { DisconnectorState = false; }
    }

    public void Reload() {
        if (ReloadTimer.ExpiredOrNotRunning(Runner) && Ammo < capacity && ReserveAmmo > 0) {
            ReloadTimer = TickTimer.CreateFromSeconds(Runner, reloadTime);
            int toLoad = Mathf.Clamp(ReserveAmmo, 0, capacity - Ammo);
            ReserveAmmo -= toLoad;
            Ammo += toLoad;
        }
    }
}
