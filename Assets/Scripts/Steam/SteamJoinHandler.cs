using Steamworks;
using UnityEngine;

public class SteamJoinHandler : MonoBehaviour
{
    public static SteamJoinHandler instance;
    public GameObject onlineUI;

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
        onlineUI.SetActive(true);
        GameLobby.instance.JoinWithCode(connectionData);
    }
}