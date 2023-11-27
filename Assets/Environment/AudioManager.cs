 using System;
using System.Collections;
using System.Collections.Generic;
 using FMOD.Studio;
 using UnityEngine;
using FMODUnity;

public class AudioManager : MonoBehaviour {
    public static AudioManager inst;
    private List<EventInstance> events = new();
    
    private void Awake() {
        if(inst) { Debug.LogError("Duplicate AudioManager found."); }
        inst = this;
    }
    
    public EventInstance CreateInstance(EventReference sound, Transform where = null) {
        EventInstance instance = RuntimeManager.CreateInstance(sound);  
        if(where) { RuntimeManager.AttachInstanceToGameObject(instance, where); }
        events.Add(instance);
        return instance;
    }

    private void OnDestroy() {
        foreach (EventInstance instance in events) {
            instance.stop(STOP_MODE.IMMEDIATE);
            instance.release();
        }
    }
}
