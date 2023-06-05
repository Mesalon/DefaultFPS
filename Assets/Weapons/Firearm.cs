using UnityEngine;
using System;
using System.Dynamic;
using Fusion;
using TMPro;
using UnityEngine.VFX;

public enum WeaponClass { Rifle, Pistol, }
public class Firearm : NetworkBehaviour {
    [Networked, HideInInspector] public NetworkBool TriggerState { get; set; }
    [Networked(OnChanged = nameof(FireFX))] private TickTimer FireTimer { get; set; }
    [Networked(OnChanged = nameof(ReloadFX))] private TickTimer ReloadTimer { get; set; }
    [Networked] NetworkBool DisconnectorState { get; set; }
    [Networked] public int Ammo { get; set; }
    [Networked] public int ReserveAmmo { get; set; }

    public Transform IKLTarget, IKRTarget, IKLPole, IKRPole;
    public Transform aimPoint;
    public Transform muzzle;
    public WeaponClass type;
    public float aimTime;
    public float aimingZoom;
    public float aimMoveSpeed;
    public float weight; // Affects handling time, smaller making the gun more snappy in it's movements.
    [SerializeField] ProjectileData projectile;
    [SerializeField] AudioClip fireSound;
    [SerializeField] AudioClip reloadSound;
    [SerializeField] GameObject muzzleFlash;
    [SerializeField] bool isFullAuto;
    [SerializeField] float cyclicTime;
    [SerializeField] float reloadTime;
    [SerializeField] int capacity;
    [SerializeField] int startAmmo;
    [Serializable] public class RecoilSettings {
        public float camSpeed, posSpeed, rotSpeed;
        public float posRecovery, rotRecovery;
        public Vector2 camRecoil, posRecoil, rotRecoil;
    } public RecoilSettings rs;
    
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
    }
    
    public override void FixedUpdateNetwork() {
        if (GetInput(out NetworkInputData input)) {
            if (TriggerState) { 
                if (FireTimer.ExpiredOrNotRunning(Runner) && ReloadTimer.ExpiredOrNotRunning(Runner) && !DisconnectorState && Ammo > 0) { // Fire
                    Ammo--;
                    FireTimer = TickTimer.CreateFromSeconds(Runner, cyclicTime);
                    ProjectileManager.inst.CreateProjectile(new(projectileIndex, Object.InputAuthority, input.muzzlePos, input.muzzleDir, Runner.Tick, 4, Runner));
                    if (!isFullAuto) { DisconnectorState = true; }
                    if (Runner.IsForward) {
                        owner.currentCamRecoil += rs.camRecoil;
                        owner.currentPosRecoil += rs.posRecoil;
                        owner.currentRotRecoil += rs.rotRecoil;
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
