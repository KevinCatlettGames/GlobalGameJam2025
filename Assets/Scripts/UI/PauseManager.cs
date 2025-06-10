using FMODUnity;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Unity.Services.Lobbies;
using Unity.Services.Authentication;

public class PauseManager : MonoBehaviour
{
    [SerializeField] private EventReference togglePauseSound; 
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject pauseMenuButtons;
    [SerializeField] private GameObject selectedGameObject;

    private EventSystem eventSystem;
    private GameObject currentSubMenu;
    private bool isPauseMenuOpen = true;

    private void Start()
    {
        eventSystem = EventSystem.current;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton6))
        {
            TogglePause();
        }
    }
    
    private void TogglePause()
    {
        RuntimeManager.PlayOneShot(togglePauseSound, transform.position);

        bool isCurrentlyPaused = pauseMenu.activeSelf;
        pauseMenu.SetActive(!isCurrentlyPaused);

        if (!isCurrentlyPaused)
        {
            GameManager.IsGamePaused = true;
            SetSelected();

            if (GameManager.Instance.PlayingLocal)
                Time.timeScale = 0f;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
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
}