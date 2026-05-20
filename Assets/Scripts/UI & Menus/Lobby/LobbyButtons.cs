using FMODUnity;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static LobbyPlayerValues;
using FMODUnity;

public class LobbyButtons : MonoBehaviour
{
    public Image startGameRadialFillImage;
    public Image backRadialFillImage;

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
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
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
            StartCoroutine(lobbyManager.LoadGameScene());
        }
    }

    private void HandleBackHold()
    {
        if (playersHoldingBack.Count == 0 || gameStarting)
            return;

        float heldTime = Time.time - backPressTime;
        float progress = Mathf.Clamp01(heldTime / backHoldDuration);

        if (progress > 0.1f && !backEmitting)
        {
            backEmitting = true;
            startProgressEmitter.Play();
        }

        if (backRadialFillImage != null)
            backRadialFillImage.fillAmount = progress;

        if (heldTime >= backHoldDuration)
        {
            buttonOnClickEmitter?.Play();
            LeaveToMainMenu();
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
        startProgressEmitter.Stop();
    }

    public void OnStartGameButtonDown() => StartGameHold(0);
    public void OnStartGameButtonUp() => StopStartGameHold(0);

    public void StartGameHold(int playerIndex)
    {
        if (!LobbyManager.instance.allPlayersReady ||
            LobbyManager.instance.players.Count <= 0)
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
        if (clientId != 1) return;

        Debug.Log("Host disconnected — returning to main menu...");
        LeaveToMainMenu();
    }

    public void LeaveToMainMenu()
    {
        if (lobbyManager.MatchSettingsSelection.activeSelf)
            return;

        buttonOnClickEmitter.Play();

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            NetworkManager.Singleton.Shutdown();

        GlobalLobby.CurrentLobby = null;
        MenuSelection.Instance.localOnline.SetActive(true);
        Destroy(lobbyParent);
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
            if (GameLobby.instance == null || GlobalLobby.CurrentLobby == null)
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

            GlobalLobby.CurrentLobby = null;
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to clean up lobby: {e}");
        }
    }

    private bool IsHost() => NetworkManager.Singleton.IsHost;
}