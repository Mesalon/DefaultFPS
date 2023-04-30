using Fusion;
using UnityEngine;

public class Kashtan : MonoBehaviour {
    private NetworkRunner runner;
    private void Start() {
        runner = FindFirstObjectByType<NetworkRunner>();
    }

    void Update() {
        float val = -30 + (30 - -30) * Mathf.PingPong(Time.time, 1);
        transform.rotation = Quaternion.Euler(0, 0, val);
        ProjectileManager.inst.CreateProjectile(new(0, new(), transform.position, transform.up));
        ProjectileManager.inst.CreateProjectile(new(1, new(), transform.position + Vector3.left, transform.up));
        ProjectileManager.inst.CreateProjectile(new(2, new(), transform.position + Vector3.right, transform.up));
    }
}
