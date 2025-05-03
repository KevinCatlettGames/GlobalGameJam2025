using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance; 
    
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
        Cursor.visible = false;
    }

    public virtual void EndGame()
    {
        OnGameEnded?.Invoke();
        gameEnded = true;
        restartGameText.SetActive(true);
    }

    public virtual void RestartGame()
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

    public virtual void DeathReport(int playerID, int killCredit) 
    {
        if (killCredit >= 0 && killCredit < maxPlayers)
        {
            playerHUDs[killCredit].AddKill();
        }
        CheckForRoundEnd();
    }
    
    public virtual void ChangePlayerState(int playerID, PlayerState playerState)
    {
        if (playerID < 0 || playerID >= maxPlayers) return;
        playerStates[playerID] = playerState;
    }

    public virtual void CheckForRoundEnd()
    {
        return;
    }
    
}
