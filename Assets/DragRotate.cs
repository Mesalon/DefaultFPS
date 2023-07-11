using System;
using UnityEngine;
using UnityEngine.Serialization;

public class DragRotate : MonoBehaviour {
    private Vector2 MouseDelta => Input.mousePosition - lastMousePos;
    [SerializeField] Camera cam;
    [SerializeField] float dragSensitivity = 1;
    [SerializeField] float rotSensitivity = 1;
    [SerializeField] float zoomSpeed = 1;
    [SerializeField] float returnSpeed = 1;
    [SerializeField] private Vector2 zoomRange;
    [SerializeField] private float dragRange;
    private Vector3 lastMousePos;
    private Vector3 startPos;
    private Quaternion startRot;
    private float zoomLevel = 1;
    private float startFOV;
    private bool doReturn;
    
    private void Start() {
        startPos = transform.position;
        startRot = transform.rotation;
        startFOV = cam.fieldOfView;
        doReturn = true;
    }

    private void Update() {
        zoomLevel = Mathf.Clamp(zoomLevel + Input.GetAxis("Mouse ScrollWheel") * 3, zoomRange.x, zoomRange.y);
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, startFOV / zoomLevel, Time.deltaTime * zoomSpeed);

        if (Input.GetMouseButtonDown(2)) { doReturn = !doReturn; }

        if (Input.GetMouseButton(1)) {
            transform.position += dragSensitivity * 0.01f * (Vector3)MouseDelta;
            if ((transform.position - startPos).magnitude > dragRange) { transform.position = startPos + (transform.position - startPos).normalized * dragRange; }
        }
        
        if (Input.GetMouseButton(0)) {
            Vector2 delta = rotSensitivity * MouseDelta;
            transform.Rotate(Vector3.up, -Vector3.Dot(delta, cam.transform.right), Space.World);
            transform.Rotate(cam.transform.right, Vector3.Dot(delta, cam.transform.up), Space.World);
        } 
        else if (doReturn) { transform.rotation = Quaternion.Lerp(transform.rotation, startRot, Time.deltaTime * returnSpeed); }
        lastMousePos = Input.mousePosition;
    }
}
