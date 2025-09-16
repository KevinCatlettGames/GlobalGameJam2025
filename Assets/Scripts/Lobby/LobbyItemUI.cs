using UnityEngine;
using Unity.Services.Lobbies.Models;
using TMPro; 
using UnityEngine.UI; 

public class LobbyItemUI : MonoBehaviour
{
    public TextMeshProUGUI lobbyNameText;
    public Button button; 
    private Lobby lobbyData;
    public void Setup(Lobby lobby)
    {
        lobbyData = lobby;
        if (lobbyNameText != null)
            lobbyNameText.text = lobby.Players.Count + "/" + lobby.MaxPlayers;
    }

    void JoinLobby()
    {
        if (!string.IsNullOrEmpty(lobbyData.Id))
            GameLobby.instance.JoinWithId(lobbyData.Id);
    }

    // Optional: button click handler
    public void OnJoinLobbyButton()
    {
        JoinLobby();
    }
}