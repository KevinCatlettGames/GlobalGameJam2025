using System;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using FMODUnity;
using UnityEngine.UI;
using Unity.VisualScripting;

public class PlayerManager : MonoBehaviour
{
    [SerializeField] private EventReference winSound;
    [SerializeField] private PlayerHUD[] playerHUDs;
    [SerializeField] private SO_Spell[] startingSpells;
    private int firstSpellIndex = 0;
    private int secondSpellIndex = 0;

    public static PlayerManager Instance;

    [SerializeField] private Transform[] spawnPoints; // Array of spawn points
    public List<GameObject> players;

    [SerializeField] private Sprite[] playerSprites;
    [SerializeField] private Color[] colors;

    public Action OnPlayerWon;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
        }
        players = new List<GameObject>();
        RerollSpells();
    }

    private void Start()
    {
        GameManager.Instance.OnGameStarted += ResetPlayers;
    }

    public void OnPlayerJoined(PlayerInput input)
    {
        int playerID = input.playerIndex;
        playerHUDs[playerID].gameObject.SetActive(true);
        playerHUDs[playerID].InitialisePlayerHUD(colors[playerID], playerSprites[playerID]);
        input.GetComponent<CharacterController>().enabled = false;
        input.transform.position = spawnPoints[playerID].position;
        players.Add(input.gameObject);
        PlayerController playerController = input.GetComponent<PlayerController>();
        Gamepad gamePad = input.GetDevice<Gamepad>();
        ControllerRumbler rumbler = null;
        if (gamePad != null)
        {
            rumbler = input.AddComponent<ControllerRumbler>();
            rumbler.SetController(gamePad);
        }
        playerController.SetUpPlayer(playerID, playerHUDs[playerID], rumbler, colors[playerID]);
        playerController.SetSpells(startingSpells[firstSpellIndex], startingSpells[secondSpellIndex]);
        input.GetComponent<CharacterController>().enabled = true;
        ItemSpawner.Instance.ChangeMaxItemAmount(true);
        GameManager.Instance.AddPlayer(playerID, playerController, playerHUDs[playerID]);
        GameManager.Instance.ChangePlayerState(playerID, PlayerState.alive);
    }

    private void ResetPlayers()
    {
        RerollSpells();

        foreach (GameObject player in players)
        {
            player.GetComponent<PlayerStateHandler>().ResetPlayer();

            player.GetComponent<PlayerController>().SetSpells(startingSpells[firstSpellIndex], startingSpells[secondSpellIndex]);
        }
    }

    private void RerollSpells()
    {
        firstSpellIndex = UnityEngine.Random.Range(0, startingSpells.Length);
        secondSpellIndex = UnityEngine.Random.Range(0,startingSpells.Length);
    }

    public void ResetPlayerPosition(int playerID)
    {
        players[playerID].transform.position = spawnPoints[playerID].transform.position;
    }
}