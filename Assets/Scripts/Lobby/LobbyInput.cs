using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class LobbyInput : MonoBehaviour
{
    public InputActionProperty readyAction;
    public InputActionProperty leaveAction;
    public InputActionProperty rightColorChange;
    public InputActionProperty leftColorChange;

    public int playerIndex;

    private Dictionary<InputDevice, float> readyActionStartTimes = new();
    private const float QuickTapThreshold = 0.2f; // seconds

    private void OnEnable()
    {
        readyAction.action.started += OnReadyStarted;
        readyAction.action.performed += OnReadyPerformed;
        readyAction.action.Enable();

        leaveAction.action.performed += OnLeavePerformed;
        leaveAction.action.Enable();

        rightColorChange.action.performed += OnRightColorChange;
        rightColorChange.action.Enable();
        
        leftColorChange.action.performed += OnLeftColorChange;
        leftColorChange.action.Enable();

        SceneManager.sceneLoaded += SceneManagerOnsceneLoaded;
    }

    private void SceneManagerOnsceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Lobby") return;

        readyAction.action.started -= OnReadyStarted;
        readyAction.action.performed -= OnReadyPerformed;
        readyAction.action.Disable();

        leaveAction.action.performed -= OnLeavePerformed;
        leaveAction.action.Disable();

        rightColorChange.action.performed -= OnRightColorChange;
        rightColorChange.action.Disable();
        
        leftColorChange.action.performed -= OnLeftColorChange;
        leftColorChange.action.Disable();

        SceneManager.sceneLoaded -= SceneManagerOnsceneLoaded;
    }

    private void OnDisable()
    {
        readyAction.action.started -= OnReadyStarted;
        readyAction.action.performed -= OnReadyPerformed;
        readyAction.action.Disable();

        leaveAction.action.performed -= OnLeavePerformed;
        leaveAction.action.Disable();

        rightColorChange.action.performed -= OnRightColorChange;
        rightColorChange.action.Disable();
        
        leftColorChange.action.performed -= OnLeftColorChange;
        leftColorChange.action.Disable();
    }

    private void OnReadyStarted(InputAction.CallbackContext context)
    {
        InputDevice device = context.control.device;
        readyActionStartTimes[device] = Time.time;
    }

    private void OnReadyPerformed(InputAction.CallbackContext context)
    {
        InputDevice device = context.control.device;

        if (!readyActionStartTimes.TryGetValue(device, out float startTime))
            return;

        float duration = Time.time - startTime;

        if (duration <= QuickTapThreshold)
        {
            int playerIndex = LobbyPlayerHandler.Instance.AssignDeviceToNextFreePlayer(device);
            if (playerIndex == -1) return; // no free slot

            LobbyManager.instance.ToggleReadyLocal(playerIndex);
            foreach (GameObject playerContainer in LobbyManager.instance.playerContainers)
            {
                playerContainer.GetComponent<PlayerContainerSkinChange>().RecheckSkinValidity();
            }
        }

        readyActionStartTimes.Remove(device);
    }

    private void OnLeavePerformed(InputAction.CallbackContext context)
    {
        // Implement your leave logic here
        // LobbyManager.instance.RemoveLocalPlayer(context);
    }

    private void OnRightColorChange(InputAction.CallbackContext context)
    {
        int playerIndex = LobbyPlayerHandler.Instance.GetPlayerIndex(context.control.device);
        if (LobbyManager.instance.players[playerIndex].IsReady) return; 
        
        LobbyManager.instance.playerContainers[playerIndex]
            .GetComponent<PlayerContainerSkinChange>()
            .SwapColorWithIncrementation(true);
    }

    private void OnLeftColorChange(InputAction.CallbackContext context)
    {
        int playerIndex = LobbyPlayerHandler.Instance.GetPlayerIndex(context.control.device);
        if (LobbyManager.instance.players[playerIndex].IsReady) return; 
        
        LobbyManager.instance.playerContainers[playerIndex]
            .GetComponent<PlayerContainerSkinChange>()
            .SwapColorWithIncrementation(false);
    }
}
