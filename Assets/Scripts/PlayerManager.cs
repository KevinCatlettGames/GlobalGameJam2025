using System;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using FMODUnity;
using TMPro;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class PlayerManager : MonoBehaviour
{
    
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
        playerPanelParent[input.playerIndex].SetActive(true);
        input.GetComponent<CharacterController>().enabled = false;
        input.transform.position = spawnPoints[input.playerIndex].position;
        input.GetComponent<PlayerStateHandler>().spawnPosition = spawnPoints[input.playerIndex].position;
        players.Add(input.gameObject);
        playerPortraits[input.playerIndex].sprite = playerSprites[input.playerIndex];
        input.GetComponent<PlayerController>().firstCoolDownCover = firstCoverImage[input.playerIndex];
        input.GetComponent<PlayerController>().secondCoolDownCover = secondCoolDownCover[input.playerIndex];
        input.GetComponent<PlayerController>().firstCoolDownImage = firstCoolDownImage[input.playerIndex];
        input.GetComponent<PlayerController>().secondCoolDownImage = secondCoolDownImage[input.playerIndex];
        input.GetComponent<PlayerController>().characters[input.playerIndex].SetActive(true);
        input.GetComponent<PlayerController>().mainAnimator = input.GetComponent<PlayerController>().characters[input.playerIndex].GetComponent<Animator>();
        playerUIBoxes[input.playerIndex].color = colors[input.playerIndex];
        
        input.GetComponent<PlayerController>().damageText = damageTexts[input.playerIndex];
        // input.GetComponent<MeshRenderer>().material = colorMaterials[input.playerIndex];
       
        firstCoverImage[input.playerIndex].SetActive(true);
        secondCoolDownCover[input.playerIndex].SetActive(true);

        input.GetComponent<CharacterController>().enabled = true;
        activePlayers++;
    }


    void ResetPlayers()
    {
        foreach (Image sprite in firstCoolDownImage)
        {
            sprite.sprite = baseSpellSprite;
        }
        foreach (Image sprite in secondCoolDownImage)
        {
            sprite.sprite = baseSpellSprite;
        }
        
        foreach (GameObject player in players)
        {
            activePlayers++;
            
            player.GetComponent<CharacterController>().enabled = false;
            
           // player.GetComponent<MeshRenderer>().enabled = true;
            
            player.GetComponent<PlayerStateHandler>().Reset();
        }
    }

    public void ReducePlayers()
    {
        activePlayers--;
        if (activePlayers <= 1)
        {
            activePlayers = 0; 
            OnPlayerWon?.Invoke();
            GameManager.Instance.EndGame();
        }
    }
}