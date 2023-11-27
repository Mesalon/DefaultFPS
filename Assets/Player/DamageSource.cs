using Fusion;
using UnityEngine;

public struct DamageSource : INetworkStruct {
    /// <summary>Creates a damage source for self inflicted deaths</summary>
    public static DamageSource Suicide(PlayerRef player) => new() {
        attacker = player,
        hitPos = Vector3.zero,
        hitVector = Vector3.zero,
        distance = 0,
        weapon = -1,
    };
    
    public PlayerRef attacker;
    public Vector3 hitVector;
    public Vector3 hitPos;
    public int limb;
    public int weapon;
    public float distance;
}
