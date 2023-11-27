using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

public class TwoBoneIK : IK { // Stolen asf code
    [SerializeField] Transform pole;

    [SerializeField] float upperTwist;
    [SerializeField] float lowerTwist;
    [SerializeField] Vector3 endRotation;
    private Transform lowerBone, endBone;
    private float a, b, c;
    private Vector3 en; // Normal of plane we want our arm to be on

    private void Awake() {
        lowerBone = transform.GetChild(0);
        endBone = lowerBone.GetChild(0);
    }

    public override void UpdateIK() {
        if (pole) {
            lowerBone = transform.GetChild(0);
            endBone = lowerBone.GetChild(0);

            a = Vector3.Scale(lowerBone.localPosition, lowerBone.lossyScale).magnitude;
            b = Vector3.Scale(endBone.localPosition, endBone.lossyScale).magnitude;
            c = Vector3.Distance(transform.position, target.position);
            en = Vector3.Cross(target.position - transform.position, pole.position - transform.position);
        
            transform.rotation = Quaternion.LookRotation(target.position - transform.position, Quaternion.AngleAxis(upperTwist, Vector3.Scale(lowerBone.localPosition, lowerBone.lossyScale)) * en);
            transform.rotation *= Quaternion.Inverse(Quaternion.FromToRotation(Vector3.forward, Vector3.Scale(lowerBone.localPosition, lowerBone.lossyScale)));
            transform.rotation = Quaternion.AngleAxis(-CosAngle(a, c, b), -en) * transform.rotation;

            lowerBone.rotation = Quaternion.LookRotation(target.position - lowerBone.position, Quaternion.AngleAxis(lowerTwist, Vector3.Scale(endBone.localPosition, endBone.lossyScale)) * en);
            lowerBone.rotation *= Quaternion.Inverse(Quaternion.FromToRotation(Vector3.forward, Vector3.Scale(endBone.localPosition, endBone.lossyScale)));
        
            endBone.rotation = target.rotation;
            endBone.Rotate(endRotation);
        }
    }

    // Finds angles using the cosine rule
    // ^ Made up words by mathemiticians
    private float CosAngle(float a, float b, float c) =>
        float.IsNaN(Mathf.Acos((-(c * c) + (a * a) + (b * b)) / (-2 * a * b)) * Mathf.Rad2Deg) ?
            1 : Mathf.Acos((-(c * c) + (a * a) + (b * b)) / (2 * a * b)) * Mathf.Rad2Deg;
}