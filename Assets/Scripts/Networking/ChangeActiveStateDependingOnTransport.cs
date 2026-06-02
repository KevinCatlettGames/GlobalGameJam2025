using System;
using UnityEngine;

public class ChangeActiveStateDependingOnTransport : MonoBehaviour
{
    public bool activeWithRelay = true;

    private void Awake()
    {
        if (TransportSwitcher.Instance && TransportSwitcher.Instance.isUsingRelay && activeWithRelay)
            gameObject.SetActive(true);
        
        if(TransportSwitcher.Instance && !TransportSwitcher.Instance.isUsingRelay && activeWithRelay)
            gameObject.SetActive(false);
            
    }
}