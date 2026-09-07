using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class UnderlineHandler : MonoBehaviour,
    ISelectHandler, IDeselectHandler,
    IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private TextMeshProUGUI targetText;

    private void Awake()
    {
        if (targetText == null)
            targetText = GetComponentInChildren<TextMeshProUGUI>();
    }

    public void OnSelect(BaseEventData eventData) => AddUnderline();
    public void OnDeselect(BaseEventData eventData) => RemoveUnderline();
    public void OnPointerEnter(PointerEventData eventData) => AddUnderline();
    public void OnPointerExit(PointerEventData eventData) => RemoveUnderline();

    private void AddUnderline()
    {
        if (targetText != null)
            targetText.fontStyle |= FontStyles.Underline;
    }

    private void RemoveUnderline()
    {
        if (targetText != null)
            targetText.fontStyle &= ~FontStyles.Underline;
    }
}