using System.Collections.Generic;
using FMOD.Studio;
using UnityEngine.Rendering;
using UnityEngine;
using Fusion.KCC;
using FMODUnity;
using Fusion;

public class Character : NetworkBehaviour {
    [Networked] public Player Player { get; set; }
    [HideInInspector, Networked(OnChanged = nameof(OnHealthChanged))] public float Health { get; set; }
    [Networked] NetworkInputData LastInput { get; set; }
    [Networked] DamageSource DmgSource { get; set; } // Most recent damage source
    [HideInInspector, Networked(OnChanged = nameof(OnAliveChanged))] public bool IsAlive { get; set; }
    public float maxHealth;
    [HideInInspector] public Handling handling;
    [SerializeField] Camera cam;
    [SerializeField] Ragdoll ragdollPF;
    [SerializeField] SpectatorCam deathCamPF;
    [SerializeField] SpectatorCam deathCam;
    [SerializeField] VolumeProfile normalProfile, spectatorProfile;
    [SerializeField] BodyHitbox[] bones;
    [SerializeField] GameObject visuals;
    [SerializeField] private float headshotDamageX;
    [SerializeField] private float limbDamageX;
    [SerializeField] private EventReference killSoumd;
    private Locomotion locomotion;
    private Controls controls;
    private UI UI;
    private Rigidbody rb;
    private KCC kcc;

    private void Awake() {
        controls = new();
        handling = GetComponent<Handling>();
        locomotion = GetComponent<Locomotion>();
        UI = GetComponent<UI>();
        rb = GetComponent<Rigidbody>();
        kcc = GetComponent<KCC>();
        
        for (int i = 0; i < bones.Length; i++) { bones[i].ID = i; }
    }
    private void OnEnable() { controls.Enable(); }
    private void OnDisable() { controls.Disable(); }

    private void OnInput(NetworkRunner runner, NetworkInput input) {
        NetworkInputData data = new();
        data.buttons.Set(Buttons.Run, controls.Player.Sprint.ReadValue<float>() == 1);
        data.buttons.Set(Buttons.Jump, controls.Player.Jump.ReadValue<float>() == 1);
        data.buttons.Set(Buttons.Fire, controls.Player.Shoot.ReadValue<float>() == 1);
        data.buttons.Set(Buttons.Aim, controls.Player.Aim.ReadValue<float>() == 1);
        data.buttons.Set(Buttons.Reload, controls.Player.Reload.ReadValue<float>() == 1);
        data.buttons.Set(Buttons.Kill, controls.Player.Kill.ReadValue<float>() == 1);
        data.buttons.Set(Buttons.Weapon1, controls.Player.Primary.ReadValue<float>() == 1);
        data.buttons.Set(Buttons.Weapon2, controls.Player.Secondary.ReadValue<float>() == 1);
        data.movement = controls.Player.Move.ReadValue<Vector2>();
        data.look = locomotion.look;
        if (Object && Object.IsValid) {
            data.muzzlePos = handling.Gun.muzzlePoint.position;
            data.muzzleDir = handling.Gun.muzzlePoint.forward;
        }
        input.Set(data);
    }
    
    public override void Spawned() {
        Player.Character = this;
        Health = maxHealth;
        IsAlive = true;
        
        if (Object.HasInputAuthority) {
            name = "Client";
            Cursor.lockState = CursorLockMode.Locked;
            GameManager.inst.SwitchCamera(cam, normalProfile);
            Runner.GetComponent<NetworkEvents>().OnInput.AddListener(OnInput);
        }
        else {
            name = "Proxy";
            cam.gameObject.SetActive(false);
        }
    }

    
    public override void FixedUpdateNetwork() {
        if (!IsAlive) {
            if (Player.RespawnTimer.Expired(Runner)) {
                if (Object.HasInputAuthority) {
                    print($"Expired. Deathcam: {deathCam}");
                    Destroy(deathCam);
                    GameManager.inst.SwitchCamera(GameManager.inst.mainCamera, GameManager.inst.menuProfile);
                    Cursor.lockState = CursorLockMode.None;
                }
                Runner.Despawn(Object);
            }
            return;
        }
        
        if (Health <= 0 && HasStateAuthority) {
            if (DmgSource.attacker.IsValid) {
                Character atk = GameManager.GetPlayer(DmgSource.attacker).Character;
                if (atk.Player.team == Team.Red) { GameManager.inst.redTeamKills++; }
                else { GameManager.inst.blueTeamKills++; }
                atk.Player.Kills++;
            }

            Player.Deaths++;
            IsAlive = false;
            Player.RespawnTimer = TickTimer.CreateFromSeconds(Runner, Player.respawnTime);
        }
        
        if (transform.position.y <= -50) { Damage(DamageSource.Suicide(Object.InputAuthority), 100); }
        if (GetInput(out NetworkInputData input)) {
            if (input.buttons.WasPressed(LastInput.buttons, Buttons.Kill)) { Damage(DamageSource.Suicide(Object.InputAuthority), 100); }
        }
    }

    public void Damage(DamageSource source, float amount, BodyPart part = BodyPart.Body) {
        if (HasStateAuthority) {
            DmgSource = source;
            amount = part switch {
                BodyPart.Head => amount * headshotDamageX,
                BodyPart.Limb => amount * limbDamageX,
                _ => amount,
            };
            Health = Mathf.Clamp(Health - amount, 0, maxHealth);
        }
    }
    
    public static void OnHealthChanged(Changed<Character> changed) {
        Character c = changed.Behaviour;
        c.UI.healthText.text = c.Health.ToString();
        if (c.Runner.LocalPlayer == c.DmgSource.attacker) {
            Character atk = GameManager.GetPlayer(c.DmgSource.attacker).Character;
            atk.UI.MarkHit(c.bones[c.DmgSource.limb].part == BodyPart.Head);
        }
    }

    public static void OnAliveChanged(Changed<Character> changed) {
        if (changed.Behaviour.IsAlive == false) {
            Character c = changed.Behaviour;
            Character atk = GameManager.GetPlayer(c.DmgSource.attacker).Character;
            
            c.locomotion.enabled = false;
            c.handling.enabled = false; 
            c.UI.enabled = false;
            c.rb.detectCollisions = false;
            c.kcc.enabled = false;
            c.visuals.SetActive(false);

            // Ragdoll
            Rigidbody[] rags = Instantiate(c.ragdollPF, c.transform).rags;
            for (int i = 0; i < c.bones.Length; i++) { rags[i].transform.localRotation = c.bones[i].transform.localRotation; }
            rags[c.DmgSource.limb].AddForceAtPosition(c.DmgSource.hitVector, c.DmgSource.hitPos);
            rags[0].GetComponent<Rigidbody>().AddForce(c.locomotion.kcc.Data.RealVelocity);
            
            // Points
            List<PointsIndicator> subIndicators = new();
            if(c.DmgSource.distance > 20) { subIndicators.Add(new(50, $"Distance bonus ({Mathf.Round(c.DmgSource.distance * 100f) / 100f})")); }
            PointsManager.inst.AwardPoints(atk.Player, new PointsIndicator(100, $"Killed <color=#eb4034>{c.Player.Name}</color>"), subIndicators);
                        
            if (c.Object.HasInputAuthority) {
                c.deathCam = Instantiate(c.deathCamPF, atk.cam.transform.position + atk.cam.transform.TransformDirection(new Vector3(0, 0, -2)), atk.cam.transform.rotation);
                c.deathCam.Initialize(atk);
                if(c.DmgSource.attacker != c.Object.InputAuthority) { c.deathCam.target =  atk.cam.transform; }
                GameManager.inst.SwitchCamera(c.deathCam.cam, c.spectatorProfile);
            }
            if (c.Runner.LocalPlayer == c.DmgSource.attacker) {
                RuntimeManager.PlayOneShot(c.killSoumd);
                EventInstance inst = RuntimeManager.CreateInstance(c.killSoumd);
                inst.set3DAttributes(c.transform.To3DAttributes());
                inst.setParameterByName("IsHeadshot", c.bones[c.DmgSource.limb].part == BodyPart.Head ? 1 : 0);
                inst.start();
                inst.release();
            }
        }
    }
}


// todo troll: In Unity disc on alt - Ask to help model a sine wave in Unity, pretending to not know what a sine wave is called and show a video of a stream of my piss moving up and down as an example