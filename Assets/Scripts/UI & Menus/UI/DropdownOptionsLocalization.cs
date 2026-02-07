using System;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.PropertyVariants.TrackedProperties;

public class DropdownOptionsLocalization : MonoBehaviour
{
    [SerializeField] private LocalizedStringProperty[] stringProperties;
    LocalizeStringEvent[] localizeStringEvents;
    private void Start()
    {
        localizeStringEvents = GetComponentsInChildren<LocalizeStringEvent>();

        for (int i = 0; i < localizeStringEvents.Length; i++)
        {
            localizeStringEvents[i].StringReference = stringProperties[i].LocalizedString;
        }
    }
}