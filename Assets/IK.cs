using UnityEngine;

[ExecuteInEditMode]
public class IK : MonoBehaviour
{
    public Transform Upper; // root of upper arm
    public Transform Lower; // root of lower arm
    public Transform End; // root of hand
    public Transform Target; // target position of hand
    public Transform Pole; // direction to bend towards 
    public float UpperElbowRotation; // Rotation offsets
    public float LowerElbowRotation;

    private float a; // values for use in cos rule
    private float b;
    private float c;
    private Vector3 en; // Normal of plane we want our arm to be on

    void Update()
    {
        a = Vector3.Scale(Lower.localPosition, Lower.lossyScale).magnitude;
        b = Vector3.Scale(End.localPosition, End.lossyScale).magnitude;
        c = Vector3.Distance(Upper.position, Target.position);
        en = Vector3.Cross(Target.position - Upper.position, Pole.position - Upper.position);
        // Set the rotation of the upper arm
        Upper.rotation = Quaternion.LookRotation(Target.position - Upper.position, Quaternion.AngleAxis(UpperElbowRotation, Vector3.Scale(Lower.localPosition, Lower.lossyScale)) * (en));
        Upper.rotation *= Quaternion.Inverse(Quaternion.FromToRotation(Vector3.forward, Vector3.Scale(Lower.localPosition, Lower.lossyScale)));
        Upper.rotation = Quaternion.AngleAxis(-CosAngle(a, c, b), -en) * Upper.rotation;

        // Set the rotation of the lower arm
        Lower.rotation = Quaternion.LookRotation(Target.position - Lower.position, Quaternion.AngleAxis(LowerElbowRotation, Vector3.Scale(End.localPosition, End.lossyScale)) * (en));
        Lower.rotation *= Quaternion.Inverse(Quaternion.FromToRotation(Vector3.forward, Vector3.Scale(End.localPosition, End.lossyScale)));
    }

    // Function that finds angles using the cosine rule 
    float CosAngle(float a, float b, float c)
    {
        if (!float.IsNaN(Mathf.Acos((-(c * c) + (a * a) + (b * b)) / (2 * a * b)) * Mathf.Rad2Deg))
        {
            return Mathf.Acos((-(c * c) + (a * a) + (b * b)) / (2 * a * b)) * Mathf.Rad2Deg;
        }
        else
        {
            return 1;
        }
    }
}
