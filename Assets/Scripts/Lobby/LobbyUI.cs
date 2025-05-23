using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class LobbyUI : MonoBehaviour
{
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button joinCodeButton;
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button createPublicButton;
    
    [SerializeField] private TMP_InputField joinCodeInputField;
    [SerializeField] private TextMeshProUGUI playerCountText;
    [SerializeField] private TextMeshProUGUI copyAndShareText;
    [SerializeField] private TextMeshProUGUI creatingLobbyText;
    [SerializeField] private TextMeshProUGUI joiningLobbyText;

    [SerializeField] private string mainMenuSceneName;

    private void Awake()
    {
        mainMenuButton.onClick.AddListener(() => SceneManager.LoadScene(mainMenuSceneName));
        joinCodeButton.onClick.AddListener(() =>
        {
            joinCodeButton.gameObject.SetActive(false);
            joinCodeInputField.gameObject.SetActive(false);
            createPublicButton.gameObject.SetActive(false);
            creatingLobbyText.gameObject.SetActive(false);
            joiningLobbyText.gameObject.SetActive(true);
            GameLobby.instance.JoinWithCode(joinCodeInputField.text);
        });
        createPublicButton.onClick.AddListener(() =>
        {
            joinCodeButton.gameObject.SetActive(false);
            joinCodeInputField.gameObject.SetActive(false);
            createPublicButton.gameObject.SetActive(false);
            creatingLobbyText.gameObject.SetActive(true);
            GameLobby.instance.CreateLobby("Empty", false);
        });
        
        joinCodeButton.interactable = false;
        
        joinCodeInputField.onValueChanged.AddListener(ValidateJoinButtonActivation);
    }
    
    public void HideOnCreateUI()
    {
        joinCodeButton.gameObject.SetActive(false);
        joinCodeInputField.gameObject.SetActive(false);
        createPublicButton.gameObject.SetActive(false);
        creatingLobbyText.gameObject.SetActive(false);
        playerCountText.gameObject.SetActive(true);
        copyAndShareText.gameObject.SetActive(true);
        NetworkManager.Singleton.OnClientConnectedCallback += UpdatePlayerCount;
    }

    public void HideOnJoinUI()
    {
        joinCodeButton.gameObject.SetActive(false);
        joinCodeInputField.gameObject.SetActive(false);
        createPublicButton.gameObject.SetActive(false);
        creatingLobbyText.gameObject.SetActive(false); 
        joiningLobbyText.gameObject.SetActive(false);
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

    private void ValidateJoinButtonActivation(string codeInput)
    {
        joinCodeButton.interactable = codeInput.Length == 6;
    }
}