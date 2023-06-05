using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using Fusion;
using TMPro;

[OrderAfter(typeof(Character))]
public class UI : NetworkBehaviour {
    [SerializeField] Camera cam;
    [SerializeField] TMP_Text ammoCounter;
    [SerializeField] TMP_Text killIndicator;
    [SerializeField] float showNametagAngle, hideNametagAngle;
    [SerializeField] TMP_Text nametagText;
    [SerializeField] Transform nametagPosition;
    [SerializeField] Transform nametagAimPoint;
    [SerializeField] TMP_Text redTeamKills;
    [SerializeField] TMP_Text blueTeamKills;
    [SerializeField] float fpsAverageDepth;
    [SerializeField] Slider healthSlider;
    [SerializeField] Gradient gradient;
    [SerializeField] Image fill;
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

        healthSlider.maxValue = character.maxHealth;
        healthSlider.value = character.Health;
        fill.color = gradient.Evaluate(healthSlider.normalizedValue);
    }

    public override void Render() {
        if (Object.HasInputAuthority) {
            healthSlider.value = character.Health;
            fill.color = gradient.Evaluate(healthSlider.normalizedValue);
            ammoCounter.text = $"{character.handling.gun.Ammo} / {character.handling.gun.ReserveAmmo}";
            Application.targetFrameRate = FPSCap;
        }else { // Nametags
            nametagText.text = character.player.Name.ToString();
            Transform activeCam = GameManager.inst.activeCamera.transform;
            float angle = Vector3.Angle(activeCam.forward, (nametagAimPoint.position - activeCam.position).normalized);
            Color col = nametagText.color;
            col.a = Mathf.InverseLerp(hideNametagAngle, showNametagAngle, angle);
            nametagText.color = col;
            Vector3 point = GameManager.inst.activeCamera.WorldToScreenPoint(nametagPosition.position);
            nametagText.transform.position = point;
        }

        //UI
        redTeamKills.text = GameManager.inst.redTeamKills.ToString();
        blueTeamKills.text = GameManager.inst.blueTeamKills.ToString();
    }

    private void OnGUI() {
        deltaTimes.Enqueue(Time.unscaledDeltaTime);
        if (deltaTimes.Count > fpsAverageDepth) { deltaTimes.Dequeue(); }
        float avg = 0;
        foreach (float time in deltaTimes) { avg += time; }
        GUI.Label(new Rect(5, 5, 100, 25), "FPS: " + System.Math.Round(1 / (avg / fpsAverageDepth), 1));
    }
    
    public void IndicateKill(Character victim) {
        IEnumerator CR() {
            killIndicator.text = $"KILL - {victim.player.Name}";
            killIndicator.gameObject.SetActive(true);
            yield return new WaitForSeconds(1.5f);
            killIndicator.gameObject.SetActive(false);
        }
        StartCoroutine(CR());
    }
}
