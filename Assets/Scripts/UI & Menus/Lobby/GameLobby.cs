using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using Unity.Netcode;
using TMPro;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using System.Threading.Tasks;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;
using Febucci.UI;

public static class GlobalLobby
{
    public static Lobby CurrentLobby;
}

public class GameLobby : MonoBehaviour
{
    private const string KEY_RELAY_JOIN_CODE = "RELAY_JOIN_CODE";

    public string sceneToLoad;

    [FormerlySerializedAs("lobbyHeartBeat")] 
    public RelayServerHeartbeat relayServerHeartbeat;

    [FormerlySerializedAs("lobbyUI")] 
    public OnlineCreationUI onlineCreationUI;

    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI joiningLobbyText;

    public static GameLobby instance { get; private set; }

    public GameObject lobby;
    public GameObject onlineMatchmakingParent;
    public GameObject publicLobbiesParent;
    public GameObject lobbyNotFoundText;
    public bool currentServerIsPrivate = true;


    private void Awake()
    {
        instance = this;

#if !UNITY_SWITCH
        InitializeUnityAuth();
#endif
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

    public async void CreateLobby(string lobbyName, bool isPrivate)
    {
        try
        {
            onlineCreationUI.lobbyUI.SetActive(false);
            ChangeJoinTextState(true);

            if(!isPrivate)
            {
                Debug.Log("Should be public, setting private for now");
                currentServerIsPrivate = false;
            }

            GlobalLobby.CurrentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, 4, new CreateLobbyOptions
            {
                IsPrivate = true,
            });

            var allocation = await AllocateRelay();
            string joinCode = await GetRelayJoinCode(allocation);
            await UpdateLobbyWithRelayCode(joinCode);
            ConfigureTransport(allocation);

            NetworkManager.Singleton.StartHost();
            relayServerHeartbeat.gameObject.SetActive(true);
            relayServerHeartbeat.joinedLobby = GlobalLobby.CurrentLobby;
            GameObject newLobby = Instantiate(lobby);
            newLobby.GetComponent<NetworkObject>().Spawn();

#if !UNITY_SWITCH
            SteamJoinHandler.instance.SetPlayerReadyToBeJoined(GlobalLobby.CurrentLobby.LobbyCode);
#endif
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

    #region Joining Lobby

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
            publicLobbiesParent.SetActive(false);
            lobbyNotFoundText.SetActive(true);
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
            Debug.Log("Joining with code");
            GlobalLobby.CurrentLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(code);
            ChangeJoinTextState(true);
            await JoinRelayAndStartClient(GlobalLobby.CurrentLobby.Data[KEY_RELAY_JOIN_CODE].Value);
        }
        catch (LobbyServiceException e)
        {
            publicLobbiesParent.SetActive(false);
            lobbyNotFoundText.SetActive(true);
        }       
    }

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
            publicLobbiesParent.SetActive(false);
            lobbyNotFoundText.SetActive(true);
        }
    }
    private async Task JoinRelayAndStartClient(string joinCode)
    {
        try
        {
            publicLobbiesParent.SetActive(false);
            onlineCreationUI.lobbyUI.SetActive(false);
            var joinAllocation = await JoinRelay(joinCode);
            ConfigureTransport(joinAllocation);
            NetworkManager.Singleton.StartClient();
            ChangeJoinTextState(true);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to join lobby via code: {e}");
            NetworkManager.Singleton.Shutdown();
        }
    }

    #endregion

    #region Relay Setup
    private async Task<Allocation> AllocateRelay()
    {
        try
        {
            return await RelayService.Instance.CreateAllocationAsync(4, null);
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

    void ChangeJoinTextState(bool value)
    {
        joiningLobbyText.gameObject.SetActive(value);
        joiningLobbyText.enabled = value;
        joiningLobbyText.GetComponent<TextAnimator_TMP>().ResetState();
    }

    public async Task ChangeServerLockState(bool makePrivate, bool isLocked)
    {
        try
        {
            var updateOptions = new UpdateLobbyOptions
            {
                IsPrivate = makePrivate,
                IsLocked = isLocked
            };
            Lobby updatedLobby = await LobbyService.Instance.UpdateLobbyAsync(GlobalLobby.CurrentLobby.Id, updateOptions);

            //Debug.Log($"Private: {updatedLobby.IsPrivate}, Locked: {updatedLobby.IsLocked}");
        }   
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to change lockstate of lobby: {e.Message}");
        }
    }
}