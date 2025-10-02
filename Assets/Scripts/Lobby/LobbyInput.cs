using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using Unity.Netcode;

/// <summary>
/// Handles player input in the lobby, including ready toggles and skin color changes.
/// Supports both local input and networked input using Unity Netcode and relay transport.
/// </summary>
public class LobbyInput : NetworkBehaviour
{
    /// <summary>
    /// Input action for toggling the ready state.
    /// </summary>
    public InputActionProperty readyAction;

    /// <summary>
    /// Input action for changing skin color to the right.
    /// </summary>
    public InputActionProperty rightColorChange;

    /// <summary>
    /// Input action for changing skin color to the left.
    /// </summary>
    public InputActionProperty leftColorChange;

    /// <summary>
    /// The index of the player associated with this input instance.
    /// </summary>
    public int playerIndex;

    /// <summary>
    /// Tracks the start times of ready button presses for each input device.
    /// </summary>
    private Dictionary<InputDevice, float> readyActionStartTimes = new();

    /// <summary>
    /// Threshold in seconds to consider a button press a "quick tap."
    /// </summary>
    private const float QuickTapThreshold = 0.2f;

    /// <summary>
    /// Start time of the ready button for the online host when using relay transport.
    /// </summary>
    private float onlineHostReadyStartTime = 0;

    /// <summary>
    /// The current callback context from the input system.
    /// </summary>
    private InputAction.CallbackContext currentContext;

    /// <summary>
    /// Unity OnEnable method. Subscribes to input actions and scene load events.
    /// </summary>
    private void OnEnable()
    {
        readyAction.action.started += OnReadyStarted;
        readyAction.action.performed += OnReadyPerformed;
        readyAction.action.Enable();

        rightColorChange.action.performed += OnRightColorChange;
        rightColorChange.action.Enable();
        
        leftColorChange.action.performed += OnLeftColorChange;
        leftColorChange.action.Enable();

        SceneManager.sceneLoaded += SceneManagerOnsceneLoaded;
    }

    /// <summary>
    /// Callback for when a new scene is loaded. Disables input actions if leaving the lobby scene.
    /// </summary>
    private void SceneManagerOnsceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "UI_Lobby") return;

        readyAction.action.started -= OnReadyStarted;
        readyAction.action.performed -= OnReadyPerformed;
        readyAction.action.Disable();

        rightColorChange.action.performed -= OnRightColorChange;
        rightColorChange.action.Disable();
        
        leftColorChange.action.performed -= OnLeftColorChange;
        leftColorChange.action.Disable();

        SceneManager.sceneLoaded -= SceneManagerOnsceneLoaded;
    }

    /// <summary>
    /// Unity OnDisable method. Unsubscribes from input actions to prevent memory leaks.
    /// </summary>
    private void OnDisable()
    {
        readyAction.action.started -= OnReadyStarted;
        readyAction.action.performed -= OnReadyPerformed;
        readyAction.action.Disable();

        rightColorChange.action.performed -= OnRightColorChange;
        rightColorChange.action.Disable();
        
        leftColorChange.action.performed -= OnLeftColorChange;
        leftColorChange.action.Disable();
    }

    /// <summary>
    /// Callback when the ready action starts. Records the press start time for quick tap detection.
    /// </summary>
    /// <param name="context">The input callback context.</param>
    private void OnReadyStarted(InputAction.CallbackContext context)
    {
        if (TransportSwitcher.Instance.isUsingRelay)
        {
            if (!NetworkManager.Singleton.IsServer) return;
            onlineHostReadyStartTime = Time.time;
        }
        else
        {
            InputDevice device = context.control.device;
            readyActionStartTimes[device] = Time.time;
        }
    }

    /// <summary>
    /// Callback when the ready action is performed. Toggles ready state and updates skins.
    /// </summary>
    /// <param name="context">The input callback context.</param>
    private void OnReadyPerformed(InputAction.CallbackContext context)
    {
        if (TransportSwitcher.Instance.isUsingRelay)
        {
            if (NetworkManager.Singleton.IsServer)
            {
                float duration = Time.time - onlineHostReadyStartTime;
                if (duration <= QuickTapThreshold)
                {
                    LobbyManager.instance.ToggleReadyServerRpc(NetworkManager.Singleton.LocalClientId);
                    onlineHostReadyStartTime = 0; 
                }
                
                foreach (GameObject playerContainer in LobbyManager.instance.playerContainers)
                {
                    if(playerContainer) 
                        playerContainer.GetComponent<PlayerContainerSkinChange>().UpdateSkinServerRpc();
                }
            }
            else
            {
                LobbyManager.instance.ToggleReadyServerRpc(NetworkManager.Singleton.LocalClientId);
                foreach (GameObject playerContainer in LobbyManager.instance.playerContainers)
                    playerContainer.GetComponent<PlayerContainerSkinChange>().UpdateSkinServerRpc();
            }
        }
        else
        {
            InputDevice device = context.control.device;

            if (!readyActionStartTimes.TryGetValue(device, out float startTime))
                return;

            float duration = Time.time - startTime;

            if (duration <= QuickTapThreshold)
            {
                int playerIndex = LobbyPlayerHandler.Instance.AssignDeviceToNextFreePlayer(device);
                if (playerIndex == -1) return; // no free slot

                LobbyManager.instance.ToggleReady(playerIndex);
                foreach (GameObject playerContainer in LobbyManager.instance.playerContainers)
                {
                    playerContainer.GetComponent<PlayerContainerSkinChange>().UpdateSkin();
                }
            }
            readyActionStartTimes.Remove(device);
        }
    }
    
    /// <summary>
    /// Callback for right color change input. Updates color locally or via network.
    /// </summary>
    /// <param name="context">The input callback context.</param>
    private void OnRightColorChange(InputAction.CallbackContext context)
    {
        if (TransportSwitcher.Instance.isUsingRelay)
            RightColorChangeServerRpc((int)NetworkManager.Singleton.LocalClientId);
        else
        {
            int playerIndex = LobbyPlayerHandler.Instance.GetPlayerIndex(context.control.device);
            if (LobbyManager.instance.players[playerIndex].IsReady) return;

            LobbyManager.instance.playerContainers[playerIndex]
                .GetComponent<PlayerContainerSkinChange>()
                .SwapColorWithIncrementation(true);
        }
    }

    /// <summary>
    /// Callback for left color change input. Updates color locally or via network.
    /// </summary>
    /// <param name="context">The input callback context.</param>
    private void OnLeftColorChange(InputAction.CallbackContext context)
    {
        if (TransportSwitcher.Instance.isUsingRelay)
            LeftColorChangeServerRpc((int)NetworkManager.Singleton.LocalClientId);
        else
        {
            int playerIndex = LobbyPlayerHandler.Instance.GetPlayerIndex(context.control.device);
            if (LobbyManager.instance.players[playerIndex].IsReady) return;

            LobbyManager.instance.playerContainers[playerIndex]
                .GetComponent<PlayerContainerSkinChange>()
                .SwapColorWithIncrementation(false);
        }
    }

    /// <summary>
    /// Server RPC to trigger right color change on clients.
    /// </summary>
    /// <param name="index">Player index to update.</param>
    [ServerRpc(RequireOwnership = false)]
    void RightColorChangeServerRpc(int index)
    {
        RightColorChangeClientRpc(index);
    }
    
    /// <summary>
    /// Server RPC to trigger left color change on clients.
    /// </summary>
    /// <param name="index">Player index to update.</param>
    [ServerRpc(RequireOwnership = false)]
    void LeftColorChangeServerRpc(int index)
    {
        LeftColorChangeClientRpc(index);
    }

    /// <summary>
    /// Client RPC to perform right color change locally.
    /// </summary>
    /// <param name="index">Player index to update.</param>
    [ClientRpc]
    void RightColorChangeClientRpc(int index)
    {
        if (LobbyManager.instance.players[index].IsReady) return;
        LobbyManager.instance.playerContainers[index]
            .GetComponent<PlayerContainerSkinChange>()
            .SwapColorWithIncrementation(true);
    }

    /// <summary>
    /// Client RPC to perform left color change locally.
    /// </summary>
    /// <param name="index">Player index to update.</param>
    [ClientRpc]
    void LeftColorChangeClientRpc(int index)
    {
        if (LobbyManager.instance.players[index].IsReady) return;
        LobbyManager.instance.playerContainers[index]
            .GetComponent<PlayerContainerSkinChange>()
            .SwapColorWithIncrementation(false);
    }
}