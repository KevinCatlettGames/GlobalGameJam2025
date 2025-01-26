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
        playerUIBoxes[input.playerIndex].color = colors[input.playerIndex];
        
        SkinnedMeshRenderer meshRenderer = input.GetComponent<PlayerStateHandler>().meshRenderer;

        // Get the current materials array
        Material[] materials = meshRenderer.materials;

        // Check if the index is valid to avoid runtime errors
        if (materials != null && materials.Length > 2)
        {
            // Replace the material at index 2
            materials[2] = colorMaterials[input.playerIndex];
    
            // Assign the updated array back to the MeshRenderer
            meshRenderer.materials = materials;
        }
        
        input.GetComponent<PlayerController>().damageText = damageTexts[input.playerIndex];
        // input.GetComponent<MeshRenderer>().material = colorMaterials[input.playerIndex];
       
        firstCoverImage[input.playerIndex].SetActive(true);
        secondCoolDownCover[input.playerIndex].SetActive(true);

        input.GetComponent<CharacterController>().enabled = true;
        activePlayers++;
    }


    void ResetPlayers()
    {
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