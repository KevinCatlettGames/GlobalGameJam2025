using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Cinemachine; 

public class MenuSelection : MonoBehaviour
{
    public static MenuSelection Instance { get; private set; }

    public CinemachineVirtualCamera menuSelectionVritualCam;
    public CinemachineVirtualCamera[] otherVirtualsCams;

    GameObject currentToSelect;
    public EventSystem eventSystem;
    public GameObject localOnline;
    public GameObject startScreen;
    public GameObject mainMenu;
    public GameObject onlineMatchmaking;
    private void Awake()
    {
        if(Instance == null)
            Instance = this;
        else
            Destroy(gameObject);      
    }

    void Start()
    {
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

    public void ResetAllCams()
    {
        foreach (var cam in otherVirtualsCams)
            cam.Priority = 0;
    }

    public void ChangeSelectedGameObject(GameObject newGameObject)
    {
        currentToSelect = newGameObject;
        Invoke(nameof(ChangeSelect), 1f);
    }

    void ChangeSelect(GameObject gameObject)
    {
        eventSystem.SetSelectedGameObject(currentToSelect);
    }

    public void MakeCamPriority(int camIndex)
    {
        ResetAllCams();
        otherVirtualsCams[camIndex].Priority = 1;
    }
}