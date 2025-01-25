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
    
    [Header("Sound Events")]
    [SerializeField] protected EventReference castEventStruct;
    
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
        RuntimeManager.PlayOneShotAttached(castEventStruct, input.gameObject);
        
        input.GetComponent<CharacterController>().enabled = false;

        input.transform.position = spawnPoints[input.playerIndex].position;
        input.GetComponent<PlayerStateHandler>().spawnPosition = spawnPoints[input.playerIndex].position;
        players.Add(input.gameObject);
        input.GetComponent<PlayerController>().firstCooldownSlider = firstPlayerCooldownSliders[input.playerIndex];
        input.GetComponent<PlayerController>().secondCooldownSlider = secondPlayerCooldownSliders[input.playerIndex];
        input.GetComponent<PlayerController>().damageText = damageTexts[input.playerIndex];
        // input.GetComponent<MeshRenderer>().material = colorMaterials[input.playerIndex];
        firstSliderImages[input.playerIndex].color = colors[input.playerIndex];
        secondSliderImages[input.playerIndex].color = colors[input.playerIndex];
        firstPlayerCooldownSliders[input.playerIndex].SetActive(true);
        secondPlayerCooldownSliders[input.playerIndex].SetActive(true);

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
             RuntimeManager.PlayOneShotAttached(castEventStruct, player.gameObject);
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