using UnityEngine;

public class DestroyImpact : MonoBehaviour {
    public float lifetime;

    public void Start() {
        Destroy(this, lifetime);
    }
}
