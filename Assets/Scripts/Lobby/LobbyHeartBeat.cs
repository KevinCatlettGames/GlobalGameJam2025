using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class LobbyHeartBeat : MonoBehaviour
{
    public Steamworks.Data.Lobby joinedLobby;
    float heartbeatTimer;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
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
