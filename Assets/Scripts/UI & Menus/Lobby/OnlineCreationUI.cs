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
    [SerializeField] private Button createPublicButton;
    [SerializeField] private Button createPrivateButton;

    [Header("UI Panels & Inputs")]
    [SerializeField] private GameObject lobbyList;
    [SerializeField] private TMP_InputField joinCodeInputField;
    [SerializeField] private TMP_InputField serverNameInputField;

    [Header("Settings")]
    [SerializeField] private EventSystem eventSystem;
    public GameObject lobbyUI;
    private void Awake()
    {
        joinCodeButton.onClick.AddListener(() =>
        {         
            GameLobby.instance.JoinWithCode(joinCodeInputField.text);
            eventSystem.SetSelectedGameObject(mainMenuButton.gameObject);
            lobbyUI.SetActive(false);
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
        lobbyList.SetActive(true);
    }

    void InitLobbyCreation(bool isPrivate)
    {
        lobbyUI.SetActive(false);

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
            serverName = serverName.Substring(0, 10);
            serverNameInputField.text = serverName;
        }
        createPublicButton.interactable = serverName.Length > 0;
    }
}