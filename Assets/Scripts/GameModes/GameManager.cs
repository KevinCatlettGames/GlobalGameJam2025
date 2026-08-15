using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameManager : NetworkBehaviour
{
    public enum GameModeType { Standard, Team }

    public static GameManager Instance;
    public static bool IsGamePaused = false;
    private bool isResetting = false;
    public bool IsResetting { get { return isResetting; } set { isResetting = value; } }

    #region Serialized & Public Fields

    [Header("Game Mode & Settings")]
    [SerializeField] protected GameModeType gameModeType;
    public GameModeType GameMode => gameModeType;

    public bool playEndless = true;
    [SerializeField] protected SO_GameSettings gameSettings;
    [SerializeField] protected MapEvent mapEvent;

    [Header("Player & Team State")]
    public GameObject playerPrefab;
    public PlayerInputManager playerInputManager;
    public Countdown countdown;
    public DeathzoneWall[] deathZones;
    public bool PlayingLocal = false;

    [SerializeField] protected int[] teamIDs = new int[maxPlayers];
    public int[] TeamIDs => teamIDs;

    [SerializeField] protected List<PlayerController> teamA = new List<PlayerController>();
    public List<PlayerController> TeamA => teamA;

    [SerializeField] protected List<PlayerController> teamB = new List<PlayerController>();
    public List<PlayerController> TeamB => teamB;

    [Header("Achievement Values")]
    public HitReference[] hitReferences;
    [SerializeField] private int damageAmountForAchievement = 300;

    #endregion

    #region Protected & Private State

    protected const int maxPlayers = 4;
    protected float gameEndDelay = 1f;
    protected bool gameEnded;
    protected bool isReadyToRestart = false;
    public bool IsReadyToRestart => isReadyToRestart;

    protected int finishedRoundCount = 0;
    public int FinishedRoundCount => finishedRoundCount;

    public Action OnGameEnded;
    public Action OnGameStarted;

    protected PlayerController[] players = new PlayerController[maxPlayers];
    public PlayerController[] Players => players;

    protected PlayerHUD[] playerHUDs = new PlayerHUD[maxPlayers];
    protected PlayerState[] playerStates = new PlayerState[maxPlayers];

    // Achievement Tracking State
    private float multiKillTimeWindow = 10f;
    private Dictionary<int, List<float>> playerKillTimestamps = new Dictionary<int, List<float>>();
    private int[] rapidShotHitStreaks = new int[maxPlayers];
    private Dictionary<int, NunchuckCastTracker> playerNunchuckTrackers = new Dictionary<int, NunchuckCastTracker>();
    private Dictionary<int, HashSet<BasicBubble.SpellType>> playerWeaponKills = new Dictionary<int, HashSet<BasicBubble.SpellType>>();
    private int[] weaponComboStreak = new int[maxPlayers];
    private BasicBubble.SpellType[] lastWeaponHit = new BasicBubble.SpellType[maxPlayers];
    private Dictionary<int, DetonationTracker> activeDetonations = new Dictionary<int, DetonationTracker>();

    #endregion

    #region Nested Classes & Structs

    [Serializable]
    public class HitReference
    {
        public BasicBubble.SpellType spellType;
        public int playerHitID = -1;
        public bool wasSlippery;
        public bool wasReflected;
        public bool wasHitByExplosion;
        public bool wasSlowed;
        public int castID;
    }

    private class NunchuckCastTracker
    {
        public int currentCastID = -1;
        public int hitCount = 0;
    }

    private class DetonationTracker
    {
        public int killerID;
        public float timestamp;
        public int koCount;
    }

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        if (LobbyManager.instance)
        {
            if(SceneManager.GetActiveScene().buildIndex != 6)
                gameModeType = LobbyManager.instance.SelectedGameMode;

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
        {
            countdown.OnCountdownStart.AddListener(StartGameAfterDelay);
        }
        else
        {
            PlayingLocal = true;
        }

        hitReferences = new HitReference[4];
        for (int i = 0; i < hitReferences.Length; i++)
        {
            hitReferences[i] = new HitReference();
            hitReferences[i].playerHitID = -1;
        }

        if (mapEvent != null)
            mapEvent?.InitialiseMapEvent();

        ResetRapidShotStreaks();
    }

    private void OnDisable()
    {
        if (LobbyManager.instance && countdown)
        {
            countdown.OnCountdownStart.RemoveListener(StartGameAfterDelay);
        }
    }

    #endregion

    #region Game Flow & Initialization

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

            for (int i = 0; i < LobbyManager.instance.players.Count; i++)
            {
                GameObject player = Instantiate(playerPrefab);
                player.GetComponent<NetworkObject>().SpawnAsPlayerObject(LobbyManager.instance.players[i].ClientIndex, true);
                PlayerManager.Instance.AddPlayerServerRpc(player);
            }

            Invoke(nameof(CallPlayerManagerInitialize), .1f);
            Invoke(nameof(EnableDeathzonesServerRpc), .2f);
        }
        ItemSpawner.Instance.InitialSpawn();
    }

    private void CallPlayerManagerInitialize()
    {
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.Initialize();
        }
    }

    public virtual void EndGame()
    {
        OnGameEnded?.Invoke();
        UIManager.Instance.SetScoreScreenActive(true);
        finishedRoundCount++;

        if (LobbyManager.instance)
            LobbyManager.instance.playedRounds++;

        ScoreManager.Instance.ResolveScores();

        foreach (HitReference hitReference in hitReferences)
        {
            hitReference.spellType = BasicBubble.SpellType.Null;
            hitReference.playerHitID = -1;
            hitReference.wasSlippery = false;
            hitReference.wasReflected = false;
        }

        isReadyToRestart = true;
    }

    public virtual void RestartGame()
    {
        OnGameStarted?.Invoke();
        gameEnded = false;
        isReadyToRestart = false;
        IsResetting = false;
        UIManager.Instance.SetScoreScreenActive(false);
        ResetRapidShotStreaks();
    }

    [ServerRpc(RequireOwnership = false)]
    public void RestartGameServerRpc()
    {
        RestartGameClientRpc();
    }

    [ClientRpc]
    private void RestartGameClientRpc()
    {
        DisableDeathzonesServerRpc();
        OnGameStarted?.Invoke();
        gameEnded = false;
        isReadyToRestart = false;
        IsResetting = false;
        UIManager.Instance.SetScoreScreenActive(false);
        Invoke(nameof(EnableDeathzonesServerRpc), .5f);
    }

    public SO_GameSettings GetGameSettings() => gameSettings;

    #endregion

    #region Player & Team Management

    public virtual void AddPlayer(int playerID, PlayerController player, PlayerHUD playerHUD, int teamID)
    {
        if (playerID < 0 || playerID >= maxPlayers) return;

        if (gameModeType == GameModeType.Team)
        {
            teamIDs[playerID] = teamID;
            switch (teamID)
            {
                case 1: teamA.Add(player); break;
                case 2: teamB.Add(player); break;
            }
        }
        players[playerID] = player;
        playerHUDs[playerID] = playerHUD;
    }

    public List<PlayerController> GetTeam(int playerID)
    {
        if (gameModeType != GameModeType.Team) return null;

        return teamIDs[playerID] switch
        {
            1 => teamA,
            2 => teamB,
            _ => null
        };
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

    #endregion

    #region Combat & Hit Tracking

    public void ChangeHitReference(int index, BasicBubble.SpellType spellType, int playerHitID, bool wasSlippery, bool wasReflected, bool wasHitByExplosion, int castID = -1)
    {
        if (index < 0 || index >= hitReferences.Length) return;

        hitReferences[index].spellType = spellType;
        hitReferences[index].playerHitID = playerHitID;
        hitReferences[index].wasSlippery = wasSlippery;
        hitReferences[index].wasReflected = wasReflected;
        hitReferences[index].wasHitByExplosion = wasHitByExplosion;
        hitReferences[index].castID = castID;

        if (playerHitID >= 0 && playerHitID < maxPlayers)
        {
            rapidShotHitStreaks[playerHitID] = 0;
        }

        UnlockHitRapidShotsWithoutGettingHitAchievement(index);

        if (spellType == BasicBubble.SpellType.Slasher && playerHitID != index)
        {
            RegisterNunchuckHit(index, castID);
        }

        UnlockCyborgAchievement(index, spellType);
    }

    [ServerRpc(RequireOwnership = false)]
    public virtual void DeathReportServerRpc(int playerID, int killCredit)
    {
        DeathReportClientRpc(playerID, killCredit);

        if (killCredit >= 0 && killCredit < maxPlayers && hitReferences[killCredit].spellType != BasicBubble.SpellType.Null && hitReferences[killCredit].playerHitID == playerID)
        {
            IncrementSmallerGiantBubbleKillAchievement(killCredit);
            UnlockMultiKillAchievements(killCredit);
            IncrementReflectedKillAchievement(killCredit);
            IncrementSlowedPlayersKilledAchievement(killCredit);
            IncrementDoubleNunchuckKillAchievement(killCredit);

            HitReference hit = hitReferences[killCredit];
            if (hit.spellType == BasicBubble.SpellType.Exploding && hit.wasHitByExplosion)
            {
                CheckDetonationMultiKillAchievement(killCredit, hit.castID);
            }
        }
        CheckForRoundEndServerRpc();
    }

    [ClientRpc]
    private void DeathReportClientRpc(int playerID, int killCredit)
    {
        if (killCredit >= 0 && killCredit < maxPlayers)
        {
            if (gameModeType == GameModeType.Standard)
                ScoreManager.Instance.AddPendingScore(killCredit, false);
            else if (gameModeType == GameModeType.Team)
                ScoreManager.Instance.AddPendingTeamScore(teamIDs[killCredit], false);
        }
    }

    public virtual void DeathReportLocal(int playerID, int killCredit)
    {
        if (killCredit >= 0 && killCredit < maxPlayers)
        {
            if (gameModeType == GameModeType.Standard)
                ScoreManager.Instance.AddPendingScore(killCredit, false);
            else if (gameModeType == GameModeType.Team)
                ScoreManager.Instance.AddPendingTeamScore(teamIDs[killCredit], false);
        }

        if (killCredit >= 0 && killCredit < maxPlayers && hitReferences[killCredit].spellType != BasicBubble.SpellType.Null && hitReferences[killCredit].playerHitID == playerID)
        {
            IncrementSmallerGiantBubbleKillAchievement(killCredit);
            UnlockMultiKillAchievements(killCredit);
            UnlockBotAchievement(killCredit, hitReferences[killCredit].spellType);
            IncrementReflectedKillAchievement(killCredit);
            IncrementSlowedPlayersKilledAchievement(killCredit);
            IncrementDoubleNunchuckKillAchievement(killCredit);

            HitReference hit = hitReferences[killCredit];
            if (hit.spellType == BasicBubble.SpellType.Exploding && hit.wasHitByExplosion)
            {
                CheckDetonationMultiKillAchievement(killCredit, hit.castID);
            }
        }
        CheckForRoundEndLocal();
    }

    [ServerRpc(RequireOwnership = false)]
    public virtual void CheckForRoundEndServerRpc() { }

    public virtual void CheckForRoundEndLocal() { }

    #endregion

    #region Map Mechanics & Deathzones

    [ServerRpc(RequireOwnership = false)]
    private void EnableDeathzonesServerRpc() => EnableDeathzonesClientRpc();

    [ClientRpc]
    private void EnableDeathzonesClientRpc()
    {
        foreach (DeathzoneWall deathZone in deathZones)
            deathZone.GetComponent<DeathzoneWall>().EnableCol();
    }

    [ServerRpc(RequireOwnership = false)]
    private void DisableDeathzonesServerRpc() => DisableDeathzonesClientRpc();

    [ClientRpc]
    private void DisableDeathzonesClientRpc()
    {
        foreach (DeathzoneWall deathZone in deathZones)
            deathZone.GetComponent<DeathzoneWall>().DisableCol();
    }

    #endregion

    #region Achievements

    protected void UnlockRoundEndWithZeroDamageAchievement(int winnerID)
    {
        if (TransportSwitcher.Instance && TransportSwitcher.Instance.isUsingRelay && NetworkManager.Singleton.LocalClientId != (ulong)winnerID
            || players[winnerID].Damage > 0
            || !AchievementSaveSystem.instance || SceneManager.GetActiveScene().buildIndex == 5 || SceneManager.GetActiveScene().buildIndex == 6) return;

        AchievementSaveSystem.instance.UnlockAchievement(16);
    }

    protected void UnlockRoundEndWithXDamageAchievement(int winnerID)
    {
        if (TransportSwitcher.Instance && TransportSwitcher.Instance.isUsingRelay && NetworkManager.Singleton.LocalClientId != (ulong)winnerID
            || players[winnerID].Damage < damageAmountForAchievement
            || !AchievementSaveSystem.instance || SceneManager.GetActiveScene().buildIndex == 5 || SceneManager.GetActiveScene().buildIndex == 6) return;

        AchievementSaveSystem.instance.UnlockAchievement(12);
    }

    private void IncrementSmallerGiantBubbleKillAchievement(int playerID)
    {
        if (TransportSwitcher.Instance && TransportSwitcher.Instance.isUsingRelay && NetworkManager.Singleton.LocalClientId != (ulong)playerID
            || hitReferences[playerID].spellType != BasicBubble.SpellType.SmallerGiant
            || !AchievementSaveSystem.instance || SceneManager.GetActiveScene().buildIndex == 5 || SceneManager.GetActiveScene().buildIndex == 6) return;

        AchievementSaveSystem achSaveSystem = AchievementSaveSystem.instance;
        achSaveSystem.IncrementStat(0, 1);
        achSaveSystem.IncrementStat(22, 1);
    }

    private void IncrementDoubleNunchuckKillAchievement(int killerID)
    {
        if (TransportSwitcher.Instance && TransportSwitcher.Instance.isUsingRelay && NetworkManager.Singleton.LocalClientId != (ulong)killerID
            || !AchievementSaveSystem.instance || SceneManager.GetActiveScene().buildIndex == 5 || SceneManager.GetActiveScene().buildIndex == 6) return;

        HitReference hit = hitReferences[killerID];

        if (hit.spellType == BasicBubble.SpellType.Slasher)
        {
            if (playerNunchuckTrackers.TryGetValue(killerID, out NunchuckCastTracker tracker))
            {
                if (tracker.currentCastID == hit.castID && tracker.hitCount >= 2)
                {
                    AchievementSaveSystem.instance.IncrementStat(10, 1);
                }
            }
        }

        if (playerNunchuckTrackers.ContainsKey(killerID))
        {
            playerNunchuckTrackers[killerID].hitCount = 0;
            playerNunchuckTrackers[killerID].currentCastID = -1;
        }
    }

    private void RegisterNunchuckHit(int attackerID, int castID)
    {
        if (!playerNunchuckTrackers.ContainsKey(attackerID))
        {
            playerNunchuckTrackers[attackerID] = new NunchuckCastTracker();
        }

        NunchuckCastTracker tracker = playerNunchuckTrackers[attackerID];

        if (tracker.currentCastID != castID)
        {
            tracker.currentCastID = castID;
            tracker.hitCount = 0;
        }

        tracker.hitCount++;
    }

    public void UnlockDieFromOwnExplosionAchievement(int playerID)
    {
        if (TransportSwitcher.Instance && TransportSwitcher.Instance.isUsingRelay && NetworkManager.Singleton.LocalClientId != (ulong)playerID
            || hitReferences[playerID].spellType != BasicBubble.SpellType.Exploding && hitReferences[playerID].spellType != BasicBubble.SpellType.Grenade
            || !AchievementSaveSystem.instance || SceneManager.GetActiveScene().buildIndex == 5 || SceneManager.GetActiveScene().buildIndex == 6) return;

        if (playerID != hitReferences[playerID].playerHitID) return;
        if (!hitReferences[playerID].wasHitByExplosion) return;

        AchievementSaveSystem.instance.UnlockAchievement(14);
    }

    private void UnlockMultiKillAchievements(int killerID)
    {
        if (TransportSwitcher.Instance && TransportSwitcher.Instance.isUsingRelay && NetworkManager.Singleton.LocalClientId != (ulong)killerID
            || !AchievementSaveSystem.instance || SceneManager.GetActiveScene().buildIndex == 5 || SceneManager.GetActiveScene().buildIndex == 6) return;

        AchievementSaveSystem achSaveSystem = AchievementSaveSystem.instance;

        if (!playerKillTimestamps.ContainsKey(killerID))
        {
            playerKillTimestamps[killerID] = new List<float>();
        }

        playerKillTimestamps[killerID].Add(Time.time);
        playerKillTimestamps[killerID].RemoveAll(t => Time.time - t > multiKillTimeWindow);

        int killsWithinWindow = playerKillTimestamps[killerID].Count;
        if (killsWithinWindow == 2)
            achSaveSystem.UnlockAchievement(13);
        else if (killsWithinWindow == 3)
            achSaveSystem.UnlockAchievement(27);
    }

    private void UnlockHitRapidShotsWithoutGettingHitAchievement(int index)
    {
        if (TransportSwitcher.Instance && TransportSwitcher.Instance.isUsingRelay && NetworkManager.Singleton.LocalClientId != (ulong)index
            || !AchievementSaveSystem.instance || SceneManager.GetActiveScene().buildIndex == 5 || SceneManager.GetActiveScene().buildIndex == 6) return;

        HitReference hit = hitReferences[index];

        if (hit.spellType != BasicBubble.SpellType.Basic) return;
        if (hit.playerHitID == index) return;

        rapidShotHitStreaks[index]++;

        if (rapidShotHitStreaks[index] >= 8)
        {
            AchievementSaveSystem.instance.UnlockAchievement(2);
            rapidShotHitStreaks[index] = 0;
        }
    }

    private void ResetRapidShotStreaks()
    {
        for (int i = 0; i < rapidShotHitStreaks.Length; i++)
        {
            rapidShotHitStreaks[i] = 0;
        }
    }

    private void IncrementReflectedKillAchievement(int killerID)
    {
        if (TransportSwitcher.Instance && TransportSwitcher.Instance.isUsingRelay && NetworkManager.Singleton.LocalClientId != (ulong)killerID
            || !AchievementSaveSystem.instance
            || !hitReferences[killerID].wasReflected || SceneManager.GetActiveScene().buildIndex == 5 || SceneManager.GetActiveScene().buildIndex == 6) return;

        AchievementSaveSystem.instance.IncrementStat(3, 1);
    }

    private void IncrementSlowedPlayersKilledAchievement(int killerID)
    {
        if (SceneManager.GetActiveScene().buildIndex == 5 || SceneManager.GetActiveScene().buildIndex == 6 | !AchievementSaveSystem.instance || !players[hitReferences[killerID].playerHitID].WasSlowedWhenLastHit) return;

        AchievementSaveSystem.instance.IncrementStat(9, 1);
    }

    private void CheckDetonationMultiKillAchievement(int killerID, int castID)
    {
        List<int> expiredKeys = new List<int>();
        foreach (var kvp in activeDetonations)
        {
            if (Time.time - kvp.Value.timestamp > 10f)
                expiredKeys.Add(kvp.Key);
        }
        foreach (int key in expiredKeys) activeDetonations.Remove(key);

        if (activeDetonations.TryGetValue(castID, out DetonationTracker tracker))
        {
            UnlockDetonationAchievement(killerID);
            activeDetonations.Remove(castID);
        }
    }

    private void UnlockDetonationAchievement(int killerID)
    {
        if (TransportSwitcher.Instance && TransportSwitcher.Instance.isUsingRelay && NetworkManager.Singleton.LocalClientId != (ulong)killerID
            || !AchievementSaveSystem.instance || SceneManager.GetActiveScene().buildIndex == 5 || SceneManager.GetActiveScene().buildIndex == 6) return;

        AchievementSaveSystem.instance.IncrementStat(5, 1);
    }

    public void RegisterExplosionHit(int attackerID, int victimID, int castID, bool wasDetonatedByBubble)
    {
        ChangeHitReference(attackerID, BasicBubble.SpellType.Exploding, victimID, false, false, true, castID);

        if (wasDetonatedByBubble)
        {
            if (!activeDetonations.ContainsKey(castID))
            {
                activeDetonations[castID] = new DetonationTracker()
                {
                    killerID = attackerID,
                    timestamp = Time.time,
                    koCount = 0
                };
            }
        }
    }

    private void UnlockBotAchievement(int killerID, BasicBubble.SpellType usedSpell)
    {
        if (TransportSwitcher.Instance && TransportSwitcher.Instance.isUsingRelay && NetworkManager.Singleton.LocalClientId != (ulong)killerID
            || !AchievementSaveSystem.instance || SceneManager.GetActiveScene().buildIndex == 5 || SceneManager.GetActiveScene().buildIndex == 6 || hitReferences[killerID].playerHitID != 5) return;

        if (!playerWeaponKills.ContainsKey(killerID))
        {
            playerWeaponKills[killerID] = new HashSet<BasicBubble.SpellType>();
        }

        if (playerWeaponKills[killerID].Add(usedSpell))
        {
            if (playerWeaponKills[killerID].Count >= ItemSpawner.Instance.SpawnableItems.Length - 1)
            {
                AchievementSaveSystem.instance.UnlockAchievement(26);
            }
        }
    }

    public void OnWeaponMissed(int attackerID)
    {
        if (attackerID >= 0 && attackerID < maxPlayers)
        {
            weaponComboStreak[attackerID] = 0;
            lastWeaponHit[attackerID] = BasicBubble.SpellType.Null;
        }
    }

    private void UnlockCyborgAchievement(int index, BasicBubble.SpellType currentWeapon)
    {
        if (TransportSwitcher.Instance && TransportSwitcher.Instance.isUsingRelay && NetworkManager.Singleton.LocalClientId != (ulong)index
            || !AchievementSaveSystem.instance || SceneManager.GetActiveScene().buildIndex == 5 || SceneManager.GetActiveScene().buildIndex == 6) return;

        if (currentWeapon == BasicBubble.SpellType.Null || hitReferences[index].playerHitID == index) return;

        if (lastWeaponHit[index] == currentWeapon)
        {
            weaponComboStreak[index] = 1;
            lastWeaponHit[index] = currentWeapon;
            return;
        }

        weaponComboStreak[index]++;
        lastWeaponHit[index] = currentWeapon;

        if (weaponComboStreak[index] >= 3)
        {
            AchievementSaveSystem.instance.UnlockAchievement(23);
            // Debug.Log($"Achievement Unlocked: Hit 3 different weapons in a row for player {index}!");

            weaponComboStreak[index] = 0;
            lastWeaponHit[index] = BasicBubble.SpellType.Null;
        }
    }
    #endregion
}