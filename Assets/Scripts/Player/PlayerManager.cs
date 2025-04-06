using System;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using FMOD.Studio;
using FMODUnity;
using TMPro;
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
    public Transform[] spawnPoints; // Array of spawn points
    public int activePlayers = 0;
    public List<GameObject> players;

    public Sprite[] playerSprites;
    public Image[] playerPortraits;
    public Image[] playerUIBoxes;
    public Color[] colors;

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
        playerHUDs[input.playerIndex].gameObject.SetActive(true);
        input.GetComponent<CharacterController>().enabled = false;
        input.transform.position = spawnPoints[input.playerIndex].position;
        input.GetComponent<PlayerStateHandler>().spawnPosition = spawnPoints[input.playerIndex].position;
        input.GetComponent<PlayerStateHandler>().aimIndicator.color = colors[input.playerIndex];
        players.Add(input.gameObject);
        PlayerController playerController = input.GetComponent<PlayerController>();
        Gamepad gamePad = input.GetDevice<Gamepad>();
        ControllerRumbler rumbler = null;
        if (gamePad != null)
        {
            rumbler = input.AddComponent<ControllerRumbler>();
            rumbler.SetController(gamePad);
        }
        playerController.SetUpPlayer(input.playerIndex, playerHUDs[input.playerIndex], rumbler);
        playerController.SetSpells(startingSpells[firstSpellIndex], startingSpells[secondSpellIndex]);
        playerPortraits[input.playerIndex].sprite = playerSprites[input.playerIndex];
        playerUIBoxes[input.playerIndex].color = colors[input.playerIndex];
        input.GetComponent<CharacterController>().enabled = true;
        activePlayers++;
        ItemSpawner.Instance.ChangeMaxItemAmount(true);
    }

    private void ResetPlayers()
    {
        RerollSpells();

        foreach (GameObject player in players)
        {
            activePlayers++;

            player.GetComponent<CharacterController>().enabled = false;

            player.GetComponent<PlayerStateHandler>().Reset();

            player.GetComponent<PlayerController>().SetSpells(startingSpells[firstSpellIndex], startingSpells[secondSpellIndex]);
        }
    }

    private void RerollSpells()
    {
        firstSpellIndex = UnityEngine.Random.Range(0, startingSpells.Length);
        secondSpellIndex = UnityEngine.Random.Range(0,startingSpells.Length);
    }

    public void ReducePlayers()
    {
        activePlayers--;
        if (activePlayers <= 1)
        {
            RuntimeManager.PlayOneShotAttached(winSound, transform.gameObject);
            activePlayers = 0;
            OnPlayerWon?.Invoke();
            GameManager.Instance.EndGame();
        }
    }

    public void AddScore(int playerID)
    {
        playerHUDs[playerID].AddScore();
    }
}