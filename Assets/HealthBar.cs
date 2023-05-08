using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public Slider HealthSlider;
    public Gradient gradient;
    public Image fill;

    public void SetHealthSlider(float health)
    {
        HealthSlider.value = health;
        fill.color = gradient.Evaluate(HealthSlider.normalizedValue);
    }
    public void SetMaxHealthSlider(float maxhealth)
    {
        HealthSlider.maxValue = maxhealth;
        fill.color = gradient.Evaluate(1);
    }
}
