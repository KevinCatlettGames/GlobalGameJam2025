using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class DynamicInputIcon : MonoBehaviour
{
    [System.Serializable]
    public struct DeviceSpriteBinding
    {
        public DeviceType deviceType;
        public Sprite sprite;
    }

    [SerializeField] private DeviceSpriteBinding[] spriteBindings;

    private Image targetImage;

    private void Awake()
    {
        targetImage = GetComponent<Image>();
    }

    private void OnEnable()
    {
        InputDeviceDetector.OnDeviceChanged += UpdateIcon;

        if (InputDeviceDetector.Instance != null)
        {
            UpdateIcon(InputDeviceDetector.Instance.CurrentDeviceType);
        }
    }

    private void OnDisable()
    {
        InputDeviceDetector.OnDeviceChanged -= UpdateIcon;
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
        if(!set)
            targetImage.sprite = spriteBindings[0].sprite;
    }
}