using Unity.Netcode;
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem; 
using System.Collections.Generic;
using System.Net;
using Unity.VisualScripting;

public class LobbyManager : NetworkBehaviour
{
    public static LobbyManager instance;
    
    public UnityEvent<ulong> OnReadyStateUpdated;

    public bool allPlayersReady = false;

    [Header("UI")]
    public GameObject[] playerContainers;
    public Button startButton;

    [Header("Local Player Settings")]
    public int maxLocalPlayers = 4; // max couch players

    public NetworkList<PlayerLobbyState> players = new NetworkList<PlayerLobbyState>();

    public int minPlayers = 1;

    public SkinSO[] possibleSkins;
  
    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        foreach (PlayerLobbyState player in players)
            playerContainers[player.ClientId].SetActive(true);
    }

    private void OnPlayersListChanged(NetworkListEvent<PlayerLobbyState> changeEvent)
    {
        UpdatePlayerUI();
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
        if(TransportSwitcher.Instance.isUsingRelay) 
            players.OnListChanged += OnPlayersListChanged;
        
        SceneManager.sceneLoaded += SceneManagerOnsceneLoaded;
    }

    private void SceneManagerOnsceneLoaded(Scene arg0, LoadSceneMode arg1)
    { 
        if(arg0.name != "Lobby") 
            enabled = false; 
    }

    private void OnDestroy()
    {
        if(TransportSwitcher.Instance.isUsingRelay) 
            players.OnListChanged -= OnPlayersListChanged;
        
        SceneManager.sceneLoaded -= SceneManagerOnsceneLoaded;
    }
    
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= SceneManagerOnsceneLoaded;
    }
    
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
            //OnReadyStateUpdated?.Invoke((ulong)playerIndex);
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
                OnReadyStateUpdated?.Invoke((ulong)playerIndex);
                CheckAllReady();
                UpdatePlayerUI();
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ToggleReadyServerRpc(ulong clientID)
    {
        Debug.Log("Player with id" + clientID + "pressed ready... debug from server");
        
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
            
            if (( !player.IsReady && !skinChange.currentlyOnLocked ) || player.IsReady)
            {
                player.IsReady = !player.IsReady;
                players[index] = player;
                InvokeOnReadyStateUpdatedClientRpc(clientID);
            }
        }
        CheckAllReady();
    }

    [ClientRpc]
    void AddNewPlayerValuesClientRpc(int clientID)
    {
        LobbyPlayerHandler.Instance.playerValues.Add(new LobbyPlayerHandler.PlayerValues
            { PlayerIndex = clientID, Device = null, Skin = possibleSkins[clientID] });
    }

    [ClientRpc]
    void InvokeOnReadyStateUpdatedClientRpc(ulong clientID)
    {
        OnReadyStateUpdated?.Invoke(clientID);
    }
    
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

        // if (TransportSwitcher.Instance.isUsingRelay && LobbyManager.instance.players.Count <= 1)
        // {
        //     allPlayersReady = false;
        //     startButton.interactable = false; 
        //     return; 
        // }
        
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

    private void UpdatePlayerUI()
    {
        for (int i = 0; i < playerContainers.Length; i++)
            playerContainers[i].SetActive(false);

        foreach (var player in players)
        {
            int containerIndex = (int)player.ClientId;
            
            if (containerIndex >= 0 && containerIndex < playerContainers.Length)
            {
                playerContainers[containerIndex].SetActive(true);
            }
        }
    }
}