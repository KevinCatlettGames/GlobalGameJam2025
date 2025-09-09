using System;
using Netcode.Transports.Facepunch;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class TransportSwitcher : MonoBehaviour
{
    private float updateDuration = 0.2f;
    private float timer = 0;

    [SerializeField] private UnityTransport unityTransport;
    [SerializeField] private FacepunchTransport facepunchTransport;

    public UnityEvent onSwitchToUnityTransport;
    public UnityEvent onSwitchToFacepunchTransport;

    [ReadOnly]
    public bool canSwitch = true;
    
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    private void Start()
    {
        SceneManager.sceneLoaded += SceneManagerOnsceneLoaded;
    }

    private void SceneManagerOnsceneLoaded(Scene arg0, LoadSceneMode arg1)
    {
        if(arg0.name == mainMenuSceneName) 
            canSwitch = true; 
    }

    private void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            timer = updateDuration;
            PerformSwitch();
        }
    }

    private void PerformSwitch()
    {
        if (!canSwitch) return; 
        
        bool steamConnected = IsSteamConnected();
        if (steamConnected)
        {
            // Use Facepunch if both are available
            if (NetworkManager.Singleton.NetworkConfig.NetworkTransport != facepunchTransport)
            {
                Debug.Log("Switching to Facepunch Transport");
                NetworkManager.Singleton.NetworkConfig.NetworkTransport = facepunchTransport;
                onSwitchToFacepunchTransport?.Invoke();
            }
        }
        else
        {
            // Otherwise use UnityTransport
            if (NetworkManager.Singleton.NetworkConfig.NetworkTransport != unityTransport)
            {
                Debug.Log("Switching to Unity Transport");
                NetworkManager.Singleton.NetworkConfig.NetworkTransport = unityTransport;
                onSwitchToUnityTransport?.Invoke();
            }
        }
    }
    
    private bool IsSteamConnected()
    {
        return Steamworks.SteamClient.IsValid;
    }

    public void SwitchToUnityTransportAndDisable()
    {
        canSwitch = false; 
        NetworkManager.Singleton.NetworkConfig.NetworkTransport = unityTransport;
    }

    public void SwitchToFacepunchTransportAndDisable()
    {
        canSwitch = false;
        NetworkManager.Singleton.NetworkConfig.NetworkTransport = facepunchTransport;
    }
}