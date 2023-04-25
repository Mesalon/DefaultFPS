using UnityEngine;
using System;
using Fusion;

public class Firearm : NetworkBehaviour {
    [Networked(OnChanged = nameof(FireFX))]
    private TickTimer FireTimer { get; set; }

    [Networked(OnChanged = nameof(ReloadFX))]
    private TickTimer ReloadTimer { get; set; }

    [Networked(OnChanged = nameof(AmmoChange))]
    private int Ammo { get; set; }

    [Networked] [HideInInspector] public NetworkBool TriggerState { get; set; }
    [Networked] private int ReserveAmmo { get; set; }
    [Networked] private NetworkBool DisconnectorState { get; set; }

    [SerializeField] private Transform muzzle;
    [SerializeField] private AudioClip fireSound;
    [SerializeField] private AudioClip reloadSound;

    [SerializeField] private ProjectileData boolet;
    [SerializeField] private GameObject tracer;
    
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
    private Player owner;
    private new AudioSource audio;

    public static void FireFX(Changed<Firearm> changed) {
        changed.Behaviour.audio.PlayOneShot(changed.Behaviour.fireSound);
    }
    public static void ReloadFX(Changed<Firearm> changed) {
        changed.Behaviour.audio.PlayOneShot(changed.Behaviour.reloadSound);
    }

    public static void AmmoChange(Changed<Firearm> changed) {
        /*print($"{changed.Behaviour.Ammo} / {changed.Behaviour.capacity}");*/
    }

    void Awake() {
        owner = transform.root.GetComponent<Player>();
        audio = GetComponent<AudioSource>();
        
        ProjectileManager.PoolTracers(tracer, 30);
    }

    public override void Spawned() {
        Ammo = capacity;
        ReserveAmmo = startAmmo;
    }

    public override void FixedUpdateNetwork() {
        print(FireTimer.RemainingTime(Runner));
        if (TriggerState && FireTimer.ExpiredOrNotRunning(Runner) && ReloadTimer.ExpiredOrNotRunning(Runner) && !DisconnectorState && Ammo > 0) { // Fire
            if (!isFullAuto) { DisconnectorState = true; }
            if (Object.HasInputAuthority) {
                FireTimer = TickTimer.CreateFromSeconds(Runner, cyclicTime);
                owner.currentCamRecoil += rs.camRecoil;
                owner.currentPosRecoil += rs.posRecoil;
                owner.currentRotRecoil += rs.rotRecoil;
                Ammo--;
            }
            ProjectileManager.CreateProjectile(new(Runner, boolet, muzzle.position, muzzle.forward));
        }
    }

    public void Reload() {
        if (ReloadTimer.ExpiredOrNotRunning(Runner) && Ammo < capacity && ReserveAmmo > 0) {
            print("Reloading . . .");
            ReloadTimer = TickTimer.CreateFromSeconds(Runner, reloadTime);
            int toLoad = Mathf.Clamp(ReserveAmmo, 0, capacity - Ammo);
            ReserveAmmo -= toLoad;
            Ammo += toLoad;
        }
    }
}
