using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

public class SelectableTextRecolor : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    public TextMeshProUGUI targetText;
    public Color normalFontColor = Color.white;
    public Color selectedFontColor = Color.grey;

    private void Reset()
    {
        targetText = GetComponentInChildren<TextMeshProUGUI>();
    }

    private void OnDisable()
    {
        ApplyNormal();
    }

    public void OnSelect(BaseEventData eventData)
    {
        ApplySelected();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        ApplyNormal();
    }

    private void ApplySelected()
    {
        if (targetText != null)
            targetText.color = selectedFontColor;
    }

    private void ApplyNormal()
    {
        if (targetText != null)
            targetText.color = normalFontColor;
    }
}