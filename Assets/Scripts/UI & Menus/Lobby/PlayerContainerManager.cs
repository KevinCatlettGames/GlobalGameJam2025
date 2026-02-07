using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages a single player UI container in the lobby, including ready state and associated visuals.
/// Updates the ready/unready sprite based on the player's readiness.
/// </summary>
public class PlayerContainerManager : MonoBehaviour
{
    [Header("Player Settings")]
    
     // The index of this container
    [SerializeField] private int uiIndex;

    /// <summary>
    /// Reference to the UI image component displaying the player's ready state.
    /// </summary>
    [SerializeField] private Image image;

    /// <summary>
    /// Sprite to display when the player is ready.
    /// </summary>
    [SerializeField] private Sprite readySprite;

    /// <summary>
    /// Sprite to display when the player is not ready.
    /// </summary>
    [SerializeField] private Sprite unreadySprite;

    /// <summary>
    /// Tracks whether the player is ready.
    /// </summary>
    public bool isReady = false;

    /// <summary>
    /// Unity OnEnable method. Subscribes to the lobby's ready state update event.
    /// </summary>
    private void OnEnable()
    {
        if (LobbyManager.instance != null)
            LobbyManager.instance.OnReadyStateUpdated.AddListener(ReadyStateUpdated);
    }

    /// <summary>
    /// Unity OnDisable method. Unsubscribes from the lobby's ready state update event.
    /// </summary>
    private void OnDisable()
    {
        if (LobbyManager.instance != null)
            LobbyManager.instance.OnReadyStateUpdated.RemoveListener(ReadyStateUpdated);
    }

    /// <summary>
    /// Unity Start method. Initializes the player's ready state and sets the corresponding sprite.
    /// Deactivates the UI container initially.
    /// </summary>
    private void Start()
    {
        image.sprite = unreadySprite;
        
        foreach (var player in LobbyManager.instance.players)
        {
            if ((int)player.ClientId == uiIndex)
            {
                isReady = player.IsReady;
                image.sprite = isReady ? readySprite : unreadySprite;
                break;
            }
        }
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Callback invoked when a player's ready state is updated.
    /// Updates the internal ready state and sprite for the corresponding UI slot.
    /// </summary>
    /// <param name="clientId">The client ID of the player whose state changed.</param>
    public void ReadyStateUpdated(ulong clientId)
    {
        if ((int)clientId != uiIndex) return;
        
        isReady = !isReady;
        image.sprite = isReady ? readySprite : unreadySprite;
    }
}
