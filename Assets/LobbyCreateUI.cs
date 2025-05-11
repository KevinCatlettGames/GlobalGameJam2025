using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyCreateUI : MonoBehaviour
{
    [SerializeField] private Button closeButton;
    [SerializeField] private Button createPublicButton;
    [SerializeField] private Button createPrivateButton;
    [SerializeField] private TMP_InputField lobbyNameInputField;
    
    private void Start()
    {
        Hide();
    }

    private void Awake()
    {
        closeButton.onClick.AddListener(() =>
        {
            Hide();
        });
        
        createPublicButton.onClick.AddListener(() =>
        {
            GameLobby.instance.CreateLobby(lobbyNameInputField.text, false);
            Hide();
        });
        
        createPrivateButton.onClick.AddListener(() =>
        {
            GameLobby.instance.CreateLobby(lobbyNameInputField.text, true);
            Hide();
        });
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    void Hide()
    {
        gameObject.SetActive(false);
    }
}
