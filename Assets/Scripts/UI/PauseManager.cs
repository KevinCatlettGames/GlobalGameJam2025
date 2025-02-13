using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseManager : MonoBehaviour
{
    public EventReference togglePauseSound; 
    public GameObject pauseMenu; 
    public EventSystem eventSystem;
    public Button continueButton;
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton7))
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        RuntimeManager.PlayOneShot(togglePauseSound, gameObject.transform.position);
        pauseMenu.SetActive(!pauseMenu.activeSelf);
        if (Time.timeScale > 0)
        {
          eventSystem.SetSelectedGameObject(continueButton.gameObject);
            Time.timeScale = 0f;
        }
        else
            Time.timeScale = 1f;
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
}
