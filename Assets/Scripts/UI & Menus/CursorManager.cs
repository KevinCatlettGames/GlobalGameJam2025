using UnityEngine;
public class CursorManager : MonoBehaviour
{
    public static CursorManager instance;
    public bool hideAtStart = true; 
    
    private void Awake()
    {
        if (instance == null)
            instance = this;
        
        if(hideAtStart)
            HideMouse();
    }
    
    public void HideMouse()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false; 
    }

    public void ShowMouse()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}