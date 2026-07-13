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
    [SerializeField] private TextMeshProUGUI youText;

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
        foreach (var player in LobbyManager.instance.players)
        {
            if (player.PlayerIndex == uiIndex)
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

    public void ReadyStateUpdated(int playerIndex, bool state)
    {
        if (playerIndex != uiIndex) return;   

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

    public void ToggleYouText(bool value)
    {
        youText.enabled = value;
    }

    public void ToggleYouText(bool value, string userName)
    {
        youText.enabled = value;
        youText.text = userName;
    }
}