using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

[ExecuteInEditMode]
public class TwoBoneIK : MonoBehaviour { // Stolen asf code
    public Transform target;
    public Transform pole;
    [SerializeField] bool runInEditor;
    [SerializeField] float upperTwist;
    [SerializeField] float lowerTwist;
    [SerializeField] private float originalZRot;
    private Transform lowerBone, endBone;
    private float a, b, c;
    private Vector3 en; // Normal of plane we want our arm to be on

    private void Awake() {
        lowerBone = transform.GetChild(0);
        endBone = lowerBone.GetChild(0);
    }

    private void Update() {
        if (!Application.isPlaying && runInEditor) { InvertKinematics(); }
    }

    public void InvertKinematics() {
        if (!target || !pole.gameObject.activeInHierarchy) {
            transform.localRotation = Quaternion.Euler(0, 0, originalZRot);
            lowerBone.localRotation = Quaternion.identity;
            endBone.localRotation = Quaternion.identity;
            return;
        }
        
        lowerBone = transform.GetChild(0);
        endBone = lowerBone.GetChild(0);

        a = Vector3.Scale(lowerBone.localPosition, lowerBone.lossyScale).magnitude;
        b = Vector3.Scale(endBone.localPosition, endBone.lossyScale).magnitude;
        c = Vector3.Distance(transform.position, target.position);
        en = Vector3.Cross(target.position - transform.position, pole.position - transform.position);
        
        // Set the rotation of the upper arm
        transform.rotation = Quaternion.LookRotation(target.position - transform.position, Quaternion.AngleAxis(upperTwist, Vector3.Scale(lowerBone.localPosition, lowerBone.lossyScale)) * (en));
        transform.rotation *= Quaternion.Inverse(Quaternion.FromToRotation(Vector3.forward, Vector3.Scale(lowerBone.localPosition, lowerBone.lossyScale)));
        transform.rotation = Quaternion.AngleAxis(-CosAngle(a, c, b), -en) * transform.rotation;

        // Set the rotation of the lower arm
        lowerBone.rotation = Quaternion.LookRotation(target.position - lowerBone.position, Quaternion.AngleAxis(lowerTwist, Vector3.Scale(endBone.localPosition, endBone.lossyScale)) * (en));
        lowerBone.rotation *= Quaternion.Inverse(Quaternion.FromToRotation(Vector3.forward, Vector3.Scale(endBone.localPosition, endBone.lossyScale)));
        
        endBone.rotation = target.rotation;
    }

    // Finds angles using the cosine rule
    // ^ Made up words by mathemiticians
    private float CosAngle(float a, float b, float c) =>
        float.IsNaN(Mathf.Acos((-(c * c) + (a * a) + (b * b)) / (-2 * a * b)) * Mathf.Rad2Deg) ?
            1 : Mathf.Acos((-(c * c) + (a * a) + (b * b)) / (2 * a * b)) * Mathf.Rad2Deg;
}