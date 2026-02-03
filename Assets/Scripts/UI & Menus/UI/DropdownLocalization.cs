using System;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.PropertyVariants.TrackedProperties;
using TMPro; 

public class DropdownLocalization : MonoBehaviour
{
    [SerializeField] private LocalizedStringProperty[] stringProperties;
    [SerializeField] private LocalizeStringEvent labelLocalizeStringEvent;

    private void OnEnable()
    {
        Localize(PlayerPrefs.GetInt("QualityLevel"));
    }

    public void Localize(int index)
    {
        labelLocalizeStringEvent.StringReference = stringProperties[index].LocalizedString;
    }
}