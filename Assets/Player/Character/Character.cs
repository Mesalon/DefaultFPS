using System;
using UnityEngine;
using Fusion;
using Fusion.KCC;
using UnityEngine.Rendering;

public class Character : NetworkBehaviour {
    [Networked] public Player Player { get; set; }
    [HideInInspector, Networked(OnChanged = nameof(OnHealthChanged))] public float Health { get; set; }
    [Networked] NetworkInputData LastInput { get; set; }
    [Networked] DamageSource dmgSource { get; set; } // Most recent damage source
    [HideInInspector, Networked(OnChanged = nameof(OnAliveChanged))] public bool IsAlive { get; set; }
    public float maxHealth;
    [HideInInspector] public Handling handling;
    [SerializeField] Camera cam;
    [SerializeField] GameObject ragdollPF;
    [SerializeField] SpectatorCam deathCamPF;
    [SerializeField] SpectatorCam deathCam;
    [SerializeField] VolumeProfile normalProfile, spectatorProfile;
    [SerializeField] GameObject ragdoll;
    [SerializeField] Transform armature;
    [SerializeField] Transform[] bones;
    [SerializeField] GameObject visuals;
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
        data.lookDelta = locomotion.localLook;
        if (Object && Object.IsValid) {
            data.muzzlePos = handling.Gun.muzzlePoint.position;
            data.muzzleDir = handling.Gun.muzzlePoint.forward;
        }
        locomotion.localLook = Vector2.zero; // Consume that mother fucker
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
        
        if (Health <= 0) {
            if (dmgSource.attacker.IsValid) {
                Character atk = GameManager.GetPlayer(dmgSource.attacker).Character;
                if (atk.Player.team == Team.Red) { GameManager.inst.redTeamKills++; }
                else { GameManager.inst.blueTeamKills++; }
                atk.Player.Kills++;
            }

            locomotion.enabled = false;
            handling.enabled = false;
            UI.enabled = false;
            rb.detectCollisions = false;
            kcc.enabled = false;
            visuals.SetActive(false);
            
            Player.Deaths++;
            IsAlive = false;
            Player.RespawnTimer = TickTimer.CreateFromSeconds(Runner, Player.respawnTime);
        }
        
        if (transform.position.y <= -50) { Damage(new(Object.InputAuthority), 100); }
        if (GetInput(out NetworkInputData input)) {
            if (input.buttons.WasPressed(LastInput.buttons, Buttons.Kill)) { Damage(new(Object.InputAuthority), 100); }
        }
    }

    public void Damage(DamageSource source, float amount) {
        dmgSource = source;
        Health = Mathf.Clamp(Health - amount, 0, maxHealth);
    }
    
    public static void OnHealthChanged(Changed<Character> changed) {
        Character c = changed.Behaviour;
        c.UI.healthText.text = c.Health.ToString();
    }

    public static void OnAliveChanged(Changed<Character> changed) {
        if (changed.Behaviour.IsAlive == false) {
            Character c = changed.Behaviour;
            Transform[] rags = Instantiate(c.ragdollPF, c.transform).GetComponent<Ragdoll>().rags;
            for (int i = 0; i < c.bones.Length; i++) { rags[i].localRotation = c.bones[i].localRotation; }
            rags[0].GetComponent<Rigidbody>().AddForce(c.locomotion.kcc.Data.RealVelocity + c.dmgSource.hitNormal * c.dmgSource.hitForce);
            Character atk = GameManager.GetPlayer(c.dmgSource.attacker).Character;
            atk.UI.IndicateKill(c.Player.name, c.dmgSource.weapon == -1 ? "self" : GameManager.GetWeapon(c.dmgSource.weapon).name, Mathf.Round(c.dmgSource.distance * 100f) / 100f);

            if (c.Object.HasInputAuthority) {
                c.deathCam = Instantiate(c.deathCamPF, atk.cam.transform.position + atk.cam.transform.TransformDirection(new Vector3(0, 0, -2)), atk.cam.transform.rotation);
                c.deathCam.Initialize(atk);
                if(c.dmgSource.attacker != c.Object.InputAuthority) { c.deathCam.target =  atk.cam.transform; }
                GameManager.inst.SwitchCamera(c.deathCam.cam, c.spectatorProfile);
            }
        }
    }
}

/*anim.SetFloat("MoveX", LastInput.movement.x, 0.1f, Time.deltaTime);
anim.SetFloat("MoveZ", LastInput.movement.y, 0.1f, Time.deltaTime);
anim.SetFloat("Aim", LastInput.buttons.IsSet(Buttons.Aim) ? 1 : 0, 0.1f, Time.deltaTime);*/
// todo troll: In Unity disc on alt - Ask to help model a sine wave in Unity, pretending to not know what a sine wave is called and show a video of a stream of my piss moving up and down as an example