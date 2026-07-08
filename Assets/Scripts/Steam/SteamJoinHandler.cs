using Steamworks;
using UnityEngine;

public class SteamJoinHandler : MonoBehaviour
{
#if !UNITY_SWITCH
    public static SteamJoinHandler instance;
    public GameObject[] objectsToDisable;
    public GameObject[] objectsToEnable;

    private void Awake()
    {
        if (instance == null)
            instance = this; 

        ClearRichPresence();
    }

    void OnEnable()
    {
        SteamFriends.OnGameRichPresenceJoinRequested += OnJoinRequested;
    }

    void OnDisable()
    {
        SteamFriends.OnGameRichPresenceJoinRequested -= OnJoinRequested;
    }

    /// <summary>
    /// Call this method right after the local player hosts or joins a multiplayer lobby.
    /// </summary>
    /// <param name="lobbyId">The Steam ID of your lobby/room</param>
    public void SetPlayerReadyToBeJoined(string lobbyId)
    {
        Debug.Log("In method");
        if (!SteamIntegration.instance.SteamInitialized) return;

        // 1. Tell Steam how a friend should connect to this player
        // The key MUST be "connect" for Steam's overlay to recognize it
        SteamFriends.SetRichPresence("connect", lobbyId);

        // 2. (Optional but recommended) Group players together in the UI
        // This activates the "Invite to Game" button in the overlay
        SteamFriends.SetRichPresence("status", "Playing with friends");
    }

    /// <summary>
    /// Call this when the player leaves the lobby to disable joining.
    /// </summary>
    public void ClearRichPresence()
    {
        if (!SteamIntegration.instance.SteamInitialized) return;

        SteamFriends.ClearRichPresence();
    }

    private void OnJoinRequested(Friend friend, string connectString)
    {
        Debug.Log($"Join requested by friend: {friend.Name}");
        Debug.Log($"Connection data passed by Steam: {connectString}");
        ConnectToLobby(connectString);
    }

    private void ConnectToLobby(string connectionData)
    {
        Debug.Log(connectionData);

        foreach (GameObject obj in objectsToEnable)
        {
            if (obj == null) continue;
            obj.SetActive(true);
        }

        foreach (GameObject obj in objectsToDisable)
        {
            if (obj == null) continue;
            obj.SetActive(false);
        }

        GameLobby.instance.JoinWithCode(connectionData);
    }
#endif
}