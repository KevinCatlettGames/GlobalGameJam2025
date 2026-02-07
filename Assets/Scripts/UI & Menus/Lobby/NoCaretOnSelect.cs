using TMPro;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class NoCaretOnSelect : MonoBehaviour, ISelectHandler
{
    private TMP_InputField input;

    void Awake()
    {
        input = GetComponent<TMP_InputField>();
    }

    public void OnSelect(BaseEventData eventData)
    {
        StartCoroutine(DisableCaretNextFrame());
    }

    private IEnumerator DisableCaretNextFrame()
    {
        yield return null;

        if (input != null && input.isFocused)
        {
            input.DeactivateInputField();
        }
        
        EventSystem.current.SetSelectedGameObject(gameObject);
    }
}