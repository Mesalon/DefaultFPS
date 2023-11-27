using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public abstract class IK : MonoBehaviour {
    public Transform target;
    [SerializeField] bool runInEditor;
    [SerializeField] bool manualUpdate;
    
    public abstract void UpdateIK();
    
    private void Update() {
        if (target && !manualUpdate) {
            if (Application.isPlaying || runInEditor) {
                UpdateIK();
            }
        }

    }
}
