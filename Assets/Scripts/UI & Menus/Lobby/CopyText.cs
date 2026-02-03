using System;
using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class CopyInputFieldText : MonoBehaviour, IPointerClickHandler
{
    public InputActionProperty copyAction; 
    
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private TextMeshProUGUI copiedText;


    private void OnEnable()
    {
        copyAction.action.Enable();
        copyAction.action.performed += OnCopyAction;
    }

    private void OnDisable()
    {
        copyAction.action.Disable();
        copyAction.action.performed -= OnCopyAction;
    }
    
    private void OnCopyAction(InputAction.CallbackContext obj)
    {
        Copy();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Copy();
    }

    void Copy()
    {
        if (inputField != null)
        {
            GUIUtility.systemCopyBuffer = inputField.text;
            copiedText.gameObject.SetActive(true);
            Debug.Log("copied");
            Invoke(nameof(DisableCopiedText), 2f);
        }
    }

    void DisableCopiedText()
    {
        copiedText.gameObject.SetActive(false);
    }
}