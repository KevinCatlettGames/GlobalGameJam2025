using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using System.Threading.Tasks;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;
using Febucci.UI;

/// <summary>
/// Holds the currently active lobby globally.
/// </summary>
public static class GlobalLobby
{
    /// <summary>
    /// The currently active lobby.
    /// </summary>
    public static Lobby CurrentLobby;
}

/// <summary>
/// Manages lobby creation, joining, leaving, and Relay/Steam integration.
/// Handles Unity Relay setup and manages UI updates for online lobbies.
/// </summary>
public class GameLobby : MonoBehaviour
{
    /// <summary>
    /// Key used to store Relay join codes in lobby metadata.
    /// </summary>
    private const string KEY_RELAY_JOIN_CODE = "RELAY_JOIN_CODE";

    /// <summary>
    /// Scene to load when starting the game.
    /// </summary>
    public string sceneToLoad;

    /// <summary>
    /// Heartbeat handler to maintain lobby connection.
    /// </summary>
    [FormerlySerializedAs("lobbyHeartBeat")] 
    public RelayServerHeartbeat relayServerHeartbeat;

    /// <summary>
    /// UI handler for online lobby creation.
    /// </summary>
    [FormerlySerializedAs("lobbyUI")] 
    public OnlineCreationUI onlineCreationUI;

    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI joiningLobbyText;
    /// <summary>
    /// Singleton instance of this class.
    /// </summary>
    public static GameLobby instance { get; private set; }

    public GameObject lobby;
    public GameObject onlineMatchmakingParent;



    /// <summary>
    /// Unity Awake method. Initializes singleton and Unity Authentication.
    /// </summary>
    private void Awake()
    {
        instance = this;
        InitializeUnityAuth();
    }

    /// <summary>
    /// Initializes Unity Services and signs in anonymously.
    /// </summary>
    private async void InitializeUnityAuth()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            var options = new InitializationOptions();
            options.SetProfile(Random.Range(0, 10000).ToString());

            await UnityServices.InitializeAsync(options);
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }

    /// <summary>
    /// Creates a new lobby, sets up Relay, and starts host.
    /// </summary>
    /// <param name="lobbyName">The lobby name.</param>
    /// <param name="isPrivate">Whether the lobby is private.</param>
    public async void CreateLobby(string lobbyName, bool isPrivate)
    {
        try
        {
            onlineCreationUI.lobbyUI.SetActive(false);
            ChangeJoinTextState(true);

            GlobalLobby.CurrentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, 4, new CreateLobbyOptions
            {
                IsPrivate = isPrivate,
            });

            var allocation = await AllocateRelay();
            string joinCode = await GetRelayJoinCode(allocation);

            await UpdateLobbyWithRelayCode(joinCode);
            ConfigureTransport(allocation);

            NetworkManager.Singleton.StartHost();
            relayServerHeartbeat.joinedLobby = GlobalLobby.CurrentLobby;

            //NetworkManager.Singleton.SceneManager.LoadScene("UI_Lobby", LoadSceneMode.Single);
            GameObject newLobby = Instantiate(lobby);
            newLobby.GetComponent<NetworkObject>().Spawn();
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }

    /// <summary>
    /// Updates the lobby with the Relay join code.
    /// </summary>
    /// <param name="joinCode">The Relay join code.</param>
    private async Task UpdateLobbyWithRelayCode(string joinCode)
    {
        await LobbyService.Instance.UpdateLobbyAsync(GlobalLobby.CurrentLobby.Id.ToString(), new UpdateLobbyOptions
        {
            Data = new Dictionary<string, DataObject>
            {
                { KEY_RELAY_JOIN_CODE, new DataObject(DataObject.VisibilityOptions.Member, joinCode) }
            }
        });
    }

    #region Joining Lobby

    /// <summary>
    /// Joins the first available lobby using Quick Join.
    /// </summary>
    public async void QuickJoin()
    {
        try
        {
            GlobalLobby.CurrentLobby = await LobbyService.Instance.QuickJoinLobbyAsync();
            ChangeJoinTextState(true);
            await JoinRelayAndStartClient(GlobalLobby.CurrentLobby.Data[KEY_RELAY_JOIN_CODE].Value);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }

    /// <summary>
    /// Leaves the current lobby and shuts down network if hosting.
    /// </summary>
    public async void LeaveLobby()
    {
        try
        {
            if (GlobalLobby.CurrentLobby != null)
            {
                if (NetworkManager.Singleton.IsHost && !string.IsNullOrEmpty(GlobalLobby.CurrentLobby.Id))
                {
                    await LobbyService.Instance.DeleteLobbyAsync(GlobalLobby.CurrentLobby.Id);
                }
                else
                {
                    string playerId = AuthenticationService.Instance.PlayerId;
                    await LobbyService.Instance.RemovePlayerAsync(GlobalLobby.CurrentLobby.Id, playerId);
                }

                GlobalLobby.CurrentLobby = null;
            }

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.Shutdown();
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to leave lobby: {e}");
        }
    }

    /// <summary>
    /// Joins a lobby using a lobby code.
    /// </summary>
    /// <param name="code">The lobby code.</param>
    public async void JoinWithCode(string code)
    {
        try
        {
            GlobalLobby.CurrentLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(code);
            ChangeJoinTextState(true);
            await JoinRelayAndStartClient(GlobalLobby.CurrentLobby.Data[KEY_RELAY_JOIN_CODE].Value);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }       
    }

    /// <summary>
    /// Joins a lobby using its ID.
    /// </summary>
    /// <param name="lobbyId">The lobby ID.</param>
    public async void JoinWithId(string lobbyId)
    {
        try
        {
            Lobby joinedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId);
            GlobalLobby.CurrentLobby = joinedLobby;
            ChangeJoinTextState(true);
            await JoinRelayAndStartClient(GlobalLobby.CurrentLobby.Data[KEY_RELAY_JOIN_CODE].Value);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to join lobby: {e}");
        }
    }

    /// <summary>
    /// Joins Relay and starts client connection for the lobby.
    /// </summary>
    /// <param name="joinCode">Relay join code.</param>
    private async Task JoinRelayAndStartClient(string joinCode)
    {
        try
        {
            var joinAllocation = await JoinRelay(joinCode);

            ConfigureTransport(joinAllocation);
            NetworkManager.Singleton.StartClient();
            onlineCreationUI.lobbyUI.SetActive(false);
            ChangeJoinTextState(false);

        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to join lobby via code: {e}");
            NetworkManager.Singleton.Shutdown();
        }
    }

    #endregion

    #region Relay Setup

    /// <summary>
    /// Allocates a Relay server for hosting.
    /// </summary>
    private async Task<Allocation> AllocateRelay()
    {
        try
        {
            return await RelayService.Instance.CreateAllocationAsync(3);
        }
        catch (RelayServiceException e)
        {
            Debug.LogError(e);
            return default;
        }
    }

    /// <summary>
    /// Gets a join code for a Relay allocation.
    /// </summary>
    private async Task<string> GetRelayJoinCode(Allocation allocation)
    {
        try
        {
            return await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
        }
        catch (RelayServiceException e)
        {
            Debug.LogError(e);
            return default;
        }
    }

    /// <summary>
    /// Joins an existing Relay allocation.
    /// </summary>
    private async Task<JoinAllocation> JoinRelay(string joinCode)
    {
        try
        {
            return await RelayService.Instance.JoinAllocationAsync(joinCode);
        }
        catch (RelayServiceException e)
        {
            Debug.LogError(e);
            return default;
        }
    }

    /// <summary>
    /// Configures the Unity Transport with Relay allocation data.
    /// </summary>
    /// <param name="allocationBase">Either Allocation or JoinAllocation object.</param>
    private void ConfigureTransport(object allocationBase)
    {
        string host = "";
        ushort port = 0;
        byte[] allocationId = null, connectionData = null, hostConnectionData = null, key = null;
        bool isSecure = false;

        if (allocationBase is Allocation allocation)
        {
            host = allocation.RelayServer.IpV4;
            port = (ushort)allocation.RelayServer.Port;
            allocationId = allocation.AllocationIdBytes;
            connectionData = allocation.ConnectionData;
            hostConnectionData = allocation.ConnectionData;
            key = allocation.Key;

            foreach (var endpoint in allocation.ServerEndpoints)
            {
                if (endpoint.ConnectionType == "dtls")
                {
                    host = endpoint.Host;
                    port = (ushort)endpoint.Port;
                    isSecure = endpoint.Secure;
                    break;
                }
            }
        }
        else if (allocationBase is JoinAllocation joinAllocation)
        {
            host = joinAllocation.RelayServer.IpV4;
            port = (ushort)joinAllocation.RelayServer.Port;
            allocationId = joinAllocation.AllocationIdBytes;
            connectionData = joinAllocation.ConnectionData;
            hostConnectionData = joinAllocation.HostConnectionData;
            key = joinAllocation.Key;

            foreach (var endpoint in joinAllocation.ServerEndpoints)
            {
                if (endpoint.ConnectionType == "dtls")
                {
                    host = endpoint.Host;
                    port = (ushort)endpoint.Port;
                    isSecure = endpoint.Secure;
                    break;
                }
            }
        }
        else
        {
            Debug.LogError("Invalid allocation type");
            return;
        }

        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(
            new RelayServerData(host, port, allocationId, connectionData, hostConnectionData, key, isSecure)
        );
    }

    #endregion

    /// <summary>
    /// Returns the currently active lobby.
    /// </summary>
    public Lobby GetLobby() => GlobalLobby.CurrentLobby;

    void ChangeJoinTextState(bool value)
    {
        joiningLobbyText.gameObject.SetActive(value);
        joiningLobbyText.enabled = value;
        joiningLobbyText.GetComponent<TextAnimator_TMP>().ResetState();
    }
}