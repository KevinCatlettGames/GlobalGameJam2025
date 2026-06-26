using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class OnlineCreationUI : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button joinCodeButton;
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button createPublicButton;
    [SerializeField] private Button createPrivateButton;
    [SerializeField] private Button refreshButton;

    [Header("UI Panels & Inputs")]
    [SerializeField] private GameObject lobbyList;
    [SerializeField] private TMP_InputField joinCodeInputField;
    [SerializeField] private TMP_InputField serverNameInputField;

    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI copyAndShareText;
    [SerializeField] private TextMeshProUGUI creatingLobbyText;
    [SerializeField] private TextMeshProUGUI joiningLobbyText;

    [Header("Settings")]
    [SerializeField] private EventSystem eventSystem;
    public GameObject lobbyUI;
    private void Awake()
    {
        joinCodeButton.onClick.AddListener(() =>
        {
            lobbyUI.SetActive(false);
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
        
        joinCodeInputField.onValueChanged.AddListener(ValidateJoinButtonActivation);
        serverNameInputField.onValueChanged.AddListener(ValidateCreatePublicActivation);
    }

    private void OnEnable()
    {
        joinCodeButton.gameObject.SetActive(true);
        joinCodeInputField.gameObject.SetActive(true);
        serverNameInputField.gameObject.SetActive(true);
        createPublicButton.gameObject.SetActive(true);
        createPrivateButton.gameObject.SetActive(true);
        refreshButton.gameObject.SetActive(false);
        lobbyList.SetActive(true);
    }

    void InitLobbyCreation(bool isPrivate)
    {
        lobbyUI.SetActive(false);
        joiningLobbyText.gameObject.SetActive(true);

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
    
    private void ValidateJoinButtonActivation(string codeInput)
    {
        joinCodeButton.gameObject.SetActive(codeInput.Length == 6);
    }
    
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