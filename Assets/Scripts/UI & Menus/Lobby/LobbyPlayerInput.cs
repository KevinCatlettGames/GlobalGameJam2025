using FMOD.Studio;
using FMODUnity;
using System.Collections;
using System.Collections.Generic;
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

    private bool isQuitting;

    private void OnEnable()
    {
        playerInput = GetComponent<PlayerInput>();
        lobbyManager = LobbyManager.instance;
        lobbyButtons = LobbyManager.instance.GetComponent<LobbyButtons>();
        //lobbyPlayerValues = LobbyManager.instance.GetComponent <LobbyPlayerValues>();

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

    private void Start()
    {
        lobbyManager.ToggleReady(playerInput.playerIndex);
        LobbyPlayerValues.Instance.AssignDeviceToPlayer(playerInput.playerIndex, playerInput.devices[0]);

        foreach (GameObject playerContainer in lobbyManager.playerContainers)
            playerContainer.GetComponent<PlayerContainerSkinChange>().UpdateSkin();

        PlaySFX(joinReference);
    }

    public void OnConfirmed(InputAction.CallbackContext context)
    {
        if (isQuitting) return;
        if (!isActiveAndEnabled) return;

        if (lobbyManager.MatchSettingsSelection.activeSelf || lobbyButtons.confirmationPromptActive)
            return;

        if (context.performed && !LobbyManager.instance.players[playerInput.playerIndex].IsReady)
        {
            lobbyManager.ToggleReady(playerInput.playerIndex);

            foreach (GameObject playerContainer in lobbyManager.playerContainers)
                playerContainer.GetComponent<PlayerContainerSkinChange>().UpdateSkin();

            PlaySFX(readyReference);
        }     
    }

    public void OnCancelled(InputAction.CallbackContext context)
    {
        if (isQuitting) return;
        if (!isActiveAndEnabled) return;

        if (lobbyManager.MatchSettingsSelection.activeSelf || lobbyButtons.confirmationPromptActive)
            return;

        if (context.started && !lobbyManager.players[playerInput.playerIndex].IsReady)
        {
            lobbyButtons.HandleMainMenuInput(playerInput.playerIndex);
            return;
        }

        lobbyManager.ToggleReady(playerInput.playerIndex);
        foreach (GameObject playerContainer in lobbyManager.playerContainers)
            playerContainer.GetComponent<PlayerContainerSkinChange>().UpdateSkin();

        PlaySFX(unreadyReference);
    }

    private bool canNavigateSkins = true;

    public void OnSkinChange(InputAction.CallbackContext context)
    {
        if (isQuitting) return;
        if (!isActiveAndEnabled) return;

        if (lobbyManager.players[playerInput.playerIndex].IsReady || lobbyManager.MatchSettingsSelection.activeSelf || lobbyButtons.confirmationPromptActive)
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
        lobbyManager.playerContainers[GetComponent<PlayerInput>().playerIndex]
                .GetComponent<PlayerContainerSkinChange>()
                .ChangeSkin(context.ReadValue<Vector2>());

        PlaySFX(skinChangeReference);
    }

    private bool canNavigateTeam = true;

    public void OnTeamNavigation(InputAction.CallbackContext context)
    {
        if (isQuitting) return;
        if (!isActiveAndEnabled) return;
        if (lobbyManager.players[playerInput.playerIndex].IsReady || lobbyManager.MatchSettingsSelection.activeSelf || lobbyButtons.confirmationPromptActive)
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

        lobbyManager.playerContainers[playerInput.playerIndex]
                 .GetComponentInChildren<TeamSelection>()
                 .ChangeTeam();
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
        if (!isActiveAndEnabled) return;

        if (context.started)
            lobbyButtons.StartGameHold(playerInput.playerIndex);

        if (context.canceled)
            lobbyButtons.StopStartGameHold(playerInput.playerIndex);
    }
}