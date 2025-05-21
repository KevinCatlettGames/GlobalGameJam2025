
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private EventSystem eventSystem;
    [SerializeField] private GameObject selectedButton;
    private bool isMainMenuOpen = false;
    void Start()
    {
        Cursor.visible = false;
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

    public void OpenSubMenu(GameObject subMenu)
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
}
