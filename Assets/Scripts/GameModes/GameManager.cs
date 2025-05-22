using System;
using System.Diagnostics;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Users;


public class GameManager : NetworkBehaviour
{
    public static GameManager Instance; 
    public GameObject playerPrefab;
    public static bool IsGamePaused = false;
    
    protected bool gameEnded;
    protected static int maxPlayers = 4;
    protected float gameEndDelay = 1f;

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
        if (Instance == null)
        {
            Instance = this; 
        }
        else
        {
            Destroy(this);
        }
                 
        for (int i = 0; i < maxPlayers; i++)
        {
            playerStates[i] = PlayerState.missing;
        }
        
        //Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        IsGamePaused = false;
        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += WaitAndStartGame;
    }
    
    void WaitAndStartGame(string scenename, LoadSceneMode loadscenemode, List<ulong> clientscompleted, List<ulong> clientstimedout)
    {
        Invoke(nameof(SceneManagerOnOnLoadEventCompletedleted), 5f);
    }
    
    private void SceneManagerOnOnLoadEventCompletedleted()
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
    void ChangePlayerStatesServerRpc(PlayerState[] playerStates)
    {
        this.playerStates = playerStates;
        ChangePlayerStatesClientRpc(this.playerStates);
    }

    [ClientRpc]
    void ChangePlayerStatesClientRpc(PlayerState[] playerStates)
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
    void RestartGameServerRpc()
    {
        RestartGameClientRpc();
    }
    
    [ClientRpc]
    void RestartGameClientRpc()
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
    void ChangePlayerHUDClientRpc(int killCredit)
    {
        playerHUDs[killCredit].AddKill();
    }

    void ChangePlayerHUDLocal(int killCredit)
    {
        playerHUDs[killCredit].AddKill();
    }
    
    
    [ServerRpc(RequireOwnership = false)]
    public virtual void ChangePlayerStateServerRpc(int playerID, PlayerState playerState)
    {
      ChangePlayerStatesClientRpc(playerID, playerState);
    }

    [ClientRpc]
    void ChangePlayerStatesClientRpc(int playerID, PlayerState playerState)
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
        return;
    }


    public virtual void CheckForRoundEndLocal()
    {
        return; 
    }
    
}
