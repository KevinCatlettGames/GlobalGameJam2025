using FMOD.Studio;
using FMODUnity;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class LobbyPlayerInput : NetworkBehaviour
{  
    LobbyManager lobbyManager;
    LobbyButtons lobbyButtons;
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
        if (lobbyManager._MatchSettingsSelection.activeSelf) return;
        lobbyPlayerInputIndex = -1;

        if (!TransportSwitcher.Instance.isUsingRelay)
        {
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
        }
        else
        {
            lobbyPlayerInputIndex = (int)NetworkManager.Singleton.LocalClientId;
            SetOccupiedPlayerContainerServerRpc(lobbyPlayerInputIndex, true);
        }

        if (!TransportSwitcher.Instance.isUsingRelay)
            lobbyManager.SetReady(lobbyPlayerInputIndex, false);
        else
            lobbyManager.ToggleReadyServerRpc((ulong)lobbyPlayerInputIndex, false);

        LobbyPlayerValues.Instance.AssignDeviceToPlayer(lobbyPlayerInputIndex, playerInput.devices[0]);

        foreach (GameObject playerContainer in lobbyManager.playerContainers)
        {
            if(playerContainer.GetComponent<PlayerContainerManager>().occupied)
                playerContainer.GetComponent<PlayerContainerSkinChange>().UpdateSkin();
        }

        PlaySFX(joinReference);
        joined = true;
    }

    [ServerRpc(RequireOwnership = false)]
    void SetOccupiedPlayerContainerServerRpc(int id, bool value)
    {
        SetOccupiedPlayerContainerClientRpc(id, value);
    }

    [ClientRpc]
    void SetOccupiedPlayerContainerClientRpc(int id, bool value)
    {
        lobbyManager.playerContainers[id].GetComponent<PlayerContainerManager>().occupied = value;
    }

    public void OnConfirmed(InputAction.CallbackContext context)
    {
        if (!joined) return;
        if (isQuitting) return;
        if (!isActiveAndEnabled) return;
        foreach (GameObject playerContainer in lobbyManager.playerContainers)
        {
            if (playerContainer.GetComponent<PlayerContainerManager>().uiIndex == lobbyPlayerInputIndex && playerContainer.GetComponent<PlayerContainerSkinChange>().currentlyOnLocked)
            {
                PlaySFX(buttonReference);
                return;
            }
        }
        if (lobbyManager._MatchSettingsSelection.activeSelf)
            return;


        if (context.performed && !LobbyManager.instance.players[lobbyPlayerInputIndex].IsReady)
        {

            if (!TransportSwitcher.Instance.isUsingRelay)
                lobbyManager.SetReady(lobbyPlayerInputIndex, true);
            else
                lobbyManager.ToggleReadyServerRpc((ulong)lobbyPlayerInputIndex, true);

            foreach (GameObject playerContainer in lobbyManager.playerContainers)
                playerContainer.GetComponent<PlayerContainerSkinChange>().UpdateSkin();

            PlaySFX(readyReference);
        }
    }

    public void OnCancelled(InputAction.CallbackContext context)
    {
        if (isQuitting) return;
        if (!isActiveAndEnabled) return;

        if (lobbyManager._MatchSettingsSelection.activeSelf)
        {
            PlaySFX(buttonReference);
            lobbyManager._MatchSettingsSelection.SetActive(!lobbyManager._MatchSettingsSelection.activeSelf);
            return;
        }

        if (joined && LobbyManager.instance.players[lobbyPlayerInputIndex].IsReady)
        {
            if (!TransportSwitcher.Instance.isUsingRelay)
                lobbyManager.SetReady(lobbyPlayerInputIndex, false);
            else
                lobbyManager.ToggleReadyServerRpc((ulong)lobbyPlayerInputIndex, false);

            PlaySFX(buttonReference);
            return;
        }

        if (joined && !LobbyManager.instance.players[lobbyPlayerInputIndex].IsReady)
        {
            if (context.started)
            {
                if (!TransportSwitcher.Instance.isUsingRelay)
                {
                    lobbyManager.playerContainers[lobbyPlayerInputIndex].GetComponent<PlayerContainerSkinChange>().ResetContainer();
                    lobbyManager.playerContainers[lobbyPlayerInputIndex].GetComponent<PlayerContainerManager>().occupied = false;    
                }
                else
                {
                    lobbyManager.playerContainers[lobbyPlayerInputIndex].GetComponent<PlayerContainerSkinChange>().ResetContainerServerRpc();
                    SetOccupiedPlayerContainerServerRpc(lobbyPlayerInputIndex, false);
                    LobbyPlayerValues.Instance.RemovePlayerValueServerRpc(lobbyPlayerInputIndex);
                }

                lobbyManager.RemovePlayer(lobbyPlayerInputIndex);

                joined = false;
                lobbyManager.CheckAllReady();
                LobbyPlayerValues.Instance.playerValuesList[lobbyPlayerInputIndex].Device = null;
                PlaySFX(buttonReference);
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
    }

    private bool canNavigateSkins = true;

    public void OnSkinChange(InputAction.CallbackContext context)
    {
        if (isQuitting) return;
        if (!joined) return;
        if (!isActiveAndEnabled) return;
        if (lobbyManager.players[lobbyPlayerInputIndex].IsReady || lobbyManager._MatchSettingsSelection.activeSelf)
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

        if (!TransportSwitcher.Instance.isUsingRelay)
            lobbyManager.playerContainers[lobbyPlayerInputIndex]
                .GetComponent<PlayerContainerSkinChange>()
                .ChangeSkin(context.ReadValue<Vector2>());
        else
        {
            lobbyManager.playerContainers[lobbyPlayerInputIndex]
                .GetComponent<PlayerContainerSkinChange>()
                .ChangeSkin(context.ReadValue<Vector2>());

            lobbyManager.playerContainers[lobbyPlayerInputIndex]
            .GetComponent<PlayerContainerSkinChange>()
            .ChangeSkinServerRpc(context.ReadValue<Vector2>());
        }

        PlaySFX(skinChangeReference);
    }

    private bool canNavigateTeam = true;

    public void OnTeamNavigation(InputAction.CallbackContext context)
    {
        if (isQuitting) return;
        if (!joined) return;
        if (!isActiveAndEnabled) return;
        if (lobbyManager._MatchSettingsSelection.activeSelf)
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
        if (TransportSwitcher.Instance.isUsingRelay)
        {
            lobbyManager.UpdateTeamServerRpc(lobbyPlayerInputIndex);
        }
        else
        {
            lobbyManager.playerContainers[lobbyPlayerInputIndex]
                     .GetComponentInChildren<TeamSelection>()
                     .ChangeTeam();
        }

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
        if (lobbyManager._MatchSettingsSelection.activeSelf) return;

        if (context.started)
            lobbyButtons.StartGameHold(lobbyPlayerInputIndex);

        if (context.canceled)
            lobbyButtons.StopStartGameHold(lobbyPlayerInputIndex);
    }

    public void OnToggleMatchSettings(InputAction.CallbackContext context)
    {
        if (isQuitting) return;
        if (!isActiveAndEnabled) return;
        if (TransportSwitcher.Instance && TransportSwitcher.Instance.isUsingRelay &&
            !NetworkManager.Singleton.IsServer) return;

        PlaySFX(buttonReference);
        lobbyManager._MatchSettingsSelection.SetActive(!lobbyManager._MatchSettingsSelection.activeSelf);
    }
}