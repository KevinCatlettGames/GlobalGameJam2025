using UnityEngine;
using TMPro;
using UnityEngine.Localization.PropertyVariants.TrackedProperties;

public class ChangeTextDependingOnTransport : MonoBehaviour
{
    public LocalizedStringProperty localLocalizedStringProperty;
    public LocalizedStringProperty onlineLocalizedStringProperty;
    public TextMeshProUGUI text;
    
    private void Start()
    {
        if (text == null) return;

        if (TransportSwitcher.Instance.isUsingRelay)
            text.text = onlineLocalizedStringProperty.LocalizedString.GetLocalizedString();
        else
            text.text = localLocalizedStringProperty.LocalizedString.GetLocalizedString();
    }
}