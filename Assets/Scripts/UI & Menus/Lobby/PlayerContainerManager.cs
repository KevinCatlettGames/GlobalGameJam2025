using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerContainerManager : MonoBehaviour
{
    [Header("Player Settings")]
    public int uiIndex;
    public GameObject readyObject; 
    [SerializeField] private Image readyImage;
    [SerializeField] private TextMeshProUGUI unreadyText;
    [SerializeField] private TextMeshProUGUI readyText;

    public bool isReady = false;
    public bool occupied = false;
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
#if (UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN) && !UNITY_SWITCH
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
#else
        foreach (var player in LobbyManager.instance.switchPlayers)
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
#endif 
        gameObject.SetActive(false);
    }

    public void ReadyStateUpdated(ulong clientId, bool state)
    {
        if ((int)clientId != uiIndex) return;   

        if(state)
        {
            isReady = true;
            readyImage.enabled = false;
            unreadyText.enabled = false;
            readyText.enabled = true;
        }
        else
        {
            isReady = false;
            readyImage.enabled = true;
            unreadyText.enabled = true;
            readyText.enabled = false;
        }
    }
}