using System;
using UnityEngine.UI;
using UnityEngine;

[RequireComponent(typeof(Slider))]
public class ContinuousSliderChange : MonoBehaviour
{
    private Slider slider; 

    private void Awake()
    { 
        slider = GetComponent<Slider>();
    }

    private void Update()
    {
        if (slider.value < slider.maxValue) 
            slider.value += Time.deltaTime;
    }

    public void ResetSlider()
    {
        slider.value = slider.minValue;
    }
}