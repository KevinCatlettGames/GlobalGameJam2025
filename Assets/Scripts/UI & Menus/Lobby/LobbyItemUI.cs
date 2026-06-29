using UnityEngine;
using Unity.Services.Lobbies.Models;
using TMPro; 
using UnityEngine.UI;

/// <summary>
/// Handles the UI representation of a single lobby in the lobby list.
/// Updates display with lobby name and player count and handles joining the lobby.
/// </summary>
public class LobbyItemUI : MonoBehaviour
{
    /// <summary>
    /// Text element to display the lobby name and player count.
    /// </summary>
    public TextMeshProUGUI lobbyNameText;

    /// <summary>
    /// Button that triggers joining this lobby.
    /// </summary>
    public Button button; 

    /// <summary>
    /// The lobby data this UI element represents.
    /// </summary>
    private Lobby lobbyData;

    /// <summary>
    /// Sets up the UI element with the provided lobby data.
    /// </summary>
    /// <param name="lobby">Lobby data to display.</param>
    public void Setup(Lobby lobby)
    {
        lobbyData = lobby;
        if (lobbyNameText != null)
            lobbyNameText.text = lobbyData.Name + " " + lobby.Players.Count + "/" + lobby.MaxPlayers;
    }

    /// <summary>
    /// Joins the lobby represented by this UI element.
    /// </summary>
    private void JoinLobby()
    {
        if (!string.IsNullOrEmpty(lobbyData.Id))
            GameLobby.instance.JoinWithId(lobbyData.Id);
    }

    /// <summary>
    /// Called by the button click to join the lobby.
    /// </summary>
    public void OnJoinLobbyButton()
    {
        JoinLobby();
    }
}