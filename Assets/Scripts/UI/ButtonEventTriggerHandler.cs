using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class ButtonEventTriggerHandler : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    ISelectHandler, IDeselectHandler
{
    [Header("Optional UI References")]
    public Image targetImage;                  // Button background
    public TMP_Text targetText;                // Button label

    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color highlightedColor = Color.yellow;
    public Color selectedColor = Color.green;

    [Header("Colors")] 
    public float normalSize = 50;
    public float highlightedSize = 53;
    public float selectedSize = 53;
    
    void Reset()
    {
        // Auto-assign if placed on a button
        targetImage = GetComponent<Image>();
        targetText = GetComponentInChildren<TMP_Text>();
    }

    // ------- POINTER ENTER -------
    public void OnPointerEnter(PointerEventData eventData)
    {
        ApplyColor(highlightedColor, highlightedSize);
    }

    // ------- POINTER EXIT -------
    public void OnPointerExit(PointerEventData eventData)
    {
        ApplyColor(normalColor, normalSize);
    }

    // ------- SELECT -------
    public void OnSelect(BaseEventData eventData)
    {
        ApplyColor(selectedColor, selectedSize);
    }

    // ------- DESELECT -------
    public void OnDeselect(BaseEventData eventData)
    {
        ApplyColor(normalColor, normalSize);
    }

    // ------- COLOR HANDLER -------
    private void ApplyColor(Color c, float s)
    {
        // if (targetImage != null)
        //     targetImage.color = c;

        if (targetText != null)
            targetText.fontSize = s;
    }
}