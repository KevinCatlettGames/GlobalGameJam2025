using System;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance; 
    public Transform[] spawnPoints; // Array of spawn points
    private int spawnPointsUsed = -1;
    public int activePlayers = 0; 
    public List<GameObject> players;

    public Action OnPlayerWon; 
    
    void Awake()
    {
        players = new List<GameObject>();
        Instance = this; 
    }

    public Vector3 GetNonUsedStartPosition(GameObject player)
    {
        activePlayers++; 
        players.Add(player);
        spawnPointsUsed++; 
        return spawnPoints[spawnPointsUsed].position;
    }

    public void ResetPlayers()
    {
        OnPlayerWon?.Invoke();
        
        foreach (GameObject player in players)
        {
            activePlayers++;
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
            Invoke(nameof(ResetPlayers), 1f);
        }
    }
}