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

public class PauseManager : MonoBehaviour
{
    [SerializeField] private EventReference togglePauseSound; 
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject pauseMenuButtons;
    [SerializeField] private GameObject selectedGameObject;
    [SerializeField] private SO_Scores scores;

    private EventSystem eventSystem;
    private GameObject currentSubMenu;
    private bool isPauseMenuOpen = true;
    private bool isCurrentlyPaused = false;

    private InputSystemUIInputModule inputModuleUI;
    private InputAction pauseAction;
    private InputAction backAction;
    private void Start()
    {
        eventSystem = EventSystem.current;
        inputModuleUI = eventSystem.gameObject.GetComponent<InputSystemUIInputModule>();
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

    private void OnEnable()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
    }
    
    private void OnClientDisconnect(ulong clientId)
    {
        if (clientId == 1)
        {
            Debug.Log("Host disconnected — returning to main menu...");
            ReturnToMainMenu();
        }
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
    }

    private void TogglePause()
    {
        RuntimeManager.PlayOneShot(togglePauseSound, transform.position);
        pauseMenu.SetActive(!isCurrentlyPaused);

        if (!isCurrentlyPaused)
        {
            isCurrentlyPaused = true;
            GameManager.IsGamePaused = true;
            SetSelected();

            if (GameManager.Instance.PlayingLocal)
                Time.timeScale = 0f;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Vector2 mousePosition = new Vector2(.35f * Screen.width, .75f * Screen.height);
            Mouse.current.WarpCursorPosition(mousePosition);
        }
        else
        {
            isCurrentlyPaused = false;
            GameManager.IsGamePaused = false;

            if (GameManager.Instance.PlayingLocal)
                Time.timeScale = 1f;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (!isPauseMenuOpen && currentSubMenu != null)
            {
                currentSubMenu.SetActive(false);
                pauseMenuButtons.SetActive(true);
                isPauseMenuOpen = true;
                currentSubMenu = null;
            }
        }
    }

    public void RestartGame()
    {
        GameManager.IsGamePaused = false;
        scores.ResetKills();
        scores.ResetWins();
        
        if (GameManager.Instance.PlayingLocal)
        {
            Time.timeScale = 1f;
            NetworkManager.Singleton.SceneManager.LoadScene(SceneManager.GetActiveScene().name, LoadSceneMode.Single);
        }
        else
            RestartGameServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void RestartGameServerRpc()
    {
        Time.timeScale = 1f;
        NetworkManager.Singleton.SceneManager.LoadScene(SceneManager.GetActiveScene().name, LoadSceneMode.Single);
    }

    public void QuitGame()
    {
        Application.Quit();
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

    public async void ReturnToMainMenu()
    {
        Cursor.visible = true;
        Time.timeScale = 1f; 
        
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
        
        if(NetworkManager.Singleton)
            Destroy(NetworkManager.Singleton.gameObject);

        GlobalLobby.CurrentLobby = null;

        LoadMainMenu();
    }

    public async void ReturnToLobby()
    {
        Cursor.visible = true;
        Time.timeScale = 1f; 
        
        if(LobbyManager.instance)
            Destroy(LobbyManager.instance.gameObject);

        LoadLobby();
    }

    void LoadLobby()
    {
        SceneManager.LoadScene("UI_Lobby");
    }

    void LoadMainMenu()
    {
        SceneManager.LoadScene("UI_MainMenu");
    }
    
    private bool IsHost()
    {
        return NetworkManager.Singleton.IsHost;
    }
    
    public void SetSelected()
    {
        eventSystem.SetSelectedGameObject(selectedGameObject);
    }
    public void SetSelectedButton(GameObject gameObject)
    {
        eventSystem.SetSelectedGameObject(gameObject);
    }
    public void OnPauseInput(InputAction.CallbackContext context)
    {
         TogglePause();       
    }
    public void OnBackInput(InputAction.CallbackContext context)
    {
        if (!isCurrentlyPaused) return;
        
        if (!isPauseMenuOpen && currentSubMenu != null)
        {
            currentSubMenu.SetActive(false);
            pauseMenuButtons.SetActive(true);
            isPauseMenuOpen = true;
            currentSubMenu = null;
            SetSelected();
        }
        else
        {
            TogglePause();
        }
    }
    private void OnDestroy()
    {
        if (pauseAction != null)
        {
            pauseAction.performed -= OnPauseInput;
        }
        if (backAction != null)
        {
            backAction.performed -= OnBackInput;
        }
    }
}