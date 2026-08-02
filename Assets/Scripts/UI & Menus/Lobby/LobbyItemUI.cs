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
            lobbyNameText.text = lobbyData.Name + " " + lobby.Players.Count + "/" + lobby.MaxPlayers;
    }

    private void JoinLobby()
    {
        if (!string.IsNullOrEmpty(lobbyData.Id))
            GameLobby.instance.JoinWithId(lobbyData.Id);
    }

    public void OnJoinLobbyButton()
    {
        JoinLobby();
    }
}