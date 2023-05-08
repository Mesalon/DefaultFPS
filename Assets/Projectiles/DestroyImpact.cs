using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyImpact : MonoBehaviour
{

    public float lifetime;

    public void Start() {
        Destroy(gameObject, lifetime);
    }
}
