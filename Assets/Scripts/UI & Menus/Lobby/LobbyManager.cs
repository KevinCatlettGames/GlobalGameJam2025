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

public class LobbyManager : NetworkBehaviour
{
    public static LobbyManager instance;

    [Header("Game Mode Settings")]
    [SerializeField] private bool loadRandomLevel = true;
    [SerializeField] private string plateLevel = "Lvl_MainScene";
    [SerializeField] private SO_Scores scores;
    [SerializeField] private GameModeSO[] gameModes;
    [SerializeField] private MapSettingsSO[] mapSettings;
    [SerializeField] private SO_Spell[] spells;

    public GameModeSO[] GameModes { get => gameModes; set => gameModes = value; }
    public MapSettingsSO[] MapSettings { get => mapSettings; set => mapSettings = value; }
    public SO_Spell[] Spells { get => spells; set => spells = value; }

    [SerializeField] private GameManager.GameModeType selectedGameMode = GameManager.GameModeType.Standard;
    public GameManager.GameModeType SelectedGameMode
    {
        get => selectedGameMode;
        set => ChangeSelectedGameModeClientRpc(value);
    }

    public int maxGameRounds = 7;
    public bool playEndless;
    public int playedRounds;

    [SerializeField] private GameObject gameModeSelection;
    [SerializeField] private GameObject matchSettingsSelection;

    public GameObject GameModeSelection { get => gameModeSelection; set => gameModeSelection = value; }
    public GameObject MatchSettingsSelection { get => matchSettingsSelection; set => matchSettingsSelection = value; }

    [Header("Player Settings")]
    [SerializeField] private int maxLocalPlayers = 4;
    [SerializeField] private int minPlayers = 1;
    [SerializeField] private SkinSO[] possibleSkins;

    public SkinSO[] PossibleSkins { get => possibleSkins; set => possibleSkins = value; }

    [Header("Network Players")]
    public NetworkList<PlayerLobbyState> players = new();
    public bool allPlayersReady;
    public UnityEvent<ulong> OnReadyStateUpdated;
    public UnityEvent OnAllPlayersLoadedIn;

    [Header("UI Elements")]
    public GameObject[] playerContainers;
    public GameObject[] teamSelections;
    public Image[] teamIndicators;
    public Sprite[] teamSprites;

    public Toggle[] weaponToggles;
    public Toggle[] mapUsageToggles;
    public Toggle[] mapEventToggles;
    public Slider[] mapRoundsSliders;

    [SerializeField] private Button startButton;
    [SerializeField] private Image startButtonImage;
    [SerializeField] private TextMeshProUGUI[] startButtonTexts;
    [SerializeField] private TextMeshProUGUI gameModeTypeText;
    [SerializeField] private Color startButtonColorWhenEnabled;

    [Header("Audio Emitters")]
    [SerializeField] private StudioEventEmitter joinEmitter;
    [SerializeField] private StudioEventEmitter selectEmitter;
    [SerializeField] private StudioEventEmitter unselectEmitter;
    [SerializeField] private StudioEventEmitter playerStartEmitter;
    [SerializeField] private StudioEventEmitter buttonOnClickEmitter;

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
            joinEmitter.Play();
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
            if (player.IsReady)
                selectEmitter.Play();
            else
                unselectEmitter.Play();
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
            EmitSoundServerRpc(0);
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

        EmitSoundServerRpc(players[index].IsReady ? 1 : 2);
        CheckAllReady();
    }

    [ServerRpc(RequireOwnership = false)]
    private void EmitSoundServerRpc(int emitterIndex)
    {
        EmitSoundClientRpc(emitterIndex);
    }

    [ClientRpc]
    private void EmitSoundClientRpc(int emitterIndex)
    {
        if (emitterIndex == 0) joinEmitter.Play();
        if (emitterIndex == 1) selectEmitter.Play();
        if (emitterIndex == 2) unselectEmitter.Play();
    }

    [ClientRpc]
    private void AddNewPlayerValuesClientRpc(int clientID)
    {
        LobbyPlayerHandler.Instance.playerValuesList.Add(
            new LobbyPlayerHandler.PlayerValues(clientID, null, possibleSkins[clientID], -1)
        );

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
                playerContainers[index].SetActive(true);
        }
    }

    public IEnumerator LoadGameScene()
    {
        PlayStartSFXClientRpc();
        yield return new WaitForSeconds(1f);

        if (loadRandomLevel)
            MapRotationSystem.Instance.CheckForMapSwitch(MapRotationSystem.Instance.MaxRounds);
        else
            NetworkManager.Singleton.SceneManager.LoadScene(plateLevel, LoadSceneMode.Single);
    }

    private void ChangeStartButtonState(bool enable)
    {
        foreach (TextMeshProUGUI text in startButtonTexts)
            text.color = enable ? Color.white : Color.gray;

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