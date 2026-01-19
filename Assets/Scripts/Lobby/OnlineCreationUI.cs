using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

/// <summary>
/// Manages the UI for online lobby creation and joining.
/// Handles button interactions, input validation, and lobby creation/joining flow.
/// </summary>
public class OnlineCreationUI : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button mainMenuButton;          // Button to return to the main menu
    [SerializeField] private Button joinCodeButton;          // Button to join a lobby via code
    [SerializeField] private Button startGameButton;         // Button to start the game (host only)
    [SerializeField] private Button createPublicButton;      // Button to create a public lobby
    [SerializeField] private Button createPrivateButton;     // Button to create a private lobby
    [SerializeField] private Button refreshButton;           // Button to refresh lobby list

    [Header("UI Panels & Inputs")]
    [SerializeField] private GameObject lobbyList;           // Panel showing the available lobbies
    [SerializeField] private TMP_InputField joinCodeInputField;  // Input field for joining via code
    [SerializeField] private TMP_InputField serverNameInputField; // Input field for entering server name

    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI copyAndShareText;   // Text displaying code to copy/share
    [SerializeField] private TextMeshProUGUI creatingLobbyText;  // Text shown when creating a lobby
    [SerializeField] private TextMeshProUGUI joiningLobbyText;   // Text shown when joining a lobby

    [Header("Settings")]
    [SerializeField] private string mainMenuSceneName;           // Scene name for main menu
    [SerializeField] private EventSystem eventSystem;           // Event system for selecting buttons programmatically

    /// <summary>
    /// Unity Awake. Sets up button listeners and input validation logic.
    /// </summary>
    private void Awake()
    {
        mainMenuButton.onClick.AddListener(() => SceneManager.LoadScene(mainMenuSceneName));

        joinCodeButton.onClick.AddListener(() =>
        {
            joinCodeButton.gameObject.SetActive(false);
            joinCodeInputField.gameObject.SetActive(false);
            serverNameInputField.gameObject.SetActive(false);
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
        
        joinCodeButton.interactable = false; 
        joinCodeInputField.onValueChanged.AddListener(ValidateJoinButtonActivation);

        createPublicButton.interactable = false;
        serverNameInputField.onValueChanged.AddListener(ValidateCreatePublicActivation);
    }

    /// <summary>
    /// Initializes lobby creation process.
    /// </summary>
    /// <param name="isPrivate">Whether the lobby should be private.</param>
    void InitLobbyCreation(bool isPrivate)
    {
        joinCodeButton.gameObject.SetActive(false);
        joinCodeInputField.gameObject.SetActive(false);
        serverNameInputField.gameObject.SetActive(false);
        createPublicButton.gameObject.SetActive(false);
        createPrivateButton.gameObject.SetActive(false);
        refreshButton.gameObject.SetActive(false);
        lobbyList.SetActive(false);
        creatingLobbyText.gameObject.SetActive(true);

        if (serverNameInputField.text.Length > 0)
        {
            GameLobby.instance.CreateLobby(serverNameInputField.text, isPrivate);
        }
        else
        {
            GameLobby.instance.CreateLobby("Default Server", isPrivate);
        }

        eventSystem.SetSelectedGameObject(mainMenuButton.gameObject);
    }
    
    /// <summary>
    /// Hides UI elements after creating a lobby.
    /// </summary>
    public void HideOnCreateUI()
    {
        joinCodeButton.gameObject.SetActive(false);
        joinCodeInputField.gameObject.SetActive(false);
        serverNameInputField.gameObject.SetActive(false);
        createPublicButton.gameObject.SetActive(false);
        creatingLobbyText.gameObject.SetActive(false);
        refreshButton.gameObject.SetActive(false);
        lobbyList.SetActive(false);
    }

    /// <summary>
    /// Hides UI elements after joining a lobby.
    /// </summary>
    public void HideOnJoinUI()
    {
        joinCodeButton.gameObject.SetActive(false);
        joinCodeInputField.gameObject.SetActive(false);
        serverNameInputField.gameObject.SetActive(false);
        createPublicButton.gameObject.SetActive(false);
        createPrivateButton.gameObject.SetActive(false);
        creatingLobbyText.gameObject.SetActive(false); 
        joiningLobbyText.gameObject.SetActive(false);
        refreshButton.gameObject.SetActive(false);
        lobbyList.SetActive(false);
    }
    
    /// <summary>
    /// Validates if the join code input is valid to enable the join button.
    /// </summary>
    /// <param name="codeInput">Current input from the code field.</param>
    private void ValidateJoinButtonActivation(string codeInput)
    {
        joinCodeButton.gameObject.SetActive(codeInput.Length == 6);
    }

    /// <summary>
    /// Validates if the public lobby creation button should be enabled.
    /// </summary>
    /// <param name="serverName">Current input from the server name field.</param>
    private void ValidateCreatePublicActivation(string serverName)
    {
        if (serverName.Length > 10)
        {
            serverName = "";
            serverNameInputField.text = "";
        }

        createPublicButton.gameObject.SetActive(serverName.Length > 0);
    }
}