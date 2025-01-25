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
    private int spawnPointsUsed = -1;
    public int activePlayers = 0; 
    public List<GameObject> players;
    
    public GameObject[] firstPlayerCooldownSliders;
    public GameObject[] secondPlayerCooldownSliders;
    public Image[] firstSliderImages;
    public Image[] secondSliderImages;
    public Material[] colorMaterials;
    public TextMeshProUGUI[] damageTexts; 
    
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

    public Vector3 AddPlayer(GameObject player)
    {
        activePlayers++; 
        players.Add(player);
        player.GetComponent<PlayerController>().firstCooldownSlider = firstPlayerCooldownSliders[activePlayers - 1];
        player.GetComponent<PlayerController>().secondCooldownSlider = secondPlayerCooldownSliders[activePlayers - 1];
        player.GetComponent<PlayerController>().damageText = damageTexts[activePlayers - 1];
        player.GetComponent<MeshRenderer>().material = colorMaterials[activePlayers - 1];
        firstSliderImages[activePlayers - 1].color = colors[activePlayers - 1];
        secondSliderImages[activePlayers - 1].color = colors[activePlayers - 1];
        firstPlayerCooldownSliders[activePlayers - 1].SetActive(true);
        secondPlayerCooldownSliders[activePlayers - 1].SetActive(true);
        spawnPointsUsed++; 
        return spawnPoints[spawnPointsUsed].position;
    }

    void ResetPlayers()
    {
        foreach (GameObject player in players)
        {
            activePlayers++;
            
            player.GetComponent<CharacterController>().enabled = false;
            
            player.GetComponent<MeshRenderer>().enabled = true;
            
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