using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class Character : NetworkBehaviour {
    [HideInInspector, Networked(OnChanged = nameof(OnHealthChanged))] public float Health { get; set; }
    [Networked] NetworkInputData LastInput { get; set; }
    [HideInInspector] public Player player, attacker; // Most recent damage source
    public float maxHealth;
    [HideInInspector] public Handling handling;
    [SerializeField] Camera cam;
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
        data.buttons.Set(Buttons.switchPrimary, controls.Player.Primary.ReadValue<float>() == 1);
        data.buttons.Set(Buttons.switchSecondary, controls.Player.Secondary.ReadValue<float>() == 1);
        data.movement = controls.Player.Move.ReadValue<Vector2>();
        data.lookDelta = locomotion.localLook;
        data.muzzlePos = handling.gun.muzzle.position;
        data.muzzleDir = handling.gun.muzzle.forward;
        locomotion.localLook = Vector2.zero; // Consume that mother fucker
        input.Set(data);
    }

    public override void Spawned() {
        player = GameManager.GetPlayer(Runner, Object.InputAuthority);
        player.character = this;
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
        if (transform.position.y <= -50) { Kill(); }
    }

    public void Kill() {
        if (Object.HasInputAuthority) {
            GameManager.inst.SwitchCamera(GameManager.inst.mainCamera);
            Cursor.lockState = CursorLockMode.None;
        }

        player.Deaths++;
        Runner.Despawn(Object);
    }

    public void Damage(Player source, float amount) {
        // todo: Implement DamageSource struct with more detailed information like weapon, distance, etc
        attacker = source;
        Health = Mathf.Clamp(Health - amount, 0, maxHealth);
    }
    
    public static void OnHealthChanged(Changed<Character> changed) {
        Character c = changed.Behaviour;
        if (c.Health <= 0) { // Die
            Character atk = c.attacker.character;
            atk.UI.IndicateKill(c);
            if (atk.player.team == Team.Red) { GameManager.inst.redTeamKills++; }
            else { GameManager.inst.blueTeamKills++; }
            atk.player.Kills++;
            c.Kill();
        }
    }
}

public enum WeaponRole { Primary, Secondary }
/*anim.SetFloat("MoveX", LastInput.movement.x, 0.1f, Time.deltaTime);
anim.SetFloat("MoveZ", LastInput.movement.y, 0.1f, Time.deltaTime);
anim.SetFloat("Aim", LastInput.buttons.IsSet(Buttons.Aim) ? 1 : 0, 0.1f, Time.deltaTime);*/
// todo troll: In Unity disc on alt - Ask to help model a sine wave in Unity, pretending to not know what a sine wave is called and show a video of a stream of my piss moving up and down as an example