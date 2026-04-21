using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI; 
using UnityEngine.EventSystems;
public class MenuInputSelectionHandler : MonoBehaviour
{
    public EventSystem eventSystem;
    public Button button;
    public bool reactOnEnable = false;
    
    private void Start()
    {
        if(MenuInputSystem.Instance) 
            MenuInputSystem.Instance.OnGameDeviceChanged += ReactToDeviceChange;
    }

    private void OnEnable()
    {
        if (MenuInputSystem.Instance && reactOnEnable)
        {
            ReactToDeviceChange(MenuInputSystem.Instance.activeGameDevice);
            MenuInputSystem.Instance.OnGameDeviceChanged += ReactToDeviceChange;
        }
    }

    private void OnDisable()
    {
        MenuInputSystem.Instance.OnGameDeviceChanged -= ReactToDeviceChange;
    }

    public void ReactToDeviceChange(MenuInputSystem.GameDevice gameDevice)
    {
        CursorManager.instance.ShowMouse();
        if (gameDevice == MenuInputSystem.GameDevice.Gamepad)
        {
            if(button.interactable) 
                eventSystem.SetSelectedGameObject(button.gameObject);
          
            
            CursorManager.instance.HideMouse();
        }
        else
        {
            CursorManager.instance.ShowMouse();
        }
    }
}