using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PointsIndicatorUI : MonoBehaviour {
    public TMP_Text text;
    public int spacing;
    [HideInInspector] public int amount;
    [HideInInspector] public string award;
    [SerializeField] private float fadeTime;
    
    private void Start() {
        IEnumerator CR() {
            List<DialogueCommand> commands = DialogueUtility.ProcessInputString($"{new string(' ', spacing - amount.ToString().Length)}[+{amount}] {award}", out string totalTextMessage);
            StartCoroutine(new DialogueVertexAnimator(text).AnimateTextIn(commands, totalTextMessage, 500, null));
            yield return new WaitForSeconds(fadeTime);
            while (text.alpha > 0.01f) {
                text.alpha -= 2 * Time.deltaTime;
                yield return null;
            }
            Destroy(gameObject);
        }
        StartCoroutine(CR());
    }

    public void Initialize(PointsIndicator settings, bool isSub = false) {
        if (isSub) {
            text.fontSize = 12;
            spacing += 6;
        }
        amount = settings.amount;
        award = settings.award;
    }
}
