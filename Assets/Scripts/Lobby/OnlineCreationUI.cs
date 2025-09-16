using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode.Transports.UTP;
using UnityEngine.EventSystems;

public class OnlineCreationUI : MonoBehaviour
{
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button joinCodeButton;
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button createPublicButton;
    [SerializeField] private Button createPrivateButton;
    [SerializeField] private Button refreshButton;
    [SerializeField] private GameObject lobbyList;
    
    [SerializeField] private TMP_InputField joinCodeInputField;
    [SerializeField] private TextMeshProUGUI playerCountText;
    [SerializeField] private TextMeshProUGUI copyAndShareText;
    [SerializeField] private TextMeshProUGUI creatingLobbyText;
    [SerializeField] private TextMeshProUGUI joiningLobbyText;

    [SerializeField] private string mainMenuSceneName;

    [SerializeField] private EventSystem eventSystem; 
    
    private void Awake()
    {
        mainMenuButton.onClick.AddListener(() => SceneManager.LoadScene(mainMenuSceneName));
        joinCodeButton.onClick.AddListener(() =>
        {
            joinCodeButton.gameObject.SetActive(false);
            joinCodeInputField.gameObject.SetActive(false);
            createPublicButton.gameObject.SetActive(false);
            createPrivateButton.gameObject.SetActive(false);
            refreshButton.gameObject.SetActive(false);
            lobbyList.SetActive(false);
            creatingLobbyText.gameObject.SetActive(false);
            joiningLobbyText.gameObject.SetActive(true);
            GameLobby.instance.JoinWithCode(joinCodeInputField.text);
            eventSystem.SetSelectedGameObject(mainMenuButton.gameObject);
        });
        createPublicButton.onClick.AddListener(() =>
        {
            InitLobbyCreation(false);
        });
        
        createPrivateButton.onClick.AddListener(() =>
        {
            InitLobbyCreation(true);
        });
        
        //joinCodeButton.interactable = false;
        
        // joinCodeInputField.onValueChanged.AddListener(ValidateJoinButtonActivation);
    }

    void InitLobbyCreation(bool isPrivate)
    {
        joinCodeButton.gameObject.SetActive(false);
        joinCodeInputField.gameObject.SetActive(false);
        createPublicButton.gameObject.SetActive(false);
        createPrivateButton.gameObject.SetActive(false);
        refreshButton.gameObject.SetActive(false);
        lobbyList.SetActive(false);
        creatingLobbyText.gameObject.SetActive(true);
        GameLobby.instance.CreateLobby("Lobby", isPrivate);
        eventSystem.SetSelectedGameObject(mainMenuButton.gameObject);
    }
    
    public void HideOnCreateUI()
    {
        joinCodeButton.gameObject.SetActive(false);
        joinCodeInputField.gameObject.SetActive(false);
        createPublicButton.gameObject.SetActive(false);
        creatingLobbyText.gameObject.SetActive(false);
        // playerCountText.gameObject.SetActive(true);
        // copyAndShareText.gameObject.SetActive(true);
        refreshButton.gameObject.SetActive(false);
        lobbyList.SetActive(false);
        NetworkManager.Singleton.OnClientConnectedCallback += UpdatePlayerCount;
    }

    public void HideOnJoinUI()
    {
        joinCodeButton.gameObject.SetActive(false);
        joinCodeInputField.gameObject.SetActive(false);
        createPublicButton.gameObject.SetActive(false);
        createPrivateButton.gameObject.SetActive(false);
        creatingLobbyText.gameObject.SetActive(false); 
        joiningLobbyText.gameObject.SetActive(false);
        refreshButton.gameObject.SetActive(false);
        lobbyList.SetActive(false);
        NetworkManager.Singleton.OnClientConnectedCallback += UpdatePlayerCount;
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= UpdatePlayerCount;
        }
    }

    private void UpdatePlayerCount(ulong clientId)
    {
        if (NetworkManager.Singleton.IsServer)
        {
            int connectedCount = NetworkManager.Singleton.ConnectedClients.Count;
            playerCountText.text = $"Player Count: {connectedCount}";
            startGameButton.interactable = connectedCount >= 2;
        }
    }

    // private void ValidateJoinButtonActivation(string codeInput)
    // {
    //     joinCodeButton.interactable = codeInput.Length == 6;
    // }
}