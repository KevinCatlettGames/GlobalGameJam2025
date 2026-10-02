using System.Collections;
using System.Threading.Tasks;
using FMODUnity;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

public class PauseManager : NetworkBehaviour
{
    public static PauseManager Instance;

    [Header("Audio & UI References")]
    [SerializeField] private EventReference togglePauseSound;
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject pauseMenuButtons;
    [SerializeField] private GameObject selectedGameObject;
    [SerializeField] private GameObject restartButton;
    [SerializeField] private SO_Scores scores;

    [Header("Input Settings")]
    [SerializeField] private float backInputCooldown = 0.2f;

    private EventSystem eventSystem;
    private GameObject currentSubMenu;
    private bool isPauseMenuOpen = false;
    private bool isCurrentlyPaused = false;
    private float allowBackInputTime;

    private InputSystemUIInputModule inputModuleUI;
    private InputAction pauseAction;
    private InputAction backAction;

    public bool PausingEnabled = true;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (restartButton != null && !GameManager.Instance.PlayingLocal)
        {
            restartButton.SetActive(IsServer || IsHost());
        }
    }

    private void Start()
    {
        eventSystem = EventSystem.current;
        if (eventSystem != null)
        {
            inputModuleUI = eventSystem.gameObject.GetComponent<InputSystemUIInputModule>();
            if (inputModuleUI != null && inputModuleUI.actionsAsset != null)
            {
                pauseAction = inputModuleUI.actionsAsset.FindAction("UI/Pause");
                backAction = inputModuleUI.actionsAsset.FindAction("UI/Back");

                if (pauseAction != null)
                {
                    pauseAction.performed += OnPauseInput;
                    pauseAction.Enable();
                }
                if (backAction != null)
                {
                    backAction.performed += OnBackInput;
                    backAction.Enable();
                }
            }
        }
    }

    private void OnEnable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;

            if (NetworkManager.Singleton.SceneManager != null)
            {
                NetworkManager.Singleton.SceneManager.OnSceneEvent += OnSceneEvent;
            }
        }
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;

            if (NetworkManager.Singleton.SceneManager != null)
            {
                NetworkManager.Singleton.SceneManager.OnSceneEvent -= OnSceneEvent;
            }
        }
    }

    private void OnDestroy()
    {
        if (pauseAction != null)
            pauseAction.performed -= OnPauseInput;

        if (backAction != null)
            backAction.performed -= OnBackInput;
    }

    private void TogglePause()
    {
        RuntimeManager.PlayOneShot(togglePauseSound, transform.position);

        isCurrentlyPaused = !isCurrentlyPaused;
        GameManager.IsGamePaused = isCurrentlyPaused;
        pauseMenu.SetActive(isCurrentlyPaused);

        if (isCurrentlyPaused)
        {
            allowBackInputTime = Time.unscaledTime + backInputCooldown;

            isPauseMenuOpen = true;
            StartCoroutine(SetSelectedNextFrame(selectedGameObject));

            if (GameManager.Instance != null && GameManager.Instance.PlayingLocal)
                Time.timeScale = 0f;
        }
        else
        {
            if (GameManager.Instance != null && GameManager.Instance.PlayingLocal)
                Time.timeScale = 1f;

            if (currentSubMenu != null)
            {
                currentSubMenu.SetActive(false);
                pauseMenuButtons.SetActive(true);
                currentSubMenu = null;
            }
            isPauseMenuOpen = false;
        }
    }

    public void OnPauseInput(InputAction.CallbackContext context)
    {
        if (!context.performed || !PausingEnabled) return;
        TogglePause();
    }

    public void OnBackInput(InputAction.CallbackContext context)
    {
        if (!context.performed || !isCurrentlyPaused || Time.unscaledTime < allowBackInputTime)
            return;

        if (!isPauseMenuOpen && currentSubMenu != null)
        {
            currentSubMenu.SetActive(false);
            pauseMenuButtons.SetActive(true);
            isPauseMenuOpen = true;
            currentSubMenu = null;
            StartCoroutine(SetSelectedNextFrame(selectedGameObject));

            allowBackInputTime = Time.unscaledTime + backInputCooldown;
        }
        else
        {
            TogglePause();
        }
    }

    private IEnumerator SetSelectedNextFrame(GameObject target)
    {
        yield return null;
        if (eventSystem != null && target != null)
        {
            eventSystem.SetSelectedGameObject(null);
            eventSystem.SetSelectedGameObject(target);
        }
    }

    public void SetSelected()
    {
        StartCoroutine(SetSelectedNextFrame(selectedGameObject));
    }

    public void SetSelectedButton(GameObject gameObject)
    {
        StartCoroutine(SetSelectedNextFrame(gameObject));
    }

    public void ToggleSubMenu(GameObject subMenu)
    {
        if (isPauseMenuOpen)
        {
            subMenu.SetActive(true);
            pauseMenuButtons.SetActive(false);
            isPauseMenuOpen = false;
            currentSubMenu = subMenu;
        }
        else
        {
            subMenu.SetActive(false);
            pauseMenuButtons.SetActive(true);
            isPauseMenuOpen = true;
            currentSubMenu = null;
        }
    }

    private void OnClientDisconnect(ulong clientId)
    {
        if(TransportSwitcher.Instance && TransportSwitcher.Instance.isUsingRelay)
            ReturnToMainMenu();
    }

    private void OnSceneEvent(SceneEvent sceneEvent)
    {
        if (sceneEvent.SceneEventType == SceneEventType.LoadEventCompleted)
        {
            GameManager.IsGamePaused = false;
            Time.timeScale = 1f;
        }
    }

    public void RestartGame()
    {
        if (!GameManager.Instance.PlayingLocal && !IsServer && !IsHost()) return;
        if (MenuTransitionHandler.Instance && MenuTransitionHandler.Instance.fadeIsOn) return;

        GameManager.IsGamePaused = false;

        if (GameManager.Instance != null && GameManager.Instance.PlayingLocal)
        {
            Time.timeScale = 1f;
            if (scores != null)
            {
                scores.ResetKills();
                scores.ResetWins();
            }

            if (MenuTransitionHandler.Instance)
            {
                MenuTransitionHandler.Instance.OnFadeComplete += LoadMapLocal;
                MenuTransitionHandler.Instance.TriggerFade();
            }
            else
            {
                LoadMapLocal();
            }
        }
        else
        {
            RestartGameServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RestartGameServerRpc()
    {
        if (!IsServer) return;

        Time.timeScale = 1f;

        ResetScoresClientRpc();
        TriggerTransitionClientRpc();

        if (MenuTransitionHandler.Instance)
        {
            MenuTransitionHandler.Instance.OnFadeComplete += LoadMapServer;
            MenuTransitionHandler.Instance.TriggerFade();
        }
        else
        {
            LoadMapServer();
        }
    }

    [ClientRpc]
    private void TriggerTransitionClientRpc()
    {
        if (IsServer) return;

        Time.timeScale = 1f;
        if (MenuTransitionHandler.Instance)
        {
            MenuTransitionHandler.Instance.TriggerFade();
        }
    }

    [ClientRpc]
    public void ResetScoresClientRpc()
    {
        if (scores != null)
        {
            scores.ResetKills();
            scores.ResetWins();
        }
    }

    private void LoadMapServer()
    {
        if (MenuTransitionHandler.Instance)
            MenuTransitionHandler.Instance.OnFadeComplete -= LoadMapServer;

        if (IsServer && NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
        {
            NetworkManager.Singleton.SceneManager.LoadScene(SceneManager.GetActiveScene().name, LoadSceneMode.Single);
        }
    }

    private void LoadMapLocal()
    {
        if (MenuTransitionHandler.Instance)
            MenuTransitionHandler.Instance.OnFadeComplete -= LoadMapLocal;

        NetworkManager.Singleton.SceneManager.LoadScene(
                  SceneManager.GetActiveScene().name,
                  LoadSceneMode.Single
              );      
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public async void ReturnToMainMenu()
    {
        if (MenuTransitionHandler.Instance && MenuTransitionHandler.Instance.fadeIsOn) return;

        try
        {
            if (GameLobby.instance != null && GlobalLobby.CurrentLobby != null)
            {
                string lobbyId = GlobalLobby.CurrentLobby.Id;
                string playerId = AuthenticationService.Instance.PlayerId;

                if (IsHost())
                {
                    var options = new UpdateLobbyOptions { IsPrivate = true };
                    await LobbyService.Instance.UpdateLobbyAsync(lobbyId, options);
                    await Task.Delay(100);
                    await LobbyService.Instance.DeleteLobbyAsync(lobbyId);
                }
                else
                {
                    await LobbyService.Instance.RemovePlayerAsync(lobbyId, playerId);
                }
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to clean up lobby: {e}");
        }

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            NetworkManager.Singleton.Shutdown();

        if (NetworkManager.Singleton)
            Destroy(NetworkManager.Singleton.gameObject);

        GlobalLobby.CurrentLobby = null;

        InitLoadMenu();
    }

    public async void ReturnToLobby()
    {
        Time.timeScale = 1f;

        if (LobbyManager.instance)
            Destroy(LobbyManager.instance.gameObject);

        LoadLobby();
    }

    private void LoadLobby()
    {
        SceneManager.LoadScene("UI_Lobby");
    }

    private void InitLoadMenu()
    {
        if (MenuTransitionHandler.Instance)
        {
            MenuTransitionHandler.Instance.OnFadeComplete += LoadMenu;
            MenuTransitionHandler.Instance.TriggerFade();
        }
        else
        {
            LoadMenu();
        }
    }

    private void LoadMenu()
    {
        if (MenuTransitionHandler.Instance)
            MenuTransitionHandler.Instance.OnFadeComplete -= LoadMenu;

        SceneManager.LoadScene("UI_MainMenu");
    }

    private bool IsHost()
    {
        return NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;
    }
}