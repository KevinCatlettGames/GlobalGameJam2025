using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using System.Collections;

public class CallUnityEventOnInputAction : MonoBehaviour
{
    [SerializeField] private InputActionProperty inputActionProperty;

    [Tooltip("Ignore input briefly after enable")]
    public bool requireFreshPressOnEnable;
    public bool allowOnSwitch = true;
    [SerializeField] private float freshPressBlockDuration = 0.2f;

    public UnityEvent OnInputActionPerformed;
    bool initialFreshPress; 
    public bool oneTimeUsePerActivation = true;

    private Coroutine enableRoutine;
    private bool canTrigger;

    private void Awake()
    {
        initialFreshPress = requireFreshPressOnEnable;
    }
    private void OnEnable()
    {
        requireFreshPressOnEnable = initialFreshPress;
        canTrigger = !requireFreshPressOnEnable;

        enableRoutine = StartCoroutine(EnableAfterDelay());

        if (requireFreshPressOnEnable)
        {
            StartCoroutine(UnlockInputAfterDelay());
        }
    }

    private IEnumerator EnableAfterDelay()
    {
        yield return new WaitForSeconds(0.2f);

        inputActionProperty.action.performed += InputActionPerformed;
        inputActionProperty.action.Enable();
    }

    private IEnumerator UnlockInputAfterDelay()
    {
        yield return new WaitForSeconds(freshPressBlockDuration);

        canTrigger = true;
        requireFreshPressOnEnable = false; // auto reset
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

    private void InputActionPerformed(InputAction.CallbackContext context)
    {

#if UNITY_SWITCH
        if (!allowOnSwitch)
            return;
#endif 

        if (!canTrigger)
            return;

        OnInputActionPerformed?.Invoke();

        if (oneTimeUsePerActivation)
        {
            inputActionProperty.action.performed -= InputActionPerformed;
            inputActionProperty.action.Disable();
        }
    }

    public void RequireFreshPress()
    {
        requireFreshPressOnEnable = true;
    }
}