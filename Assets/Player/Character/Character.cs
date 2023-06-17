using UnityEngine;
using Fusion;

public class Character : NetworkBehaviour {
    [HideInInspector, Networked(OnChanged = nameof(OnHealthChanged))] public float Health { get; set; }
    [Networked] NetworkInputData LastInput { get; set; }
    [Networked] DamageSource dmgSource { get; set; } // Most recent damage source
    public Player Player => GameManager.GetPlayer(Object.InputAuthority);
    public float maxHealth;
    [HideInInspector] public Handling handling;
    [SerializeField] Camera cam;
    [SerializeField] GameObject ragdoll;
    [SerializeField] Transform armature;
    [SerializeField] Transform[] bones;
    private Locomotion locomotion;
    private Controls controls;
    private UI UI;

    private void Awake() {
        controls = new();
        handling = GetComponent<Handling>();
        locomotion = GetComponent<Locomotion>();
        UI = GetComponent<UI>();
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
        data.muzzlePos = handling.equippedGun.muzzlePoint.position;
        data.muzzleDir = handling.equippedGun.muzzlePoint.forward;
        locomotion.localLook = Vector2.zero; // Consume that mother fucker
        input.Set(data);
    }

    public override void Spawned() {
        Player.character = this;
        Health = maxHealth;

        if (Object.HasInputAuthority) {
            name = "Client";
            Cursor.lockState = CursorLockMode.Locked;
            GameManager.inst.SwitchCamera(cam);
            Runner.GetComponent<NetworkEvents>().OnInput.AddListener(OnInput);
        }
        else {
            name = "Proxy";
            cam.gameObject.SetActive(false);
        }
    }

    public override void FixedUpdateNetwork() {
        if (GetInput(out NetworkInputData input)) {
            if (input.buttons.WasPressed(LastInput.buttons, Buttons.Kill)) { Health = 0; }
        }
        if (transform.position.y <= -50) { Health = 0; }
    }

    public void Die() {
        if (Object.HasInputAuthority) {
            GameManager.inst.SwitchCamera(GameManager.inst.mainCamera);
            Cursor.lockState = CursorLockMode.None;
        }
        
        Player.Deaths++;
        /*handling.gun.Active = false; todo: Fix this shit
        handling.gun.transform.SetParent(null);*/
        Transform[] rags = Instantiate(ragdoll, transform.position, transform.rotation).GetComponent<Ragdoll>().rags;
        for (int i = 0; i < bones.Length; i++) { rags[i].localRotation = bones[i].localRotation; }
        rags[0].GetComponent<Rigidbody>().AddForce(locomotion.cc.velocity + dmgSource.hitNormal * dmgSource.hitForce);
        Runner.Despawn(Object, true);
    }
    
    public void Damage(DamageSource source, float amount) {
        // todo: Implement DamageSource struct with more detailed information like weapon, distance, etc
        dmgSource = source;
        Health = Mathf.Clamp(Health - amount, 0, maxHealth);
    }
    
    public static void OnHealthChanged(Changed<Character> changed) {
        Character c = changed.Behaviour;
        c.UI.healthText.text = c.Health.ToString();
        if (c.Health <= 0) { // Die
            if (c.dmgSource.attacker.IsValid) {
                Character atk = GameManager.GetPlayer(c.dmgSource.attacker).character;
                atk.UI.IndicateKill(c, c.handling.equippedGun, Vector3.Distance(atk.transform.position, c.transform.position));
                if (atk.Player.team == Team.Red) { GameManager.inst.redTeamKills++; }
                else { GameManager.inst.blueTeamKills++; }
                atk.Player.Kills++;
            }
            c.Die();
        }
    }
}

public enum WeaponRole { Primary, Secondary }
/*anim.SetFloat("MoveX", LastInput.movement.x, 0.1f, Time.deltaTime);
anim.SetFloat("MoveZ", LastInput.movement.y, 0.1f, Time.deltaTime);
anim.SetFloat("Aim", LastInput.buttons.IsSet(Buttons.Aim) ? 1 : 0, 0.1f, Time.deltaTime);*/
// todo troll: In Unity disc on alt - Ask to help model a sine wave in Unity, pretending to not know what a sine wave is called and show a video of a stream of my piss moving up and down as an example