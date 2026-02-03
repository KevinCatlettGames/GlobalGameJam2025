using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

[RequireComponent(typeof(TMP_InputField))]
public class BetterInputFieldUX : MonoBehaviour, IPointerClickHandler
{
    private TMP_InputField input;
    private TextMeshProUGUI placeholder;

    void Awake()
    {
        input = GetComponent<TMP_InputField>();
        placeholder = input.placeholder as TextMeshProUGUI;

        input.onSelect.AddListener(_ =>
        {
            HidePlaceholder();
            StartCoroutine(ActivateNextFrame());
        });

        input.onDeselect.AddListener(_ =>
        {
            if (string.IsNullOrEmpty(input.text))
                ShowPlaceholder();
        });
    }

    void Update()
    {
        if (!input.isFocused)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ExitInput(true);
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            ExitInput(false);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        HidePlaceholder();
        StartCoroutine(ActivateNextFrame());
    }

    private void ExitInput(bool cancel)
    {
        if (cancel)
            input.text = "";

        input.DeactivateInputField();
        ShowPlaceholder();

        EventSystem.current.SetSelectedGameObject(null);
        StartCoroutine(RestoreNavigationNextFrame());
    }

    private System.Collections.IEnumerator ActivateNextFrame()
    {
        yield return null;
        input.ActivateInputField();
    }

    private System.Collections.IEnumerator RestoreNavigationNextFrame()
    {
        yield return null;

        if (EventSystem.current != null)
        {
            var selectable = input.FindSelectableOnDown()
                           ?? input.FindSelectableOnUp()
                           ?? input.FindSelectableOnRight()
                           ?? input.FindSelectableOnLeft();

            if (selectable != null)
                EventSystem.current.SetSelectedGameObject(selectable.gameObject);
        }
    }

    private void HidePlaceholder()
    {
        if (placeholder != null)
            placeholder.gameObject.SetActive(false);
    }

    private void ShowPlaceholder()
    {
        if (placeholder != null)
            placeholder.gameObject.SetActive(true);
    }
}