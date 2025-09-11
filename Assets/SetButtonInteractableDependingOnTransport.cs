using System;
using UnityEngine;
using UnityEngine.UI; 

public class SetButtonInteractableDependingOnTransport : MonoBehaviour
{
    TransportSwitcher transportSwitcher;
    public Button button;
    private void Start()
    {
       transportSwitcher = TransportSwitcher.Instance;
       transportSwitcher.onSwitchToFacepunchTransport.AddListener(MakeInteractable);
       transportSwitcher.onSwitchToUnityTransport.AddListener(MakeNonInteractable);
    }

    private void OnDisable()
    {
        transportSwitcher.onSwitchToFacepunchTransport.RemoveListener(MakeInteractable);
        transportSwitcher.onSwitchToUnityTransport.RemoveListener(MakeNonInteractable);
    }

    void MakeInteractable()
    {
        button.interactable = true; 
    }

    void MakeNonInteractable()
    {
        button.interactable = false; 
    }
}
