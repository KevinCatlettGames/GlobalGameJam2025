using UnityEngine;
using TMPro;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.PropertyVariants.TrackedProperties;

public class ChangeTextDependingOnTransport : MonoBehaviour
{
    public LocalizedStringProperty localLocalizedStringProperty;
    public LocalizedStringProperty onlineLocalizedStringProperty;
    public LocalizeStringEvent localizeStringEvent;
    private void Start()
    {
        if (localizeStringEvent == null) return;

        if (TransportSwitcher.Instance.isUsingRelay)
            localizeStringEvent.StringReference = onlineLocalizedStringProperty.LocalizedString;
        else
            localizeStringEvent.StringReference = localLocalizedStringProperty.LocalizedString;
    }
}