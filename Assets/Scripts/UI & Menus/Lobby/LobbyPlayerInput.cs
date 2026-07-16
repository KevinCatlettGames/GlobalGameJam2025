using FMOD.Studio;
using FMODUnity;
using Steamworks;
using System;
using System.Collections;
using System.Globalization;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.UI;

public class LobbyPlayerInput : NetworkBehaviour
{
    LobbyManager lobbyManager;
    LobbyButtons lobbyButtons;
    private PlayerInput playerInput;
    [SerializeField] EventReference[] eventReferences;
    private bool isQuitting;
    bool joined = false;
    public readonly NetworkVariable<int> playerIndex = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );
    [SerializeField] InputActionAsset switchInputActionAsset;
    private System.IDisposable anyButtonListener;

    public readonly NetworkVariable<ulong> networkSteamId = new NetworkVariable<ulong>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    public readonly NetworkVariable<ulong> ownerClientID = new NetworkVariable<ulong>(
    0,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Owner
);

    private void OnEnable()
    {
        playerInput = GetComponent<PlayerInput>();

#if UNITY_SWITCH && !UNITY_EDITOR
        playerInput.actions = switchInputActionAsset;
#endif

        lobbyManager = LobbyManager.instance;
        if (!lobbyManager.allLobbyPlayerInputs.Contains(this))
        {
            lobbyManager.allLobbyPlayerInputs.Add(this);
        }

        lobbyButtons = LobbyManager.instance.GetComponent<LobbyButtons>();
        lobbyManager.OnLeavingLobby.AddListener(DestroySelf);

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallback;               
    }

    private void Start()
    {
        if (TransportSwitcher.Instance && TransportSwitcher.Instance.isUsingRelay && IsServer && IsOwner)
        {
            StartCoroutine(WaitAndJoinOnlinePlayer());
        }
#if UNITY_SWITCH
        StartCoroutine(WaitAndJoinOnlinePlayer());
#endif
    }

    public void HandleJoinAfterValuesAreShared()
    {
        if(IsOwner)
        StartCoroutine(WaitAndJoinOnlinePlayer());
    }

    private IEnumerator WaitAndJoinOnlinePlayer()
    {
        playerInput.DeactivateInput();
        yield return new WaitForSeconds(.5f);
        InputAction.CallbackContext ctx = new InputAction.CallbackContext();
        OnJoined(ctx);
        playerInput.ActivateInput();
    }

    private void OnDisable()
    {
        if (lobbyManager != null)
            lobbyManager.OnLeavingLobby.RemoveListener(DestroySelf);

        if (TransportSwitcher.Instance && !TransportSwitcher.Instance.isUsingRelay) return;
        anyButtonListener?.Dispose();
    }

    public override void OnNetworkSpawn()
    {
        if (TransportSwitcher.Instance && !TransportSwitcher.Instance.isUsingRelay) return; 

        if (!IsOwner)
        {
            playerInput.enabled = false;
            return;
        }

        playerInput.enabled = false;
        if (playerInput.user.valid)
            playerInput.user.UnpairDevices();
        playerInput.enabled = true;

        if (IsOwner)
        {
            anyButtonListener = InputSystem.onEvent.Call(OnInputEventReceived);
            if (Steamworks.SteamClient.IsValid)
            {
                networkSteamId.Value = Steamworks.SteamClient.SteamId;
            }
            ownerClientID.Value = NetworkManager.Singleton.LocalClientId;
        }
    }

    private void OnInputEventReceived(InputEventPtr eventPtr)
    {
        // 1. We only care about events that contain actual input state data
        if (!eventPtr.IsA<StateEvent>() && !eventPtr.IsA<DeltaStateEvent>())
            return;

        // 2. Find which device sent this raw event
        InputDevice clickedDevice = InputSystem.GetDeviceById(eventPtr.deviceId);
        if (clickedDevice == null) return;

        // 3. Run your safety/ownership checks
        if (!IsOwner || isQuitting || !playerInput.enabled) return;

        // 4. Filter out Mouse, Touchscreen, and other non-physical device polling
        if (clickedDevice is Mouse || clickedDevice is Touchscreen) return;

        // 5. Check if we actually need to switch devices
        if (playerInput.devices.Count > 0 && playerInput.devices[0] == clickedDevice) return;

        // 6. We found a NEW active device! Run your pairing/switching logic
        playerInput.user.UnpairDevices();
        UnityEngine.InputSystem.Users.InputUser.PerformPairingWithDevice(clickedDevice, user: playerInput.user);

        if (clickedDevice is Gamepad)
            playerInput.user.ActivateControlScheme("Gamepad");
        else if (clickedDevice is Keyboard)
            playerInput.user.ActivateControlScheme("Keyboard");

        if (joined && playerIndex.Value != -1)
            LobbyPlayerValues.Instance.AssignDeviceToPlayer(playerIndex.Value, clickedDevice);
    }

    void OnClientConnectedCallback(ulong clientID)
    {
#if !UNITY_SWITCH
        UpdateSteamAccountInfoServerRpc(playerIndex.Value, true, GetSteamUserName(), networkSteamId.Value);
#endif
    }

    private void DestroySelf()
    {
        isQuitting = true;

        anyButtonListener?.Dispose();

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
        playerIndex.Value = -1;

        foreach (GameObject go in lobbyManager.playerContainers)
        {
            if (go.activeSelf)
                continue;
            else
            {
                playerIndex.Value = go.GetComponent<PlayerContainerManager>().uiIndex;
                if (!TransportSwitcher.Instance.isUsingRelay)
                    go.GetComponent<PlayerContainerManager>().occupied = true;
                else
                {
                    go.GetComponent<PlayerContainerManager>().ToggleYouText(true);
                    SetOccupiedPlayerContainerServerRpc(playerIndex.Value, true);

#if !UNITY_SWITCH
                    UpdateSteamAccountInfoServerRpc(playerIndex.Value, true, GetSteamUserName(), networkSteamId.Value);
#endif
                }

                break;
            }
        }

        if (playerIndex.Value == -1)
            return;

        if (!TransportSwitcher.Instance.isUsingRelay)
            lobbyManager.SetReady(playerIndex.Value, false);
        else
            lobbyManager.ToggleReadyServerRpc(playerIndex.Value, NetworkManager.Singleton.LocalClientId, false);

        if (playerInput.devices.Count > 0)
        {
            LobbyPlayerValues.Instance.AssignDeviceToPlayer(playerIndex.Value, playerInput.devices[0]);
        }

        foreach (GameObject playerContainer in lobbyManager.playerContainers)
        {
            if (playerContainer.GetComponent<PlayerContainerManager>().occupied)
                playerContainer.GetComponent<PlayerContainerSkinChange>().UpdateSkin();
        }

        PlaySFX(true, 1);
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

    [ServerRpc(RequireOwnership = false)]
    void UpdateSteamAccountInfoServerRpc(int playerID, bool isActive, string userName, ulong networkSteamID)
    {
        UpdateSteamAccountInfoClientRpc(playerID, isActive, userName, networkSteamID);
    }

    [ClientRpc]
    void UpdateSteamAccountInfoClientRpc(int playerID, bool isActive, string userName, ulong networkSteamID)
    {
        lobbyManager.playerContainers[playerID].GetComponent<PlayerContainerManager>().SetAccountInfo(isActive, userName, networkSteamID);
    }

    public void OnConfirmed(InputAction.CallbackContext context)
    {
        if (!joined) return;
        if (isQuitting) return;
        if (!isActiveAndEnabled) return;
        foreach (GameObject playerContainer in lobbyManager.playerContainers)
        {
            if (playerContainer.GetComponent<PlayerContainerManager>().uiIndex == playerIndex.Value && playerContainer.GetComponent<PlayerContainerSkinChange>().currentlyOnLocked)
            {
                PlaySFX(true, 3);
                return;
            }
        }
        if (lobbyManager._MatchSettingsSelection.activeSelf)
            return;

        int playersListID = -1;

        for (int i = 0; i < lobbyManager.players.Count; i++)
        {
            if (playerIndex.Value == lobbyManager.players[i].PlayerIndex)
                playersListID = i;
        }

        if (context.performed && !LobbyManager.instance.players[playersListID].IsReady)
        {
            if (SteamIntegration.instance && !SteamIntegration.instance.IsFullVersion && !LobbyPlayerValues.Instance.playerValuesList[playerIndex.Value].Skin.AvailableInDemo)
            {
                PlaySFX(false, 3);
                return;
            }

            if (!TransportSwitcher.Instance.isUsingRelay)
                lobbyManager.SetReady(playersListID, true);
            else
                lobbyManager.ToggleReadyServerRpc(playerIndex.Value, NetworkManager.Singleton.LocalClientId, true);

            foreach (GameObject playerContainer in lobbyManager.playerContainers)
            {
                if (playerContainer.activeSelf)
                    playerContainer.GetComponent<PlayerContainerSkinChange>().UpdateSkin();
            }

            PlaySFX(true, 2);
        }
    }

    public void OnCancelled(InputAction.CallbackContext context)
    {
        if (isQuitting) return;
        if (!isActiveAndEnabled) return;

        if (lobbyManager._MatchSettingsSelection.activeSelf)
        {
            PlaySFX(true, 3);
            lobbyManager._MatchSettingsSelection.SetActive(!lobbyManager._MatchSettingsSelection.activeSelf);
            return;
        }

        int playersListID = -1;

        for (int i = 0; i < lobbyManager.players.Count; i++)
        {
            if (playerIndex.Value == lobbyManager.players[i].PlayerIndex)
                playersListID = i;
        }

        if (joined && LobbyManager.instance.players[playersListID].IsReady)
        {
            if (!TransportSwitcher.Instance.isUsingRelay)
                lobbyManager.SetReady(playerIndex.Value, false);
            else
                lobbyManager.ToggleReadyServerRpc(playerIndex.Value, NetworkManager.Singleton.LocalClientId, false);

            PlaySFX(true, 3);
            return;
        }

        if (joined && !LobbyManager.instance.players[playersListID].IsReady)
        {
            if (context.started)
            {
                if (!TransportSwitcher.Instance.isUsingRelay)
                {
                    lobbyManager.playerContainers[playerIndex.Value].GetComponent<PlayerContainerSkinChange>().ResetContainer();
                    lobbyManager.playerContainers[playerIndex.Value].GetComponent<PlayerContainerManager>().occupied = false;
                }
                else
                {
                    lobbyManager.playerContainers[playerIndex.Value].GetComponent<PlayerContainerSkinChange>().ResetContainerServerRpc();
                    lobbyManager.playerContainers[playerIndex.Value].GetComponent<PlayerContainerManager>().ToggleYouText(false);

                    SetOccupiedPlayerContainerServerRpc(playerIndex.Value, false);
                    UpdateSteamAccountInfoServerRpc(playerIndex.Value, false, "Wizzo", networkSteamId.Value);
                    LobbyPlayerValues.Instance.RemovePlayerValueServerRpc(playerIndex.Value);
                }

                lobbyManager.RemovePlayer(playerIndex.Value);
                joined = false;
                lobbyManager.CheckAllReady();
                PlaySFX(true, 3);
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
        if (lobbyManager.players[playerIndex.Value].IsReady || lobbyManager._MatchSettingsSelection.activeSelf)
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
            lobbyManager.playerContainers[playerIndex.Value]
                .GetComponent<PlayerContainerSkinChange>()
                .ChangeSkin(context.ReadValue<Vector2>());
        else
        {

            lobbyManager.playerContainers[playerIndex.Value]
            .GetComponent<PlayerContainerSkinChange>()
            .ChangeSkin(context.ReadValue<Vector2>());

            lobbyManager.playerContainers[playerIndex.Value]
            .GetComponent<PlayerContainerSkinChange>()
            .ChangeSkinServerRpc(context.ReadValue<Vector2>(), NetworkManager.Singleton.LocalClientId);
        }

        PlaySFX(true, 0);

    }

    private bool canNavigateTeam = true;

    public void OnTeamNavigation(InputAction.CallbackContext context)
    {
        if (isQuitting) return;
        if (!joined) return;
        if (!isActiveAndEnabled) return;
        if (lobbyManager._MatchSettingsSelection.activeSelf)
            return;
        if (LobbyManager.instance.SelectedGameMode != GameManager.GameModeType.Team) return;

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
            lobbyManager.UpdateTeamServerRpc(playerIndex.Value);
        }
        else
        {
            lobbyManager.playerContainers[playerIndex.Value]
                     .GetComponentInChildren<TeamSelection>()
                     .ChangeTeam();
        }

        PlaySFX(true, 0);
    }

    private void PlaySFX(bool shareWithClients, int referenceID)
    {
        EventInstance fmodEvent = RuntimeManager.CreateInstance(eventReferences[referenceID]);
        RuntimeManager.AttachInstanceToGameObject(fmodEvent, transform);
        fmodEvent.start();
        fmodEvent.release();

        if(TransportSwitcher.Instance.isUsingRelay && shareWithClients)
        {
            PlaySFXServerRpc(playerIndex.Value, referenceID);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void PlaySFXServerRpc(int playerID, int referenceID)
    {
        PlaySFXClientRpc(playerID, referenceID);
    }


    [ClientRpc]
    private void PlaySFXClientRpc(int playerID, int referenceID)
    {
        if (playerIndex.Value == playerID) return; 

        EventInstance fmodEvent = RuntimeManager.CreateInstance(eventReferences[referenceID]);
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
            lobbyButtons.StartGameHold(playerIndex.Value);

        if (context.canceled)
            lobbyButtons.StopStartGameHold(playerIndex.Value);
    }

    public void OnToggleMatchSettings(InputAction.CallbackContext context)
    {
        if (isQuitting) return;
        if (!isActiveAndEnabled) return;
        if (TransportSwitcher.Instance && TransportSwitcher.Instance.isUsingRelay &&
            !NetworkManager.Singleton.IsServer) return;

        PlaySFX(false, 3);
        lobbyManager._MatchSettingsSelection.SetActive(!lobbyManager._MatchSettingsSelection.activeSelf);
    }

#if !UNITY_SWITCH
    string GetSteamUserName()
    {
        if (!SteamIntegration.instance || !SteamClient.IsValid)
        {
            Debug.Log("Steam not init");
            return "Wizzo";
        }
        string fullName = null;
        try
        {
            Friend friend = new Friend(networkSteamId.Value);
            fullName = friend.Name;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Steam] Failed to read SteamClient.Name on this platform: {ex.Message}");
            return "Wizzo";
        }
        if (string.IsNullOrWhiteSpace(fullName))
        {
            Debug.Log("Has only white spaces");
            return "Wizzo";
        }
        fullName = fullName.Trim();
        StringInfo stringInfo = new StringInfo(fullName);
        int visibleLength = stringInfo.LengthInTextElements;
        if (visibleLength <= 7)
        {
            return fullName;
        }
        string truncatedName = stringInfo.SubstringByTextElements(0, 7);
        if (truncatedName.Length > 0 && char.IsHighSurrogate(truncatedName[truncatedName.Length - 1]))
        {
            truncatedName = truncatedName.Substring(0, truncatedName.Length - 1);
        }
        return truncatedName + ".";
    }
#endif
}