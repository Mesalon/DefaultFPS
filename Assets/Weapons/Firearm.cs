using UnityEngine;
using System;
using System.Dynamic;
using Fusion;
using TMPro;
using UnityEngine.VFX;

public class Firearm : NetworkBehaviour {
    [Networked(OnChanged = nameof(FireFX))] private TickTimer FireTimer { get; set; }
    [Networked(OnChanged = nameof(ReloadFX))] private TickTimer ReloadTimer { get; set; }
    [Networked(OnChanged = nameof(AmmoChange))] int Ammo { get; set; }
    [Networked, HideInInspector] public NetworkBool TriggerState { get; set; }
    [Networked] private int ReserveAmmo { get; set; }
    [Networked] private NetworkBool DisconnectorState { get; set; }

    [Header("References")]
    public Transform muzzle;
    [SerializeField] private ProjectileData projectile;
    [SerializeField] private AudioClip fireSound;
    [SerializeField] private AudioClip reloadSound;
    [SerializeField] private GameObject muzzleFlash;
    private int projectileIndex = -1;
    [Header("Settings")]
    public Transform aimPoint;
    public float aimTime;
    public float aimingZoom;
    public float aimMoveSpeed;
    public float weight; // Affects handling time, smaller making the gun more snappy in it's movements.
    [SerializeField] private bool isFullAuto;
    [SerializeField] private float reloadTime;
    [SerializeField] private float cyclicTime;
    [SerializeField] private int capacity;
    [SerializeField] private int startAmmo;
    [SerializeField] private TMP_Text ammoCounter;
    [Serializable] public class RecoilSettings {
        public float camSpeed, posSpeed, rotSpeed;
        public float posRecovery, rotRecovery;
        public Vector2 camRecoil, posRecoil, rotRecoil;
    } public RecoilSettings rs;
    
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
    public static void AmmoChange(Changed<Firearm> changed) {
        changed.Behaviour.ammoCounter.text = $"{changed.Behaviour.Ammo} / {changed.Behaviour.ReserveAmmo}";
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
                    ProjectileManager.inst.CreateProjectile(new(projectileIndex, new(), input.muzzlePos, input.muzzleDir, Runner.Tick, 4, Runner));
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
