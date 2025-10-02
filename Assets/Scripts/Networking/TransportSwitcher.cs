using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Netcode.Transports.Facepunch;

/// <summary>
/// Dynamically switches between Unity Transport and Relay Transport depending on internet availability.
/// Provides events for switching and ensures safe transport changes at runtime.
/// </summary>
public class TransportSwitcher : MonoBehaviour
{
    /// <summary>Singleton instance of the TransportSwitcher.</summary>
    public static TransportSwitcher Instance;

    private float updateDuration = 0.2f; // How often to check for connection
    private float timer = 0f;

    /// <summary>True if Relay Transport is currently in use.</summary>
    public bool isUsingRelay = false; 

    [SerializeField] private UnityTransport unityTransport;
    [SerializeField] private UnityTransport relayTransport;

    /// <summary>Event triggered when switching to Unity Transport.</summary>
    public UnityEvent onSwitchToUnityTransport;

    /// <summary>Event triggered when switching to Relay Transport.</summary>
    public UnityEvent onSwitchToRelayTransport;

    [ReadOnly] 
    public bool canSwitch = true; // Whether transport switching is allowed

    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private bool isCheckingConnection = false; // Prevent multiple simultaneous checks
    private bool hasConnection = false;        // Stores internet connection status

    private MonoBehaviour currentTransport;     // Reference to the currently active transport

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

    /// <summary>
    /// Resets transport switching when returning to the main menu.
    /// </summary>
    private void SceneManagerOnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == mainMenuSceneName)
        {
            canSwitch = true;
            isUsingRelay = false; 
        }
    }

    private void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            timer = updateDuration;

            // Skip if network is active or already connected
            if (!NetworkManager.Singleton || NetworkManager.Singleton.IsListening || NetworkManager.Singleton.IsConnectedClient)
                return;

            if (!isCheckingConnection && canSwitch)
            {
                StartCoroutine(PerformSwitchRoutine());
            }
        }
    }

    /// <summary>
    /// Coroutine that checks internet connection and switches transport accordingly.
    /// </summary>
    private IEnumerator PerformSwitchRoutine()
    {
        isCheckingConnection = true;

        yield return StartCoroutine(CheckInternetConnection("https://www.google.com"));

        if (!canSwitch)
        {
            isCheckingConnection = false;
            yield break;
        }

        MonoBehaviour newTransport = hasConnection ? relayTransport : unityTransport;

        if (NetworkManager.Singleton.NetworkConfig.NetworkTransport != newTransport)
        {
            if (newTransport == relayTransport)
            {
                NetworkManager.Singleton.NetworkConfig.NetworkTransport = relayTransport;
                isUsingRelay = true;
                onSwitchToRelayTransport?.Invoke();
            }
            else
            {
                NetworkManager.Singleton.NetworkConfig.NetworkTransport = unityTransport;
                isUsingRelay = false; 
                onSwitchToUnityTransport?.Invoke();
            }

            currentTransport = newTransport;
        }

        isCheckingConnection = false;
    }

    /// <summary>
    /// Checks internet connectivity by sending a GET request to a specified URI.
    /// </summary>
    /// <param name="uri">URL to test connectivity.</param>
    private IEnumerator CheckInternetConnection(string uri)
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
        {
            yield return webRequest.SendWebRequest();
            hasConnection = (webRequest.result == UnityWebRequest.Result.Success);
        }
    }

    /// <summary>
    /// Returns true if Steamworks client is valid/connected.
    /// </summary>
    private bool IsSteamConnected()
    {
        return Steamworks.SteamClient.IsValid;
    }

    /// <summary>
    /// Forces a switch to Unity Transport and disables further automatic switching.
    /// </summary>
    public void SwitchToUnityTransportAndDisable()
    {
        canSwitch = false;
        NetworkManager.Singleton.NetworkConfig.NetworkTransport = unityTransport;
        currentTransport = unityTransport;
        isUsingRelay = false;
        onSwitchToUnityTransport?.Invoke();
    }

    /// <summary>
    /// Forces a switch to Relay Transport and disables further automatic switching.
    /// </summary>
    public void SwitchToRelayTransportAndDisable()
    {
        canSwitch = false;
        NetworkManager.Singleton.NetworkConfig.NetworkTransport = relayTransport;
        currentTransport = relayTransport;
        isUsingRelay = true; 
        onSwitchToRelayTransport?.Invoke();
    }
}