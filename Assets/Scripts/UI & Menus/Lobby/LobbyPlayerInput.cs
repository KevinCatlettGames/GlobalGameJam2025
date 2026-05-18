using FMOD.Studio;
using FMODUnity;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class LobbyPlayerInput : MonoBehaviour
{  
    LobbyManager lobbyManager;
    LobbyButtons lobbyButtons;
    //LobbyPlayerValues lobbyPlayerValues;
    private PlayerInput playerInput;

    [SerializeField] EventReference skinChangeReference;
    [SerializeField] EventReference joinReference;
    [SerializeField] EventReference readyReference;
    [SerializeField] EventReference unreadyReference;
    [SerializeField] EventReference buttonReference;
    bool firstJoined = true;
    private bool isQuitting;
    bool joined = false;
    int lobbyPlayerInputIndex = -1;

    private void OnEnable()
    {
        playerInput = GetComponent<PlayerInput>();
      
        lobbyManager = LobbyManager.instance;
        if(!lobbyManager.allLobbyPlayerInputs.Contains(this))
        {
            lobbyManager.allLobbyPlayerInputs.Add(this);
        }

        lobbyButtons = LobbyManager.instance.GetComponent<LobbyButtons>();
        lobbyManager.OnLeavingLobby.AddListener(DestroySelf);    
    }

    private void OnDisable()
    {
        if (lobbyManager != null)
            lobbyManager.OnLeavingLobby.RemoveListener(DestroySelf);
    }

    private void DestroySelf()
    {
        isQuitting = true;

        if (playerInput != null)
            playerInput.enabled = false;

        StartCoroutine(DestroyNextFrame());
    }

    private IEnumerator DestroyNextFrame()
    {
        yield return null;
        Destroy(gameObject);
    }

    public void OnJoined(InputAction.CallbackContext context)
    {
        if (joined) return;
        lobbyPlayerInputIndex = -1;
        foreach (GameObject go in lobbyManager.playerContainers)
        {
            if (go.activeSelf)
                continue;
            else
            {
                lobbyPlayerInputIndex = go.GetComponent<PlayerContainerManager>().uiIndex;
                go.GetComponent<PlayerContainerManager>().occupied = true;
                break;
            }
        }

        if (lobbyPlayerInputIndex == -1)
            return;

        lobbyManager.SetReady(lobbyPlayerInputIndex, false);
        LobbyPlayerValues.Instance.AssignDeviceToPlayer(lobbyPlayerInputIndex, playerInput.devices[0]);

        foreach (GameObject playerContainer in lobbyManager.playerContainers)
        {
            if(playerContainer.GetComponent<PlayerContainerManager>().occupied)
                playerContainer.GetComponent<PlayerContainerSkinChange>().UpdateSkin();
        }

        PlaySFX(joinReference);
        joined = true;
    }

    public void OnConfirmed(InputAction.CallbackContext context)
    {
        if (!joined) return;
        if (isQuitting) return;
        if (!isActiveAndEnabled) return;

        if (lobbyManager.MatchSettingsSelection.activeSelf)
            return;

        if (context.performed && !LobbyManager.instance.players[lobbyPlayerInputIndex].IsReady)
        {
            lobbyManager.SetReady(lobbyPlayerInputIndex, true);

            foreach (GameObject playerContainer in lobbyManager.playerContainers)
                playerContainer.GetComponent<PlayerContainerSkinChange>().UpdateSkin();

            PlaySFX(readyReference);
        }     
    }

    public void OnCancelled(InputAction.CallbackContext context)
    {
        if (isQuitting) return;
        if (!isActiveAndEnabled) return;

        if (lobbyManager.MatchSettingsSelection.activeSelf)
        {
            PlaySFX(buttonReference);
            lobbyManager.MatchSettingsSelection.SetActive(!lobbyManager.MatchSettingsSelection.activeSelf);
            return;
        }

        if (joined && LobbyManager.instance.players[lobbyPlayerInputIndex].IsReady)
        {
            lobbyManager.SetReady(lobbyPlayerInputIndex, false);
            return;
        }

        if (joined && !LobbyManager.instance.players[lobbyPlayerInputIndex].IsReady)
        {
            if (context.started)
            {
                // remove here
                lobbyManager.playerContainers[lobbyPlayerInputIndex].GetComponent<PlayerContainerSkinChange>().ResetContainer();
                lobbyManager.playerContainers[lobbyPlayerInputIndex].GetComponent<PlayerContainerManager>().occupied = false;
                lobbyManager.RemovePlayer(lobbyPlayerInputIndex);
                joined = false;
                lobbyManager.CheckAllReady();
                LobbyPlayerValues.Instance.playerValuesList[lobbyPlayerInputIndex].Device = null;
            }
            return;
        }

        if (!joined)
        {
            if (context.started)
                lobbyButtons.OnBackToMenuButtonDown();
            else if (context.canceled)
                lobbyButtons.OnBackToMenuButtonUp();
        }

            foreach (GameObject playerContainer in lobbyManager.playerContainers)
                playerContainer.GetComponent<PlayerContainerSkinChange>().UpdateSkin();

        PlaySFX(unreadyReference);
    }

    private bool canNavigateSkins = true;

    public void OnSkinChange(InputAction.CallbackContext context)
    {
        if (isQuitting) return;
        if (!joined) return;
        if (!isActiveAndEnabled) return;

        if (lobbyManager.players[lobbyPlayerInputIndex].IsReady || lobbyManager.MatchSettingsSelection.activeSelf)
            return;

        Vector2 input = context.ReadValue<Vector2>();

        if (input.magnitude < 0.5f || input.x == 0 && input.y == 0)
        {
            canNavigateSkins = true;
            return;
        }

        if (!canNavigateSkins)
            return;

        canNavigateSkins = false;
        lobbyManager.playerContainers[lobbyPlayerInputIndex]
                .GetComponent<PlayerContainerSkinChange>()
                .ChangeSkin(context.ReadValue<Vector2>());

        PlaySFX(skinChangeReference);
    }

    private bool canNavigateTeam = true;

    public void OnTeamNavigation(InputAction.CallbackContext context)
    {
        if (isQuitting) return;
        if (!joined) return;
        if (!isActiveAndEnabled) return;
        if (lobbyManager.players[playerInput.playerIndex].IsReady || lobbyManager.MatchSettingsSelection.activeSelf)
            return;
        if(LobbyManager.instance.SelectedGameMode != GameManager.GameModeType.Team) return;

        Vector2 input = context.ReadValue<Vector2>();

        if (input.magnitude < 0.5f || input.x == 0 && input.y == 0)
        {
            canNavigateTeam = true;
            return;
        }

        if (!canNavigateTeam)
            return;

        canNavigateTeam = false;

        lobbyManager.playerContainers[lobbyPlayerInputIndex]
                 .GetComponentInChildren<TeamSelection>()
                 .ChangeTeam();

        PlaySFX(skinChangeReference);
    }

    private void PlaySFX(EventReference eventReference)
    {
        EventInstance fmodEvent = RuntimeManager.CreateInstance(eventReference);
        RuntimeManager.AttachInstanceToGameObject(fmodEvent, transform);
        fmodEvent.start();
        fmodEvent.release();
    }

    public void OnStartGame(InputAction.CallbackContext context)
    {
        if (isQuitting) return;
        if (!joined) return;
        if (!isActiveAndEnabled) return;
        if (lobbyManager.MatchSettingsSelection.activeSelf) return;

        if (context.started)
            lobbyButtons.StartGameHold(lobbyPlayerInputIndex);

        if (context.canceled)
            lobbyButtons.StopStartGameHold(lobbyPlayerInputIndex);
    }

    public void OnToggleMatchSettings(InputAction.CallbackContext context)
    {
        if (isQuitting) return;
        if (!joined) return;
        if (!isActiveAndEnabled) return;
        if (!SteamIntegration.instance.IsFullVersion) return;
        if (TransportSwitcher.Instance && TransportSwitcher.Instance.isUsingRelay &&
            !NetworkManager.Singleton.IsServer) return;

        PlaySFX(buttonReference);
        lobbyManager.MatchSettingsSelection.SetActive(!lobbyManager.MatchSettingsSelection.activeSelf);
    }
}