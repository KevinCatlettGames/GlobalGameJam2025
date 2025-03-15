using System;
using UnityEngine.UI;
using UnityEngine;

[RequireComponent(typeof(Slider))] // Ensures a Slider component is attached
public class ContinuousSliderChange : MonoBehaviour
{
    private Slider slider; 

    private void Awake()
    { 
        slider = GetComponent<Slider>(); // Get the Slider component
    }

    private void Update()
    {
        if (slider.value < slider.maxValue) 
            slider.value += Time.deltaTime; // Increase value over time
    }

    public void ResetSlider()
    {
        slider.value = slider.minValue; // Reset to minimum value
    }
}