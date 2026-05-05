using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerContainerManager : MonoBehaviour
{
    [Header("Player Settings")]
    [SerializeField] private int uiIndex;
    public GameObject readyObject; 
    [SerializeField] private Image readyImage;
    [SerializeField] private TextMeshProUGUI unreadyText;
    [SerializeField] private TextMeshProUGUI readyText;

    public bool isReady = false;

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
        foreach (var player in LobbyManager.instance.players)
        {
            if ((int)player.ClientId == uiIndex)
            {
                isReady = player.IsReady;
                readyImage.enabled = isReady ? false : true;
                unreadyText.enabled = isReady ? false : true;
                readyText.enabled = isReady ? true : false;
                readyObject.SetActive(true);
                break;
            }
        }

        gameObject.SetActive(false);
    }

    public void ReadyStateUpdated(ulong clientId)
    {
        if ((int)clientId != uiIndex) return;     
        isReady = !isReady;
        readyImage.enabled = isReady ? false : true;
        unreadyText.enabled = isReady ? false : true;
        readyText.enabled = isReady ? true : false;
    }
}