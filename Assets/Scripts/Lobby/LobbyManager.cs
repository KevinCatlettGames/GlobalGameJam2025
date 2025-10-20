using Unity.Netcode;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using FMODUnity;

public class LobbyManager : NetworkBehaviour
{
    [SerializeField] private SO_Scores scores;

    public static LobbyManager instance;
    public UnityEvent<ulong> OnReadyStateUpdated;
    public bool allPlayersReady = false;

    [Header("UI")]
    public GameObject[] playerContainers;
    public Button startButton;

    [Header("Local Player Settings")]
    public int maxLocalPlayers = 4;
    public NetworkList<PlayerLobbyState> players = new NetworkList<PlayerLobbyState>();
    public int minPlayers = 1;
    public SkinSO[] possibleSkins;
    public UnityEvent OnAllPlayersLoadedIn;

    public StudioEventEmitter joinEmitter;
    public StudioEventEmitter selectEmitter;
    public StudioEventEmitter unselectEmitter;
    public StudioEventEmitter playerStartEmitter;

    public string levelToLoad = "Lvl_MainScene";

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        scores.ResetKills();
        scores.ResetWins();

        foreach (PlayerLobbyState player in players)
            playerContainers[player.ClientId].SetActive(true);

        if (IsServer && TransportSwitcher.Instance.isUsingRelay)
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnLoadEventCompleted;

        startButton.gameObject.SetActive(false);
    }

    private void OnLoadEventCompleted(string scenename, LoadSceneMode loadscenemode, List<ulong> clientscompleted, List<ulong> clientstimedout)
    {
        if (scenename != "UI_Lobby" && scenename != "UI_MainMenu")
        {
            Debug.Log("loaded");
            Invoke(nameof(InvokeEvent), 2f);
        }
    }

    private void InvokeEvent()
    {
        Debug.Log("Invoked");
        OnAllPlayersLoadedIn?.Invoke();
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
        if (TransportSwitcher.Instance.isUsingRelay)
            players.OnListChanged += OnPlayersListChanged;
    }

    private void OnDestroy()
    {
        if (TransportSwitcher.Instance.isUsingRelay)
            players.OnListChanged -= OnPlayersListChanged;

        if (IsServer && TransportSwitcher.Instance.isUsingRelay && NetworkManager.Singleton)
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnLoadEventCompleted;
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
            CheckAllReady();
            UpdatePlayerUI();
            joinEmitter.Play();
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

                if (player.IsReady)
                    selectEmitter.Play();
                else
                    unselectEmitter.Play();
            }
        }
    }

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

    [ClientRpc]
    private void AddNewPlayerValuesClientRpc(int clientID)
    {
        LobbyPlayerHandler.Instance.playerValues.Add(
            new LobbyPlayerHandler.PlayerValues(clientID, null, possibleSkins[clientID]));
        LobbyPlayerHandler.Instance.SortPlayerValues();
    }

    [ClientRpc]
    private void InvokeOnReadyStateUpdatedClientRpc(ulong clientID)
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
                    startButton.gameObject.SetActive(false);
                }
                return;
            }
        }

        allPlayersReady = true;

        if (players.Count >= minPlayers)
        {
            allPlayersReady = true;
            startButton.gameObject.SetActive(true);
        }

        if (TransportSwitcher.Instance.isUsingRelay &&
            NetworkManager.Singleton.ConnectedClients.Count > players.Count)
        {
            allPlayersReady = false;
            startButton.gameObject.SetActive(false);
        }
    }

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

    public IEnumerator LoadGameScene()
    {
        playerStartEmitter.Play();
        yield return new WaitForSeconds(1f);
        NetworkManager.Singleton.SceneManager.LoadScene(levelToLoad, LoadSceneMode.Single);
    }
}