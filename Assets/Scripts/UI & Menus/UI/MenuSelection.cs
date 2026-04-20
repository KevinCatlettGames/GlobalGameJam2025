using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Cinemachine; 

public class MenuSelection : MonoBehaviour
{
    public static MenuSelection Instance { get; private set; }

    public CinemachineVirtualCamera menuSelectionVritualCam;
    public CinemachineVirtualCamera[] otherVirtualsCams; 

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
}