using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;

public class MenuInputSystem : MonoBehaviour
{
    #region Singleton
    public static MenuInputSystem Instance { get; private set; }
    #endregion

    #region Enums
    public enum GameDevice { KeyboardMouse, Gamepad }
    #endregion

    #region Fields
    public GameDevice activeGameDevice;

    [Header("Deadzone Settings")]
    [SerializeField, Range(0.01f, 0.9f)]
    private float stickDeadzone = 0.25f;

    [SerializeField, Range(0.01f, 0.9f)]
    private float triggerDeadzone = 0.1f;

    [SerializeField]
    private float mouseMoveThreshold = 1.0f;
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

    private void OnEnable()
    {
        InputSystem.onEvent += OnInputEvent;
    }

    private void OnDisable()
    {
        InputSystem.onEvent -= OnInputEvent;
    }

    #endregion

    #region Input Detection

    private void OnInputEvent(InputEventPtr eventPtr, InputDevice device)
    {
        if (device == null)
            return;

        if (device is Gamepad gamepad)
        {
            if (HasGamepadInputExceededDeadzone(eventPtr, gamepad))
            {
                ChangeActiveGameDevice(GameDevice.Gamepad);
            }
        }
        else if (device is Keyboard)
        {
            ChangeActiveGameDevice(GameDevice.KeyboardMouse);
        }
        else if (device is Mouse mouse)
        {
            if (HasMouseInputExceededThreshold(eventPtr, mouse))
            {
                ChangeActiveGameDevice(GameDevice.KeyboardMouse);
            }
        }
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
        if (mouse.delta.ReadValue().sqrMagnitude >= (mouseMoveThreshold * mouseMoveThreshold))
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