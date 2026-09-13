using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.XInput;
using UnityEngine.UI;

public class PlayerDeviceDependendInputIcon : MonoBehaviour
{
    [System.Serializable]
    public struct DeviceSpriteBinding
    {
        public DeviceType deviceType;
        public Sprite sprite;
    }

    [SerializeField] private DeviceSpriteBinding[] spriteBindings;
    [SerializeField] private int playerIndex;

    private Image targetImage;
    
    private void Awake()
    {
        targetImage = GetComponent<Image>();
    }

    private void OnEnable()
    {
        Invoke(nameof(Init), .2f);
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(Init));
    }

    private void Init()
    {
        DeviceType deviceType = DeviceType.KeyboardMouse;
        InputDevice device = LobbyPlayerValues.Instance.playerValuesList[playerIndex].Device;

#if UNITY_SWITCH
        deviceType = DeviceType.Joycon;
        UpdateIcon(deviceType);
        return;
#endif

#if UNITY_STANDALONE_LINUX
        deviceType = DeviceType.Xbox;
        UpdateIcon(deviceType);
        return;
#endif

        if (device is Keyboard || device is Mouse)
        {
            deviceType = DeviceType.KeyboardMouse;
        }
        else if (device is DualSenseGamepadHID || device is DualShockGamepad)
        {
            deviceType = DeviceType.PlayStation;
        }
        else if (device is XInputController)
        {
            deviceType = DeviceType.Xbox;
        }

        UpdateIcon(deviceType);
    }

    private void UpdateIcon(DeviceType newDeviceType)
    {
        bool set = false;
        foreach (var binding in spriteBindings)
        {
            if (binding.deviceType == newDeviceType && binding.sprite != null)
            {
                set = true;
                targetImage.sprite = binding.sprite;
            }
        }
        if (!set)
            targetImage.sprite = spriteBindings[0].sprite;
    }
}