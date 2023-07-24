using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SpectatorCam : MonoBehaviour {
    public Camera cam;
    public Transform target;
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] TextMeshProUGUI weaponText;
    [SerializeField] TextMeshProUGUI rankText;
    [SerializeField] TextMeshProUGUI healthText;
    [SerializeField] Transform attachmentsContainer;
    [SerializeField] Bubble attachmentBubble;
    [SerializeField] private Vector3 offset;
    private Character killer;

    private void Awake() {
        cam = GetComponent<Camera>();
    }

    private void Update() {
        healthText.text = $"<color=#{ColorUtility.ToHtmlStringRGB(Color.Lerp(Color.red, Color.green, killer.Health / 100))}>{killer.Health}</color>";
    }

    void LateUpdate() {
        if (target) {
            transform.SetPositionAndRotation(target.position + target.TransformDirection(offset), target.rotation);
        }
    }

    public void Initialize(Character killer) {
        this.killer = killer;
        nameText.text = killer.Player.Name.ToString();
        weaponText.text = killer.handling.Gun.stats.name;
        rankText.text = "0";
        print(string.Join(", ", killer.handling.Gun.Attachments));
        foreach (Attachment a in killer.handling.Gun.Attachments) {
            Bubble b = Instantiate(attachmentBubble, attachmentsContainer);
            b.item.text = a.type.ToString();
            b.data.text = a.name;
        }
    }
}