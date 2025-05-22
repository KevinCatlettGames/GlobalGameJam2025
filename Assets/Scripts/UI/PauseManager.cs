using FMODUnity;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseManager : NetworkBehaviour
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
            TogglePauseServerRpc();
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
        RuntimeManager.PlayOneShot(togglePauseSound, gameObject.transform.position);
        controlsGraphic.SetActive(false);
        pauseMenu.SetActive(!pauseMenu.activeSelf);
        if (Time.timeScale > 0)
        {
            GameManager.IsGamePaused = true;
            SetSelected();
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            GameManager.IsGamePaused = false;
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
        Time.timeScale = 1f;
        GameManager.IsGamePaused = false;
         RestartGameServerRpc();
    }
    
     [ServerRpc(RequireOwnership = false)]
        public void RestartGameServerRpc()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            Unity.Netcode.NetworkManager.Singleton.SceneManager.LoadScene(SceneManager.GetActiveScene().name, LoadSceneMode.Single);
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
    public void ReturnToMainMenu()
    {
        SceneManager.LoadScene(0);
    }
    public void SetSelected()
    {
        eventSystem.SetSelectedGameObject(selectedGameObject.gameObject);
    }
}
