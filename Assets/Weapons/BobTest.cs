using System;
using UnityEngine;

public class BobTest : MonoBehaviour {
    [SerializeField] private Transform gun;
    [SerializeField] private Transform aimPoint;
    private Vector3 originalPos;

    private void Start() {
        originalPos = gun.transform.localPosition;
    }
    private void Update() {
        print(aimPoint.position - transform.position);
        if (Input.GetButton("Fire2")) {
            Vector3 toCamera = aimPoint.position - transform.position;
            gun.transform.localPosition -= toCamera;
        }
        else {
            gun.transform.localPosition = originalPos;
        }
    }
}
