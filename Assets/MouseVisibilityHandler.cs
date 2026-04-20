using UnityEngine;

/// <summary>
/// This class manages the visibility and locking state of the mouse cursor.
/// </summary>
public class MouseVisibilityHandler : MonoBehaviour
{
    [Tooltip("Singleton instance of the MouseVisibilityHandler.")]
    public static MouseVisibilityHandler instance; // Singleton instance for accessing from other scripts

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