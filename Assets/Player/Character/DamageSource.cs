using Fusion;
using UnityEngine;

public struct DamageSource : INetworkStruct {
    /// <summary>Creates a damage source for self inflicted deaths</summary>
    public DamageSource(PlayerRef player) {
        attacker = player;
        hitNormal = Vector3.zero;
        distance = 0;
        hitForce =
        weapon = -1;
    }
    
    public PlayerRef attacker;
    public Vector3 hitNormal;
    public int weapon;
    public float distance;
    public float hitForce;
}
