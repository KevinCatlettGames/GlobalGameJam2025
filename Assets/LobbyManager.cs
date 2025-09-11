using Unity.Netcode;
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem; 

public class LobbyManager : NetworkBehaviour
{
    public static LobbyManager instance;

    public UnityEvent<ulong> OnJoinedLobby;
    public UnityEvent<ulong> OnLeftLobby;
    public UnityEvent<ulong> OnReadyStateUpdated;
    public UnityEvent OnAllPlayersReady;
    public UnityEvent OnNoLongerAllPlayersReady;

    private bool allPlayersReady = false;

    [Header("UI")]
    public GameObject[] playerContainers;
    public Button startButton;

    [Header("Local Player Settings")]
    public int maxLocalPlayers = 4; // max couch players
    private ulong localPlayerOffset = 1000; // starting ClientId for local players

    public NetworkList<PlayerLobbyState> players = new NetworkList<PlayerLobbyState>();

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    public struct PlayerLobbyState : INetworkSerializable, IEquatable<PlayerLobbyState>
    {
        public ulong ClientId;
        public bool IsReady;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ClientId);
            serializer.SerializeValue(ref IsReady);
        }

        public bool Equals(PlayerLobbyState other)
        {
            return ClientId == other.ClientId;
        }
    }

    private void OnEnable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    // private void Start()
    // {
    //     if (!IsServer) return;
    //
    //     Debug.Log("Server is fully started in lobby scene");
    //
    //     // Initialize local player slots
    //     for (int i = 0; i < maxLocalPlayers; i++)
    //     {
    //         ulong fakeClientId = localPlayerOffset + (ulong)i;
    //         players.Add(new PlayerLobbyState { ClientId = fakeClientId, IsReady = false });
    //     }
    //
    //     // Add connected online clients
    //     foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
    //     {
    //         OnClientConnected(client.ClientId);
    //     }
    //
    //     UpdatePlayerUI();
    // }

    private void OnClientConnected(ulong clientId)
    {
        if (!IsServer) return;

        // Avoid duplicates
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].ClientId == clientId) return;
        }

        players.Add(new PlayerLobbyState
        {
            ClientId = clientId,
            IsReady = false
        });

        OnJoinedLobby?.Invoke(clientId);
        UpdatePlayerUI();
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (!IsServer) return;

        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].ClientId == clientId)
            {
                players.RemoveAt(i);
                break;
            }
        }

        OnLeftLobby?.Invoke(clientId);
        UpdatePlayerUI();
    }

    [ServerRpc(RequireOwnership = false)]
    public void ToggleReadyServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].ClientId == clientId)
            {
                var player = players[i];
                player.IsReady = !player.IsReady;
                players[i] = player;
                OnReadyStateUpdated?.Invoke(clientId);
                break;
            }
        }

        CheckAllReady();
        UpdatePlayerUI();
    }

    public void ToggleReadyLocal(int playerIndex)
    {
        ulong clientId = localPlayerOffset + (ulong)playerIndex;

        // Find existing player
        int index = -1;
        for (int i = 0; i < players.Count; i++)
            if (players[i].ClientId == clientId)
                index = i;

        if (index == -1)
        {
            // Add new player dynamically
            players.Add(new PlayerLobbyState { ClientId = clientId, IsReady = false });
            index = players.Count - 1;
        }
        else
        {
            var player = players[index];
            player.IsReady = !player.IsReady;
            players[index] = player;
        }

        OnReadyStateUpdated?.Invoke(clientId);
        CheckAllReady();
        UpdatePlayerUI();
    }
    
    public void RemoveLocalPlayer(InputAction.CallbackContext context)
    {
        UnityEngine.InputSystem.InputDevice tempDevice = context.control.device;
        int playerIndex = LocalPlayerInputManager.Instance.GetPlayerIndex(tempDevice);

        ulong clientId = (ulong)playerIndex + 1000; // adjust if you use 1000+ offset for locals

        int removeAt = -1;
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].ClientId == clientId)
            {
                removeAt = i;
                break;
            }
        }

        if (removeAt != -1)
        {
            players.RemoveAt(removeAt);
            LocalPlayerInputManager.Instance.RemoveDevice(playerIndex);

            // Shift devices down
            for (int i = playerIndex + 1; i < LocalPlayerInputManager.Instance.playerDevices.Count; i++)
            {
                var device = LocalPlayerInputManager.Instance.GetDevice(i);
                if (device != null)
                {
                    LocalPlayerInputManager.Instance.AssignDeviceToPlayer(i - 1, device);
                }
            }

            // Refresh UI to reflect changes
            RefreshPlayerContainers();

            CheckAllReady();
            OnLeftLobby?.Invoke(clientId);
        }
    }


    private void CheckAllReady()
    {
        if (!IsServer) return;

        if (players.Count == 0)
        {
            allPlayersReady = false;
            startButton.gameObject.SetActive(false);
            return;
        }

        foreach (var player in players)
        {
            if (!player.IsReady)
            {
                if (allPlayersReady)
                {
                    allPlayersReady = false;
                    startButton.gameObject.SetActive(false);
                    OnNoLongerAllPlayersReady?.Invoke();
                }
                return;
            }
        }

        allPlayersReady = true;
        startButton.gameObject.SetActive(true);
        OnAllPlayersReady?.Invoke();
    }

    private void UpdatePlayerUI()
    {
        // Reset all containers
        for (int i = 0; i < playerContainers.Length; i++)
            playerContainers[i].SetActive(false);

        foreach (var player in players)
        {
            int containerIndex;
            if (player.ClientId < localPlayerOffset)
                containerIndex = (int)player.ClientId; // online clients
            else
                containerIndex = (int)(player.ClientId - localPlayerOffset); // local players

            if (containerIndex >= 0 && containerIndex < playerContainers.Length)
            {
                playerContainers[containerIndex].SetActive(true);
                // Here you could also update UI (e.g., ready icon)
            }
        }
    }

    public void StartGame()
    {
        if (!IsServer) return;
        NetworkManager.Singleton.SceneManager.LoadScene("Lvl_MainScene", LoadSceneMode.Single);
    }
    
    private void RefreshPlayerContainers()
    {
        // First disable all
        for (int i = 0; i < playerContainers.Length; i++)
        {
            playerContainers[i].SetActive(false);
        }

        // Then enable only the ones we actually have players for
        for (int i = 0; i < players.Count; i++)
        {
            if (i < playerContainers.Length)
            {
                playerContainers[i].SetActive(true);
            }
        }
    }
    
    public void LoadMainMenu()
    {
        NetworkManager.Singleton.Shutdown();
        try
        {
            GlobalLobby.CurrentLobby.Leave();
        }
        catch
        {
            
        }

        SceneManager.LoadScene("MainMenu"); 
    }
}
