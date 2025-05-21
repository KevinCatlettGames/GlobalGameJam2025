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
    
    private Lobby joinedLobby;
    public string sceneToLoad;
    public Button startGameButton;
    public TextMeshProUGUI waitForHostText;
    [SerializeField] private TextMeshProUGUI lobbyCodeText;
    
    public LobbyHeartBeat lobbyHeartBeat;
    public LobbyUI lobbyUI;
    
    
    public static GameLobby instance {get; private set;}
    private void Awake()
    {
        instance = this;
        InitializeUnityAuth();
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

    public async void CreateLobby(string lobbyName, bool isPrivate)
    {
        try
        {
            joinedLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, 4, new CreateLobbyOptions
            {
                IsPrivate = isPrivate,
            });

            Allocation allocation =  await AllocateRelay();
            string relayJoinCode = await GetRelayJoinCode(allocation);
            LobbyService.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    {
                        KEY_RELAY_JOIN_CODE, new DataObject(DataObject.VisibilityOptions.Member, relayJoinCode)
                    }
                }
            });

            // Assume 'allocation' is the Allocation object from AllocateRelayAsync
            string host = allocation.RelayServer.IpV4; 
            ushort port = (ushort)allocation.RelayServer.Port; 
            byte[] joinAllocationId = allocation.AllocationIdBytes; 
            byte[] connectionData = allocation.ConnectionData; 
            byte[] hostConnectionData = allocation.ConnectionData; 
            byte[] key = allocation.Key;
            bool isSecure = false;
            
            foreach (var endpoint in allocation.ServerEndpoints)
            {
                if (endpoint.ConnectionType == "dtls")
                {
                    host = endpoint.Host;
                    port = (ushort)endpoint.Port;
                    isSecure = endpoint.Secure;
                }
            }
            
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(
                new RelayServerData(host, port, joinAllocationId, 
                    connectionData, hostConnectionData, key, isSecure));
            
            NetworkManager.Singleton.StartHost();
            lobbyHeartBeat.joinedLobby = joinedLobby;
            startGameButton.gameObject.SetActive(true);
            startGameButton.onClick.AddListener(() =>
            {
                NetworkManager.Singleton.SceneManager.LoadScene(sceneToLoad, LoadSceneMode.Single);
            });
            
            lobbyCodeText.gameObject.SetActive(true);
            lobbyCodeText.text = "Share to invite: " + GetLobby().LobbyCode;
            
            lobbyUI.HideUI();
        }
        catch(LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    public async void QuickJoin()
    {
        try
        {
            joinedLobby = await LobbyService.Instance.QuickJoinLobbyAsync();
            string relayJoinCode = joinedLobby.Data[KEY_RELAY_JOIN_CODE].Value;
            JoinAllocation joinAllocation = await JoinRelay(relayJoinCode);
            
            // Assume 'allocation' is the Allocation object from AllocateRelayAsync
            string host = joinAllocation.RelayServer.IpV4; 
            ushort port = (ushort)joinAllocation.RelayServer.Port; 
            byte[] joinAllocationId = joinAllocation.AllocationIdBytes; 
            byte[] connectionData = joinAllocation.ConnectionData; 
            byte[] hostConnectionData = joinAllocation.HostConnectionData; 
            byte[] key = joinAllocation.Key;
            bool isSecure = false;
            
            foreach (var endpoint in joinAllocation.ServerEndpoints)
            {
                if (endpoint.ConnectionType == "dtls")
                {
                    host = endpoint.Host;
                    port = (ushort)endpoint.Port;
                    isSecure = endpoint.Secure;
                }
            }
            
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(
                new RelayServerData(host, port, joinAllocationId, 
                    connectionData, hostConnectionData, key, isSecure));
            
            NetworkManager.Singleton.StartClient();
            lobbyUI.HideUI();
            waitForHostText.gameObject.SetActive(true);
        }
        catch(LobbyServiceException e)
        {
            Debug.Log(e);
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

    public Lobby GetLobby()
    {
        return joinedLobby;
    }

    public async void JoinWithCode(string code)
    {
        try
        {
            joinedLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(code);

            string relayJoinCode = joinedLobby.Data[KEY_RELAY_JOIN_CODE].Value;

            JoinAllocation joinAllocation = await JoinRelay(relayJoinCode);

            // NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(joinAllocation, "dtls"));
            
            // Note: the new MPS SDK (as of 0.5.0) does not ship an assembly for Relay therefore we cannot use the Relay
            // allocation based constructors for UnityTransport and instead must construct the RelayServerData ourselves
            string host = joinAllocation.RelayServer.IpV4; 
            ushort port = (ushort)joinAllocation.RelayServer.Port; 
            byte[] joinAllocationId = joinAllocation.AllocationIdBytes; 
            byte[] connectionData = joinAllocation.ConnectionData; 
            byte[] hostConnectionData = joinAllocation.HostConnectionData; 
            byte[] key = joinAllocation.Key;
            bool isSecure = false;
            
            foreach (var endpoint in joinAllocation.ServerEndpoints)
            {
                if (endpoint.ConnectionType == "dtls")
                {
                    host = endpoint.Host;
                    port = (ushort)endpoint.Port;
                    isSecure = endpoint.Secure;
                }
            }
            
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(host, port, joinAllocationId, connectionData, hostConnectionData, key, isSecure));
            
            NetworkManager.Singleton.StartClient();
            lobbyUI.HideUI();
            waitForHostText.gameObject.SetActive(true);
        }
        catch(LobbyServiceException e)
        {
            Debug.Log(e);
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