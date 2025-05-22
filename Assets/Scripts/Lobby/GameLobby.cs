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

public class GameLobby : MonoBehaviour
{
    private const string KEY_RELAY_JOIN_CODE = "RELAY_JOIN_CODE";
    
    public string sceneToLoad;
    public Button startGameButton;
    public TextMeshProUGUI waitForHostText;
    [SerializeField] private TextMeshProUGUI lobbyCodeText;

    public LobbyHeartBeat lobbyHeartBeat;
    public LobbyUI lobbyUI;

    private Lobby currentLobby;
    public static GameLobby instance { get; private set; }

    private void Awake()
    {
        instance = this;
        InitializeUnityAuth();
    }

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

    #region Lobby Creation

    public async void CreateLobby(string lobbyName, bool isPrivate)
    {
        try
        {
            currentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, 4, new CreateLobbyOptions
            {
                IsPrivate = isPrivate,
            });

            var allocation = await AllocateRelay();
            string joinCode = await GetRelayJoinCode(allocation);

            await UpdateLobbyWithRelayCode(joinCode);

            ConfigureTransport(allocation);
            NetworkManager.Singleton.StartHost();

            lobbyHeartBeat.joinedLobby = currentLobby;

            startGameButton.gameObject.SetActive(true);
            startGameButton.onClick.AddListener(() =>
            {
                NetworkManager.Singleton.SceneManager.LoadScene(sceneToLoad, LoadSceneMode.Single);
            });

            lobbyCodeText.gameObject.SetActive(true);
            lobbyCodeText.text = $"Share to invite: {currentLobby.LobbyCode}";

            lobbyUI.HideUI();
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }

    private async Task UpdateLobbyWithRelayCode(string joinCode)
    {
        await LobbyService.Instance.UpdateLobbyAsync(currentLobby.Id, new UpdateLobbyOptions
        {
            Data = new Dictionary<string, DataObject>
            {
                { KEY_RELAY_JOIN_CODE, new DataObject(DataObject.VisibilityOptions.Member, joinCode) }
            }
        });
    }

    #endregion

    #region Joining Lobby

    public async void QuickJoin()
    {
        try
        {
            currentLobby = await LobbyService.Instance.QuickJoinLobbyAsync();
            await JoinRelayAndStartClient(currentLobby.Data[KEY_RELAY_JOIN_CODE].Value);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }

    public async void JoinWithCode(string code)
    {
        try
        {
            currentLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(code);
            await JoinRelayAndStartClient(currentLobby.Data[KEY_RELAY_JOIN_CODE].Value);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }

    private async Task JoinRelayAndStartClient(string joinCode)
    {
        var joinAllocation = await JoinRelay(joinCode);

        ConfigureTransport(joinAllocation);
        NetworkManager.Singleton.StartClient();

        lobbyUI.HideUI();
        waitForHostText.gameObject.SetActive(true);
    }

    #endregion

    #region Relay Setup

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

    private void ConfigureTransport(object allocationBase)
    {
        string host = "";
        ushort port = 0;
        byte[] allocationId, connectionData, hostConnectionData, key;
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

    public Lobby GetLobby() => currentLobby;
}
