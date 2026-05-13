using Unity.Netcode;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using FMODUnity;
using TMPro;
using UnityEngine.InputSystem;

/// <summary>
/// Handles the multiplayer lobby including:
/// - Player ready states
/// - Game mode selection
/// - Map & spell configuration
/// - UI synchronization
/// - Network synchronization (RPCs)
/// </summary>
public class LobbyManager : NetworkBehaviour
{
    #region Singleton

    /// <summary>
    /// Global instance of the LobbyManager.
    /// </summary>
    public static LobbyManager instance;

    #endregion

    #region Game Mode Settings

    [Header("Game Mode Settings")]

    [Tooltip("If enabled, maps will be selected randomly instead of loading a fixed scene.")]
    [SerializeField] private bool loadRandomLevel = true;

    [Tooltip("Scene name used when random loading is disabled.")]
    [SerializeField] private string plateLevel = "Lvl_Teller";

    [Tooltip("Reference to score tracking ScriptableObject.")]
    [SerializeField] private SO_Scores scores;

    [Tooltip("Available game modes.")]
    [SerializeField] private GameModeSO[] gameModes;

    [Tooltip("All available map configurations.")]
    [SerializeField] private MapSettingsSO[] mapSettings;

    [Tooltip("Available spells/weapons.")]
    [SerializeField] private SO_Spell[] spells;

    [SerializeField] GameObject uiParent;
    public GameModeSO[] GameModes { get => gameModes; set => gameModes = value; }
    public MapSettingsSO[] MapSettings { get => mapSettings; set => mapSettings = value; }
    public SO_Spell[] Spells { get => spells; set => spells = value; }

    [Tooltip("Currently selected game mode.")]
    [SerializeField] private GameManager.GameModeType selectedGameMode = GameManager.GameModeType.Standard;

    public GameManager.GameModeType SelectedGameMode
    {
        get => selectedGameMode;
        set => ChangeSelectedGameModeClientRpc(value);
    }

    [Tooltip("Maximum number of rounds before game ends.")]
    public int maxGameRounds = 7;

    public int winsNeededToWin = 8;

    [Tooltip("If enabled, the game runs endlessly.")]
    public bool playEndless;

    [Tooltip("Number of rounds already played.")]
    public int playedRounds;

    [Tooltip("UI panel for match settings.")]
    [SerializeField] private GameObject matchSettingsSelection;
    public GameObject MatchSettingsSelection { get => matchSettingsSelection; set => matchSettingsSelection = value; }

    public LobbyPlayerInput lobbyInput;

    public UnityEvent OnLeavingLobby;

    #endregion

    #region Player Settings

    [Header("Player Settings")]

    [Tooltip("Maximum number of local players allowed.")]
    [SerializeField] private int maxLocalPlayers = 4;

    [Tooltip("Minimum players required to start a match.")]
    [SerializeField] private int minPlayers = 1;

    [Tooltip("Available player skins.")]
    [SerializeField] private SkinSO[] possibleSkins;

    public SkinSO[] PossibleSkins { get => possibleSkins; set => possibleSkins = value; }

    #endregion

    #region Network Players

    [Header("Network Players")]

    /// <summary>
    /// Network-synced list of players in the lobby.
    /// </summary>
    public NetworkList<PlayerLobbyState> players = new();

    [Tooltip("True if all players are ready.")]
    public bool allPlayersReady;

    /// <summary>
    /// Invoked when a player's ready state changes.
    /// </summary>
    public UnityEvent<ulong> OnReadyStateUpdated;

    /// <summary>
    /// Invoked when all players finished loading a scene.
    /// </summary>
    public UnityEvent OnAllPlayersLoadedIn;

    #endregion

    #region UI Elements

    [Header("UI Elements")]

    [Tooltip("Empty player slot placeholders.")]
    public GameObject[] emptyPlayerContainers;

    [Tooltip("Active player UI containers.")]
    public GameObject[] playerContainers;

    [Tooltip("Team selection UI elements per player.")]
    public GameObject[] teamSelections;

    [Tooltip("Team indicator images.")]
    public Image[] teamIndicators;

    [Tooltip("Sprites used for team indicators.")]
    public Sprite[] teamSprites;

    [Tooltip("Weapon toggle buttons.")]
    public Toggle[] weaponToggles;

    [Tooltip("Map enable/disable toggles.")]
    public Toggle[] mapUsageToggles;

    [Tooltip("Map event toggles.")]
    public Toggle[] mapEventToggles;

    [Tooltip("Round sliders for each map.")]
    public Slider[] mapRoundsSliders;

    [Tooltip("Start game button.")]
    [SerializeField] private Button startButton;

    [Tooltip("Image component of start button.")]
    [SerializeField] private Image startButtonImage;

    [Tooltip("Texts displayed on start button.")]
    [SerializeField] private TextMeshProUGUI[] startButtonTexts;

    [Tooltip("Text displaying current game mode.")]
    [SerializeField] private TextMeshProUGUI gameModeTypeText;

    [Tooltip("Button color when enabled.")]
    [SerializeField] private Color startButtonColorWhenEnabled;

    #endregion

    #region Audio

    [Header("Audio Emitters")]

    [Tooltip("Played when starting the game.")]
    [SerializeField] private StudioEventEmitter playerStartEmitter;

    [Tooltip("Played on UI button click.")]
    [SerializeField] private StudioEventEmitter buttonOnClickEmitter;

    #endregion

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);

        if (!GetComponent<NetworkObject>().IsSpawned)
            GetComponent<NetworkObject>().Spawn();
    }

    private void Start()
    {
        scores.ResetKills();
        scores.ResetWins();

        gameModeTypeText.text =
            gameModes[0].GameModeLocalizationProperty.LocalizedString.GetLocalizedString();

        foreach (PlayerLobbyState player in players)
            playerContainers[player.ClientId].SetActive(true);

        if (IsServer && TransportSwitcher.Instance.isUsingRelay)
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnLoadEventCompleted;

        ChangeStartButtonState(false);

        if (TransportSwitcher.Instance.isUsingRelay && !IsHost)
            UpdateSelectedGameModeForNewClientServerRpc();
    }

    private void OnEnable()
    {
        if (TransportSwitcher.Instance.isUsingRelay)
            players.OnListChanged += OnPlayersListChanged;

        foreach (MapSettingsSO mapSetting in mapSettings)
        {
            mapSetting.PlayMap = true;
            mapSetting.PlayWithMapEvent = true;
            mapSetting.MapRounds = 3;
            mapSetting.PlayedThisLoop = false;
        }

        foreach (SO_Spell spell in spells)
            spell.CanUse = true;
    }

    private void OnDestroy()
    {

        OnLeavingLobby?.Invoke();

        if (TransportSwitcher.Instance.isUsingRelay)
            players.OnListChanged -= OnPlayersListChanged;

        if (IsServer && TransportSwitcher.Instance.isUsingRelay && NetworkManager.Singleton)
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnLoadEventCompleted;
    }
    private void OnLoadEventCompleted(string sceneName, LoadSceneMode mode, List<ulong> completed, List<ulong> timedOut)
    {
        if (sceneName != "UI_Lobby" && sceneName != "UI_MainMenu")
            Invoke(nameof(InvokeEvent), 2f);
    }
    private void InvokeEvent()
    {
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
            CheckAllReady();
            UpdatePlayerUI();
            return;
        }

        var player = players[index];
        var skinChange = playerContainers[index].GetComponent<PlayerContainerSkinChange>();
        TeamSelection teamSelection = teamSelections[index].GetComponent<TeamSelection>();

        if ((!player.IsReady && !skinChange.currentlyOnLocked) || player.IsReady)
        {
            if (selectedGameMode == GameManager.GameModeType.Team && !teamSelection.SetTeamIsValid) return; 
            
            player.IsReady = !player.IsReady;
            players[index] = player;

            OnReadyStateUpdated?.Invoke((ulong)playerIndex);
            CheckAllReady();
            UpdatePlayerUI();         
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ToggleReadyServerRpc(ulong clientID)
    {
        int index = -1;

        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].ClientId == clientID)
            {
                index = i;
                break;
            }
        }

        if (index == -1)
        {
            players.Add(new PlayerLobbyState { ClientId = clientID, IsReady = false });
            AddNewPlayerValuesClientRpc((int)clientID);
            index = players.Count - 1;
        }
        else
        {
            var player = players[index];
            var skinChange = playerContainers[index].GetComponent<PlayerContainerSkinChange>();
            TeamSelection teamSelection = teamSelections[index].GetComponent<TeamSelection>();
            
            if ((!player.IsReady && !skinChange.currentlyOnLocked) || player.IsReady)
            {
                if (selectedGameMode == GameManager.GameModeType.Team && !teamSelection.SetTeamIsValid) return; 
                
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
        LobbyPlayerValues.Instance.playerValuesList.Add(
            new LobbyPlayerValues.PlayerValues(clientID, null, possibleSkins[clientID], -1)
        );

        LobbyPlayerValues.Instance.SortPlayerValues();
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
            ChangeStartButtonState(false);
            return;
        }

        foreach (var player in players)
        {
            if (!player.IsReady)
            {
                allPlayersReady = false;
                ChangeStartButtonState(false);
                return;
            }
        }

        allPlayersReady = players.Count >= minPlayers;

        if (TransportSwitcher.Instance.isUsingRelay &&
            NetworkManager.Singleton.ConnectedClients.Count > players.Count)
        {
            allPlayersReady = false;
        }

        ChangeStartButtonState(allPlayersReady);
    }

    public void UpdatePlayerUI()
    {
        foreach (GameObject container in playerContainers)
            container.SetActive(false);

        foreach (var player in players)
        {
            int index = (int)player.ClientId;
            if (index >= 0 && index < playerContainers.Length)
            {
                emptyPlayerContainers[index].SetActive(false);
                playerContainers[index].SetActive(true);
            }
        }
    }

    public IEnumerator LoadGameScene()
    {
        PlayStartSFXClientRpc();
        uiParent.SetActive(false);
        GetComponent<PlayerInputManager>().enabled = false;
        yield return new WaitForSeconds(1f);

        if (loadRandomLevel && SteamIntegration.instance.IsFullVersion)
            MapRotationSystem.Instance.CheckForMapSwitch(MapRotationSystem.Instance.MaxRounds);
        else
            NetworkManager.Singleton.SceneManager.LoadScene(plateLevel, LoadSceneMode.Single);
    }

    private void ChangeStartButtonState(bool enable)
    {
        foreach (TextMeshProUGUI text in startButtonTexts)
            text.color = enable ? Color.white : Color.gray;

        startButton.gameObject.SetActive(enable);
        startButtonImage.color = enable ? startButtonColorWhenEnabled : Color.gray;
        startButton.interactable = enable;
    }

    [ServerRpc(RequireOwnership = false)]
    private void UpdateSelectedGameModeForNewClientServerRpc()
    {
        ChangeSelectedGameModeClientRpc(selectedGameMode);
    }

    [ClientRpc]
    private void ChangeSelectedGameModeClientRpc(GameManager.GameModeType gameModeType)
    {
        GameModeSO selectedSO = gameModes[0];

        foreach (GameModeSO so in gameModes)
        {
            if (so.GameModeType == gameModeType)
            {
                selectedSO = so;
                break;
            }
        }

        selectedGameMode = gameModeType;
        gameModeTypeText.text =
            selectedSO.GameModeLocalizationProperty.LocalizedString.GetLocalizedString();
    }

    [ClientRpc]
    private void PlayStartSFXClientRpc()
    {
        playerStartEmitter.Play();
    }

    public void ToggleUsageOfPlateMap(bool toggle)
    {
        buttonOnClickEmitter.Play();
        mapSettings[0].PlayMap = toggle;
        mapEventToggles[0].interactable = toggle;
        mapRoundsSliders[0].interactable = toggle;
        HandleMapUsageToggleActiveState();
    }

    public void TogglePlateMapEvent(bool toggle)
    {
        buttonOnClickEmitter.Play();
        mapSettings[0].PlayWithMapEvent = toggle;
    }

    public void SetPlateMapRounds()
    {
        buttonOnClickEmitter.Play();
        mapSettings[0].MapRounds = (int)mapRoundsSliders[0].value;
    }

    public void ToggleUsageOfPotMap(bool toggle)
    {
        buttonOnClickEmitter.Play();
        mapSettings[1].PlayMap = toggle;
        mapEventToggles[1].interactable = toggle;
        mapRoundsSliders[1].interactable = toggle;
        HandleMapUsageToggleActiveState();
    }

    public void TogglePotMapEvent(bool toggle)
    {
        buttonOnClickEmitter.Play();
        mapSettings[1].PlayWithMapEvent = toggle;
    }

    public void SetPotMapRounds()
    {
        buttonOnClickEmitter.Play();
        mapSettings[1].MapRounds = (int)mapRoundsSliders[1].value;
    }

    public void ToggleUsageOfBucketMap(bool toggle)
    {
        buttonOnClickEmitter.Play();
        mapSettings[2].PlayMap = toggle;
        mapEventToggles[2].interactable = toggle;
        mapRoundsSliders[2].interactable = toggle;
        HandleMapUsageToggleActiveState();
    }

    public void ToggleBucketMapEvent(bool toggle)
    {
        buttonOnClickEmitter.Play();
        mapSettings[2].PlayWithMapEvent = toggle;
    }

    public void SetBucketMapRounds()
    {
        buttonOnClickEmitter.Play();
        mapSettings[2].MapRounds = (int)mapRoundsSliders[2].value;
    }

    public void ToggleUsageOfTunaMap(bool toggle)
    {
        buttonOnClickEmitter.Play();
        mapSettings[3].PlayMap = toggle;
        mapEventToggles[3].interactable = toggle;
        mapRoundsSliders[3].interactable = toggle;
        HandleMapUsageToggleActiveState();
    }

    public void ToggleTunaMapEvent(bool toggle)
    {
        buttonOnClickEmitter.Play();
        mapSettings[3].PlayWithMapEvent = toggle;
    }

    public void SetTunaMapRounds()
    {
        buttonOnClickEmitter.Play();
        mapSettings[3].MapRounds = (int)mapRoundsSliders[3].value;
    }

    private void HandleMapUsageToggleActiveState()
    {
        int disabledCount = 0;

        foreach (MapSettingsSO mapSetting in mapSettings)
        {
            if (!mapSetting.PlayMap)
                disabledCount++;
        }

        bool lockActive = disabledCount > 2;

        foreach (Toggle toggle in mapUsageToggles)
        {
            if (toggle.isOn)
                toggle.interactable = !lockActive;
            else
                toggle.interactable = true;
        }
    }

    public void ToggleSpellUsage(int spellID)
    {
        spells[spellID].CanUse = !spells[spellID].CanUse;

        int activeCount = 0;

        foreach (Toggle toggle in weaponToggles)
        {
            if (toggle.isOn)
                activeCount++;
        }

        bool lockActive = activeCount < 2;

        foreach (Toggle toggle in weaponToggles)
        {
            if (toggle.isOn)
                toggle.interactable = !lockActive;
            else
                toggle.interactable = true;
        }
    }

    public void SetMaxRounds(float value)
    {
        maxGameRounds = (int)value;
    }

    public void ToggleEndless(bool toggle)
    {
        playEndless = toggle;
    }
}