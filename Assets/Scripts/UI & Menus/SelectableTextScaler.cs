using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

public class SelectableTextScaler : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    public TextMeshProUGUI targetText;
    public bool changeFontSize = true;
    public float normalFontSize = 24f;
    public float selectedFontSize = 32f;

    [Header("Behavior")]
    public bool forceSelectOnEnable = false;

    private void Reset()
    {
        targetText = GetComponentInChildren<TextMeshProUGUI>();
    }

    private void OnEnable()
    {
        StartCoroutine(HandleEnable());
    }

    private void OnDisable()
    {
        ApplyNormal();
    }

    private IEnumerator HandleEnable()
    {
        yield return null; // wait one frame so EventSystem is ready

        if (EventSystem.current == null)
            yield break;

        if (forceSelectOnEnable)
        {
            EventSystem.current.SetSelectedGameObject(gameObject);
            ApplySelected();
        }
        else
        {
            // fallback: just sync visual state
            if (EventSystem.current.currentSelectedGameObject == gameObject)
                ApplySelected();
            else
                ApplyNormal();
        }
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
        if (targetText != null && changeFontSize)
            targetText.fontSize = selectedFontSize;
    }

    private void ApplyNormal()
    {
        if (targetText != null && changeFontSize)
            targetText.fontSize = normalFontSize;
    }

    public void ToggleFocus(bool toggle)
    {
        forceSelectOnEnable = toggle;
    }
}