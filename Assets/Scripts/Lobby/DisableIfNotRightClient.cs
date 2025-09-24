using System;
using Unity.Netcode;
using UnityEngine;

public class DisableIfNotRightClient : MonoBehaviour
{
    public ulong clientID;

    private void Awake()
    {
        if(TransportSwitcher.Instance && TransportSwitcher.Instance.isUsingRelay && clientID != NetworkManager.Singleton.LocalClientId)
            gameObject.SetActive(false);
    }
}
