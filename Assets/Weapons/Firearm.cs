using UnityEngine.VFX;
using UnityEngine;
using System;
using Fusion;

public enum WeaponClass { Rifle, Pistol, }
public class Firearm : NetworkBehaviour {
    [Networked, HideInInspector] public NetworkBool TriggerState { get; set; }
    [Networked(OnChanged = nameof(FireFX))] private TickTimer FireTimer { get; set; }
    [Networked(OnChanged = nameof(ReloadFX))] private TickTimer ReloadTimer { get; set; }
    [Networked] NetworkBool DisconnectorState { get; set; }
    [Networked] public int Ammo { get; set; }
    [Networked] public int ReserveAmmo { get; set; }

    public Transform IKLTarget, IKRTarget;
    public Transform aimPoint;
    public Transform muzzle;
    public WeaponClass type;
    public float aimTime;
    public float aimingZoom;
    public float walkSpeed;
    public float aimingSpeedMult = 1.25f;
    public float runningSpeedMult = 0.75f;
    public float weight; // Affects handling time, smaller making the gun more snappy in it's movements.
    [SerializeField] ProjectileData projectile;
    [SerializeField] AudioClip fireSound;
    [SerializeField] AudioClip reloadSound;
    [SerializeField] GameObject muzzleFlash;
    [SerializeField] bool isFullAuto;
    [SerializeField] float cyclicRate;
    [SerializeField] float reloadTime;
    [SerializeField] int capacity;
    [SerializeField] int startAmmo;
    [Serializable] public class RecoilSettings {
        public float camSpeed, posSpeed, rotSpeed;
        public float posRecovery, rotRecovery;
        public float recoilY = 8;
        public float minRecoilX = -3, maxRecoilX = 3; 
        public float stability = 0.85f;
        public float posRecoilMult = 0.1f, rotRecoilMult = 1;
    } public RecoilSettings rs;
    public float recoilX;
    
    private int projectileIndex = -1;
    private Character owner;
    private new AudioSource audio;
    
    public static void FireFX(Changed<Firearm> changed) {
        changed.Behaviour.audio.PlayOneShot(changed.Behaviour.fireSound);
        VisualEffect fx = changed.Behaviour.muzzleFlash.GetComponent<VisualEffect>();
        fx.pause = false;
        fx.playRate = 0.01f;
        fx.Play();
    }
    public static void ReloadFX(Changed<Firearm> changed) {
        changed.Behaviour.audio.PlayOneShot(changed.Behaviour.reloadSound);
    }

    void Awake() {
        owner = transform.root.GetComponent<Character>();
        audio = GetComponent<AudioSource>();
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
        recoilX = (rs.minRecoilX + rs.maxRecoilX) / 2;
    }
    
    public override void FixedUpdateNetwork() {
        if (GetInput(out NetworkInputData input)) {
            if (TriggerState) { 
                if (FireTimer.ExpiredOrNotRunning(Runner) && ReloadTimer.ExpiredOrNotRunning(Runner) && !DisconnectorState && Ammo > 0) { // Fire
                    Ammo--;
                    FireTimer = TickTimer.CreateFromSeconds(Runner, 1 / (cyclicRate / 60));
                    ProjectileManager.inst.CreateProjectile(new(projectileIndex, Object.InputAuthority, input.muzzlePos, input.muzzleDir, Runner.Tick, 4, Runner));
                    if (!isFullAuto) { DisconnectorState = true; }
                    if (Runner.IsForward) {
                        Random.InitState(Runner.Tick);
                        recoilX = Mathf.Clamp(Mathf.Lerp(Random.Range(rs.minRecoilX, rs.maxRecoilX), recoilX, rs.stability), rs.minRecoilX, rs.maxRecoilX);
                        Vector2 recoil = new(recoilX, rs.recoilY);
                        owner.handling.currentCamRecoil += recoil;
                        owner.handling.currentPosRecoil -= new Vector2(rs.recoilY, 0) * rs.posRecoilMult;
                        owner.handling.currentRotRecoil -= recoil * rs.rotRecoilMult;
                    }
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
}
