using System.Linq;
using UnityEngine;
using Fusion;

public class Player : NetworkBehaviour {
    [HideInInspector, Networked] public NetworkString<_32> Name { get; set; }
    [HideInInspector, Networked] public Team team { set; get; }
    [HideInInspector, Networked] public int Kills { get; set; }
    [HideInInspector, Networked] public int Deaths { get; set; }
    [SerializeField] private NetworkPrefabRef characterPF;
    [HideInInspector] public Character character;

    public override void Spawned() {
        name = $"Player {Object.InputAuthority.PlayerId}";
        Name = name;
        if(Runner.ActivePlayers.Count() % 2 == 0) { team = Team.Red; } 
        else { team = Team.Blue; }
        print(team);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_SpawnCharacter(Vector3 position, PlayerRef player) {
        if (character) {
            Debug.LogError("Attempted to spawn character when not dead yet! This indicates a catastrophic blunder somewhere in code. You have to be an extremely retarded to let this happen . . .");
            character.Kill();
        }
        if (Runner.IsServer && !character) {
            print("Spawning...");
            Runner.Spawn(characterPF, position, Quaternion.identity, player).GetComponent<Character>();
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_SetName(NetworkString<_32> name) { Name = name; }
}
