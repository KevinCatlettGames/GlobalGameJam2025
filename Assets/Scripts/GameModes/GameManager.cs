using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;
    public GameObject playerPrefab;
    public static bool IsGamePaused = false;

    protected const int maxPlayers = 4;
    protected float gameEndDelay = 1f;
    protected bool gameEnded;

    public Action OnGameEnded;
    public Action OnGameStarted;

    [SerializeField] protected GameObject restartGameText;
    [SerializeField] protected Animator victoryAnimator;

    protected PlayerController[] players = new PlayerController[maxPlayers];
    protected PlayerHUD[] playerHUDs = new PlayerHUD[maxPlayers];
    protected PlayerState[] playerStates = new PlayerState[maxPlayers];

    public PlayerInputManager playerInputManager;
    public bool playingLocal = false;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        for (int i = 0; i < maxPlayers; i++)
            playerStates[i] = PlayerState.missing;

        Cursor.lockState = CursorLockMode.Locked;
        IsGamePaused = false;

        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnSceneLoadCompleted;
    }

    private void OnDisable()
    {
        if(NetworkManager.Singleton != null) 
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnSceneLoadCompleted;
    }

    private void OnSceneLoadCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        StartCoroutine(DelayedStartGame());
    }

    private IEnumerator DelayedStartGame()
    {
        yield return new WaitForSeconds(.2f);
        StartGameAfterDelay();
    }

    private void StartGameAfterDelay()
    {
        if (NetworkManager.Singleton.ConnectedClients.Count < 2)
        {
            ChangePlayerStatesLocal(playerStates);
            playingLocal = true;
            playerInputManager.enabled = true;
        }
        else if (IsServer)
        {
            ChangePlayerStatesServerRpc(playerStates);

            foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
            {
                GameObject player = Instantiate(playerPrefab);
                player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);
                PlayerManager.Instance.AddPlayerServerRpc(player);
            }

            PlayerManager.Instance.Initialize();
        }

        ItemSpawner.Instance.InitialSpawn();
    }

    [ServerRpc(RequireOwnership = false)]
    private void ChangePlayerStatesServerRpc(PlayerState[] playerStates)
    {
        this.playerStates = playerStates;
        ChangePlayerStatesClientRpc(this.playerStates);
    }

    [ClientRpc]
    private void ChangePlayerStatesClientRpc(PlayerState[] playerStates)
    {
        this.playerStates = playerStates;
    }

    public void ChangePlayerStatesLocal(PlayerState[] playerStates)
    {
        this.playerStates = playerStates;
    }

    public virtual void EndGame()
    {
        OnGameEnded?.Invoke();
        gameEnded = true;
        restartGameText.SetActive(true);
    }

    public virtual void RestartGame()
    {
        RestartGameServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void RestartGameServerRpc()
    {
        RestartGameClientRpc();
    }

    [ClientRpc]
    private void RestartGameClientRpc()
    {
        OnGameStarted?.Invoke();
        gameEnded = false;
        restartGameText.SetActive(false);
    }

    public virtual void AddPlayer(int playerID, PlayerController player, PlayerHUD playerHUD)
    {
        if (playerID < 0 || playerID >= maxPlayers) return;

        players[playerID] = player;
        playerHUDs[playerID] = playerHUD;
    }

    [ServerRpc(RequireOwnership = false)]
    public virtual void DeathReportServerRpc(int playerID, int killCredit)
    {
        if (killCredit >= 0 && killCredit < maxPlayers)
        {
            ChangePlayerHUDClientRpc(killCredit);
        }

        CheckForRoundEndServerRpc();
    }

    public virtual void DeathReportLocal(int playerID, int killCredit)
    {
        if (killCredit >= 0 && killCredit < maxPlayers)
        {
            ChangePlayerHUDLocal(killCredit);
        }

        CheckForRoundEndLocal();
    }

    [ClientRpc]
    private void ChangePlayerHUDClientRpc(int killCredit)
    {
        playerHUDs[killCredit].AddKill();
    }

    private void ChangePlayerHUDLocal(int killCredit)
    {
        playerHUDs[killCredit].AddKill();
    }

    [ServerRpc(RequireOwnership = false)]
    public virtual void ChangePlayerStateServerRpc(int playerID, PlayerState playerState)
    {
        ChangePlayerStateClientRpc(playerID, playerState);
    }

    [ClientRpc]
    private void ChangePlayerStateClientRpc(int playerID, PlayerState playerState)
    {
        if (playerID < 0 || playerID >= maxPlayers) return;
        playerStates[playerID] = playerState;
    }

    public void ChangePlayerStateLocal(int playerID, PlayerState playerState)
    {
        if (playerID < 0 || playerID >= maxPlayers) return;
        playerStates[playerID] = playerState;
    }

    [ServerRpc(RequireOwnership = false)]
    public virtual void CheckForRoundEndServerRpc()
    {
        // To be overridden
    }

    public virtual void CheckForRoundEndLocal()
    {
        // To be overridden
    }
}
