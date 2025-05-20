using FMODUnity;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseManager : MonoBehaviour
{
    [SerializeField] private EventReference togglePauseSound; 
    [SerializeField] private GameObject pauseMenu; 
    [SerializeField] private EventSystem eventSystem;
    [SerializeField] private Button continueButton;
    [SerializeField] private GameObject controlsGraphic;
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton6))
        {
            TogglePause();
        }
    }

    public void TogglePause()
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
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void ToggleControlsGraphic()
    {
        controlsGraphic.SetActive(!controlsGraphic.activeSelf);
    }

    public void ReturnToMainMenu()
    {
        SceneManager.LoadScene(0);
    }
}
