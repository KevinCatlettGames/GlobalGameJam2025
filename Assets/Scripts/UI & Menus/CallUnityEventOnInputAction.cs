using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using System.Collections;

public class CallUnityEventOnInputAction : MonoBehaviour
{
    [SerializeField] InputActionProperty inputActionProperty;
    public UnityEvent OnInputActionPerformed;
    public bool oneTimeUsePerActivation = true;

    private Coroutine enableRoutine;

    private void OnEnable()
    {
        enableRoutine = StartCoroutine(EnableAfterDelay());
    }

    private IEnumerator EnableAfterDelay()
    {
        yield return new WaitForSeconds(0.2f);

        inputActionProperty.action.performed += InputActionPerformed;
        inputActionProperty.action.Enable();
    }

    private void OnDisable()
    {
        if (enableRoutine != null)
        {
            StopCoroutine(enableRoutine);
            enableRoutine = null;
        }

        inputActionProperty.action.performed -= InputActionPerformed;
        inputActionProperty.action.Disable();
    }

    private void InputActionPerformed(InputAction.CallbackContext obj)
    {
        OnInputActionPerformed?.Invoke();

        if (oneTimeUsePerActivation)
        {
            inputActionProperty.action.performed -= InputActionPerformed;
            inputActionProperty.action.Disable();
        }
    }
}