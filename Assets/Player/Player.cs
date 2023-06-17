using System.Linq;
using UnityEngine;
using Fusion;
using Fusion.Editor;

public class Player : NetworkBehaviour {
    [HideInInspector, Networked] public NetworkString<_32> Name { get; set; }
    [HideInInspector, Networked] public Team team { set; get; }
    [HideInInspector, Networked] public int Kills { get; set; }
    [HideInInspector, Networked] public int Deaths { get; set; }
    [HideInInspector] public Character character;
    public int gun1, gun2;
    [SerializeField] NetworkPrefabRef characterPF;

    public override void Spawned() {
        name = $"Player {Object.InputAuthority.PlayerId}";
        Name = name;
        if(Runner.ActivePlayers.Count() % 2 == 1) { team = Team.Red; } 
        else { team = Team.Blue; }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_SpawnCharacter(Vector3 position, PlayerRef player) {
        if (character) {
            Debug.LogError("Attempted to spawn character when not dead yet! This indicates a catastrophic blunder somewhere in code. You have to be an extremely retarded to let this happen . . .");
        } else {
            Runner.Spawn(characterPF, position, Quaternion.identity, player, (runner, o) => {
                o.GetComponent<Handling>().SelectedGun1 = gun1;
                o.GetComponent<Handling>().SelectedGun2 = gun2;
            });
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_SetName(NetworkString<_32> name) { Name = name; }
}
