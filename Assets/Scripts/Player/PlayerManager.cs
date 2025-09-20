using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using UnityEngine.UI;
using FMODUnity;
using Unity.VisualScripting;
using UnityEngine.Animations;
using UnityEngine.InputSystem.Users;

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
        Invoke(nameof(EnableJoinText), .3f);
    }

    private void OnDisable()
    {
        startGameInputAction.action.performed -= ActionOnPerformed;
        startGameInputAction.action.Disable();
    }

    private void EnableJoinText()
    {
        GameManager.Instance.OnGameStarted += ResetPlayers;
        RerollSpells();
        if (GameManager.Instance.PlayingLocal)
        {
            if (!TransportSwitcher.Instance)
            {
                //Debug.Log("Enabling join text");
                joinGameText.SetActive(true);
                startGameInputAction.action.performed += ActionOnPerformed;
                startGameInputAction.action.Enable();
            }

            playerInputManager.enabled = true;
            
            // if (TransportSwitcher.Instance)
            // {
            //     playerInputManager.joinBehavior = PlayerJoinBehavior.JoinPlayersManually;
            // }
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
        //Debug.Log("Spawning");
        startGameInputAction.action.performed -= ActionOnPerformed;
        startGameInputAction.action.Disable();
        
        if (LobbyPlayerHandler.Instance != null)
        {
            LobbyPlayerHandler lobbyPlayerHandler = LobbyPlayerHandler.Instance;

            foreach (LobbyPlayerHandler.PlayerValues playerDevice in lobbyPlayerHandler.playerValues)
            {
                if (playerDevice == null) continue; // use continue, not return

                InputDevice device = playerDevice.Device;

                if (device is Keyboard)
                {
                    // Join player manually with the correct device
                    PlayerInput newPlayer = PlayerInputManager.instance.JoinPlayer(
                        playerDevice.PlayerIndex, // player index (0,1,2,3)
                        -1, // split-screen index (optional)
                        "Keyboard", // control scheme (optional, null = default)
                        playerDevice.Device // assign this device
                    );
                    //Debug.Log($"Joined {newPlayer.playerIndex} with scheme {newPlayer.currentControlScheme}, devices: {string.Join(",", newPlayer.devices)}");
                }
                else if (device is Gamepad)
                {
                    // Join player manually with the correct device
                    PlayerInput newPlayer = PlayerInputManager.instance.JoinPlayer(
                        playerDevice.PlayerIndex, // player index (0,1,2,3)
                        -1, // split-screen index (optional)
                        null, // control scheme (optional, null = default)
                        playerDevice.Device // assign this device
                    );

                    if (newPlayer != null)
                    {
                        // Optional: switch control scheme explicitly
                        newPlayer.SwitchCurrentControlScheme(playerDevice.Device);
                        //Debug.Log($"Joined {newPlayer.playerIndex} with scheme {newPlayer.currentControlScheme}, devices: {string.Join(",", newPlayer.devices)}");
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
                if (playerDevice == null) continue; // use continue, not return

                InputDevice device = playerDevice.Device;

                if (device is Keyboard)
                {
                    // Join player manually with the correct device
                    PlayerInput newPlayer = PlayerInputManager.instance.JoinPlayer(
                        playerDevice.PlayerIndex, // player index (0,1,2,3)
                        -1, // split-screen index (optional)
                        "Keyboard", // control scheme (optional, null = default)
                        playerDevice.Device // assign this device
                    );
                    //Debug.Log($"Joined {newPlayer.playerIndex} with scheme {newPlayer.currentControlScheme}, devices: {string.Join(",", newPlayer.devices)}");
                }
                else if (device is Gamepad)
                {
                    // Join player manually with the correct device
                    PlayerInput newPlayer = PlayerInputManager.instance.JoinPlayer(
                        playerDevice.PlayerIndex, // player index (0,1,2,3)
                        -1, // split-screen index (optional)
                        null, // control scheme (optional, null = default)
                        playerDevice.Device // assign this device
                    );

                    if (newPlayer != null)
                    {
                        // Optional: switch control scheme explicitly
                        newPlayer.SwitchCurrentControlScheme(playerDevice.Device);
                        //Debug.Log($"Joined {newPlayer.playerIndex} with scheme {newPlayer.currentControlScheme}, devices: {string.Join(",", newPlayer.devices)}");
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
        //Debug.Log("JoinLocal");
        int playerID = playersInitializedCount++;
        if (!ValidatePlayerID(playerID)) return;

        SetupPlayerHUD(playerID);

        // Disable character controller before moving to spawn position
        characterController.enabled = false;

        input.transform.position = spawnPoints[playerID].position;
        input.transform.rotation = spawnPoints[playerID].rotation;
        
        var playerController = input.GetComponent<PlayerController>();
        playerController.InitializeLocal();

        // Setup controller rumbler if gamepad exists
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

        //Debug.Log("OnPlayerJoined");
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
        //Debug.Log($"Initializing players: Count = {players.Count}");
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
                else
                {
                    //Debug.LogWarning("Failed to resolve player reference.");
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
            //Debug.LogError("Too many players for available HUDs or spawn points!");
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
            else
            {
                //Debug.LogError("Invalid playerID for ResetPlayerPosition (local).");
            }
        }
        else
        {
            if (playerID >= players.Count)
            {
                //Debug.LogError("Invalid playerID for ResetPlayerPosition (networked).");
                return;
            }

            if (players[playerID].TryGet(out NetworkObject networkObject))
            {
                var playerGameObject = networkObject.gameObject;
                playerGameObject.transform.position = spawnPoints[playerID].position;
                playerGameObject.transform.rotation = spawnPoints[playerID].rotation;
            }
            else
            {
                //Debug.LogError("Failed to resolve NetworkObjectReference for resetting position!");
            }
        }
    }

    #endregion
}
