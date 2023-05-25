using UnityEngine;
using Fusion;
using UnityEngine.UI;

public class Player : NetworkBehaviour {
    [Networked] public NetworkString<_32> Name { get; set; }
    [SerializeField] private NetworkPrefabRef characterPF;
    public Character character;

    public override void Spawned() {
        name = $"Player {Object.InputAuthority.PlayerId}";
        Name = name;
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_SpawnCharacter(Vector3 position, PlayerRef player) {
        print("called");
        if (character) {
            Debug.LogError("Attempted to spawn character when not dead yet! Killing . . .");
            character.Kill();
        }
        if (Runner.IsServer && !character) {
            print("Spawning...");
            character = Runner.Spawn(characterPF, position, Quaternion.identity, player).GetComponent<Character>();
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_SetName(NetworkString<_32> name) { Name = name; }
}
