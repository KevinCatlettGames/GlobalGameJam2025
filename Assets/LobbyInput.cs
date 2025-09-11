using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class LobbyInput : MonoBehaviour
{
    public InputActionProperty readyAction;
    public InputActionProperty leaveAction;
    public int playerIndex; 
    
    private void OnEnable()
    {
        readyAction.action.performed += OnReadyPerformed;
        readyAction.action.Enable();
        
        leaveAction.action.performed += OnLeavePerformed;
        leaveAction.action.Enable();
    }

    private void OnDisable()
    {
        readyAction.action.performed -= OnReadyPerformed;
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
        Debug.Log("Leave performed");
        LobbyManager.instance.RemoveLocalPlayer(context);
    }
}