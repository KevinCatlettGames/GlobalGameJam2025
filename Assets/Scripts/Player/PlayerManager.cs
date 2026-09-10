using FMODUnity;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;

public class PlayerManager : NetworkBehaviour
{
    public static PlayerManager Instance;

    [Header("Audio")]
    [SerializeField] private EventReference winSound;

    [Header("Player Setup")]
    [SerializeField] private bool isTutorial = false;
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

    private bool dropInJoin = false;

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
        countdown.OnCountdownStart.AddListener(StartPlayerJoining);
        if (!TransportSwitcher.Instance)
        {
            dropInJoin = true;
        }
        InputSystem.onDeviceChange += OnDeviceChange;
    }

    private void OnDisable()
    {
        startGameInputAction.action.performed -= ActionOnPerformed;
        startGameInputAction.action.Disable();

        InputSystem.onDeviceChange -= OnDeviceChange;
        startGameInputAction.action.performed -= ActionOnPerformed;
        startGameInputAction.action.Disable();
    }

    private void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        if (change == InputDeviceChange.Reconnected && device is Gamepad gamepad)
        {
            foreach (var player in GetPlayers())
            {
                var input = player.GetComponent<PlayerInput>();

                if (input != null && (input.devices.Count == 0 || !input.devices[0].added))
                {
                    input.user.UnpairDevices();
                    input.SwitchCurrentControlScheme("Gamepad", gamepad);

                    if (player.TryGetComponent<PlayerController>(out var controller))
                    {
                        controller.IsUsingGamepad = true;
                    }

                    if (!player.TryGetComponent<ControllerRumbler>(out var rumbler))
                    {
                        rumbler = player.gameObject.AddComponent<ControllerRumbler>();
                    }
                    rumbler.SetController(gamepad);
                    break;
                }
            }
        }
    }


    public void StartPlayerJoining()
    {
      Invoke(nameof(StartPlayerJoiningMethod), .1f);
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
                bool isDeviceAvailable = device != null && device.added;

                if (device is Keyboard)
                {
                    PlayerInput newPlayer = PlayerInputManager.instance.JoinPlayer(
                        playerDevice.PlayerIndex,
                        -1,
                        "Keyboard",
                        isDeviceAvailable ? device : null
                    );
                }
                else if (device is Gamepad || !isDeviceAvailable)
                {
                    PlayerInput newPlayer = PlayerInputManager.instance.JoinPlayer(
                        playerDevice.PlayerIndex,
                        -1,
                        null,
                        isDeviceAvailable ? device : null
                    );

                    if (newPlayer != null && isDeviceAvailable)
                    {
                        newPlayer.SwitchCurrentControlScheme(device);
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
                bool isDeviceAvailable = device != null && device.added;
                if (device is Keyboard)
                {
                    PlayerInput newPlayer = PlayerInputManager.instance.JoinPlayer(
                        playerDevice.PlayerIndex,
                        -1,
                        "Keyboard",
                        isDeviceAvailable ? device : null
                    );
                }
                else if (device is Gamepad || !isDeviceAvailable)
                {
                    PlayerInput newPlayer = PlayerInputManager.instance.JoinPlayer(
                        playerDevice.PlayerIndex,
                        -1,
                        null,
                        isDeviceAvailable ? device : null
                    );

                    if (newPlayer != null && isDeviceAvailable)
                    {
                        newPlayer.SwitchCurrentControlScheme(device);
                    }
                }

                if (LobbyManager.instance.selectedLoadoutType == LoadoutSelection.LoadOutType.IndividualRandom)
                    RerollSpells();
            }
        }
        ItemSpawner.Instance.InitialSpawn();
        playerInputManager.enabled = false;
        StartPlayerEntrance();
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
        
        playerController.SetUpPlayer(playerID, playerHUDs[playerID], rumbler, LobbyPlayerValues.Instance.playerValuesList[playerID].Skin, dropInJoin);
        if (!isTutorial)
            playerController.SetSpells(syncedFirstSpellIndex.Value, syncedSecondSpellIndex.Value);

        characterController.enabled = true;

        ItemSpawner.Instance.ChangeMaxItemAmount(true);
        GameManager.Instance.AddPlayer(playerID, playerController, playerHUDs[playerID], LobbyPlayerValues.Instance.playerValuesList[playerID].TeamIndex);
        GameManager.Instance.ChangePlayerStateLocal(playerID, PlayerState.alive);
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
        
        playerController.SetUpPlayer(playerID, playerHUDs[playerID], rumbler, LobbyPlayerValues.Instance.playerValuesList[playerID].Skin, dropInJoin);
        if (!isTutorial)
            playerController.SetSpells(syncedFirstSpellIndex.Value, syncedSecondSpellIndex.Value);

        characterController.enabled = true;

        ItemSpawner.Instance.ChangeMaxItemAmount(true);
        GameManager.Instance.AddPlayer(playerID, playerController, playerHUDs[playerID], LobbyPlayerValues.Instance.playerValuesList[playerID].TeamIndex);
        GameManager.Instance.ChangePlayerStateServerRpc(playerID, PlayerState.alive);
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
        bool isFullVersion = true;

        if (SteamIntegration.instance)
            isFullVersion = SteamIntegration.instance.IsFullVersion;

        for (int i = 0; i < ItemSpawner.Instance.GetSpellCount(); i++)
        {
            var item = ItemSpawner.Instance.SpawnableItems[i];

            if (item.CanUse && (isFullVersion || item.AvailableInDemo))
            {
                legalSpells.Add(i);
            }
        }

        syncedFirstSpellIndex.Value = legalSpells[UnityEngine.Random.Range(0, legalSpells.Count)];
        syncedSecondSpellIndex.Value = legalSpells[UnityEngine.Random.Range(0, legalSpells.Count)];
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
                ScoreManager.Instance.InitialiseScorePanel(playerID, LobbyPlayerValues.Instance.playerValuesList[playerID].Skin.HeadSprites[0], LobbyPlayerValues.Instance.playerValuesList[playerID].Skin.Color);
            else if (GameManager.Instance.GameMode == GameManager.GameModeType.Team)
                ScoreManager.Instance.InitialiseTeamScorePanel(GameManager.Instance.TeamIDs[playerID], playerID);
        }
        else
        {
            Debug.Log("PlayerManager: THERE IS NO LOBBY MANAGER! DEATH TO ALL");
        }
    }

    public void ResetPlayerPosition(int playerID)
    {
        if (playerID < 0 || playerID >= spawnPoints.Length) return;

        Vector3 targetPos = spawnPoints[playerID].position;
        Quaternion targetRot = spawnPoints[playerID].rotation;

        if (GameManager.Instance.PlayingLocal)
        {
            if (playerID < localPlayers.Count && localPlayers[playerID] != null)
            {
                var localPlayer = localPlayers[playerID];
                if (localPlayer.TryGetComponent<PlayerController>(out var playerController))
                {
                    playerController.Teleport(targetPos, targetRot);
                }
                else
                {
                    localPlayer.transform.SetPositionAndRotation(targetPos, targetRot);
                }
            }
        }
        else
        {
            if (playerID >= players.Count) return;

            if (players[playerID].TryGet(out NetworkObject networkObject))
            {
                var playerGameObject = networkObject.gameObject;

                if (playerGameObject.TryGetComponent<PlayerController>(out var playerController))
                {
                    playerController.Teleport(targetPos, targetRot);
                }
                else
                {
                    playerGameObject.transform.SetPositionAndRotation(targetPos, targetRot);
                }

                TargetGroupManager.Instance?.AddToGroup(playerGameObject.transform);
            }
        }
    }

    public List<PlayerController> GetPlayers()
    {
        List<PlayerController> playerControllers = new List<PlayerController>();
        if (localPlayers.Count > 0)
        {
            foreach (GameObject p in localPlayers)
            {
                playerControllers.Add(p.GetComponent<PlayerController>());
            }
        }
        else
        {
            foreach (NetworkObjectReference p in players)
            {
                if (p.TryGet(out NetworkObject networkObject))
                {
                    playerControllers.Add(networkObject.GetComponent<PlayerController>());
                }
            }
        }
        return playerControllers;
    }

    public void StartPlayerEntrance()
    {
        StartCoroutine(PlayerEntrance(GetPlayers()));
    }

    private IEnumerator PlayerEntrance(List<PlayerController> playerControllers)
    {
        float remainingTime = .6f * 3f; // Time between Countdown Elements * Countdown Count
        float timeBetweenEntrance = remainingTime / playerControllers.Count;
        foreach (PlayerController player in playerControllers)
        {
            yield return new WaitForSeconds(timeBetweenEntrance * .5f);
            player.StartEntrence(false);
            yield return new WaitForSeconds(timeBetweenEntrance * .5f);
        }
    }

    public void EnablePlayerInput()
    {
        List<PlayerController> players = GetPlayers();
        foreach (PlayerController player in players)
        {
            player.ToggleInput(true);
        }
    }
    #endregion
}