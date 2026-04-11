using UnityEngine;
using UnityEngine.UI;

public class PlayerContainerManager : MonoBehaviour
{
    [Header("Player Settings")]
    [SerializeField] private int uiIndex;

    [SerializeField] private Image image;
    [SerializeField] private Sprite readySprite;
    [SerializeField] private Sprite unreadySprite;

    public bool isReady = false;

    private void OnEnable()
    {
        Debug.Log("On enable");
        if (LobbyManager.instance != null)
            LobbyManager.instance.OnReadyStateUpdated.AddListener(ReadyStateUpdated);
    }

    private void OnDisable()
    {
        Debug.Log("On disable");
        if (LobbyManager.instance != null)
            LobbyManager.instance.OnReadyStateUpdated.RemoveListener(ReadyStateUpdated);
    }

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

    public void ReadyStateUpdated(ulong clientId)
    {
        if ((int)clientId != uiIndex) return;

        Debug.Log("Ready updated");
        isReady = !isReady;
        image.sprite = isReady ? readySprite : unreadySprite;
    }
}