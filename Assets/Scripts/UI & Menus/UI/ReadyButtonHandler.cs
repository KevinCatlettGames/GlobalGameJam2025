using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using FMODUnity;

public class ReadyButtonWithRadial_AutoLongPress : MonoBehaviour
{
    [Header("Input")]
    public InputActionProperty readyAction;

    [Header("UI")]
    public Image radialFillImage; // Assign a radial UI Image

    [Header("Settings")]
    public float holdTimeThreshold = 1f; // Time to trigger long press

    private float pressStartTime;
    private bool isPressing;
    private bool longPressTriggered;
    public StudioEventEmitter emitter;
    public StudioEventEmitter onReadyTriggeredEmitter;
    public UnityEvent OnReadyTriggered; 
    
    private void OnEnable()
    {
        // Reset radial fill when enabled
        if (radialFillImage != null)
            radialFillImage.fillAmount = 0f;

        readyAction.action.started += OnReadyStarted;
        readyAction.action.canceled += OnReadyCanceled;
        readyAction.action.Enable();
    }

    private void OnDisable()
    {
        readyAction.action.started -= OnReadyStarted;
        readyAction.action.canceled -= OnReadyCanceled;
        readyAction.action.Disable();
    }

    private void Update()
    {
        if (isPressing && radialFillImage != null)
        {
            float duration = Time.time - pressStartTime;

            // Fill the radial proportionally
            radialFillImage.fillAmount = Mathf.Clamp01(duration / holdTimeThreshold);

            // Trigger long press automatically when threshold is reached
            if (!longPressTriggered && duration >= holdTimeThreshold)
            {
                longPressTriggered = true;
                radialFillImage.fillAmount = 1f; // ensure radial is full
                OnLongPress();
            }
        }
    }

    public void StartReady()
    {
        pressStartTime = Time.time;
        isPressing = true;
        longPressTriggered = false;

        if (radialFillImage != null)
            radialFillImage.fillAmount = 0f;

        emitter?.Play();
    }

    public void EndReady()
    {
        isPressing = false;

        float duration = Time.time - pressStartTime;

        if (!longPressTriggered)
        {
            // Quick tap: reset radial immediately
            if (radialFillImage != null)
                radialFillImage.fillAmount = 0f;

            OnQuickTap();
        }

        emitter?.Stop();
    }

    private void OnReadyStarted(InputAction.CallbackContext context)
    {
        pressStartTime = Time.time;
        isPressing = true;
        longPressTriggered = false;

        if (radialFillImage != null)
            radialFillImage.fillAmount = 0f;

        emitter?.Play();
    }

    private void OnReadyCanceled(InputAction.CallbackContext context)
    {
        isPressing = false;

        float duration = Time.time - pressStartTime;

        if (!longPressTriggered)
        {
            // Quick tap: reset radial immediately
            if (radialFillImage != null)
                radialFillImage.fillAmount = 0f;

            OnQuickTap();
        }

        emitter?.Stop();
    }

    private void OnQuickTap()
    {
    }

    private void OnLongPress()
    {
        onReadyTriggeredEmitter?.Play();
        Invoke(nameof(CallEvent), 1f);
    }

    void CallEvent()
    {
        OnReadyTriggered?.Invoke();
    }
}
