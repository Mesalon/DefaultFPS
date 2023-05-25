using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShitPissController : MonoBehaviour {
    [SerializeField] private Transform head, abd, chest;
    private Controls controls;
    private Vector2 look;

    private void Awake() {
        Cursor.lockState = CursorLockMode.Locked;
        controls = new();
        controls.Enable();
    }

    private void Update() {
        look += controls.Player.Look.ReadValue<Vector2>();
        print(look);
        head.localRotation = Quaternion.Euler(-look.y / 3, 0, 0);
        chest.localRotation = Quaternion.Euler(-look.y / 3, 0, 0);
        abd.localRotation = Quaternion.Euler(-look.y / 3, 0, 0);
    }
}
