using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject selectedGameObject;
    private GameObject currentSubMenu;

    private InputSystemUIInputModule inputModuleUI;
    private InputAction backAction;
    void Start()
    {
        SetSelected();
        inputModuleUI = EventSystem.current.gameObject.GetComponent<InputSystemUIInputModule>();
        backAction = inputModuleUI.actionsAsset.FindAction("UI/Back");

        if (backAction != null)
        {
            backAction.performed += OnBackInput;
            backAction.Enable();
        }

        Vector2 mousePosition = new Vector2(.3f * Screen.width, .65f * Screen.height);
        Mouse.current.WarpCursorPosition(mousePosition);
    }

    void Update()
    {
        if (!Cursor.visible && (Input.GetKeyDown(KeyCode.Mouse0) || Input.GetKeyDown(KeyCode.Mouse1)))
        {
            Cursor.visible = true;
        }
    }
    
    public void QuitGame()
    {
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }

    public void ToggleSubMenu(GameObject subMenu)
    {
        if (currentSubMenu != null)
        {
            currentSubMenu.SetActive(false);
            mainMenu.SetActive(true);
            SetSelected();
            currentSubMenu = null;
        }
        else
        {
            currentSubMenu = subMenu;
            currentSubMenu.SetActive(true);
            mainMenu.SetActive(false);
        }
    }
    public void SetSelected()
    {
        EventSystem eventSystem = EventSystem.current;
        eventSystem.SetSelectedGameObject(selectedGameObject);
    }
    public void SetSelected(GameObject selectedObject)
    {
        EventSystem eventSystem = EventSystem.current;
        eventSystem.SetSelectedGameObject(selectedObject);
    }

    public void OnBackInput(InputAction.CallbackContext context)
    {
        ToggleSubMenu(null);
    }

    private void OnDestroy()
    {
        if (backAction != null)
        {
            backAction.performed -= OnBackInput;
        }
    }

    public void OpenOnlineCreation()
    {
        SceneManager.LoadScene("OnlineCreation");
    }
}
