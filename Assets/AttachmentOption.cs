using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AttachmentOption : MonoBehaviour {
    [SerializeField] TextMeshProUGUI text;
    public Button button;
    public int id;

    public void Init(string name, int id) {
        text.text = name;
        this.id = id;
    }
}
