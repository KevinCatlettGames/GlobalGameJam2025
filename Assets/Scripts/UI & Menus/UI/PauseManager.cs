using FMODUnity;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Unity.Services.Lobbies;
using Unity.Services.Authentication;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using System.Threading.Tasks;
using System.Collections;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance;

    [Header("Audio & UI References")]
    [SerializeField] private EventReference togglePauseSound;
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject pauseMenuButtons;
    [SerializeField] private GameObject selectedGameObject;
    [SerializeField] private SO_Scores scores;

    [Header("Input Settings")]
    [SerializeField] private float backInputCooldown = 0.2f; // Cooldown duration in seconds

    private EventSystem eventSystem;
    private GameObject currentSubMenu;
    private bool isPauseMenuOpen = false;
    private bool isCurrentlyPaused = false;
    private float allowBackInputTime; // Tracks unscaled time threshold for back input

    private InputSystemUIInputModule inputModuleUI;
    private InputAction pauseAction;
    private InputAction backAction;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
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
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
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
            // Set cooldown to prevent back action from triggering in the same frame
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
        if (!context.performed) return;
        TogglePause();
    }

    public void OnBackInput(InputAction.CallbackContext context)
    {
        // Block back input if not performed, not paused, or still within cooldown
        if (!context.performed || !isCurrentlyPaused || Time.unscaledTime < allowBackInputTime)
            return;

        if (!isPauseMenuOpen && currentSubMenu != null)
        {
            currentSubMenu.SetActive(false);
            pauseMenuButtons.SetActive(true);
            isPauseMenuOpen = true;
            currentSubMenu = null;
            StartCoroutine(SetSelectedNextFrame(selectedGameObject));

            // Refresh cooldown when navigating back out of a submenu
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

    // --- Multiplayer / Scene Management ---

    private void OnClientDisconnect(ulong clientId)
    {
        ReturnToMainMenu();
    }

    public void RestartGame()
    {
        if (MenuTransitionHandler.Instance && MenuTransitionHandler.Instance.fadeIsOn) return;
        if (TransportSwitcher.Instance && TransportSwitcher.Instance.isUsingRelay && !NetworkManager.Singleton.IsServer) return;

        GameManager.IsGamePaused = false;
        scores.ResetKills();
        scores.ResetWins();

        if (GameManager.Instance.PlayingLocal)
        {
            Time.timeScale = 1f;
            if (MenuTransitionHandler.Instance)
            {
                MenuTransitionHandler.Instance.OnFadeComplete += LoadMap;
                MenuTransitionHandler.Instance.TriggerFade();
            }
            else
            {
                LoadMap();
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
        Time.timeScale = 1f;

        if (MenuTransitionHandler.Instance)
        {
            if (TransportSwitcher.Instance && TransportSwitcher.Instance.isUsingRelay && NetworkManager.Singleton.IsServer)
                TriggerTransitionClientRpc();

            MenuTransitionHandler.Instance.OnFadeComplete += LoadMap;
            MenuTransitionHandler.Instance.TriggerFade();
        }
        else
        {
            LoadMap();
        }
    }

    [ClientRpc]
    private void TriggerTransitionClientRpc()
    {
        MenuTransitionHandler.Instance.TriggerFade();
    }

    private void LoadMap()
    {
        if (MenuTransitionHandler.Instance && MenuTransitionHandler.Instance.fadeIsOn) return;

        if (MenuTransitionHandler.Instance)
            MenuTransitionHandler.Instance.OnFadeComplete -= LoadMap;

        NetworkManager.Singleton.SceneManager.LoadScene(SceneManager.GetActiveScene().name, LoadSceneMode.Single);
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
        return NetworkManager.Singleton.IsHost;
    }
}