using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;

public class LobbyUI : MonoBehaviour
{
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button joinCodeButton;
    [SerializeField] private TMP_InputField joinCodeInputField;
    [SerializeField] private TextMeshProUGUI playerCountText;
    public Button startGameButton;
    [SerializeField] private Button createPublicButton;
    
    public string mainMenuSceneName;
    private void Awake()
    {
        mainMenuButton.onClick.AddListener(() =>
        { 
            SceneManager.LoadScene(mainMenuSceneName);
        });
        
        joinCodeButton.onClick.AddListener(() =>
        {
            GameLobby.instance.JoinWithCode(joinCodeInputField.text);
        });
        
        createPublicButton.onClick.AddListener(() =>
        {
            GameLobby.instance.CreateLobby("Empty", false);
        });
    }

    public void HideUI()
    {
        joinCodeButton.gameObject.SetActive(false);
        joinCodeInputField.gameObject.SetActive(false);
        playerCountText.gameObject.SetActive(true);
        createPublicButton.gameObject.SetActive(false);
        NetworkManager.Singleton.OnClientConnectedCallback += UpdatePlayerCount;
    }

    void UpdatePlayerCount(ulong signature)
    {
        if (NetworkManager.Singleton.IsServer && NetworkManager.Singleton.ConnectedClients.Count >= 2)
        {
            startGameButton.interactable = true; 
        }
        playerCountText.text = "Player Count: " + NetworkManager.Singleton.ConnectedClients.Count;
    }
}