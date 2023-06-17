using Fusion;
using UnityEngine;

public struct DamageSource : INetworkStruct {
    public PlayerRef attacker;
    public Vector3 hitNormal;
    public float hitForce;
}
