using System;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using FMOD.Studio;
using FMODUnity;
using TMPro;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class PlayerManager : MonoBehaviour
{
    [SerializeField] private EventReference winSound;
    [SerializeField] private PlayerHUD[] playerHUDs;
    [SerializeField] private SO_Spell[] startingSpells;
    
    public static PlayerManager Instance; 
    public Transform[] spawnPoints; // Array of spawn points
    public int activePlayers = 0; 
    public List<GameObject> players;

    public GameObject[] playerPanelParent;
    public GameObject[] firstCoverImage;
    public GameObject[] secondCoolDownCover;
    public Image[] firstCoolDownImage;
    public Image[] secondCoolDownImage;
    public Material[] colorMaterials;
    public TextMeshProUGUI[] damageTexts;
    public Sprite[] playerSprites;
    public Image[] playerPortraits;
    public Image[] playerUIBoxes; 
    public Color[] colors;
    public Sprite baseSpellSprite; 
    
    public Action OnPlayerWon; 
    
    void Awake()
    {
        players = new List<GameObject>();
        Instance = this; 
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
        playerPortraits[input.playerIndex].sprite = playerSprites[input.playerIndex];
        PlayerController playerController = input.GetComponent<PlayerController>();
        playerController.characters[input.playerIndex].SetActive(true);
        playerController.mainAnimator = input.GetComponent<PlayerController>().characters[input.playerIndex].GetComponent<Animator>();
        playerController.SetPlayerHUD(playerHUDs[input.playerIndex]);
        playerUIBoxes[input.playerIndex].color = colors[input.playerIndex];
        

        // input.GetComponent<MeshRenderer>().material = colorMaterials[input.playerIndex];
       
        firstCoverImage[input.playerIndex].SetActive(true);
        secondCoolDownCover[input.playerIndex].SetActive(true);

        input.GetComponent<CharacterController>().enabled = true;
        activePlayers++;
        ItemSpawner.Instance.ChangeMaxItemAmount(true);
    }


    void ResetPlayers()
    {
        SO_Spell firstSpell;
        SO_Spell secondSpell;
        int r = UnityEngine.Random.Range(0, startingSpells.Length);
        firstSpell = startingSpells[r];
        r = UnityEngine.Random.Range(0, startingSpells.Length);
        secondSpell = startingSpells[r];

        foreach (GameObject player in players)
        {
            activePlayers++;
            
            player.GetComponent<CharacterController>().enabled = false;
            
            player.GetComponent<PlayerStateHandler>().Reset();

            player.GetComponent<PlayerController>().SetSpells(firstSpell, secondSpell);
        }
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
}