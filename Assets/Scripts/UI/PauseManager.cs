using FMODUnity;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Authentication;

public class PauseManager : MonoBehaviour
{
    [SerializeField] private EventReference togglePauseSound; 
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject pauseMenuButtons;
    private EventSystem eventSystem;
    [SerializeField] private GameObject selectedGameObject;
    [SerializeField] private GameObject controlsGraphic;

    private GameObject currentSubMenu;
    private bool isPauseMenuOpen = true;

    private void Start()
    {
        eventSystem = EventSystem.current;
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton6))
        {
            TogglePause();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void TogglePauseServerRpc()
    {
        TogglePauseClientRpc();
    }

    [ClientRpc]
    public void TogglePauseClientRpc()
    {
        TogglePause();
    }

    void TogglePause()
    {
        RuntimeManager.PlayOneShot(togglePauseSound, gameObject.transform.position);
        controlsGraphic.SetActive(false);
        pauseMenu.SetActive(!pauseMenu.activeSelf);
        if (Time.timeScale > 0)
        {
            GameManager.IsGamePaused = true;
            SetSelected();
            
            if(GameManager.Instance.playingLocal) 
                Time.timeScale = 0f;
            
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            GameManager.IsGamePaused = false;
            
            if(GameManager.Instance.playingLocal) 
                Time.timeScale = 1f;
            
            Cursor.lockState= CursorLockMode.Locked;
            Cursor.visible = false;
            if (!isPauseMenuOpen)
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
        if (GameManager.Instance.playingLocal)
        {
            Time.timeScale = 1f;
            NetworkManager.Singleton.SceneManager.LoadScene(SceneManager.GetActiveScene().name, LoadSceneMode.Single);
        }
        else 
            RestartGameServerRpc();
    }
    
     [ServerRpc(RequireOwnership = false)]
        public void RestartGameServerRpc()
        {
            Time.timeScale = 1f;
            NetworkManager.Singleton.SceneManager.LoadScene(SceneManager.GetActiveScene().name, LoadSceneMode.Single);
        }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void ToggleControlsGraphic()
    {
        controlsGraphic.SetActive(!controlsGraphic.activeSelf);
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
        Time.timeScale = 1; 
        try
        {
            // Leave lobby if we’re in one
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

        // Shutdown networking
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
        }

        // Load main menu scene
        SceneManager.LoadScene(0);
    }
    public void SetSelected()
    {
        eventSystem.SetSelectedGameObject(selectedGameObject.gameObject);
    }
}
