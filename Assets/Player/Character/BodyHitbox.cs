using UnityEngine;
using Fusion;

public class BodyHitbox : Hitbox {
    public BodyPart part;
     public int ID;
}
public enum BodyPart { Head, Body, Limb }