using FMODUnity;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Localization.SmartFormat.Core.Parsing;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyManager : NetworkBehaviour
{
    #region Singleton

    public static LobbyManager instance;

    #endregion

    #region Game Mode Settings

    public PlayerInputManager playerInputManager;
    public GameObject playerInput;

    public int connectedDevices;


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

    public LoadoutSelection.LoadOutType selectedLoadoutType = LoadoutSelection.LoadOutType.SharedRandom;
    public int selectedLeftSpellIndex = 0;
    public int selectedRightSpellIndex = 0;

    [SerializeField] GameObject uiParent;
    public GameModeSO[] GameModes { get => gameModes; set => gameModes = value; }
    public MapSettingsSO[] MapSettings { get => mapSettings; set => mapSettings = value; }
    public SO_Spell[] Spells { get => spells; set => spells = value; }

    [Tooltip("Currently selected game mode.")]
    [SerializeField] private GameManager.GameModeType selectedGameMode = GameManager.GameModeType.Standard;

    public List<LobbyPlayerInput> allLobbyPlayerInputs = new List<LobbyPlayerInput>();

    public bool canAddNewDevices = true; 

    public GameManager.GameModeType SelectedGameMode
    {
        get => selectedGameMode;
        set => ChangeSelectedGameModeClientRpc(value);
    }

    public int winsNeeded = 8;

    [Tooltip("If enabled, the game runs endlessly.")]
    public bool playEndless;

    [Tooltip("Number of rounds already played.")]
    public int playedRounds;

    [Tooltip("UI panel for match settings.")]
    [SerializeField] public GameObject matchSettingsSelection;
    public GameObject _MatchSettingsSelection { get => matchSettingsSelection; set => matchSettingsSelection = value; }

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
    public UnityEvent<ulong, bool> OnReadyStateUpdated;

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

    [SerializeField] private Color[] teamColors;
    public Color[] TeamColors { get { return teamColors; } }

    #endregion

    #region Audio

    public GameObject lobbyPlayer; 

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

        if (GameObject.FindWithTag("OnlineMatchmakingUI"))
            GameObject.FindWithTag("OnlineMatchmakingUI").SetActive(false);
    }

    private void Start()
    {
        scores.ResetKills();
        scores.ResetWins();
        selectedLoadoutType = LoadoutSelection.LoadOutType.SharedRandom;
        selectedLeftSpellIndex = 0;
        selectedRightSpellIndex = 0;

        gameModeTypeText.text =
            gameModes[0].GameModeLocalizationProperty.LocalizedString.GetLocalizedString();

        foreach (PlayerLobbyState player in players)
            playerContainers[player.ClientId].SetActive(true);

        if (IsServer && TransportSwitcher.Instance.isUsingRelay)
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnLoadEventCompleted;
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallback;
        }

        ChangeStartButtonState(false);

        if (!TransportSwitcher.Instance.isUsingRelay)
        {
            foreach (var device in InputSystem.devices)
            {
                PlayerInput playerInput = playerInputManager.JoinPlayer(playerIndex: -1, controlScheme: null, pairWithDevice: device);
            }
        }
        else if (IsServer)
        {
            Debug.Log("Spawning player");
            GameObject player = Instantiate(lobbyPlayer);
            player.GetComponent<NetworkObject>().SpawnAsPlayerObject(0, true);
        }
    }

    private void OnEnable()
    {
        InputSystem.onDeviceChange += OnDeviceChange;

        if (TransportSwitcher.Instance.isUsingRelay)
            if (players != null)
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

    void OnClientConnectedCallback(ulong playerIndex)
    {
        Debug.Log("Updating selectedgame mode");
        ChangeSelectedGameModeServerRpc();
    }

    private void OnDisable()
    {
        InputSystem.onDeviceChange -= OnDeviceChange;
        if (players != null)
            players.OnListChanged -= OnPlayersListChanged;

        LobbyManager.instance = null;
    }

    private void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        if (!canAddNewDevices || !TransportSwitcher.Instance.isUsingRelay) return; 

        switch (change)
        {
            case InputDeviceChange.Added:
                Debug.Log($"Device added: {device.displayName}");
                playerInputManager.JoinPlayer(playerIndex: -1, controlScheme: null, pairWithDevice: device);
                break;

            case InputDeviceChange.Removed:
                Debug.Log($"Device removed: {device.displayName}");
                break;

            case InputDeviceChange.Reconnected:
                Debug.Log($"Device reconnected: {device.displayName}");
                break;

            case InputDeviceChange.Disconnected:
                Debug.Log($"Device disconnected: {device.displayName}");
                break;
        }
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

    public void SetReady(int playerIndex, bool value)
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

        if(value)
        {
            player.IsReady = true;
            players[index] = player;

            OnReadyStateUpdated?.Invoke((ulong)playerIndex, true);
            CheckAllReady();
            UpdatePlayerUI();
        }
        else
        {
            player.IsReady = false;
            players[index] = player;


            OnReadyStateUpdated?.Invoke((ulong)playerIndex, false);
            CheckAllReady();
            UpdatePlayerUI();
        }
    }

    public void RemovePlayer(int playerIndex)
    {
        if (!TransportSwitcher.Instance.isUsingRelay)
        {
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i].ClientId == (ulong)playerIndex)
                {
                    var state = players[i];
                    state.IsReady = false;
                    players[i] = state;

                    OnReadyStateUpdated?.Invoke((ulong)playerIndex, false);
                    return;
                }
            }
        }
        else
        {
            RemovePlayerServerRpc(playerIndex);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void RemovePlayerServerRpc(int playerIndex)
    {
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].ClientId == (ulong)playerIndex)
            {
                var state = players[i];
                state.IsReady = false;
                players[i] = state;

                OnReadyStateUpdated?.Invoke((ulong)playerIndex, false);
                return;
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ToggleReadyServerRpc(ulong clientID, bool state)
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

          
            LobbyPlayerValues.Instance.AddNewPlayerValueServerRpc((int)clientID, possibleSkins[clientID].Index, false);           
            index = players.Count - 1;
        }
        else
        {
            var player = players[index];
            var skinChange = playerContainers[index].GetComponent<PlayerContainerSkinChange>();
            TeamSelection teamSelection = teamSelections[index].GetComponent<TeamSelection>();

            if ((!player.IsReady && !skinChange.currentlyOnLocked) || player.IsReady)
            {
                player.IsReady = state;
                players[index] = player;
                InvokeOnReadyStateUpdatedClientRpc(clientID, state);
            }
        }

        CheckAllReady();
    }

    [ClientRpc]
    private void InvokeOnReadyStateUpdatedClientRpc(ulong clientID, bool state)
    {
        OnReadyStateUpdated?.Invoke(clientID, state);
    }

    public void CheckAllReady()
    {
        if (players.Count == 0)
        {
            allPlayersReady = false;
            ChangeStartButtonState(false);
            return;
        }

        for (int i = 0; i < players.Count; i++) 
        {
            if(!players[i].IsReady && playerContainers[i].GetComponent<PlayerContainerManager>().occupied)
            {
                allPlayersReady = false;
                ChangeStartButtonState(false);
                return;
            }
        }

        int occupiedContainers = 0;
        foreach(GameObject container in playerContainers)
        {
            if(container.GetComponent<PlayerContainerManager>().occupied)
            {
                occupiedContainers++;
            }
        }
        if(occupiedContainers <= 0)
        {
            allPlayersReady = false;
            ChangeStartButtonState(false);
            return;
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
                if (playerContainers[index].GetComponent<PlayerContainerManager>().occupied)
                {
                    emptyPlayerContainers[index].SetActive(false);
                    playerContainers[index].SetActive(true);
                }
            }
        }
    }

    public void UpdatePlayerUIAndOccupiedState()
    {
        foreach (GameObject container in playerContainers)
            container.SetActive(false);

        foreach (var player in players)
        {
            int index = (int)player.ClientId;
            if (index >= 0 && index < playerContainers.Length)
            {
                playerContainers[index].GetComponent<PlayerContainerManager>().occupied = true;
                emptyPlayerContainers[index].SetActive(false);
                playerContainers[index].SetActive(true);
            }
        }
    }

    public IEnumerator LoadGameScene()
    {
        HandleLobbyContinueServerRpc();
        yield return new WaitForSeconds(1f);

        if (loadRandomLevel && SteamIntegration.instance.IsFullVersion)
            MapRotationSystem.Instance.CheckForMapSwitch(MapRotationSystem.Instance.MaxRounds);
        else
        {
            NetworkManager.Singleton.SceneManager.LoadScene(plateLevel, LoadSceneMode.Single);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void HandleLobbyContinueServerRpc()
    {
        HandleLobbyContinueClientRpc();
    }

    [ClientRpc]
    void HandleLobbyContinueClientRpc()
    {
        PlayStartSFXClientRpc();
        uiParent.SetActive(false);
        GetComponent<PlayerInputManager>().enabled = false;
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
    public void ChangeSelectedGameModeServerRpc()
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

        foreach (GameObject teamSelection in teamSelections)
        {
            teamSelection.SetActive(
                gameModeType == GameManager.GameModeType.Team
            );
        }

        foreach (GameObject skin in LobbyManager.instance.playerContainers)
        {
            skin.GetComponent<PlayerContainerSkinChange>().UpdateBlur();
        }
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

    public void SetWinsNeeded(float value)
    {   
        SetWinsNeededServerRpc((int)value);
    }

    [ServerRpc(RequireOwnership = false)]
    void SetWinsNeededServerRpc(int value)
    {
        SetWinsNeededClientRpc(value);
    }

    [ClientRpc]
    void SetWinsNeededClientRpc(int value)
    {
        winsNeeded = value;
        MatchSettingsSelection.Instance.ApplyLoadoutConditionalNavigation();
    }

    public void ToggleEndless(bool toggle)
    {
       ToggleEndlessServerRpc(toggle);
    }

    [ServerRpc(RequireOwnership = false)]
    void ToggleEndlessServerRpc(bool toggle)
    {
        ToggleEndlessClientRpc(toggle);
    }
    [ClientRpc]
    void ToggleEndlessClientRpc(bool toggle)
    {
        playEndless = toggle;
    }


    [ServerRpc(RequireOwnership = false)]
    public void UpdateTeamServerRpc(int playerIndex)
    {
        UpdateTeamClientRpc(playerIndex);
    }

    [ClientRpc]
    public void UpdateTeamClientRpc(int playerIndex)
    {
        playerContainers[playerIndex]
       .GetComponentInChildren<TeamSelection>()
       .ChangeTeam();
    }
}