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
    [SerializeField] private SO_Spell[] startingSpells;
    [SerializeField] private Transform[] spawnPoints;

    [Header("UI")]
    public GameObject joinGameText;
    
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
        RerollSpells();
        if (GameManager.Instance.PlayingLocal)
        {
            if (!TransportSwitcher.Instance)
            {
                joinGameText.SetActive(true);
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
        
        if (LobbyPlayerHandler.Instance != null)
        {
            LobbyPlayerHandler lobbyPlayerHandler = LobbyPlayerHandler.Instance;

            foreach (LobbyPlayerHandler.PlayerValues playerDevice in lobbyPlayerHandler.playerValues)
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
        if (LobbyPlayerHandler.Instance != null)
        {
            LobbyPlayerHandler lobbyPlayerHandler = LobbyPlayerHandler.Instance;

            foreach (LobbyPlayerHandler.PlayerValues playerDevice in lobbyPlayerHandler.playerValues)
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
        ItemSpawner.Instance.InitialSpawn();
    }

    #region Player Joining and Initialization

    public void JoinLocal(PlayerInput input)
    {
        if (!input.TryGetComponent<CharacterController>(out var characterController)) return;

        joinGameText.SetActive(false);
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
        
        playerController.SetUpPlayer(playerID, playerHUDs[playerID], rumbler, LobbyPlayerHandler.Instance.playerValues[playerID].Skin.Color);
        playerController.SetSpells(startingSpells[syncedFirstSpellIndex.Value], startingSpells[syncedSecondSpellIndex.Value]);

        characterController.enabled = true;

        ItemSpawner.Instance.ChangeMaxItemAmount(true);
        GameManager.Instance.AddPlayer(playerID, playerController, playerHUDs[playerID]);
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
        
        playerController.SetUpPlayer(playerID, playerHUDs[playerID], rumbler, LobbyPlayerHandler.Instance.playerValues[playerID].Skin.Color);
        playerController.SetSpells(startingSpells[syncedFirstSpellIndex.Value], startingSpells[syncedSecondSpellIndex.Value]);

        characterController.enabled = true;

        ItemSpawner.Instance.ChangeMaxItemAmount(true);
        GameManager.Instance.AddPlayer(playerID, playerController, playerHUDs[playerID]);
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
            }
        }
        else
        {
            foreach (var playerRef in players)
            {
                if (playerRef.TryGet(out NetworkObject networkObject))
                {
                    ResetPlayerComponents(networkObject.gameObject);
                }
            }
        }
    }

    private void ResetPlayerComponents(GameObject player)
    {
        var stateHandler = player.GetComponent<PlayerStateHandler>();
        var controller = player.GetComponent<PlayerController>();

        stateHandler.ResetPlayer();

        controller.SetSpells(startingSpells[syncedFirstSpellIndex.Value], startingSpells[syncedSecondSpellIndex.Value]);

        var animator = controller.mainAnimator;
        animator.SetBool("IsDead", false);
        animator.SetBool("Victory", false);
    }

    private void RerollSpells()
    {
        if (!IsServer && !GameManager.Instance.PlayingLocal) return;

        syncedFirstSpellIndex.Value = UnityEngine.Random.Range(0, startingSpells.Length);
        syncedSecondSpellIndex.Value = UnityEngine.Random.Range(0, startingSpells.Length);
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

        if (LobbyPlayerHandler.Instance)
        {
            playerHUDs[playerID].InitialisePlayerHUD(LobbyPlayerHandler.Instance.playerValues[playerID].Skin.Color, LobbyPlayerHandler.Instance.playerValues[playerID].Skin.Sprite);
            ScoreManager.Instance.InitialiseScorePanel(playerID, LobbyPlayerHandler.Instance.playerValues[playerID].Skin.Sprite, LobbyPlayerHandler.Instance.playerValues[playerID].Skin.Color);
        }
        else
        {
            playerHUDs[playerID].InitialisePlayerHUD(LobbyPlayerHandler.Instance.playerValues[playerID].Skin.Color, LobbyPlayerHandler.Instance.playerValues[playerID].Skin.Sprite);
            ScoreManager.Instance.InitialiseScorePanel(playerID, LobbyPlayerHandler.Instance.playerValues[playerID].Skin.Sprite, LobbyPlayerHandler.Instance.playerValues[playerID].Skin.Color);
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
            }
        }
    }

    #endregion
}