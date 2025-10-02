using System;
using Unity.Netcode;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RelayServerHeartbeat : MonoBehaviour
{
    public Lobby joinedLobby;
    float heartbeatTimer;
    
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        
        SceneManager.sceneLoaded += SceneManagerOnsceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= SceneManagerOnsceneLoaded;
    }

    private void SceneManagerOnsceneLoaded(Scene arg0, LoadSceneMode arg1)
    {
       if(arg0.name == "MainMenu")
           Destroy(gameObject);
    }

    void Update()
    {
        if (NetworkManager.Singleton && NetworkManager.Singleton.IsServer)
        {
            heartbeatTimer -= Time.deltaTime;
            if (heartbeatTimer <= 0)
            {
                float heartBeatTimerMax = 15f; 
                heartbeatTimer = heartBeatTimerMax;
                LobbyService.Instance.SendHeartbeatPingAsync(joinedLobby.Id.ToString());
            }
        }
    }
}
