using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

public class GameManager : NetworkBehaviour
{
    public enum GameModeType {Standard, Team}

    [SerializeField] protected GameModeType gameModeType;
    public GameModeType GameMode {get{return gameModeType;}}
    [SerializeField] protected int[] teamIDs = new int[maxPlayers];
    public int[] TeamIDs {get{return teamIDs;}}
    
    [SerializeField] protected List<PlayerController> teamA = new List<PlayerController>();
    public List<PlayerController> TeamA {get{return teamA;}}
    
    [SerializeField] protected List<PlayerController> teamB = new List<PlayerController>();
    public List<PlayerController> TeamB {get{return teamB;}}
    
    public static GameManager Instance;
    public GameObject playerPrefab;
    public static bool IsGamePaused = false;
    
    protected const int maxPlayers = 4;
    protected float gameEndDelay = 1f;
    protected bool gameEnded;
    protected bool isReadyToRestart = false;
    public int maxGameRounds = 7;
    public bool playEndless = true;
    protected int finishedRoundCount = 0;
    public Action OnGameEnded;
    public Action OnGameStarted;

    [SerializeField] protected SO_GameSettings gameSettings;
    [SerializeField] protected MapEvent mapEvent;

    protected PlayerController[] players = new PlayerController[maxPlayers];
    public PlayerController[] Players {get{return players;}}
    
    protected PlayerHUD[] playerHUDs = new PlayerHUD[maxPlayers];
    protected PlayerState[] playerStates = new PlayerState[maxPlayers];
    
    public PlayerInputManager playerInputManager;
    public bool PlayingLocal = false;
    public Countdown countdown;

    public DeathzoneWall[] deathZones; 

    [Header("Achievement Values")]
    public HitReference[] hitReferences;
    [Serializable]
    public class HitReference
    {
        public BasicBubble.SpellType spellType;
        public int playerHitID = -1;
        public bool wasSlippery;
        public bool wasReflected;
    }
    
    [SerializeField] private float multiKillTimeWindow = 3f;
    private Dictionary<int, List<float>> playerKillTimestamps  = new Dictionary<int, List<float>>();
    [SerializeField] private int damageAmountForAchievement = 300;
    [SerializeField] private bool enableArchievents = true;
    
    private void Awake()
    {
        if (LobbyManager.instance)
        {
            gameModeType = LobbyManager.instance.SelectedGameMode;
            maxGameRounds = LobbyManager.instance.maxGameRounds;
            playEndless = LobbyManager.instance.playEndless;
        }

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
        
        if (TransportSwitcher.Instance && TransportSwitcher.Instance.isUsingRelay)
            countdown.onCountdownComplete.AddListener(StartGameAfterDelay);
        else
            PlayingLocal = true;

        hitReferences = new HitReference[4];
        for (int i = 0; i < hitReferences.Length; i++)
        {
            hitReferences[i] = new HitReference(); 
            hitReferences[i].playerHitID = -1;
        }

        if (mapEvent != null)
        {
            mapEvent?.InitialiseMapEvent();
            Debug.Log("Starting Map event");
        }
        else
        {
            Debug.Log("MapEvent == null");
        }
    }

    private void OnDisable()
    {
        if(LobbyManager.instance && countdown) 
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

    public void ChangeHitReference(int index, BasicBubble.SpellType spellType, int playerHitID, bool wasSlippery, bool wasReflected)
    {
        if (index < 0 || index >= hitReferences.Length) 
            return;
        hitReferences[index].spellType = spellType;
        hitReferences[index].playerHitID = playerHitID;
        hitReferences[index].wasSlippery = wasSlippery;
        hitReferences[index].wasReflected = wasReflected;

        foreach (PlayerController playerController in players)
        {
            if (playerController == null) continue; 
            
            if (index == playerController.PlayerID)
            {
                // Found the player that caused a hit on an enemy
                playerController.UnlockShotsHitInARowAchievement(true);
            }

            if (playerHitID == playerController.PlayerID)
            {
                // Found the player that was hit by an enemy
                playerController.UnlockShotsHitInARowAchievement(false);
            }
        }
    }

    public virtual void EndGame()
    {
        OnGameEnded?.Invoke();
        UIManager.Instance.SetScoreScreenActive(true);
        finishedRoundCount++;
        if (LobbyManager.instance)
        {
            LobbyManager.instance.playedRounds++;
            if (LobbyManager.instance.playedRounds >= maxGameRounds && !playEndless)
            {
                if (ScoreManager.Instance)
                    ScoreManager.Instance.showWinner = true;
            }
        }

        ScoreManager.Instance.ResolveScores();
        
        foreach (HitReference hitReference in hitReferences)
        {
            hitReference.spellType = BasicBubble.SpellType.Null;
            hitReference.playerHitID = -1;
            hitReference.wasSlippery = false;
            hitReference.wasReflected = false;
        }

        if (LobbyManager.instance && LobbyManager.instance.playedRounds < maxGameRounds || playEndless)
        {
            isReadyToRestart = true;
        }
        else
        {
            isReadyToRestart = true;
        }
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

    public virtual void AddPlayer(int playerID, PlayerController player, PlayerHUD playerHUD, int teamID)
    {
        if (playerID < 0 || playerID >= maxPlayers) return;
        if(gameModeType == GameModeType.Team)
        {
            teamIDs[playerID] = teamID;
            switch (teamID)
            {
                case 1:
                    teamA.Add(player);
                    break;
                case 2:
                    teamB.Add(player);
                    break;
                default:
                    break;
            }
        }
        players[playerID] = player;
        playerHUDs[playerID] = playerHUD;
    }
    public List<PlayerController> GetTeam(int playerID)
    {
        if (gameModeType != GameModeType.Team)
            return null;
        int t = teamIDs[playerID];
        switch (t)
        {
            case 1:
                return teamA;
            case 2:
                return teamB;
            default:
                return null;
        }
    }
    [ServerRpc(RequireOwnership = false)]
    public virtual void DeathReportServerRpc(int playerID, int killCredit)
    {
        if (killCredit >= 0 && killCredit < maxPlayers)
        {
            ScoreManager.Instance.AddPendingScore(killCredit, false);
        }

        if (enableArchievents && killCredit >= 0 && killCredit < maxPlayers && hitReferences[killCredit].spellType != BasicBubble.SpellType.Null && hitReferences[killCredit].playerHitID == playerID)
        {
            // Debug.Log("Kill registered by player "+ killCredit +", killing player "+ hitReferences[killCredit].playerHitID  +", with the spell " + hitReferences[killCredit].spellType);
            IncrementSmallerGiantBubbleKillAchievement(killCredit);
            UnlockMultiKillAchievements(killCredit);
            IncrementSlipperyKillAchievement(killCredit);
            IncrementReflectedKillAchievement(killCredit);
        }
        CheckForRoundEndServerRpc();       
    }

    public virtual void DeathReportLocal(int playerID, int killCredit)
    {
        if (killCredit >= 0 && killCredit < maxPlayers)
        {
            ScoreManager.Instance.AddPendingScore(killCredit, false);
        }
        
        if (enableArchievents && killCredit >= 0 && killCredit < maxPlayers && hitReferences[killCredit].spellType != BasicBubble.SpellType.Null && hitReferences[killCredit].playerHitID == playerID)
        {
            // Debug.Log("Kill registered by player "+ killCredit +", killing player "+ hitReferences[killCredit].playerHitID  +", with the spell " + hitReferences[killCredit].spellType);
            IncrementSmallerGiantBubbleKillAchievement(killCredit);
            UnlockMultiKillAchievements(killCredit);
            IncrementSlipperyKillAchievement(killCredit);
            IncrementReflectedKillAchievement(killCredit);
        }
        CheckForRoundEndLocal();
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

    public SO_GameSettings GetGameSettings()
    {
        return gameSettings;
    }

    #region Achievements
    
    protected void UnlockRoundEndWithZeroDamageAchievement(int winnerID)
    {
        if (!enableArchievents) return;
        if (TransportSwitcher.Instance && TransportSwitcher.Instance.isUsingRelay && NetworkManager.Singleton.LocalClientId != (ulong)winnerID 
            || players[winnerID].Damage > 0
            || !SteamIntegration.instance) return;

        SteamIntegration steamIntegration = SteamIntegration.instance;
        SteamIntegration.instance.UnlockAchievement(steamIntegration.zeroDamageAchievementID);
    }

    protected void UnlockRoundEndWithXDamageAchievement(int winnerID)
    {
        if (!enableArchievents) return;
        if (TransportSwitcher.Instance && TransportSwitcher.Instance.isUsingRelay && NetworkManager.Singleton.LocalClientId != (ulong)winnerID 
            || players[winnerID].Damage < damageAmountForAchievement
            || !SteamIntegration.instance) return;
        
        SteamIntegration steamIntegration = SteamIntegration.instance;
        SteamIntegration.instance.UnlockAchievement(steamIntegration.damagedAchievementID);
    }

    private void IncrementSmallerGiantBubbleKillAchievement(int playerID)
    {
        if (!enableArchievents) return;
        if (TransportSwitcher.Instance && TransportSwitcher.Instance.isUsingRelay && NetworkManager.Singleton.LocalClientId != (ulong)playerID 
            || hitReferences[playerID].spellType != BasicBubble.SpellType.SmallerGiant
            || !SteamIntegration.instance) return;
        
        SteamIntegration steamIntegration = SteamIntegration.instance;
        SteamIntegration.instance.IncrementIntSteamStat(steamIntegration.smallerGiantKillsStatID, 
            1, 
            steamIntegration.StatThresholds[steamIntegration.smallerGiantKillsStatID], 
            steamIntegration.smallerGiantKillsAchievementID);
    }
    
    private void UnlockMultiKillAchievements(int killerID)
    {
        if (!enableArchievents) return;
        if (TransportSwitcher.Instance && TransportSwitcher.Instance.isUsingRelay && NetworkManager.Singleton.LocalClientId != (ulong)killerID 
            || !SteamIntegration.instance) return;
        
        SteamIntegration steamIntegration = SteamIntegration.instance;

        if (!playerKillTimestamps.ContainsKey(killerID))
            playerKillTimestamps[killerID] = new List<float>();

        playerKillTimestamps[killerID].Add(Time.time);
        playerKillTimestamps[killerID].RemoveAll(t => Time.time - t > multiKillTimeWindow);
            
        int killsWithinWindow = playerKillTimestamps[killerID].Count;
        if (killsWithinWindow == 2)
            SteamIntegration.instance.UnlockAchievement(steamIntegration.doubleKillAchievementID);
        else if (killsWithinWindow == 3)
            SteamIntegration.instance.UnlockAchievement(steamIntegration.tripleKillAchievementID);
    }

    private void IncrementSlipperyKillAchievement(int killerID)
    {
        if (!enableArchievents) return;
        if (TransportSwitcher.Instance && TransportSwitcher.Instance.isUsingRelay && NetworkManager.Singleton.LocalClientId != (ulong)killerID 
            || !SteamIntegration.instance 
            || !hitReferences[killerID].wasSlippery) return;
        
        SteamIntegration steamIntegration = SteamIntegration.instance;
        SteamIntegration.instance.IncrementIntSteamStat(steamIntegration.slipperyKillStatID, 
            1, 
            steamIntegration.StatThresholds[steamIntegration.slipperyKillStatID], 
            steamIntegration.slipperyKillAchievementID);
    }
    
    private void IncrementReflectedKillAchievement(int killerID)
    {
        if (!enableArchievents) return;
        if (TransportSwitcher.Instance && TransportSwitcher.Instance.isUsingRelay && NetworkManager.Singleton.LocalClientId != (ulong)killerID 
            || !SteamIntegration.instance 
            || !hitReferences[killerID].wasReflected) return;

        SteamIntegration steamIntegration = SteamIntegration.instance;
        SteamIntegration.instance.IncrementIntSteamStat(steamIntegration.reflectedKillStatID, 
            1, 
            steamIntegration.StatThresholds[steamIntegration.reflectedKillStatID], 
            steamIntegration.reflectedKillAchievementID);
    }
    #endregion
}