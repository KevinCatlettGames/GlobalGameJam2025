using System;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using FMODUnity;
using Unity.Netcode;
using UnityEngine.UI;
using Unity.VisualScripting;
using System.Collections; 

public class PlayerManager : NetworkBehaviour
{
    [SerializeField] private EventReference winSound;
    [SerializeField] private PlayerHUD[] playerHUDs;
    [SerializeField] private SO_Spell[] startingSpells;
    private NetworkVariable<int> syncedFirstSpellIndex = new NetworkVariable<int>();
    private NetworkVariable<int> syncedSecondSpellIndex = new NetworkVariable<int>();


    public static PlayerManager Instance;

    [SerializeField] private Transform[] spawnPoints; // Array of spawn points
    public NetworkList<NetworkObjectReference> players = new NetworkList<NetworkObjectReference>();
    [SerializeField] private Sprite[] playerSprites;
    [SerializeField] private Color[] colors;
    public Button startGameButton; 
    public Action OnPlayerWon;
    private int playersInitializedCount = 0; 
    
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
    }
    
    private void Start()
    {
        GameManager.Instance.OnGameStarted += ResetPlayers;
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void AddPlayerServerRpc(NetworkObjectReference input)
    {
        players.Add(input);
    }
    
    public void Initialize()
    {
        Debug.Log(players.Count);
        foreach (NetworkObjectReference playerRef in players)
        {
            if (playerRef.TryGet(out NetworkObject networkObject))
            {
                networkObject.GetComponent<PlayerController>().InitializeClientRpc();
            }
        }
    }
    
    public void OnPlayerJoined(PlayerInput input)
    {
        int playerID = playersInitializedCount;
        playersInitializedCount++;
        if (playerID >= playerHUDs.Length || playerID >= spawnPoints.Length)
        {
            Debug.LogError("Too many players for available HUDs or spawn points!");
            return;
        }

        playerHUDs[playerID].gameObject.SetActive(true);
        playerHUDs[playerID].InitialisePlayerHUD(colors[playerID], playerSprites[playerID]);
        
        input.GetComponent<CharacterController>().enabled = false;
        input.transform.position = spawnPoints[playerID].position;
        
        PlayerController playerController = input.GetComponent<PlayerController>();
        Gamepad gamePad = input.GetDevice<Gamepad>();
        ControllerRumbler rumbler = null;

         if (gamePad != null)
         {
             rumbler = input.AddComponent<ControllerRumbler>();
             rumbler.SetController(gamePad);
         }

        playerController.SetUpPlayer(playerID, playerHUDs[playerID], rumbler, colors[playerID]);
        RerollSpells();
        playerController.SetSpells(startingSpells[syncedFirstSpellIndex.Value], startingSpells[syncedSecondSpellIndex.Value]);
        
        input.GetComponent<CharacterController>().enabled = true;
        
        ItemSpawner.Instance.ChangeMaxItemAmount(true);
        GameManager.Instance.AddPlayer(playerID, playerController, playerHUDs[playerID]);
        GameManager.Instance.ChangePlayerStateServerRpc(playerID, PlayerState.alive);
    }

    private void ResetPlayers()
    {
        foreach (var playerRef in players)
        {
            RerollSpells();
            
            if (playerRef.TryGet(out NetworkObject networkObject))
            {
                GameObject player = networkObject.gameObject;
                player.GetComponent<PlayerStateHandler>().ResetPlayer();
                player.GetComponent<PlayerController>().SetSpells(startingSpells[syncedFirstSpellIndex.Value], startingSpells[syncedSecondSpellIndex.Value]);
                player.GetComponent<PlayerController>().mainAnimator.SetBool("IsDead", false);
                player.GetComponent<PlayerController>().mainAnimator.SetBool("Victory", false);
            }
            else
            {
                Debug.LogWarning("Failed to resolve player reference.");
            }
        }
    }

    private void RerollSpells()
    {
        if (!IsServer) return;

        syncedFirstSpellIndex.Value = UnityEngine.Random.Range(0, startingSpells.Length);
        syncedSecondSpellIndex.Value = UnityEngine.Random.Range(0, startingSpells.Length);
    }

    public void ResetPlayerPosition(int playerID)
    {
        if (playerID >= players.Count)
        {
            Debug.LogError("Invalid playerID for ResetPlayerPosition.");
            return;
        }

        if (players[playerID].TryGet(out NetworkObject networkObject))
        {
            GameObject playerGameObject = networkObject.gameObject;
            playerGameObject.transform.position = spawnPoints[playerID].transform.position;
        }
        else
        {
            Debug.LogError("Failed to resolve NetworkObjectReference for resetting position!");
        }
    }
}
