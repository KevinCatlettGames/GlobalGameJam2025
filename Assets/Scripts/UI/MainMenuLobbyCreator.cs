using System;
using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using Unity.Netcode; 
using UnityEngine.SceneManagement; 
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using System.Threading.Tasks;
using Steamworks;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Lobby = Steamworks.Data.Lobby;
using UnityEngine.UI; 

public class MainMenuLobbyCreator : MonoBehaviour
{
    public static MainMenuLobbyCreator Instance;
    private Steamworks.Data.Lobby joinedLobby;
    private bool isStartMenuOpen = false;
    private const string KEY_RELAY_JOIN_CODE = "RELAY_JOIN_CODE";
    public LobbyHeartBeat lobbyHeartBeat;
    public Button acceptButton; 
    
    private void Awake()
    {
        if(Instance == null) 
            Instance = this;
    }

    void Start()
    {
        SteamMatchmaking.OnLobbyInvite += SteamMatchmakingOnLobbyInvite;
        SteamMatchmaking.OnLobbyEntered += SteamMatchmakingOnOnLobbyEntered;

        //InitializeUnityAuth();
    }

    private void SteamMatchmakingOnOnLobbyEntered(Lobby obj)
    {
        Debug.Log("Entered Lobby");
        SteamIntegration.instance.lobbyIDToJoin = obj.ToString();
        acceptButton.onClick.AddListener(() => OpenLobby());
    }

    private void SteamMatchmakingOnLobbyInvite(Friend arg1, Lobby arg2)
    {
       Debug.Log("Was invited to lobby");
       SteamIntegration.instance.steamFriendToJoin = arg1;
       SteamIntegration.instance.lobbyIDToJoin = arg2.Id.ToString();
       acceptButton.gameObject.SetActive(true);
       acceptButton.onClick.AddListener(() => OpenLobby());
    }

    public async void StartGameLocal()
    {
         // joinedLobby = await LobbyService.Instance.CreateLobbyAsync("Empty", 4, new CreateLobbyOptions
         //    {
         //        IsPrivate = false,
         //    });

            // Allocation allocation =  await AllocateRelay();
            // string relayJoinCode = await GetRelayJoinCode(allocation);
            // LobbyService.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions
            // {
            //     Data = new Dictionary<string, DataObject>
            //     {
            //         {
            //             KEY_RELAY_JOIN_CODE, new DataObject(DataObject.VisibilityOptions.Member, relayJoinCode)
            //         }
            //     }
            // });
            //
            // // Assume 'allocation' is the Allocation object from AllocateRelayAsync
            // string host = allocation.RelayServer.IpV4; 
            // ushort port = (ushort)allocation.RelayServer.Port; 
            // byte[] joinAllocationId = allocation.AllocationIdBytes; 
            // byte[] connectionData = allocation.ConnectionData; 
            // byte[] hostConnectionData = allocation.ConnectionData; 
            // byte[] key = allocation.Key;
            // bool isSecure = false;
            //
            // foreach (var endpoint in allocation.ServerEndpoints)
            // {
            //     if (endpoint.ConnectionType == "dtls")
            //     {
            //         host = endpoint.Host;
            //         port = (ushort)endpoint.Port;
            //         isSecure = endpoint.Secure;
            //     }
            // }
            //
            // NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(
            //     new RelayServerData(host, port, joinAllocationId, 
            //         connectionData, hostConnectionData, key, isSecure));
            //
            NetworkManager.Singleton.StartHost();
            lobbyHeartBeat.joinedLobby = joinedLobby;
            NetworkManager.Singleton.SceneManager.LoadScene("Lvl_MainScene", LoadSceneMode.Single);
    }
    
    public void OpenLobby()
    {
        SceneManager.LoadScene("Lobby", LoadSceneMode.Single);
    }
    
    private async void InitializeUnityAuth()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            InitializationOptions initializationOptions = new InitializationOptions();
            initializationOptions.SetProfile(UnityEngine.Random.Range(0,10000).ToString());
            await UnityServices.InitializeAsync(initializationOptions);
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }
    
    private async Task<JoinAllocation> JoinRelay(string joinCode)
    {
        try
        { 
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            return joinAllocation;
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
            return default;
        }
    }
    
    private async Task<Allocation>  AllocateRelay()
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(3);
            return allocation;
        }
        catch(RelayServiceException e)
        {
            Debug.Log(e);
            return default;
        }
    }

    private async Task<string> GetRelayJoinCode(Allocation allocation)
    {
        try
        {
            string relayJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            return relayJoinCode;
        }
        catch(RelayServiceException e)
        {
            Debug.Log(e);
            return default;
        }
    }
}
