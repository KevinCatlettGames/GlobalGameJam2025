using FMODUnity;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using UnityEngine;
using UnityEngine.UI;

public class LobbyButtons : MonoBehaviour
{
    public Image startGameRadialFillImage;
    public Image backRadialFillImage;
    private bool isLeaving;
    public GameObject mainMenuButton;

    [SerializeField] private float startGameHoldDuration = 1f;
    [SerializeField] private float backHoldDuration = 1f;

    private float startGamePressTime;
    private float backPressTime;

    private HashSet<int> playersHoldingStart = new();
    private HashSet<int> playersHoldingBack = new();

    private bool gameStarting;

    private bool startEmitting;
    private bool backEmitting;

    [Tooltip("Audio emitter for start game hold progress")]
    public StudioEventEmitter startProgressEmitter;

    [Tooltip("Audio emitter for main menu hold progress")]
    public StudioEventEmitter mainMenuProgressEmitter;

    [Tooltip("Audio emitter for button click feedback")]
    public StudioEventEmitter buttonOnClickEmitter;

    private LobbyManager lobbyManager;

    [Tooltip("Parent object for the lobby UI")]
    public GameObject lobbyParent;

    private void OnEnable()
    {
        lobbyManager = LobbyManager.instance;

        ResetStartRadial();

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
            NetworkManager.Singleton.OnServerStopped += OnServerStopped;
        }
    }

    private void OnServerStopped(bool obj)
    {
        LeaveToMainMenu();
    }

    private void OnDisable()
    {
        LeaveToMainMenu();

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
            NetworkManager.Singleton.OnServerStopped -= OnServerStopped;
        }
    }

    private void Update()
    {
        HandleStartGameHold();
        HandleBackHold();
    }

    private void HandleStartGameHold()
    {
        if (playersHoldingStart.Count == 0 || gameStarting)
            return;

        float heldTime = Time.time - startGamePressTime;
        float progress = Mathf.Clamp01(heldTime / startGameHoldDuration);

        if (progress > 0.1f && !startEmitting)
        {
            startEmitting = true;
            startProgressEmitter.Play();
        }

        if (startGameRadialFillImage != null)
            startGameRadialFillImage.fillAmount = progress;

        if (heldTime >= startGameHoldDuration)
        {
            gameStarting = true;
            ResetStartRadial();
            StartCoroutine(lobbyManager.LoadGameScene());
        }
    }

    private void HandleBackHold()
    {
        if (playersHoldingBack.Count == 0 || gameStarting || isLeaving)
            return;

        float heldTime = Time.time - backPressTime;
        float progress = Mathf.Clamp01(heldTime / backHoldDuration);

        if (progress > 0.1f && !backEmitting)
        {
            backEmitting = true;
            mainMenuProgressEmitter.Play();
        }

        if (backRadialFillImage != null)
            backRadialFillImage.fillAmount = progress;

        if (heldTime >= backHoldDuration)
        {
            isLeaving = true;
            ResetBackRadial();

            buttonOnClickEmitter?.Play();
            GoToMainMenu();
        }
    }

    private void ResetStartRadial()
    {
        if (startGameRadialFillImage != null)
            startGameRadialFillImage.fillAmount = 0f;

        startEmitting = false;
        startProgressEmitter.Stop();
    }

    private void ResetBackRadial()
    {
        if (backRadialFillImage != null)
            backRadialFillImage.fillAmount = 0f;

        backEmitting = false;
        mainMenuProgressEmitter.Stop();
    }

    public void OnStartGameButtonDown() => StartGameHold(0);
    public void OnStartGameButtonUp() => StopStartGameHold(0);

    public void StartGameHold(int playerIndex)
    {
        if (!LobbyManager.instance.allPlayersReady || LobbyManager.instance.players.Count <= 0)
            return;
        if (gameStarting)
            return;

        if (playersHoldingStart.Contains(playerIndex))
            return;

        playersHoldingStart.Add(playerIndex);

        if (playersHoldingStart.Count == 1)
        {
            startGamePressTime = Time.time;
            ResetStartRadial();
        }
    }

    public void StopStartGameHold(int playerIndex)
    {
        playersHoldingStart.Remove(playerIndex);

        if (playersHoldingStart.Count <= 0)
        {
            startGamePressTime = 0f;

            if (!gameStarting)
                ResetStartRadial();
        }
    }

    public void OnBackToMenuButtonDown() => StartMainMenuHold(0);
    public void OnBackToMenuButtonUp() => StopMainMenuHold(0);

    public void StartMainMenuHold(int playerIndex)
    {
        if (gameStarting)
            return;

        if (playersHoldingBack.Contains(playerIndex))
            return;

        playersHoldingBack.Add(playerIndex);

        if (playersHoldingBack.Count == 1)
        {
            backPressTime = Time.time;
            ResetBackRadial();
        }
    }

    public void StopMainMenuHold(int playerIndex)
    {
        playersHoldingBack.Remove(playerIndex);

        if (playersHoldingBack.Count <= 0)
        {
            backPressTime = 0f;

            if (!gameStarting)
                ResetBackRadial();
        }
    }

    private void OnClientDisconnect(ulong clientId)
    {
        if (clientId != NetworkManager.ServerClientId) return;
        LobbyPlayerInput inputOfDisconnectedClient = null;    
        LobbyManager.instance.allLobbyPlayerInputs.Remove(inputOfDisconnectedClient);

        Debug.Log("Host disconnected — returning to main menu...");
        LeaveToMainMenu();
    }

    private async void GoToMainMenu()
    {
        await CleanupLobby();
        LeaveToMainMenu();
    }

    private async Task CleanupLobby()
    {
        try
        {
            if (GlobalLobby.CurrentLobby == null)
                return;

            string lobbyId = GlobalLobby.CurrentLobby.Id;
            string playerId = AuthenticationService.Instance.PlayerId;

            if (IsHost())
            {    
                var options = new UpdateLobbyOptions { IsPrivate = true };
                await LobbyService.Instance.UpdateLobbyAsync(lobbyId, options);
                await Task.Delay(100);
                await LobbyService.Instance.DeleteLobbyAsync(lobbyId);
            }
            else
            {
                await LobbyService.Instance.RemovePlayerAsync(lobbyId, playerId);
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to clean up lobby: {e}");
        }
        finally
        {
            GlobalLobby.CurrentLobby = null;
        }
    }

    private bool IsHost() => NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;

    public void LeaveToMainMenu()
    {
        if (lobbyManager._MatchSettingsSelection.activeSelf)
            return;

        buttonOnClickEmitter.Play();

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            NetworkManager.Singleton.Shutdown();

        GlobalLobby.CurrentLobby = null;

#if UNITY_SWITCH
        MenuSelection.Instance.mainMenu.SetActive(true);
        MenuSelection.Instance.ResetAllCams();
#else
        MenuSelection.Instance.localOnline.SetActive(true);
#endif
        Destroy(lobbyParent);
    }
}