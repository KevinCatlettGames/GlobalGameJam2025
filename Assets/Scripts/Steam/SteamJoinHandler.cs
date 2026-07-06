using Steamworks;
using UnityEngine;

public class SteamJoinHandler : MonoBehaviour
{
    void OnEnable()
    {
        SteamFriends.OnGameRichPresenceJoinRequested += OnJoinRequested;
    }

    void OnDisable()
    {
        SteamFriends.OnGameRichPresenceJoinRequested -= OnJoinRequested;
    }

    private void OnJoinRequested(Friend friend, string connectString)
    {
        Debug.Log($"Join requested by friend: {friend.Name}");
        Debug.Log($"Connection data passed by Steam: {connectString}");
        ConnectToLobby(connectString);
    }

    private void ConnectToLobby(string connectionData)
    {
        // Your actual transport/multiplayer connection logic goes here
    }
}