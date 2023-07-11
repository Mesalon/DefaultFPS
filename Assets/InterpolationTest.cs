using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class InterpolationTest : NetworkBehaviour {
    [SerializeField] private Transform vis;
    [SerializeField] private float speed;
    private Vector3 startPos;
    private Vector3 lastNetPos;
    private float clock;
    private float gizmoClock;
    private float interClock;

    private void Awake() {
        startPos = transform.position;
    }

    private void OnDrawGizmos() {
        Gizmos.DrawWireSphere(startPos + new Vector3(Mathf.Sin(gizmoClock), Mathf.Cos(gizmoClock), 0), 0.2f);
    }

    public override void FixedUpdateNetwork() {
        lastNetPos = transform.position;
        clock = Runner.SimulationTime * speed;
        gizmoClock = clock;
        interClock = 0;
        vis.position = lastNetPos;
        transform.position = startPos + new Vector3(Mathf.Sin(clock), Mathf.Cos(clock), 0);
    }

    public override void Render() {
        gizmoClock += Time.deltaTime * speed;
        interClock += Time.deltaTime;
        vis.position = Vector3.Lerp(lastNetPos, transform.position, interClock / Runner.DeltaTime);
    }
}