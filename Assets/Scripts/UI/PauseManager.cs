using FMODUnity;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Unity.Services.Lobbies;
using Unity.Services.Authentication;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

public class PauseManager : MonoBehaviour
{
    [SerializeField] private EventReference togglePauseSound; 
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject pauseMenuButtons;
    [SerializeField] private GameObject selectedGameObject;

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
        if (GameManager.Instance.PlayingLocal)
        {
            Time.timeScale = 1f;
            NetworkManager.Singleton.SceneManager.LoadScene(SceneManager.GetActiveScene().name, LoadSceneMode.Single);
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
        Time.timeScale = 1f;

        try
        {
            if (GlobalLobby.CurrentLobby != null)
            {
                if (NetworkManager.Singleton.IsHost)
                {
                    await LobbyService.Instance.DeleteLobbyAsync(GlobalLobby.CurrentLobby.Id);
                }
                else
                {
                    string playerId = AuthenticationService.Instance.PlayerId;
                    await LobbyService.Instance.RemovePlayerAsync(GlobalLobby.CurrentLobby.Id, playerId);
                }
                GlobalLobby.CurrentLobby = null;
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Error leaving lobby: {e.Message}");
        }

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
            Destroy(NetworkManager.Singleton.gameObject);
        }

        SceneManager.LoadScene(0);
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