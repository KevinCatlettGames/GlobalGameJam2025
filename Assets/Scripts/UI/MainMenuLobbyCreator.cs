using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;
using UnityEngine.Serialization;

/// <summary>
/// Handles the creation and management of lobbies in the main menu,
/// including initializing Unity services, starting local games,
/// and managing relay server connections.
/// </summary>
public class MainMenuLobbyCreator : MonoBehaviour
{
    /// <summary>
    /// Singleton instance of the MainMenuLobbyCreator for global access.
    /// </summary>
    public static MainMenuLobbyCreator Instance;

    /// <summary>
    /// The lobby that the player has joined or created.
    /// </summary>
    private Lobby joinedLobby;

    /// <summary>
    /// Reference to the relay server heartbeat component to manage lobby heartbeat.
    /// </summary>
    [FormerlySerializedAs("lobbyHeartBeat")] 
    public RelayServerHeartbeat relayServerHeartbeat;

    /// <summary>
    /// Unity Awake method, ensures that this class follows the Singleton pattern.
    /// </summary>
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    /// <summary>
    /// Unity Start method, initializes Unity Authentication asynchronously.
    /// </summary>
    private void Start()
    {
        InitializeUnityAuth();
    }

    /// <summary>
    /// Starts a local game, switches the network transport, starts the host, 
    /// sets the current lobby, and loads the Lobby scene.
    /// </summary>
    /// <param name="sceneName">The name of the scene to load (currently loads "Lobby").</param>
    public async void StartGameLocal(string sceneName)
    {
        NetworkManager.Singleton
            .GetComponent<TransportSwitcher>()
            .SwitchToUnityTransportAndDisable();

        NetworkManager.Singleton.StartHost();
        relayServerHeartbeat.joinedLobby = joinedLobby;
        GlobalLobby.CurrentLobby = joinedLobby;
        NetworkManager.Singleton.SceneManager.LoadScene("UI_Lobby", LoadSceneMode.Single);
    }

    /// <summary>
    /// Opens the Lobby scene directly.
    /// </summary>
    public void OpenLobby()
    {
        SceneManager.LoadScene("Lobby", LoadSceneMode.Single);
    }

    /// <summary>
    /// Initializes Unity Services and signs in the player anonymously.
    /// Ensures services are initialized only once.
    /// </summary>
    private async void InitializeUnityAuth()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            var initializationOptions = new InitializationOptions()
                .SetProfile(Random.Range(0, 10000).ToString());

            await UnityServices.InitializeAsync(initializationOptions);
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }
}
