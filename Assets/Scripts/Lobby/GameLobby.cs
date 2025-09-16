using System;
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
using Netcode.Transports.Facepunch;
using Steamworks;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Random = UnityEngine.Random;

public static class GlobalLobby
{
    public static Lobby CurrentLobby;
}

public class GameLobby : MonoBehaviour
{
    private const string KEY_RELAY_JOIN_CODE = "RELAY_JOIN_CODE";

    public string sceneToLoad;
    public Button startGameButton;
    public TextMeshProUGUI waitForHostText;
    [SerializeField] private TMP_InputField lobbyCodeText;

    public LobbyHeartBeat lobbyHeartBeat;
    public LobbyUI lobbyUI;
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
            GlobalLobby.CurrentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, 4, new CreateLobbyOptions
            {
                IsPrivate = isPrivate,
            });

            var allocation = await AllocateRelay();
            string joinCode = await GetRelayJoinCode(allocation);

            await UpdateLobbyWithRelayCode(joinCode);
            ConfigureTransport(allocation);

            NetworkManager.Singleton.StartHost();
            lobbyHeartBeat.joinedLobby = GlobalLobby.CurrentLobby;
            
            startGameButton.gameObject.SetActive(true);
            startGameButton.onClick.AddListener(() =>
            {
                NetworkManager.Singleton.SceneManager.LoadScene(sceneToLoad, LoadSceneMode.Single);
            });
            
            lobbyCodeText.gameObject.SetActive(true);
            lobbyCodeText.text = GlobalLobby.CurrentLobby.LobbyCode;
            
            lobbyUI.HideOnCreateUI();
           // NetworkManager.Singleton.SceneManager.LoadScene("Lobby", LoadSceneMode.Single);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }

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

    #region Facepunch Lobby Logic

    private void OnEnable()
    {
        if (SteamIntegration.instance && SteamIntegration.instance.lobbyIDToJoin != "")
        {
            // connectString is exactly what you set earlier ("lobby.Id.ToString()")
            if (ulong.TryParse(SteamIntegration.instance.lobbyIDToJoin, out ulong lobbyId))
            {
                JoinSteamLobbyWithID(lobbyId.ToString());
            }
            else
            {
                Debug.LogError($"Invalid connect string: {SteamIntegration.instance.lobbyIDToJoin}");
            }
        }
    }
    // private void OnDisable()
    // {
    //     SteamMatchmaking.OnLobbyCreated -= LobbyCreated;
    //     SteamMatchmaking.OnLobbyEntered -= LobbyEntered;
    //     SteamFriends.OnGameLobbyJoinRequested -= GameLobbyJoinRequested;
    // }
    
    private void LobbyCreated(Result result, Lobby lobby)
    {
        if (result == Result.OK)
        {
            // lobby.SetPublic();
            // lobby.SetJoinable(true);
            lobbyCodeText.gameObject.SetActive(true);
            lobbyCodeText.text = lobby.Id.ToString();
            SteamFriends.SetRichPresence("connect", lobby.Id.ToString());
            SteamFriends.SetRichPresence("steam_display", "#Status_InLobby");
            NetworkManager.Singleton.StartHost();

        }
    }

    private void SteamFriendsOnOnGameRichPresenceJoinRequested(Friend arg1, string arg2)
    {
        throw new NotImplementedException();
    }

    private void LobbyEntered(Lobby lobby)
    {
        GlobalLobby.CurrentLobby = lobby;
        lobbyHeartBeat.joinedLobby = lobby; 
        Debug.Log("Entered a steam lobby");
        
        if (NetworkManager.Singleton.IsHost) return; 
        //NetworkManager.Singleton.gameObject.GetComponent<FacepunchTransport>().targetSteamId = lobby.Owner.Id;
        NetworkManager.Singleton.StartClient();
    }

    // private async void GameLobbyJoinRequested(Lobby lobby, SteamId id)
    // {
    //    await lobby.Join();
    // }

    public async void HostSteamLobby()
    {
        await SteamMatchmaking.CreateLobbyAsync(4);
        lobbyHeartBeat.joinedLobby = GlobalLobby.CurrentLobby;

        startGameButton.gameObject.SetActive(true);
        startGameButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.SceneManager.LoadScene(sceneToLoad, LoadSceneMode.Single);
        });

        lobbyUI.HideOnCreateUI();
    }

    public async void JoinSteamLobbyWithID(string id)
    {
        ulong ID;
        if (!ulong.TryParse(id, out ID))
            return;

        Steamworks.Data.Lobby[] lobbies = await SteamMatchmaking.LobbyList.WithSlotsAvailable(1).RequestAsync();

        foreach (Steamworks.Data.Lobby lobby in lobbies)
        {
            if (lobby.Id == ID)
            {
                await lobby.Join();
                return; 
            }
        }
    }
    #endregion
    
    
    
    #endregion

    #region Joining Lobby

    public async void QuickJoin()
    {
        try
        {
            GlobalLobby.CurrentLobby = await LobbyService.Instance.QuickJoinLobbyAsync();
            await JoinRelayAndStartClient(GlobalLobby.CurrentLobby.Data[KEY_RELAY_JOIN_CODE].Value);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }
    
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
    
    public async void JoinWithCode(string code)
    {
        try
        {
            GlobalLobby.CurrentLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(code);
            await JoinRelayAndStartClient(GlobalLobby.CurrentLobby.Data[KEY_RELAY_JOIN_CODE].Value);
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

        lobbyUI.HideOnJoinUI();
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

    public Lobby GetLobby() => GlobalLobby.CurrentLobby;
}
