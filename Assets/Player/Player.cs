using UnityEngine;
using System.Linq;
using Fusion;

public class Player : NetworkBehaviour {
    [HideInInspector, Networked] public NetworkString<_32> Name { get; set; }
    [HideInInspector, Networked] public Team team { set; get; }
    [HideInInspector, Networked] public int Kills { get; set; }
    [HideInInspector, Networked] public int Deaths { get; set; }
    [Networked] public Character Character { get; set; }
    [SerializeField] Character characterPF;

    public override void Spawned() {
        Name = name = $"Player {Object.InputAuthority.RawEncoded}";
        team = Runner.ActivePlayers.Count() % 2 == 1 ? Team.Red : Team.Blue;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_SpawnCharacter(Vector3 position, WeaponConfiguration[] weapons) {
        if (Character) {
            Debug.LogError("Attempted to spawn character when not dead yet! This indicates a catastrophic blunder somewhere in code. You have to be an extremely retarded to let this happen . . .");
        } else {
            print($"Spawning character (Expand for loadout details)\nWeapon1 ID: {weapons[0].id}, Weapon1 attachments: {string.Join(", ", weapons[0].Attachments.ToArray())}\nWeapon2 ID: {weapons[1].id}, Weapon2 attachments: {string.Join(", ", weapons[1].Attachments.ToArray())}");
            Character = Runner.Spawn(characterPF, position, Quaternion.identity, Object.InputAuthority, (_, o) => {
                Character c = o.GetComponent<Character>();
                c.Player = this;
                c.handling.Gun1 = Runner.Spawn(GameManager.GetWeapon(weapons[0].id), inputAuthority: Object.InputAuthority, onBeforeSpawned: (_, o) => {
                    Firearm f = o.GetComponent<Firearm>();
                    f.Configuration = weapons[0];
                    f.Owner = c;

                });
                
                c.handling.Gun2 = Runner.Spawn(GameManager.GetWeapon(weapons[1].id), inputAuthority: Object.InputAuthority, onBeforeSpawned: (_, o) => {
                    Firearm f = o.GetComponent<Firearm>(); 
                    f.Configuration = weapons[1];
                    f.Owner = c;
                });
            });
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_SetName(NetworkString<_32> name) { Name = name; }
}

public struct WeaponConfiguration : INetworkStruct {
    public int id;
    [Networked, Capacity(32)] public NetworkArray<int> Attachments { get; }
}