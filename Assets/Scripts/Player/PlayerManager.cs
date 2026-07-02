using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using FMODUnity;

public class PlayerManager : NetworkBehaviour
{
    public static PlayerManager Instance;

    [Header("Audio")]
    [SerializeField] private EventReference winSound;

    [Header("Player Setup")]
    [SerializeField] private PlayerHUD[] playerHUDs;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private int[] teamIDs;
    
    private NetworkVariable<int> syncedFirstSpellIndex = new NetworkVariable<int>();
    private NetworkVariable<int> syncedSecondSpellIndex = new NetworkVariable<int>();
    
    public NetworkList<NetworkObjectReference> players = new NetworkList<NetworkObjectReference>();
    private List<GameObject> localPlayers = new List<GameObject>();

    public Action OnPlayerWon;

    private int playersInitializedCount = 0;

    public InputActionProperty startGameInputAction; 
    public PlayerInputManager playerInputManager;
    public Countdown countdown;

    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        
        playerInputManager = GetComponent<PlayerInputManager>();
    }

    private void Start()
    {
        countdown.onCountdownComplete.AddListener(StartPlayerJoining);
    }

    private void OnDisable()
    {
        startGameInputAction.action.performed -= ActionOnPerformed;
        startGameInputAction.action.Disable();
    }

    public void StartPlayerJoining()
    {
      Invoke(nameof(StartPlayerJoiningMethod), .5f);
    }

    void StartPlayerJoiningMethod()
    {
        CameraHandler.Instance.onCinematicEnd.RemoveListener(StartPlayerJoining);
        GameManager.Instance.OnGameStarted += ResetPlayers;
        if (GameManager.Instance.PlayingLocal)
        {
            if (!TransportSwitcher.Instance)
            {
                startGameInputAction.action.performed += ActionOnPerformed;
                startGameInputAction.action.Enable();
            }

            playerInputManager.enabled = true;
        }
        
        if(TransportSwitcher.Instance && TransportSwitcher.Instance.isUsingRelay)
        {
            playerInputManager.enabled = false;
        }
        else if (TransportSwitcher.Instance && !TransportSwitcher.Instance.isUsingRelay)
        {
            StartLocalGame();
        }
    }
    
    private void ActionOnPerformed(InputAction.CallbackContext context)
    {
        startGameInputAction.action.performed -= ActionOnPerformed;
        startGameInputAction.action.Disable();
        
        if (LobbyPlayerValues.Instance != null)
        {
            LobbyPlayerValues lobbyPlayerHandler = LobbyPlayerValues.Instance;

            foreach (LobbyPlayerValues.PlayerValues playerDevice in lobbyPlayerHandler.playerValuesList)
            {
                if (playerDevice == null) continue;

                InputDevice device = playerDevice.Device;

                if (device is Keyboard)
                {
                    PlayerInput newPlayer = PlayerInputManager.instance.JoinPlayer(
                        playerDevice.PlayerIndex, 
                        -1,
                        "Keyboard", 
                        playerDevice.Device 
                    );
                }
                else if (device is Gamepad)
                {
                    PlayerInput newPlayer = PlayerInputManager.instance.JoinPlayer(
                        playerDevice.PlayerIndex, 
                        -1,
                        null,
                        playerDevice.Device 
                    );

                    if (newPlayer != null)
                    {
                        newPlayer.SwitchCurrentControlScheme(playerDevice.Device);
                    }
                }
            }
        }
    }

    void StartLocalGame()
    {
        RerollSpells();
        if (LobbyPlayerValues.Instance != null)
        {
            LobbyPlayerValues lobbyPlayerHandler = LobbyPlayerValues.Instance;

            foreach (LobbyPlayerValues.PlayerValues playerDevice in lobbyPlayerHandler.playerValuesList)
            {
                if (playerDevice == null) continue;

                InputDevice device = playerDevice.Device;

                if (device is Keyboard)
                {
                    PlayerInput newPlayer = PlayerInputManager.instance.JoinPlayer(
                        playerDevice.PlayerIndex,
                        -1,
                        "Keyboard",
                        playerDevice.Device
                    );
                }
                else if (device is Gamepad)
                {
                    PlayerInput newPlayer = PlayerInputManager.instance.JoinPlayer(
                        playerDevice.PlayerIndex,
                        -1,
                        null,
                        playerDevice.Device
                    );

                    if (newPlayer != null)
                    {
                        newPlayer.SwitchCurrentControlScheme(playerDevice.Device);
                    }
                }

                if (LobbyManager.instance.selectedLoadoutType == LoadoutSelection.LoadOutType.IndividualRandom)
                    RerollSpells();
            }
        }
        ItemSpawner.Instance.InitialSpawn();
        playerInputManager.enabled = false;
    }

    #region Player Joining and Initialization

    public void JoinLocal(PlayerInput input)
    {
        if (!input.TryGetComponent<CharacterController>(out var characterController)) return;

        int playerID = playersInitializedCount++;
        if (!ValidatePlayerID(playerID)) return;

        SetupPlayerHUD(playerID);

        characterController.enabled = false;

        input.transform.position = spawnPoints[playerID].position;
        input.transform.rotation = spawnPoints[playerID].rotation;
        
        var playerController = input.GetComponent<PlayerController>();
        playerController.InitializeLocal();

        var gamePad = input.GetDevice<Gamepad>();
        ControllerRumbler rumbler = null;
        if (gamePad != null)
        {
            rumbler = input.gameObject.AddComponent<ControllerRumbler>();
            rumbler.SetController(gamePad);
        }
        
        playerController.SetUpPlayer(playerID, playerHUDs[playerID], rumbler, LobbyPlayerValues.Instance.playerValuesList[playerID].Skin);
        playerController.SetSpells(syncedFirstSpellIndex.Value, syncedSecondSpellIndex.Value);

        characterController.enabled = true;

        ItemSpawner.Instance.ChangeMaxItemAmount(true);
        GameManager.Instance.AddPlayer(playerID, playerController, playerHUDs[playerID], LobbyPlayerValues.Instance.playerValuesList[playerID].TeamIndex);
        GameManager.Instance.ChangePlayerStateLocal(playerID, PlayerState.alive);

        TargetGroupManager.Instance.AddToGroup(input.transform);
    }

    public void OnPlayerJoined(PlayerInput input)
    {
        if(!input.TryGetComponent<CharacterController>(out var characterController)) return;

        int playerID = playersInitializedCount++;
        if (!ValidatePlayerID(playerID)) return;

        SetupPlayerHUD(playerID);

        characterController.enabled = false;

        input.transform.position = spawnPoints[playerID].position;
        input.transform.rotation = spawnPoints[playerID].rotation;

        var playerController = input.GetComponent<PlayerController>();

        var gamePad = input.GetDevice<Gamepad>();
        ControllerRumbler rumbler = null;
        if (gamePad != null)
        {
            rumbler = input.gameObject.AddComponent<ControllerRumbler>();
            rumbler.SetController(gamePad);
        }
        
        playerController.SetUpPlayer(playerID, playerHUDs[playerID], rumbler, LobbyPlayerValues.Instance.playerValuesList[playerID].Skin);
        playerController.SetSpells(syncedFirstSpellIndex.Value, syncedSecondSpellIndex.Value);

        characterController.enabled = true;

        ItemSpawner.Instance.ChangeMaxItemAmount(true);
        GameManager.Instance.AddPlayer(playerID, playerController, playerHUDs[playerID], LobbyPlayerValues.Instance.playerValuesList[playerID].TeamIndex);
        GameManager.Instance.ChangePlayerStateServerRpc(playerID, PlayerState.alive);

        TargetGroupManager.Instance.AddToGroup(input.transform);
    }

    [ServerRpc(RequireOwnership = false)]
    public void AddPlayerServerRpc(NetworkObjectReference playerRef)
    {
        players.Add(playerRef);
    }

    public void AddPlayerLocal(PlayerInput input)
    {
        localPlayers.Add(input.gameObject);
    }

    public void Initialize()
    {
        RerollSpells();
        foreach (var playerRef in players)
        {
            if (playerRef.TryGet(out NetworkObject networkObject))
            {
                networkObject.GetComponent<PlayerController>().InitializeClientRpc();
            }
            if (LobbyManager.instance.selectedLoadoutType == LoadoutSelection.LoadOutType.IndividualRandom)
                RerollSpells();
        }
    }

    #endregion

    #region Player Reset and Spell Management

    private void ResetPlayers()
    {
        RerollSpells();

        if (GameManager.Instance.PlayingLocal)
        {
            foreach (var player in localPlayers)
            {
                ResetPlayerComponents(player);
                if (LobbyManager.instance && LobbyManager.instance.selectedLoadoutType == LoadoutSelection.LoadOutType.IndividualRandom)
                    RerollSpells();
            }
        }
        else
        {
            foreach (var playerRef in players)
            {
                if (playerRef.TryGet(out NetworkObject networkObject))
                {
                    ResetPlayerComponents(networkObject.gameObject);
                    if (LobbyManager.instance.selectedLoadoutType == LoadoutSelection.LoadOutType.IndividualRandom)
                        RerollSpells();
                }
            }
        }
    }

    private void ResetPlayerComponents(GameObject player)
    {
        var stateHandler = player.GetComponent<PlayerStateHandler>();
        var controller = player.GetComponent<PlayerController>();

        stateHandler.ResetPlayer();

        controller.SetSpells(syncedFirstSpellIndex.Value, syncedSecondSpellIndex.Value);

        var animator = controller.mainAnimator;
        animator.SetBool("IsDead", false);
        animator.SetBool("Victory", false);
    }

    private void RerollSpells()
    {
        if (!IsServer && !GameManager.Instance.PlayingLocal) return;
        if (LobbyManager.instance && LobbyManager.instance.selectedLoadoutType == LoadoutSelection.LoadOutType.SharedCustom)
        {
            syncedFirstSpellIndex.Value = LobbyManager.instance.selectedLeftSpellIndex;
            syncedSecondSpellIndex.Value = LobbyManager.instance.selectedRightSpellIndex;
            return;
        }

        List<int> legalSpells = new List<int>();
        for (int i = 0; i < ItemSpawner.Instance.GetSpellCount(); i++)
        {
            if (ItemSpawner.Instance.SpawnableItems[i].CanUse)
            {
                legalSpells.Add(i);
            }
        }
        syncedFirstSpellIndex.Value = UnityEngine.Random.Range(0, ItemSpawner.Instance.SpawnableItems.Length);
        syncedSecondSpellIndex.Value = UnityEngine.Random.Range(0, ItemSpawner.Instance.SpawnableItems.Length);
    }


    #endregion

    #region Utilities

    private bool ValidatePlayerID(int playerID)
    {
        if (playerID >= playerHUDs.Length || playerID >= spawnPoints.Length)
        {
            return false;
        }
        return true;
    }

    private void SetupPlayerHUD(int playerID)
    {
        playerHUDs[playerID].gameObject.SetActive(true);

        if (LobbyPlayerValues.Instance)
        {
            playerHUDs[playerID].InitialisePlayerHUD(playerID);

            if (GameManager.Instance.GameMode == GameManager.GameModeType.Standard)
                ScoreManager.Instance.InitialiseScorePanel(playerID, LobbyPlayerValues.Instance.playerValuesList[playerID].Skin.GameSprites[0], LobbyPlayerValues.Instance.playerValuesList[playerID].Skin.Color);
            else if (GameManager.Instance.GameMode == GameManager.GameModeType.Team)
                ScoreManager.Instance.InitialiseTeamScorePanel(GameManager.Instance.TeamIDs[playerID], playerID);
        }
        else
        {
            Debug.Log("PlayerManager: THER IS NO LOBBY MANAGER! DEATH TO ALL");
        }
    }

    public void ResetPlayerPosition(int playerID)
    {
        if (GameManager.Instance.PlayingLocal)
        {
            if (playerID < localPlayers.Count)
            {
                var localPlayer = localPlayers[playerID];
                localPlayer.transform.position = spawnPoints[playerID].position;
                localPlayer.transform.rotation = spawnPoints[playerID].rotation;
                TargetGroupManager.Instance.AddToGroup(localPlayer.transform);
            }
        }
        else
        {
            if (playerID >= players.Count)
            {
                return;
            }

            if (players[playerID].TryGet(out NetworkObject networkObject))
            {
                var playerGameObject = networkObject.gameObject;
                playerGameObject.transform.position = spawnPoints[playerID].position;
                playerGameObject.transform.rotation = spawnPoints[playerID].rotation;
                TargetGroupManager.Instance.AddToGroup(playerGameObject.transform);
            }
        }
    }

    #endregion
}