using System;
using System.Collections;
using System.Collections.Generic;
using FMODUnity;
using UnityEngine.UI;
using UnityEngine;
using Fusion;
using TMPro;

[OrderAfter(typeof(Character))]
public class UI : NetworkBehaviour {
    public TMP_Text healthText;
    [SerializeField] TMP_Text ammoCounter;
    [SerializeField] TMP_Text killIndicator;
    [SerializeField] TMP_Text nametagText;
    [SerializeField] Transform nametagPosition;
    [SerializeField] Transform nametagAimPoint;
    [SerializeField] TMP_Text redTeamKills;
    [SerializeField] TMP_Text blueTeamKills;
    [SerializeField] private EventReference hitmarkSound;
    [SerializeField] private Image hitmarker;
    [SerializeField] float fpsAverageDepth;
    [SerializeField] float showNametagAngle, hideNametagAngle;
    [SerializeField] int FPSCap = -1;
    private Queue<float> deltaTimes = new();
    private Character character;

    private void Awake() { 
        character = GetComponent<Character>();
    }

    public override void Spawned() {
        if (Object.HasInputAuthority) {
            nametagText.gameObject.SetActive(false);
        }
    }

    public override void Render() {
        if (Object.HasInputAuthority) {
            ammoCounter.text = $"{character.handling.Gun.Ammo} / {character.handling.Gun.ReserveAmmo}";
            Application.targetFrameRate = FPSCap;
        } else {
            nametagText.text = character.Player.Name.ToString();
            Transform activeCam = GameManager.inst.activeCamera.transform;
            float angle = Vector3.Angle(activeCam.forward, (nametagAimPoint.position - activeCam.position).normalized);
            Color col = nametagText.color;
            col.a = Mathf.InverseLerp(hideNametagAngle, showNametagAngle, angle);
            nametagText.color = col;
            Vector3 point = GameManager.inst.activeCamera.WorldToScreenPoint(nametagPosition.position);
            nametagText.transform.position = point;
            redTeamKills.text = GameManager.inst.redTeamKills.ToString();
            blueTeamKills.text = GameManager.inst.blueTeamKills.ToString();
        }
    }

    private void OnGUI() {
        if (Object.HasInputAuthority) {
            deltaTimes.Enqueue(Time.unscaledDeltaTime);
            if (deltaTimes.Count > fpsAverageDepth) { deltaTimes.Dequeue(); }
            float avg = 0;
            foreach (float time in deltaTimes) { avg += time; }
            GUI.Label(new Rect(5, 5, 100, 25), "FPS: " + Math.Round(1 / (avg / fpsAverageDepth), 1));
        }
    }

    public void MarkHit(bool headshot) {
        IEnumerator CR() {
            hitmarker.color = headshot ? Color.red : Color.white;
            RuntimeManager.PlayOneShotAttached(hitmarkSound, gameObject);
            hitmarker.gameObject.SetActive(true);
            yield return new WaitForSeconds(0.1f);
            hitmarker.gameObject.SetActive(false);
        }
        StartCoroutine(CR());
    }
}
