using System.Collections;
using System.Collections.Generic;
using FMODUnity;
using UnityEngine;

public class AnimSoundHooks : MonoBehaviour {
    [SerializeField] EventReference footsteps;
    
    public void FootstepNoise() {
        print("Step!");
        RuntimeManager.PlayOneShot(footsteps, transform.position);
    }
}