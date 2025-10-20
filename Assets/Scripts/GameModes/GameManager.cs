using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using Object = System.Object;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;
    public GameObject playerPrefab;
    public static bool IsGamePaused = false;

    public DeathzoneWall[] deathZones; 
    protected const int maxPlayers = 4;
    protected float gameEndDelay = 1f;
    protected bool gameEnded;
    protected bool isReadyToRestart = false;
    protected int finishedRoundCount = 0;

    public Action OnGameEnded;
    public Action OnGameStarted;

    [SerializeField] protected SO_GameSettings gameSettings;

    protected PlayerController[] players = new PlayerController[maxPlayers];
    protected PlayerHUD[] playerHUDs = new PlayerHUD[maxPlayers];
    protected PlayerState[] playerStates = new PlayerState[maxPlayers];

    public PlayerInputManager playerInputManager;
    public bool PlayingLocal = false;
    public Countdown countdown; 
    
    
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        if(SteamIntegration.instance) 
            SteamIntegration.instance.UnlockAchievement(0);
        
        for (int i = 0; i < maxPlayers; i++)
            playerStates[i] = PlayerState.missing;

        Cursor.lockState = CursorLockMode.Locked;
        IsGamePaused = false;

        if (TransportSwitcher.Instance && TransportSwitcher.Instance.isUsingRelay)
            countdown.onCountdownComplete.AddListener(StartGameAfterDelay);
        else
            PlayingLocal = true;
    }

    private IEnumerator DelayedStartGame()
    {
        yield return new WaitForSeconds(10f);
        StartGameAfterDelay();
    }

    private void OnDisable()
    {
        if(LobbyManager.instance) 
            countdown.onCountdownComplete.RemoveListener(StartGameAfterDelay);    
    }

    private void StartGameAfterDelay()
    {
        if (!TransportSwitcher.Instance && NetworkManager.Singleton.ConnectedClients.Count < 2)
        {
            ChangePlayerStatesLocal(playerStates);
            PlayingLocal = true;
            playerInputManager.enabled = true;
        }
        else if (IsServer || NetworkManager.Singleton.ConnectedClients.Count == 1)
        {
            ChangePlayerStatesServerRpc(playerStates);

            foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
            {
                GameObject player = Instantiate(playerPrefab);
                player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);
                PlayerManager.Instance.AddPlayerServerRpc(player);
            }
            PlayerManager.Instance.Initialize();
            Invoke(nameof(EnableDeathzonesServerRpc), 1f);
        }
        ItemSpawner.Instance.InitialSpawn();
    }

    [ServerRpc]
    void EnableDeathzonesServerRpc()
    {
        EnableDeathzonesClientRpc();
    }

    [ClientRpc]
    void EnableDeathzonesClientRpc()
    {
        foreach (DeathzoneWall deathZone in deathZones)
            deathZone.GetComponent<DeathzoneWall>().EnableCol();
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
        UIManager.Instance.SetScoreScreenActive(true);
        ScoreManager.Instance.ResolveScores();
        finishedRoundCount++;
        isReadyToRestart = true;
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
        isReadyToRestart = false;
        UIManager.Instance.SetScoreScreenActive(false);
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
            ScoreManager.Instance.AddPendingScore(killCredit, false);
        }
        CheckForRoundEndServerRpc();       
    }

    public virtual void DeathReportLocal(int playerID, int killCredit)
    {
        if (killCredit >= 0 && killCredit < maxPlayers)
        {
            ChangePlayerHUDLocal(killCredit);
            ScoreManager.Instance.AddPendingScore(killCredit, false);
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
