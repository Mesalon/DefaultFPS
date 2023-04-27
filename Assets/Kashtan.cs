using System;
using System.Collections;
using System.Collections.Generic;
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
        ProjectileManager.CreateProjectile(transform.position, transform.up, 0, runner);
        ProjectileManager.CreateProjectile(transform.position + Vector3.left, transform.up, 2, runner);
        ProjectileManager.CreateProjectile(transform.position + Vector3.right, transform.up, 1, runner);

    }
}
