using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using Unity.Netcode;

public class LobbyInput : NetworkBehaviour
{
    public InputActionProperty readyAction;
    public InputActionProperty rightColorChange;
    public InputActionProperty leftColorChange;
    public int playerIndex;
    private Dictionary<InputDevice, float> readyActionStartTimes = new();
    private const float QuickTapThreshold = 0.2f;
    private float onlineHostReadyStartTime = 0;
    private InputAction.CallbackContext currentContext;
    
    LobbyManager lobbyManager;
    
    private void OnEnable()
    {
        lobbyManager = LobbyManager.instance; 
        
        readyAction.action.started += OnReadyStarted;
        readyAction.action.performed += OnReadyPerformed;
        readyAction.action.Enable();

        rightColorChange.action.performed += OnRightColorChange;
        rightColorChange.action.Enable();
        
        leftColorChange.action.performed += OnLeftColorChange;
        leftColorChange.action.Enable();

        SceneManager.sceneLoaded += SceneManagerOnsceneLoaded;
    }
    
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
    
    private void OnReadyPerformed(InputAction.CallbackContext context)
    {
        if (lobbyManager.GameModeSelection.activeSelf || lobbyManager.MatchSettingsSelection.activeSelf)
            return;
        
        if (TransportSwitcher.Instance.isUsingRelay)
        {
            if (NetworkManager.Singleton.IsServer)
            {
                float duration = Time.time - onlineHostReadyStartTime;
                if (duration <= QuickTapThreshold)
                {
                    lobbyManager.ToggleReadyServerRpc(NetworkManager.Singleton.LocalClientId);
                    onlineHostReadyStartTime = 0; 
                }
                
                foreach (GameObject playerContainer in lobbyManager.playerContainers)
                {
                    if(playerContainer) 
                        playerContainer.GetComponent<PlayerContainerSkinChange>().UpdateSkinServerRpc();
                }
            }
            else
            {
                lobbyManager.ToggleReadyServerRpc(NetworkManager.Singleton.LocalClientId);
                foreach (GameObject playerContainer in lobbyManager.playerContainers)
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
                if (playerIndex == -1) return;

                lobbyManager.ToggleReady(playerIndex);
                foreach (GameObject playerContainer in lobbyManager.playerContainers)
                {
                    playerContainer.GetComponent<PlayerContainerSkinChange>().UpdateSkin();
                }
            }
            readyActionStartTimes.Remove(device);
        }
    }

    private void OnRightColorChange(InputAction.CallbackContext context)
    {
        if (TransportSwitcher.Instance.isUsingRelay && IsHost || !TransportSwitcher.Instance.isUsingRelay)
        {
            if (lobbyManager.GameModeSelection.activeSelf || lobbyManager.MatchSettingsSelection.activeSelf)
                return;
        }
        
        if (TransportSwitcher.Instance.isUsingRelay)
            RightColorChangeServerRpc((int)NetworkManager.Singleton.LocalClientId);
        else
        {
            int playerIndex = LobbyPlayerHandler.Instance.GetPlayerIndex(context.control.device);
            if (lobbyManager.players[playerIndex].IsReady) return;

            lobbyManager.playerContainers[playerIndex]
                .GetComponent<PlayerContainerSkinChange>()
                .SwapColorWithIncrementation(true);
        }
    }
    
    private void OnLeftColorChange(InputAction.CallbackContext context)
    {
        if (TransportSwitcher.Instance.isUsingRelay && IsHost || !TransportSwitcher.Instance.isUsingRelay)
        {
            if (lobbyManager.GameModeSelection.activeSelf || lobbyManager.MatchSettingsSelection.activeSelf)
                return;
        }
        
        if (TransportSwitcher.Instance.isUsingRelay)
            LeftColorChangeServerRpc((int)NetworkManager.Singleton.LocalClientId);
        else
        {
            int playerIndex = LobbyPlayerHandler.Instance.GetPlayerIndex(context.control.device);
            if (lobbyManager.players[playerIndex].IsReady) return;

            lobbyManager.playerContainers[playerIndex]
                .GetComponent<PlayerContainerSkinChange>()
                .SwapColorWithIncrementation(false);
        }
    }
    
    [ServerRpc(RequireOwnership = false)]
    void RightColorChangeServerRpc(int index)
    {
        RightColorChangeClientRpc(index);
    }
    
    [ServerRpc(RequireOwnership = false)]
    void LeftColorChangeServerRpc(int index)
    {
        LeftColorChangeClientRpc(index);
    }
    
    [ClientRpc]
    void RightColorChangeClientRpc(int index)
    {
        if (lobbyManager.players[index].IsReady) return;
        lobbyManager.playerContainers[index]
            .GetComponent<PlayerContainerSkinChange>()
            .SwapColorWithIncrementation(true);
    }
    
    [ClientRpc]
    void LeftColorChangeClientRpc(int index)
    {
        if (lobbyManager.players[index].IsReady) return;
        lobbyManager.playerContainers[index]
            .GetComponent<PlayerContainerSkinChange>()
            .SwapColorWithIncrementation(false);
    }
}