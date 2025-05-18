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
    [SerializeField] private EventSystem eventSystem;
    [SerializeField] private Button continueButton;
    [SerializeField] private GameObject controlsGraphic;
    // Update is called once per frame
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
            eventSystem.SetSelectedGameObject(continueButton.gameObject);
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Time.timeScale = 1f;
            Cursor.lockState= CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
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
}
