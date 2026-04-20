using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI; 
using UnityEngine.EventSystems;
public class MenuDeviceChangeHandler : MonoBehaviour
{
    public EventSystem eventSystem;
    public Button button;
    public bool reactOnEnable = false;
    
    private void Start()
    {
        if(GameInput.Instance) 
            GameInput.Instance.OnGameDeviceChanged += ReactToDeviceChange;
    }

    private void OnEnable()
    {
        if (GameInput.Instance && reactOnEnable)
        {
            ReactToDeviceChange(GameInput.Instance.activeGameDevice);
            GameInput.Instance.OnGameDeviceChanged += ReactToDeviceChange;
        }
    }

    private void OnDisable()
    {
        GameInput.Instance.OnGameDeviceChanged -= ReactToDeviceChange;
    }

    public void ReactToDeviceChange(GameInput.GameDevice gameDevice)
    {
        MouseVisibilityHandler.instance.ShowMouse();
        if (gameDevice == GameInput.GameDevice.Gamepad)
        {
            if(button.interactable) 
                eventSystem.SetSelectedGameObject(button.gameObject);
          
            
            MouseVisibilityHandler.instance.HideMouse();
        }
        else
        {
            MouseVisibilityHandler.instance.ShowMouse();
        }
    }
}