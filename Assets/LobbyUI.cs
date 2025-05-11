using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro; 

public class LobbyUI : MonoBehaviour
{
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button createLobbyButton;
    [SerializeField] private Button quickJoinButton;
    [SerializeField] private LobbyCreateUI lobbyCreateUI;
    [SerializeField] private Button joinCodeButton;
    [SerializeField] private TMP_InputField joinCodeInputField;
    [SerializeField] private TextMeshProUGUI playerCountText;
    public Button startGameButton;

    
    public string mainMenuSceneName = "MainMenu";
    private void Awake()
    {
        mainMenuButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.SceneManager.LoadScene(mainMenuSceneName, LoadSceneMode.Single);
        });
        
        createLobbyButton.onClick.AddListener(() =>
        {
            lobbyCreateUI.Show();
        });
        
        quickJoinButton.onClick.AddListener(() =>
        {
           GameLobby.instance.QuickJoin();
        });
        
        joinCodeButton.onClick.AddListener(() =>
        {
            GameLobby.instance.JoinWithCode(joinCodeInputField.text);
        });
        
        
    }

    public void HideUI()
    {
        mainMenuButton.gameObject.SetActive(false);
        createLobbyButton.gameObject.SetActive(false);
        quickJoinButton.gameObject.SetActive(false);
        joinCodeButton.gameObject.SetActive(false);
        joinCodeInputField.gameObject.SetActive(false);
        playerCountText.gameObject.SetActive(true);

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