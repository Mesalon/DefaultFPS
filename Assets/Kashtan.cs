using Fusion;
using UnityEngine;

public class Kashtan : MonoBehaviour {
    private NetworkRunner runner;
    private void Start() {
        runner = FindFirstObjectByType<NetworkRunner>();
    }

    void Update() {
    }
}
