using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine.Rendering;
using FMODUnity;

public class LobbyPlayerInput : NetworkBehaviour
{
    public InputActionProperty readyAction;
    public InputActionProperty unreadyAction;

    public InputActionProperty skinChange;
    public StudioEventEmitter skinChangeEmitter;
    public StudioEventEmitter unreadyEmitter;
    public InputActionProperty rightTeamChange;
    public InputActionProperty leftTeamChange;
    public LobbyButtons lobbyButtons;
    public int playerIndex;
    private Dictionary<InputDevice, float> readyActionStartTimes = new();
    private float onlineHostReadyStartTime = 0;
    private InputAction.CallbackContext currentContext;
    
    LobbyManager lobbyManager;

    private void OnEnable()
    {
        lobbyManager = LobbyManager.instance; 
        
        readyAction.action.started += OnReadyStarted;
        readyAction.action.performed += OnReadyPerformed;
        readyAction.action.Enable();

        unreadyAction.action.performed += OnUnreadyPerformed;
        unreadyAction.action.Enable();

        skinChange.action.started += OnSkinChange;
        skinChange.action.Enable();

        rightTeamChange.action.performed += OnRightTeamChange;
        rightTeamChange.action.Enable();

        leftTeamChange.action.performed += OnLeftTeamChange;
        leftTeamChange.action.Enable();

        SceneManager.sceneLoaded += SceneManagerOnsceneLoaded;
    }
    
    private void SceneManagerOnsceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "UI_Lobby") return;

        readyAction.action.started -= OnReadyStarted;
        readyAction.action.performed -= OnReadyPerformed;
        readyAction.action.Disable();

        unreadyAction.action.performed -= OnUnreadyPerformed;
        unreadyAction.action.Disable();

        skinChange.action.started -= OnSkinChange;
        skinChange.action.Disable();

        rightTeamChange.action.performed -= OnRightTeamChange;
        rightTeamChange.action.Disable();

        leftTeamChange.action.performed -= OnLeftTeamChange;
        leftTeamChange.action.Disable();

        SceneManager.sceneLoaded -= SceneManagerOnsceneLoaded;
    }
    
    private void OnDisable()
    {
        readyAction.action.started -= OnReadyStarted;
        readyAction.action.performed -= OnReadyPerformed;
        readyAction.action.Disable();

        unreadyAction.action.performed -= OnUnreadyPerformed;
        unreadyAction.action.Disable();

        skinChange.action.performed -= OnSkinChange;
        skinChange.action.Disable();

        rightTeamChange.action.performed -= OnRightTeamChange;
        rightTeamChange.action.Disable();

        leftTeamChange.action.performed -= OnLeftTeamChange;
        leftTeamChange.action.Disable();
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
        if (lobbyManager.MatchSettingsSelection.activeSelf)
            return;    

        if (TransportSwitcher.Instance.isUsingRelay)
        {
            if (NetworkManager.Singleton.IsServer)
            {
                float duration = Time.time - onlineHostReadyStartTime;
                if (!lobbyManager.players[(int)NetworkManager.Singleton.LocalClientId].IsReady)
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
                if (lobbyManager.players[(int)NetworkManager.Singleton.LocalClientId].IsReady) return;

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

            int playerIndex = LobbyPlayerValues.Instance.AssignDeviceToNextFreePlayer(device);
            if (playerIndex == -1) return;

            if (lobbyButtons.confirmationPromptActive)
            {
                lobbyButtons.OnBackPressed(playerIndex);
                return;
            }

            lobbyManager.ToggleReady(playerIndex);
            foreach (GameObject playerContainer in lobbyManager.playerContainers)
            {
                playerContainer.GetComponent<PlayerContainerSkinChange>().UpdateSkin();
            }
            readyActionStartTimes.Remove(device);
        }
    }

    private void OnUnreadyPerformed(InputAction.CallbackContext context)
    {
        bool deviceRegistered = false;
        foreach(LobbyPlayerValues.PlayerValues playerValues in LobbyPlayerValues.Instance.playerValuesList)
        {
            if(context.control.device == playerValues.Device)
            {
                deviceRegistered = true;
            }
        }
        if (!deviceRegistered) return;

        if (TransportSwitcher.Instance.isUsingRelay)
        {
            if (NetworkManager.Singleton.IsServer)
            {
                if (!LobbyManager.instance.players[(int)NetworkManager.Singleton.LocalClientId].IsReady) return;
                lobbyManager.ToggleReadyServerRpc(NetworkManager.Singleton.LocalClientId);    

                foreach (GameObject playerContainer in lobbyManager.playerContainers)
                {
                    if (playerContainer)
                        playerContainer.GetComponent<PlayerContainerSkinChange>().UpdateSkinServerRpc();
                }
            }
            else
            {
                if (!LobbyManager.instance.players[(int)NetworkManager.Singleton.LocalClientId].IsReady) return;
                lobbyManager.ToggleReadyServerRpc(NetworkManager.Singleton.LocalClientId);
                foreach (GameObject playerContainer in lobbyManager.playerContainers)
                    playerContainer.GetComponent<PlayerContainerSkinChange>().UpdateSkinServerRpc();
            }
        }
        else
        {
            InputDevice device = context.control.device;      
            int playerIndex = LobbyPlayerValues.Instance.AssignDeviceToNextFreePlayer(device);
            if (playerIndex == -1) return;
            if (!LobbyManager.instance.players[playerIndex].IsReady)
            {
                lobbyButtons.OnBackPressed(playerIndex);
                return;
            }
            lobbyManager.ToggleReady(playerIndex);
            foreach (GameObject playerContainer in lobbyManager.playerContainers)
            {
                playerContainer.GetComponent<PlayerContainerSkinChange>().UpdateSkin();
            }
            unreadyEmitter.Play();
        }
    }

    private void OnSkinChange(InputAction.CallbackContext context)
    {
        if (lobbyButtons.confirmationPromptActive)
            return;

        if ((TransportSwitcher.Instance.isUsingRelay && IsHost) || !TransportSwitcher.Instance.isUsingRelay)
        {
            if (lobbyManager.MatchSettingsSelection.activeSelf)
                return;
        }

        if (lobbyManager.playerContainers.Length <= 0)
            return;

        int playerIndex = LobbyPlayerValues.Instance.GetPlayerIndex(context.control.device);

        if (playerIndex < 0 || playerIndex >= lobbyManager.players.Count)
            return;

        if (lobbyManager.players[playerIndex].IsReady)
            return;

        lobbyManager.playerContainers[playerIndex]
            .GetComponent<PlayerContainerSkinChange>()
            .ChangeSkin(context.ReadValue<Vector2>());

        skinChangeEmitter.Play();
    }

    private void OnRightTeamChange(InputAction.CallbackContext context)
    {
        if (TransportSwitcher.Instance.isUsingRelay && IsHost || !TransportSwitcher.Instance.isUsingRelay)
        {
            if (lobbyManager.MatchSettingsSelection.activeSelf)
                return;
        }

        if (LobbyManager.instance.SelectedGameMode != GameManager.GameModeType.Team) return;
        
        int playerIndex = LobbyPlayerValues.Instance.GetPlayerIndex(context.control.device);

        lobbyManager.playerContainers[playerIndex]
            .GetComponentInChildren<TeamSelection>()
            .ChangeTeam();
    }
    
    private void OnLeftTeamChange(InputAction.CallbackContext context)
    {
        if (TransportSwitcher.Instance.isUsingRelay && IsHost || !TransportSwitcher.Instance.isUsingRelay)
        {
            if (lobbyManager.MatchSettingsSelection.activeSelf)
                return;
        }

        if (LobbyManager.instance.SelectedGameMode != GameManager.GameModeType.Team) return;
        
        int playerIndex = LobbyPlayerValues.Instance.GetPlayerIndex(context.control.device);

            lobbyManager.playerContainers[playerIndex]
                .GetComponentInChildren<TeamSelection>()
                .ChangeTeam();
    }
}