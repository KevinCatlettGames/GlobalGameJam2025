using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject selectedGameObject;
    private bool isMainMenuOpen = true;
    void Start()
    {
        Cursor.visible = false;
        SetSelected();
        int fullScreen = PlayerPrefs.GetInt("Fullscreen", 1);
        if (fullScreen == 1)
        {
            Screen.fullScreen = true;
        }
        else
        {
            Screen.fullScreen = false;
        }
    }

    void Update()
    {
        if (!Cursor.visible && (Input.GetKeyDown(KeyCode.Mouse0) || Input.GetKeyDown(KeyCode.Mouse1)))
        {
            Cursor.visible = true;
        }
    }

    public void StartGameLocal()
    {
        SceneManager.LoadScene(1);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void ToggleSubMenu(GameObject subMenu)
    {
        if (isMainMenuOpen)
        {
            subMenu.SetActive(true);
            mainMenu.SetActive(false);
            isMainMenuOpen = false;
        }
        else
        {
            subMenu.SetActive(false);
            mainMenu.SetActive(true);
            isMainMenuOpen = true;
        }
    }
    public void SetSelected()
    {
        EventSystem eventSystem = EventSystem.current;
        eventSystem.SetSelectedGameObject(selectedGameObject);
    }
}
