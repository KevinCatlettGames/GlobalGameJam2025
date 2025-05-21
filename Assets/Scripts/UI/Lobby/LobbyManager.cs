using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviour
{
    // UI References
    [Header("UI Elements")]
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button joinCodeButton;
    [SerializeField] private TMP_InputField joinCodeInputField;
    [SerializeField] private TextMeshProUGUI playerCountText;
    [SerializeField] private Button createPublicButton;
    [SerializeField] private TextMeshProUGUI waitForHostText;
    [SerializeField] private TextMeshProUGUI lobbyCodeText;
    public Button startGameButton;

    [Header("Scenes")]
    public string mainMenuSceneName;
    public string sceneToLoad;

    [Header("Lobby")]
    public LobbyHeartBeat lobbyHeartBeat;

    // Internal State
    private Lobby joinedLobby;
    private const string KEY_RELAY_JOIN_CODE = "RELAY_JOIN_CODE";
    public static LobbyManager instance { get; private set; }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        InitializeUnityAuth();

        // UI Button setup
        mainMenuButton.onClick.AddListener(() => SceneManager.LoadScene(mainMenuSceneName));

        joinCodeButton.onClick.AddListener(() =>
        {
            JoinWithCode(joinCodeInputField.text);
        });

        createPublicButton.onClick.AddListener(() =>
        {
            CreateLobby("Empty", false);
            createPublicButton.interactable = false;
        });

        startGameButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.SceneManager.LoadScene(sceneToLoad, LoadSceneMode.Single);
        });
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
            joinedLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, 4, new CreateLobbyOptions { IsPrivate = isPrivate });

            Allocation allocation = await AllocateRelay();
            string relayJoinCode = await GetRelayJoinCode(allocation);

            await LobbyService.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    { KEY_RELAY_JOIN_CODE, new DataObject(DataObject.VisibilityOptions.Member, relayJoinCode) }
                }
            });

            SetRelayServerData(allocation);
            NetworkManager.Singleton.StartHost();

            lobbyHeartBeat.joinedLobby = joinedLobby;
            startGameButton.gameObject.SetActive(true);
            HideUI();
            lobbyCodeText.gameObject.SetActive(true);
            lobbyCodeText.text = "Share to invite: " + GetLobby().LobbyCode;

        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }

    public async void QuickJoin()
    {
        try
        {
            joinedLobby = await LobbyService.Instance.QuickJoinLobbyAsync();
            string relayJoinCode = joinedLobby.Data[KEY_RELAY_JOIN_CODE].Value;
            JoinAllocation joinAllocation = await JoinRelay(relayJoinCode);

            SetRelayServerData(joinAllocation);
            NetworkManager.Singleton.StartClient();

            HideUI();
            waitForHostText.gameObject.SetActive(true);
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
            joinedLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(code);
            string relayJoinCode = joinedLobby.Data[KEY_RELAY_JOIN_CODE].Value;
            JoinAllocation joinAllocation = await JoinRelay(relayJoinCode);

            SetRelayServerData(joinAllocation);
            NetworkManager.Singleton.StartClient();

            HideUI();
            waitForHostText.gameObject.SetActive(true);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }

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

    private void SetRelayServerData(Allocation allocation)
    {
        RelayServerData data = CreateRelayServerData(
            allocation.RelayServer.IpV4,
            (ushort)allocation.RelayServer.Port,
            allocation.AllocationIdBytes,
            allocation.ConnectionData,
            allocation.ConnectionData,
            allocation.Key,
            allocation.ServerEndpoints
        );

        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(data);
    }

    private void SetRelayServerData(JoinAllocation allocation)
    {
        RelayServerData data = CreateRelayServerData(
            allocation.RelayServer.IpV4,
            (ushort)allocation.RelayServer.Port,
            allocation.AllocationIdBytes,
            allocation.ConnectionData,
            allocation.HostConnectionData,
            allocation.Key,
            allocation.ServerEndpoints
        );

        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(data);
    }

    private RelayServerData CreateRelayServerData(string defaultHost, ushort defaultPort, byte[] allocationId, byte[] connectionData, byte[] hostConnectionData, byte[] key, List<RelayServerEndpoint> endpoints)
    {
        string host = defaultHost;
        ushort port = defaultPort;
        bool isSecure = false;

        foreach (var endpoint in endpoints)
        {
            if (endpoint.ConnectionType == "dtls")
            {
                host = endpoint.Host;
                port = (ushort)endpoint.Port;
                isSecure = endpoint.Secure;
            }
        }

        return new RelayServerData(host, port, allocationId, connectionData, hostConnectionData, key, isSecure);
    }

    public Lobby GetLobby()
    {
        return joinedLobby;
    }

    private void HideUI()
    {
        joinCodeButton.gameObject.SetActive(false);
        joinCodeInputField.gameObject.SetActive(false);
        createPublicButton.gameObject.SetActive(false);
        playerCountText.gameObject.SetActive(true);

        NetworkManager.Singleton.OnClientConnectedCallback += UpdatePlayerCount;
    }

    private void UpdatePlayerCount(ulong clientId)
    {
        int count = NetworkManager.Singleton.ConnectedClients.Count;
        playerCountText.text = "Player Count: " + count;

        if (NetworkManager.Singleton.IsServer && count >= 2)
        {
            startGameButton.interactable = true;
        }
    }
}
