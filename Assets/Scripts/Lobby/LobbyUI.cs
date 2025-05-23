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
    
    [SerializeField] private string mainMenuSceneName;

    private void Awake()
    {
        mainMenuButton.onClick.AddListener(() => SceneManager.LoadScene(mainMenuSceneName));
        joinCodeButton.onClick.AddListener(() => GameLobby.instance.JoinWithCode(joinCodeInputField.text));
        createPublicButton.onClick.AddListener(() => GameLobby.instance.CreateLobby("Empty", false));
        
        joinCodeButton.interactable = false;
        
        joinCodeInputField.onValueChanged.AddListener(ValidateJoinButtonActivation);
    }

    public void HideUI()
    {
        joinCodeButton.gameObject.SetActive(false);
        joinCodeInputField.gameObject.SetActive(false);
        createPublicButton.gameObject.SetActive(false);

        if (NetworkManager.Singleton.IsServer)
        {
            playerCountText.gameObject.SetActive(true);
            NetworkManager.Singleton.OnClientConnectedCallback += UpdatePlayerCount;
        }
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