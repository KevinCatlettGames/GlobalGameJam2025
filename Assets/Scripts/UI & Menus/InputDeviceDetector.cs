using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;

public enum DeviceType
{
    KeyboardMouse,
    Xbox,
    PlayStation,
    Joycon
}

public class InputDeviceDetector : MonoBehaviour
{
    public static InputDeviceDetector Instance { get; private set; }
    public static event Action<DeviceType> OnDeviceChanged;

    public DeviceType CurrentDeviceType { get; private set; } = DeviceType.KeyboardMouse;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        transform.parent = null;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        InputSystem.onActionChange += OnActionChange;
    }

    private void OnDisable()
    {
        InputSystem.onActionChange -= OnActionChange;
    }

    private void OnActionChange(object obj, InputActionChange change)
    {
        if (change != InputActionChange.ActionPerformed) return;

        var action = obj as InputAction;
        var control = action?.activeControl;

        if (control == null) return;

        DeviceType detectedType = IdentifyDevice(control.device);

        if (detectedType != CurrentDeviceType)
        {
            CurrentDeviceType = detectedType;
            OnDeviceChanged?.Invoke(CurrentDeviceType);
        }
    }

    private DeviceType IdentifyDevice(InputDevice device)
    {
#if UNITY_SWITCH
        return DeviceType.Joycon;
#endif

        if (device is Keyboard || device is Mouse)
            return DeviceType.KeyboardMouse;

        if (device is DualShockGamepad ||
            device.name.Contains("DualShock", StringComparison.OrdinalIgnoreCase) ||
            device.name.Contains("DualSense", StringComparison.OrdinalIgnoreCase))
        {
            return DeviceType.PlayStation;
        }

        return DeviceType.Xbox;
    }
}