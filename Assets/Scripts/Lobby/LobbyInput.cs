using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class LobbyInput : MonoBehaviour
{
    public InputActionProperty readyAction;
    public InputActionProperty leaveAction;
    public int playerIndex; 
    
    
    
    private void OnEnable()
    {
        readyAction.action.canceled += OnReadyPerformed;
        readyAction.action.Enable();
        
        leaveAction.action.performed += OnLeavePerformed;
        leaveAction.action.Enable();
        
        SceneManager.sceneLoaded += SceneManagerOnsceneLoaded;
    }

    private void SceneManagerOnsceneLoaded(Scene arg0, LoadSceneMode arg1)
    {
        if (arg0.name == "Lobby") return; 
        
        readyAction.action.canceled -= OnReadyPerformed;
        readyAction.action.Disable();
        
        leaveAction.action.performed -= OnLeavePerformed;
        leaveAction.action.Disable();
        
        SceneManager.sceneLoaded -= SceneManagerOnsceneLoaded;
    }

    private void OnDisable()
    {
        readyAction.action.canceled -= OnReadyPerformed;
        readyAction.action.Disable();
        
        leaveAction.action.performed -= OnLeavePerformed;
        leaveAction.action.Disable();
    }

    private void OnReadyPerformed(InputAction.CallbackContext context)
    {
        InputDevice device = context.control.device;
        
        int playerIndex = LocalPlayerInputManager.Instance.AssignDeviceToNextFreePlayer(device);
        if (playerIndex == -1) return; // no free slot

        LobbyManager.instance.ToggleReadyLocal(playerIndex);
    }
    
    private void OnLeavePerformed(InputAction.CallbackContext context)
    {
        // Debug.Log("Leave performed");
        // LobbyManager.instance.RemoveLocalPlayer(context);
    }
}