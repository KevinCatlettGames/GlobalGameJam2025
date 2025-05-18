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
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class StartScreen : MonoBehaviour
{
    [SerializeField] private GameObject startMenu;
    [SerializeField] private GameObject anyKeyText;
    [SerializeField] private EventSystem eventSystem;
    [SerializeField] private GameObject selectedButton;
    private Lobby joinedLobby;
    private bool isStartMenuOpen = false;
    private const string KEY_RELAY_JOIN_CODE = "RELAY_JOIN_CODE";
    public LobbyHeartBeat lobbyHeartBeat;
    void Start()
    {
        Cursor.visible = false;
        InitializeUnityAuth();
    }

    void Update()
    {
        if (!isStartMenuOpen && Input.anyKeyDown)
        {
            startMenu.SetActive(true);
            anyKeyText.SetActive(false);
            isStartMenuOpen = true;
            eventSystem.SetSelectedGameObject(selectedButton);
        }
        if (!Cursor.visible && (Input.GetKeyDown(KeyCode.Mouse0) || Input.GetKeyDown(KeyCode.Mouse1)))
        {
            Cursor.visible = true;
        }
    }

    public async void StartGameLocal()
    {
         joinedLobby = await LobbyService.Instance.CreateLobbyAsync("Empty", 4, new CreateLobbyOptions
            {
                IsPrivate = false,
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
            NetworkManager.Singleton.SceneManager.LoadScene("Lvl_MainScene", LoadSceneMode.Single);
    }

    public void QuitGame()
    {
        Application.Quit();
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
