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
using UnityEngine.Localization.PropertyVariants.TrackedProperties;

public class LobbyManager : NetworkBehaviour
{
    public static LobbyManager instance;
    
    [Header("Game Mode Settings")]
    bool loadRandomLevel = true;
    [SerializeField] SO_Scores scores;
    [SerializeField] string plateLevel = "Lvl_MainScene";
    
    [SerializeField] GameModeSO[] gameModes;
    public GameModeSO[]  GameModes { get => gameModes; set => gameModes = value; }
    
    [SerializeField] MapSettingsSO[] mapSettings;
    public  MapSettingsSO[] MapSettings { get => mapSettings; set => mapSettings = value; }
    
    [SerializeField] GameManager.GameModeType selectedGameMode = GameManager.GameModeType.SingleElimination;
    public GameManager.GameModeType SelectedGameMode
    {
        get => selectedGameMode;
        set
        {
            ChangeSelectedGameModeClientRpc(value);
        }
    }

    public Toggle[] mapUsageToggles;
    
    public TextMeshProUGUI gameModeTypeText;
    
    [SerializeField] GameObject gameModeSelection;
    public GameObject GameModeSelection { get => gameModeSelection; set => gameModeSelection = value; }

    [SerializeField] GameObject matchSettingsSelection; 
    public GameObject MatchSettingsSelection  { get => matchSettingsSelection; set => matchSettingsSelection = value; }
    
    [Header("Player Settings")]
    [SerializeField] int maxLocalPlayers = 4;
    [SerializeField] int minPlayers = 1;
    
    [SerializeField] SkinSO[] possibleSkins;
    public SkinSO[] PossibleSkins  { get => possibleSkins; set => possibleSkins = value; }
    
    [Header("Network Players")]
    public NetworkList<PlayerLobbyState> players = new NetworkList<PlayerLobbyState>();
    public bool allPlayersReady = false;
    public UnityEvent<ulong> OnReadyStateUpdated;
    public UnityEvent OnAllPlayersLoadedIn;
    
    [Header("UI Elements")]
    public GameObject[] playerContainers;
    [SerializeField] Button startButton;
    [SerializeField] Image startButtonImage;
    [SerializeField] private TextMeshProUGUI[] startButtonTexts;
    [SerializeField] Color startButtonColorWhenEnabled;
    
    
    [Header("Audio Emitters")]
    [SerializeField] StudioEventEmitter joinEmitter;
    [SerializeField] StudioEventEmitter selectEmitter;
    [SerializeField] StudioEventEmitter unselectEmitter;
    [SerializeField] StudioEventEmitter playerStartEmitter;
    [SerializeField] StudioEventEmitter buttonOnClickEmitter;
    
    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        scores.ResetKills();
        scores.ResetWins();
        
        gameModeTypeText.text = gameModes[0].GameModeLocalizationProperty.LocalizedString.GetLocalizedString();

        foreach (PlayerLobbyState player in players)
            playerContainers[player.ClientId].SetActive(true);

        if (IsServer && TransportSwitcher.Instance.isUsingRelay)
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnLoadEventCompleted;

        ChangeStartButtonState(false);

        if (TransportSwitcher.Instance.isUsingRelay && !IsHost)
            UpdateSelectedGameModeForNewClientServerRpc();
    }

    private void OnLoadEventCompleted(string scenename, LoadSceneMode loadscenemode, List<ulong> clientscompleted, List<ulong> clientstimedout)
    {
        if (scenename != "UI_Lobby" && scenename != "UI_MainMenu")
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

    private void OnEnable()
    {
        if (TransportSwitcher.Instance.isUsingRelay)
            players.OnListChanged += OnPlayersListChanged;

        foreach (MapSettingsSO mapSetting in MapSettings)
        {
            mapSetting.PlayMap = true;
            mapSetting.PlayWithMapEvent = true;
        }
    }

    private void OnDestroy()
    {
        if (TransportSwitcher.Instance.isUsingRelay)
            players.OnListChanged -= OnPlayersListChanged;

        if (IsServer && TransportSwitcher.Instance.isUsingRelay && NetworkManager.Singleton)
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnLoadEventCompleted;
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
            index = players.Count - 1;
            CheckAllReady();
            UpdatePlayerUI();
            joinEmitter.Play();
        }
        else
        {
            var player = players[index];
            var skinChange = playerContainers[index].GetComponent<PlayerContainerSkinChange>();

            if ((!player.IsReady && !skinChange.currentlyOnLocked) || player.IsReady)
            {
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
    }

    [ServerRpc(RequireOwnership = false)]
    void EmitSoundServerRpc(int emitterIndex)
    {
        EmitSoundClientRpc(emitterIndex);
    }

    [ClientRpc]
    void EmitSoundClientRpc(int emitterIndex)
    {
        switch  (emitterIndex)
        {
            case 0:
                joinEmitter.Play();
                break;
            case 1:
                selectEmitter.Play();
                break;
            case 2:
                unselectEmitter.Play();
                break;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ToggleReadyServerRpc(ulong clientID)
    {
        int index = -1;
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].ClientId == (ulong)clientID)
            {
                index = i;
                break;
            }
        }

        if (index == -1)
        {
            players.Add(new PlayerLobbyState { ClientId = (ulong)clientID, IsReady = false });
            AddNewPlayerValuesClientRpc((int)clientID);
            index = players.Count - 1;
            EmitSoundServerRpc(0);
        }
        else
        {
            var player = players[index];
            var skinChange = playerContainers[index].GetComponent<PlayerContainerSkinChange>();

            if ((!player.IsReady && !skinChange.currentlyOnLocked) || player.IsReady)
            {
                player.IsReady = !player.IsReady;
                players[index] = player;
                InvokeOnReadyStateUpdatedClientRpc(clientID);
            }
        }
        
        if (players[index].IsReady)
            EmitSoundServerRpc(1);
        else
            EmitSoundServerRpc(2);

        CheckAllReady();
    }

    [ClientRpc]
    private void AddNewPlayerValuesClientRpc(int clientID)
    {
        LobbyPlayerHandler.Instance.playerValuesList.Add(
            new LobbyPlayerHandler.PlayerValues(clientID, null, possibleSkins[clientID]));
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
                if (allPlayersReady)
                {
                    allPlayersReady = false;
                    ChangeStartButtonState(false);
                }
                return;
            }
        }

        allPlayersReady = true;

        if (players.Count >= minPlayers)
        {
            allPlayersReady = true;
            ChangeStartButtonState(true);
        }

        if (TransportSwitcher.Instance.isUsingRelay &&
            NetworkManager.Singleton.ConnectedClients.Count > players.Count)
        {
            allPlayersReady = false;
            ChangeStartButtonState(false);
        }
    }

    public void UpdatePlayerUI()
    {
        for (int i = 0; i < playerContainers.Length; i++)
            playerContainers[i].SetActive(false);

        foreach (var player in players)
        {
            int containerIndex = (int)player.ClientId;
            if (containerIndex >= 0 && containerIndex < playerContainers.Length)
                playerContainers[containerIndex].SetActive(true);
        }
    }

    public IEnumerator LoadGameScene()
    {
        PlayStartSFXClientRpc();
        yield return new WaitForSeconds(1f);
            
        if(loadRandomLevel) 
            MapRotationSystem.Instance.CheckForMapSwitch(MapRotationSystem.Instance.MaxRounds);
        else
            NetworkManager.Singleton.SceneManager.LoadScene(plateLevel, LoadSceneMode.Single);
    }

    void ChangeStartButtonState(bool enable)
    {
        if (enable)
        {
            foreach (TextMeshProUGUI text in startButtonTexts)
                text.color = Color.white; 
            startButtonImage.color = startButtonColorWhenEnabled;
            startButton.interactable = true;
        }
        else
        {
            foreach (TextMeshProUGUI text in startButtonTexts)
                text.color = Color.gray; 
            startButtonImage.color = Color.gray;
            startButton.interactable = false;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void UpdateSelectedGameModeForNewClientServerRpc()
    {
        ChangeSelectedGameModeClientRpc(selectedGameMode);
    }

    [ClientRpc]
    void ChangeSelectedGameModeClientRpc(GameManager.GameModeType gameModeType)
    {
        GameModeSO gameModeSoToUse = gameModes[0];
        foreach (GameModeSO gameModeSo in gameModes)
        {
            if (gameModeType == gameModeSo.GameModeType)
            {
                gameModeSoToUse = gameModeSo;
                break;
            }
        }
        selectedGameMode = gameModeType;
        gameModeTypeText.text = gameModeSoToUse.GameModeLocalizationProperty.LocalizedString.GetLocalizedString();
    }

    [ClientRpc]
    void PlayStartSFXClientRpc()
    {
        playerStartEmitter.Play();
    }

    public void TogglePlateMapEvent(bool toggle)
    {
        buttonOnClickEmitter.Play();
        mapSettings[0].PlayWithMapEvent = toggle;

        int unactiveToggles = 0;
        foreach (MapSettingsSO mapSetting in mapSettings)
        {
            if(mapSetting.PlayWithMapEvent == false)
                unactiveToggles++;
        }

        if (unactiveToggles > 2)
        {
            foreach (Toggle tog in mapUsageToggles)
            {
                if(tog.isOn) 
                    tog.interactable = false;
            }
        }
        else
        {
            foreach(Toggle tog in mapUsageToggles)
                tog.interactable = true;
        }
    }

    public void ToggleUsageOfPlateMap(bool toggle)
    {
        buttonOnClickEmitter.Play();
        mapSettings[0].PlayMap = toggle;
        HandleMapUsageToggleActiveState();
    }
    
    public void TogglePotMapEvent(bool toggle)
    {
        buttonOnClickEmitter.Play();
        mapSettings[1].PlayWithMapEvent = toggle;
        HandleMapUsageToggleActiveState();
    }
    
    public void ToggleUsageOfPotMap(bool toggle)
    {
        buttonOnClickEmitter.Play();
        mapSettings[1].PlayMap = toggle;
        HandleMapUsageToggleActiveState();
    }
    
    public void ToggleBucketMapEvent(bool toggle)
    {
        buttonOnClickEmitter.Play();
        mapSettings[2].PlayWithMapEvent = toggle;
        HandleMapUsageToggleActiveState();
    }
    
    public void ToggleUsageOfBucketMap(bool toggle)
    {
        buttonOnClickEmitter.Play();
        mapSettings[2].PlayMap = toggle;
        HandleMapUsageToggleActiveState();
    }
    
    public void ToggleTunaMapEvent(bool toggle)
    {
        buttonOnClickEmitter.Play();
        mapSettings[3].PlayWithMapEvent = toggle;
        HandleMapUsageToggleActiveState();
    }
    
    public void ToggleUsageOfTunaMap(bool toggle)
    {
        buttonOnClickEmitter.Play();
        mapSettings[3].PlayMap = toggle;
        HandleMapUsageToggleActiveState();
    }

    void HandleMapUsageToggleActiveState()
    {
        int unactiveToggles = 0;
        foreach (MapSettingsSO mapSetting in mapSettings)
        {
            if(!mapSetting.PlayMap)
                unactiveToggles++;
        }

        if (unactiveToggles > 2)
        {
            foreach (Toggle tog in mapUsageToggles)
            {
                if(tog.isOn) 
                    tog.interactable = false;
            }
        }
        else
        {
            foreach(Toggle tog in mapUsageToggles)
                tog.interactable = true;
        }
        Debug.Log(unactiveToggles);
    }
}