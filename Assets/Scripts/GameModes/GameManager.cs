using System;
using UnityEngine;
using Unity.Netcode; 

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance; 
    
    protected bool gameEnded;
    protected static int maxPlayers = 4;
    protected float gameEndDelay = 1f;

    public Action OnGameEnded;
    public Action OnGameStarted;

    public GameObject restartGameText; 

    protected PlayerController[] players = new PlayerController[maxPlayers];
    protected PlayerHUD[] playerHUDs = new PlayerHUD[maxPlayers];
    protected PlayerState[] playerStates = new PlayerState[maxPlayers];


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
        ChangePlayerStatesServerRpc(playerStates);
        Cursor.visible = false;
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

    [ClientRpc]
    void ChangePlayerHUDClientRpc(int killCredit)
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

    [ServerRpc(RequireOwnership = false)]
    public virtual void CheckForRoundEndServerRpc()
    {
        return;
    }
    
}
