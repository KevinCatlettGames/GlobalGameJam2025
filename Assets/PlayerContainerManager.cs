using UnityEngine;
using UnityEngine.UI;

public class PlayerContainerManager : MonoBehaviour
{
    [Header("Player Settings")]
    [SerializeField] private int uiIndex; // Inspector slot index
    [SerializeField] private Image image;
    [SerializeField] private Sprite readySprite;
    [SerializeField] private Sprite unreadySprite;
    private bool isReady = false;

    private void OnEnable()
    {
        if (LobbyManager.instance != null)
            LobbyManager.instance.OnReadyStateUpdated.AddListener(ReadyStateUpdated);
    }

    private void OnDisable()
    {
        if (LobbyManager.instance != null)
            LobbyManager.instance.OnReadyStateUpdated.RemoveListener(ReadyStateUpdated);
    }

    private void Start()
    {
        // Default state
        image.color = Color.red;
        image.sprite = unreadySprite; 

        // Update UI based on current LobbyManager state
        foreach (var player in LobbyManager.instance.players)
        {
            ulong clientId = player.ClientId;

            int containerIndex;

            if (clientId >= 1000) // Local player offset
                containerIndex = (int)(clientId - 1000);
            else
                containerIndex = (int)clientId; // Online player

            if (containerIndex == uiIndex)
            {
                isReady = player.IsReady;
                image.color = isReady ? Color.green : Color.red;
                image.sprite = isReady ? readySprite : unreadySprite;
                break;
            }
        }
    }

    private void ReadyStateUpdated(ulong clientId)
    {
        int containerIndex;

        if (clientId >= 1000)
            containerIndex = (int)(clientId - 1000);
        else
            containerIndex = (int)clientId;

        if (containerIndex != uiIndex) return;

        // Toggle ready state for UI
        isReady = !isReady;
        image.color = isReady ? Color.green : Color.red;
        image.sprite = isReady ? readySprite : unreadySprite;

    }
}