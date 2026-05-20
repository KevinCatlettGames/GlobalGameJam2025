using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI; 

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

        // Gamepad detected
        if (device is Gamepad)
        {
            ChangeActiveGameDevice(GameDevice.Gamepad);
        }
        // Keyboard or Mouse detected
        else if (device is Keyboard || device is Mouse)
        {
            ChangeActiveGameDevice(GameDevice.KeyboardMouse);
        }
    }

    #endregion

    #region Device Switching

    private void ChangeActiveGameDevice(GameDevice newDevice)
    {
        if (activeGameDevice == newDevice)
            return;

        //if (newDevice == GameDevice.Gamepad)
        //{
        //    EventSystem.current.SetSelectedGameObject(FindFirstObjectByType<Button>().gameObject);
        //}

        activeGameDevice = newDevice;
        OnGameDeviceChanged?.Invoke(activeGameDevice);

        SetMouseVisibility(activeGameDevice == GameDevice.KeyboardMouse);
    }

    #endregion

    #region Mouse Control

    public void SetMouseVisibility(bool value)
    {
        Cursor.visible = value && activeGameDevice != GameDevice.Gamepad;
        Cursor.lockState = (value && activeGameDevice != GameDevice.Gamepad)
            ? CursorLockMode.None
            : CursorLockMode.Locked;
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