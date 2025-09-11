using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Netcode.Transports.Facepunch;

public class TransportSwitcher : MonoBehaviour
{
    public static TransportSwitcher Instance;
    
    private float updateDuration = 0.2f;
    private float timer = 0f;

    public bool isUsingRelay = false; 
    
    [SerializeField] private UnityTransport unityTransport;
    [SerializeField] private UnityTransport relayTransport;

    public UnityEvent onSwitchToUnityTransport;
    public UnityEvent onSwitchToRelayTransport;

    [ReadOnly] public bool canSwitch = true;

    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private bool isCheckingConnection = false;
    private bool hasConnection = false;

    private MonoBehaviour currentTransport;  // Store which concrete transport is currently in use

    private void Awake()
    {
        if(Instance == null)
            Instance = this;
        else
            Destroy(this.gameObject);
    }

    private void Start()
    {
        SceneManager.sceneLoaded += SceneManagerOnSceneLoaded;
        currentTransport = NetworkManager.Singleton.NetworkConfig.NetworkTransport as MonoBehaviour;
    }

    private void SceneManagerOnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == mainMenuSceneName)
        {
            canSwitch = true;
        }
    }

    private void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            timer = updateDuration;

            if (!NetworkManager.Singleton || NetworkManager.Singleton.IsListening || NetworkManager.Singleton.IsConnectedClient)
                return;

            if (!isCheckingConnection && canSwitch)
            {
                StartCoroutine(PerformSwitchRoutine());
            }
        }
    }

    private IEnumerator PerformSwitchRoutine()
    {
        isCheckingConnection = true;

        yield return StartCoroutine(CheckInternetConnection("https://www.google.com"));

        if (!canSwitch)
        {
            isCheckingConnection = false;
            yield break;
        }

        MonoBehaviour newTransport = hasConnection
            ? relayTransport
            : unityTransport;

        if (NetworkManager.Singleton.NetworkConfig.NetworkTransport != newTransport)
        {
            if (newTransport == relayTransport)
            {
                Debug.Log("[TransportSwitcher] Switching to Relay Transport");
                NetworkManager.Singleton.NetworkConfig.NetworkTransport = relayTransport;
                isUsingRelay = true;
                onSwitchToRelayTransport?.Invoke();
            }
            else
            {
                Debug.Log("[TransportSwitcher] Switching to Unity Transport");
                isUsingRelay = false; 
                NetworkManager.Singleton.NetworkConfig.NetworkTransport = unityTransport;
                onSwitchToUnityTransport?.Invoke();
            }

            currentTransport = newTransport;
        }

        isCheckingConnection = false;
    }

    private IEnumerator CheckInternetConnection(string uri)
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
        {
            yield return webRequest.SendWebRequest();

            hasConnection = (webRequest.result == UnityWebRequest.Result.Success);
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
        currentTransport = unityTransport;
        isUsingRelay = false;
        onSwitchToUnityTransport?.Invoke();
    }

    public void SwitchToRelayTransportAndDisable()
    {
        canSwitch = false;
        NetworkManager.Singleton.NetworkConfig.NetworkTransport = relayTransport;
        currentTransport = relayTransport;
        isUsingRelay = true; 
        onSwitchToRelayTransport?.Invoke();
    }
}
