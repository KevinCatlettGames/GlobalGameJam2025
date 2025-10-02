using Unity.Netcode;
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Manages the lobby system, including player readiness, UI updates, 
/// and handling local and networked players using Unity Netcode.
/// </summary>
public class LobbyManager : NetworkBehaviour
{
    /// <summary>
    /// Singleton instance of the LobbyManager.
    /// </summary>
    public static LobbyManager instance;
    
    /// <summary>
    /// Event invoked when a player's ready state changes. Provides the client ID.
    /// </summary>
    public UnityEvent<ulong> OnReadyStateUpdated;

    /// <summary>
    /// Tracks whether all players are ready.
    /// </summary>
    public bool allPlayersReady = false;

    [Header("UI")]
    
    public GameObject[] playerContainers;

    /// <summary>
    /// Button used to start the game once all players are ready.
    /// </summary>
    public Button startButton;

    [Header("Local Player Settings")]
    
    public int maxLocalPlayers = 4;

    /// <summary>
    /// List of players in the lobby with their network state.
    /// </summary>
    public NetworkList<PlayerLobbyState> players = new NetworkList<PlayerLobbyState>();

    /// <summary>
    /// Minimum number of players required to start the game.
    /// </summary>
    public int minPlayers = 1;

    /// <summary>
    /// Array of possible skins for players.
    /// </summary>
    public SkinSO[] possibleSkins;

    /// <summary>
    /// Event invoked when all players have loaded into the scene.
    /// </summary>
    public UnityEvent OnAllPlayersLoadedIn; 
    
    /// <summary>
    /// Unity Awake method. Initializes the singleton instance.
    /// </summary>
    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// Unity Start method. Activates player UI containers and subscribes to server scene load events if needed.
    /// </summary>
    private void Start()
    {
        foreach (PlayerLobbyState player in players)
            playerContainers[player.ClientId].SetActive(true);
        
        if(IsServer && TransportSwitcher.Instance.isUsingRelay)
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnLoadEventCompleted;
    }

    /// <summary>
    /// Callback invoked when the server finishes loading a scene. Triggers OnAllPlayersLoadedIn event.
    /// </summary>
    private void OnLoadEventCompleted(string scenename, LoadSceneMode loadscenemode, List<ulong> clientscompleted, List<ulong> clientstimedout)
    {
        if (scenename != "UI_Lobby" && scenename != "UI_MainMenu")
        {
            Debug.Log("loaded");
            Invoke(nameof(InvokeEvent), 2f);
        }
    }

    /// <summary>
    /// Invokes the OnAllPlayersLoadedIn event after a delay.
    /// </summary>
    void InvokeEvent()
    {
        Debug.Log("Invoked");
        OnAllPlayersLoadedIn?.Invoke();
    }

    /// <summary>
    /// Callback for changes to the networked player list. Updates the UI accordingly.
    /// </summary>
    private void OnPlayersListChanged(NetworkListEvent<PlayerLobbyState> changeEvent)
    {
        UpdatePlayerUI();
    }

    /// <summary>
    /// Represents the networked state of a player in the lobby.
    /// </summary>
    public struct PlayerLobbyState : INetworkSerializable, IEquatable<PlayerLobbyState>
    {
        /// <summary>
        /// Network client ID of the player.
        /// </summary>
        public ulong ClientId;

        /// <summary>
        /// Whether the player is ready.
        /// </summary>
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

    /// <summary>
    /// Unity OnEnable method. Subscribes to networked player list changes if using relay transport.
    /// </summary>
    private void OnEnable()
    {
        if(TransportSwitcher.Instance.isUsingRelay) 
            players.OnListChanged += OnPlayersListChanged;
    }

    /// <summary>
    /// Unity OnDestroy method. Unsubscribes from events to prevent memory leaks.
    /// </summary>
    private void OnDestroy()
    {
        if(TransportSwitcher.Instance.isUsingRelay) 
            players.OnListChanged -= OnPlayersListChanged;
        
        if(IsServer && TransportSwitcher.Instance.isUsingRelay && NetworkManager.Singleton)
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnLoadEventCompleted;
    }
    
    /// <summary>
    /// Toggles the ready state of a local player and updates the UI.
    /// </summary>
    /// <param name="playerIndex">Index of the player to toggle ready state for.</param>
    public void ToggleReady(int playerIndex)
    {
        int index = -1;
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].ClientId == (ulong)playerIndex)
            {
                index = i;
                break;
            }
        }

        if (index == -1)
        {
            players.Add(new PlayerLobbyState { ClientId = (ulong)playerIndex, IsReady = false });
            index = players.Count - 1;
            CheckAllReady();
            UpdatePlayerUI();
        }
        else
        {
            var player = players[index];
            var skinChange = playerContainers[index].GetComponent<PlayerContainerSkinChange>();
            
            if ((!player.IsReady && !skinChange.currentlyOnLocked) || player.IsReady)
            {
                player.IsReady = !player.IsReady;
                players[index] = player;
                OnReadyStateUpdated?.Invoke((ulong)playerIndex);
                CheckAllReady();
                UpdatePlayerUI();
            }
        }
    }

    /// <summary>
    /// Server RPC to toggle a player's ready state on the server.
    /// </summary>
    /// <param name="clientID">Network client ID of the player.</param>
    [ServerRpc(RequireOwnership = false)]
    public void ToggleReadyServerRpc(ulong clientID)
    {
        int index = -1;
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].ClientId == (ulong)clientID)
            {
                index = i;
                break;
            }
        }

        if (index == -1)
        {
            players.Add(new PlayerLobbyState { ClientId = (ulong)clientID, IsReady = false });
            AddNewPlayerValuesClientRpc((int)clientID);
            index = players.Count - 1;
        }
        else
        {
            var player = players[index];
            var skinChange = playerContainers[index].GetComponent<PlayerContainerSkinChange>();
            
            if ((!player.IsReady && !skinChange.currentlyOnLocked) || player.IsReady)
            {
                player.IsReady = !player.IsReady;
                players[index] = player;
                InvokeOnReadyStateUpdatedClientRpc(clientID);
            }
        }
        CheckAllReady();
    }

    /// <summary>
    /// Client RPC to add a new player's values for local UI/logic updates.
    /// </summary>
    /// <param name="clientID">Index of the new player.</param>
    [ClientRpc]
    void AddNewPlayerValuesClientRpc(int clientID)
    {
        LobbyPlayerHandler.Instance.playerValues.Add(
            new LobbyPlayerHandler.PlayerValues(clientID, null, possibleSkins[clientID]));
        LobbyPlayerHandler.Instance.SortPlayerValues();
    }

    /// <summary>
    /// Client RPC to invoke OnReadyStateUpdated event for a specific player.
    /// </summary>
    /// <param name="clientID">Network client ID of the player.</param>
    [ClientRpc]
    void InvokeOnReadyStateUpdatedClientRpc(ulong clientID)
    {
        OnReadyStateUpdated?.Invoke(clientID);
    }
    
    /// <summary>
    /// Checks whether all players are ready and updates the start button accordingly.
    /// </summary>
    private void CheckAllReady()
    {
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
                }
                return;
            }
        }
        
        allPlayersReady = true;
        
        if (players.Count >= minPlayers)
        {
            allPlayersReady = true;
            startButton.interactable = true;
        }

        if (TransportSwitcher.Instance.isUsingRelay && NetworkManager.Singleton.ConnectedClients.Count > players.Count)
        {
            allPlayersReady = false;
            startButton.interactable = false; 
        }
    }

    /// <summary>
    /// Updates the player UI containers to match the current players in the lobby.
    /// </summary>
    public void UpdatePlayerUI()
    {
        for (int i = 0; i < playerContainers.Length; i++)
            playerContainers[i].SetActive(false);

        foreach (var player in players)
        {
            int containerIndex = (int)player.ClientId;
            
            if (containerIndex >= 0 && containerIndex < playerContainers.Length)
                playerContainers[containerIndex].SetActive(true);
        }
    }
}