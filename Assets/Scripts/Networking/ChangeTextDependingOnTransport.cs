using UnityEngine;
using TMPro;

public class ChangeTextDependingOnTransport : MonoBehaviour
{
    public string localText;
    public string onlineText;
    public TextMeshProUGUI text;

    private void Start()
    {
        if (text == null) return;
        
        if (TransportSwitcher.Instance.isUsingRelay)
            text.text = onlineText;
        else
            text.text = localText;
    }
}