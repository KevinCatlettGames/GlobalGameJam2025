using Unity.Netcode;
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem; 
using System.Collections.Generic;
using System.Net;

public class LobbyManager : NetworkBehaviour
{
    public static LobbyManager instance;

    public UnityEvent<ulong> OnJoinedLobby;
    public UnityEvent<ulong> OnLeftLobby;
    public UnityEvent<ulong> OnReadyStateUpdated;
    public UnityEvent OnAllPlayersReady;
    public UnityEvent OnNoLongerAllPlayersReady;

    public bool allPlayersReady = false;

    [Header("UI")]
    public GameObject[] playerContainers;
    public Button startButton;

    [Header("Local Player Settings")]
    public int maxLocalPlayers = 4; // max couch players
    private ulong localPlayerOffset = 1000; // starting ClientId for local players

    public NetworkList<PlayerLobbyState> players = new NetworkList<PlayerLobbyState>();

    public int minPlayers = 1;

    public SkinSO[] possibleSkins;
  
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
            Debug.Log("Subscribed to onclientconnected");
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }

        SceneManager.sceneLoaded += SceneManagerOnsceneLoaded;
    }

    private void SceneManagerOnsceneLoaded(Scene arg0, LoadSceneMode arg1)
    {
        if (arg0.name == "Lobby" && TransportSwitcher.Instance.isUsingRelay)
            playerContainers[0].SetActive(true);
        else if(arg0.name != "Lobby")
            enabled = false; 
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        
        SceneManager.sceneLoaded -= SceneManagerOnsceneLoaded;
    }
    
    private void OnDisable()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        
        SceneManager.sceneLoaded -= SceneManagerOnsceneLoaded;
    }
    
    private void OnClientConnected(ulong clientId)
    {
        Debug.Log("Client connected");
        if (!IsServer || !TransportSwitcher.Instance.isUsingRelay) return;
        Debug.Log("is server");

        ActivatePlayerContainersClientRpc();
    }

    [ClientRpc]
    void ActivatePlayerContainersClientRpc()
    {
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClients.Keys)
        {
            playerContainers[clientId].SetActive(true);
        }
    }
    
    public void ToggleReadyLocal(int playerIndex)
    {
        ulong clientId = localPlayerOffset + (ulong)playerIndex;

        int index = -1;
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].ClientId == clientId)
            {
                index = i;
                break;
            }
        }

        if (index == -1)
        {
            players.Add(new PlayerLobbyState { ClientId = clientId, IsReady = false });
            index = players.Count - 1;
            OnReadyStateUpdated?.Invoke(clientId);
            CheckAllReady();
            UpdatePlayerUI();
        }
        else
        {
            var player = players[index];
            var skinChange = playerContainers[index].GetComponent<PlayerContainerSkinChange>();
            
            if (( !player.IsReady && !skinChange.currentlyOnLocked ) || player.IsReady)
            {
                player.IsReady = !player.IsReady;
                players[index] = player;
                OnReadyStateUpdated?.Invoke(clientId);
                CheckAllReady();
                UpdatePlayerUI();
            }
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
                    startButton.interactable = false; 
                    OnNoLongerAllPlayersReady?.Invoke();
                }
                return;
            }
        }

        if (TransportSwitcher.Instance.isUsingRelay && LobbyManager.instance.players.Count <= 1)
        {
            allPlayersReady = false;
            startButton.interactable = false; 
            OnNoLongerAllPlayersReady?.Invoke();
            return; 
        }
        
        allPlayersReady = true;
        
        if (players.Count >= minPlayers)
        {
            allPlayersReady = true;
            startButton.interactable = true;
            OnAllPlayersReady?.Invoke();
        }
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
}
