using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class WeaponOption : MonoBehaviour {
    [SerializeField] TextMeshProUGUI text;
    public GameObject preview;
    public List<AttachmentMountBubble> mountOptions;
    public Button button;
    public int id;

    public void Init(string name, int id) {
        text.text = name;
        this.id = id;
    }
}
