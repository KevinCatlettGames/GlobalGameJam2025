
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class StartScreen : MonoBehaviour
{
    [SerializeField] private GameObject startMenu;
    [SerializeField] private GameObject anyKeyText;
    [SerializeField] private EventSystem eventSystem;
    [SerializeField] private GameObject selectedButton;
    private bool isStartMenuOpen = false;
    void Start()
    {
        Cursor.visible = false;
    }

    void Update()
    {
        if (!isStartMenuOpen && Input.anyKeyDown)
        {
            startMenu.SetActive(true);
            anyKeyText.SetActive(false);
            isStartMenuOpen = true;
            eventSystem.SetSelectedGameObject(selectedButton);
        }
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
}
