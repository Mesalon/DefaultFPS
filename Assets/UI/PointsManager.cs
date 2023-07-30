using System.Collections.Generic;
using UnityEngine;

public class PointsManager : MonoBehaviour {
    public static PointsManager inst;
    public PointsIndicatorUI indicatorPF;

    private void Awake() {
        if(inst) { Debug.LogError("Duplicate PointsManager found."); }
        inst = this;
    }

    public void AwardPoints(Player to, PointsIndicator indicator, List<PointsIndicator> subIndicators = null) {
        bool local = to.Object.InputAuthority == to.Runner.LocalPlayer;
        if (subIndicators != null) {
            foreach (PointsIndicator sub in subIndicators) {
                if(local) { Instantiate(indicatorPF, transform).Initialize(sub, true); }
                to.Score += sub.amount;
            }
        }
        if(local) { Instantiate(indicatorPF, transform).Initialize(indicator); }
        to.Score += indicator.amount;
    }
}

public struct PointsIndicator {
    public int amount;
    public string award;
    public PointsIndicator(int amount, string award) {
        this.amount = amount;
        this.award = award;
    }
}

/*private void Start() { My retarded testing
    IEnumerator cr() {
        Notify(new(1000, "I fucked ur mum"));
        yield return new WaitForSeconds(0.5f);
        Notify(new(30, "I fucked ur dad"));
        yield return new WaitForSeconds(0.5f);
        Notify(new(10000, "I fucked ur dog"));
        yield return new WaitForSeconds(0.5f);
        Notify(new(100, "enemy successfuly fucked"));
        yield return new WaitForSeconds(4f);
        Notify(new(100, "tower 1 hit"), 
            new() {
                new(100, "MR CRABS THERE'S A SECOND PLANE!!!"),
                new(420, "I don't even know anymore")
            });
    }
    StartCoroutine(cr());
}*/