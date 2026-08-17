using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MenuInputSystem : MonoBehaviour
{
    #region Singleton
    public static MenuInputSystem Instance { get; private set; }
    #endregion

    #region Enums
    public enum GameDevice { KeyboardMouse, Gamepad }
    #endregion

    #region Fields
    public GameDevice activeGameDevice = GameDevice.Gamepad; // Set default state to Gamepad

    [SerializeField] bool onlyWhenPaused = false;

    [Header("Deadzone Settings")]
    [SerializeField, Range(0.01f, 0.9f)]
    private float stickDeadzone = 0.25f;

    [SerializeField, Range(0.01f, 0.9f)]
    private float triggerDeadzone = 0.1f;

    [SerializeField]
    private float mouseMoveThreshold = 1.0f;

    private bool isInitialized = false;
    #endregion

    #region Events
    public Action<GameDevice> OnGameDeviceChanged;
    #endregion

    #region Unity Methods

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        transform.parent = null;
    }

    private void Start()
    {
        activeGameDevice = GameDevice.Gamepad;
        SetMouseVisibility(false);
        OnGameDeviceChanged?.Invoke(activeGameDevice);

        StartCoroutine(EnableInputDetectionRoutine());
    }

    private IEnumerator EnableInputDetectionRoutine()
    {
        yield return null;
        isInitialized = true;
    }

    private void OnEnable()
    {
        InputSystem.onEvent += OnInputEvent;
    }

    private void OnDisable()
    {
        InputSystem.onEvent -= OnInputEvent;
        isInitialized = false;
    }

    #endregion

    #region Input Detection

    private void OnInputEvent(InputEventPtr eventPtr, InputDevice device)
    {
        if (!isInitialized) return;

        if (!eventPtr.IsA<StateEvent>() && !eventPtr.IsA<DeltaStateEvent>())
            return;

        if (onlyWhenPaused && Time.timeScale > 0) return;

        if (device == null)
            return;

        if (device is Gamepad gamepad)
        {
            if (HasGamepadInputExceededDeadzone(eventPtr, gamepad))
            {
                ChangeActiveGameDevice(GameDevice.Gamepad);
                if (EventSystem.current == null) return;

                if (EventSystem.current.currentSelectedGameObject == null)
                {
                    Button firstButton = FindFirstObjectByType<Button>();

                    if (firstButton != null)
                    {
                        EventSystem.current.SetSelectedGameObject(firstButton.gameObject);
                    }
                }
            }
        }
        else if (device is Keyboard keyboard)
        {
            if (HasKeyboardInputBeenPressed(eventPtr, keyboard))
            {
                ChangeActiveGameDevice(GameDevice.KeyboardMouse);
            }
        }
        else if (device is Mouse mouse)
        {
            if (HasMouseInputExceededThreshold(eventPtr, mouse))
            {
                ChangeActiveGameDevice(GameDevice.KeyboardMouse);
            }
        }
    }

    private bool HasKeyboardInputBeenPressed(InputEventPtr eventPtr, Keyboard keyboard)
    {
        foreach (var control in eventPtr.EnumerateChangedControls(keyboard))
        {
            if (control is KeyControl keyControl && keyControl.ReadValueFromEvent(eventPtr) >= keyControl.pressPoint)
            {
                return true;
            }
        }
        return false;
    }

    private bool HasGamepadInputExceededDeadzone(InputEventPtr eventPtr, Gamepad gamepad)
    {
        foreach (var control in eventPtr.EnumerateChangedControls(gamepad))
        {
            if (control is Vector2Control vectorControl)
            {
                Vector2 value = vectorControl.ReadValueFromEvent(eventPtr);
                if (value.magnitude >= stickDeadzone)
                    return true;
            }
            else if (control is AxisControl axisControl)
            {
                float value = axisControl.ReadValueFromEvent(eventPtr);
                if (Mathf.Abs(value) >= triggerDeadzone)
                    return true;
            }
            else if (control is ButtonControl buttonControl)
            {
                if (buttonControl.ReadValueFromEvent(eventPtr) >= buttonControl.pressPoint)
                    return true;
            }
        }

        return false;
    }

    private bool HasMouseInputExceededThreshold(InputEventPtr eventPtr, Mouse mouse)
    {
        Vector2 deltaFromEvent = mouse.delta.ReadValueFromEvent(eventPtr);

        if (deltaFromEvent.sqrMagnitude >= (mouseMoveThreshold * mouseMoveThreshold))
        {
            return true;
        }

        foreach (var control in eventPtr.EnumerateChangedControls(mouse))
        {
            if (control == mouse.position || control == mouse.delta)
                continue;

            if (control is ButtonControl button && button.ReadValueFromEvent(eventPtr) >= button.pressPoint)
            {
                return true;
            }
            else if (control is Vector2Control scroll && scroll == mouse.scroll)
            {
                if (scroll.ReadValueFromEvent(eventPtr).sqrMagnitude > 0.01f)
                    return true;
            }
        }

        return false;
    }

    #endregion

    #region Device Switching

    private void ChangeActiveGameDevice(GameDevice newDevice)
    {
        if (activeGameDevice == newDevice)
            return;

        activeGameDevice = newDevice;
        OnGameDeviceChanged?.Invoke(activeGameDevice);

        SetMouseVisibility(activeGameDevice == GameDevice.KeyboardMouse);
    }

    #endregion

    #region Mouse Control

    public void SetMouseVisibility(bool isKeyboardMouse)
    {
        Cursor.visible = isKeyboardMouse;
        Cursor.lockState = isKeyboardMouse ? CursorLockMode.None : CursorLockMode.Locked;
    }

    #endregion

    #region Rumble

    public IEnumerator RumbleController(float low, float high, float duration)
    {
        if (Gamepad.current == null)
            yield break;

        Gamepad.current.SetMotorSpeeds(low, high);
        yield return new WaitForSeconds(duration);
        Gamepad.current.SetMotorSpeeds(0f, 0f);
    }

    #endregion
}