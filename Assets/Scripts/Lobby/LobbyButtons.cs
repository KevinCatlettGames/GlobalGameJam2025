using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using FMODUnity;

public class LobbyButtons : MonoBehaviour
{
    public InputActionProperty toggleGameModeInputAction;
    public InputActionProperty toggleMatchSettingsInputAction;
    public InputActionProperty goToMainMenuInputAction;
    public InputActionProperty startGameInputAction;
        
    public Image radialFillImage;

    [SerializeField] private float startGameHoldDuration = 1f;

    private float startGamePressTime;
    private bool isPressingStartGame;
    private bool gameStarting;
    private bool emitting;

    public StudioEventEmitter progressEmitter;
    public StudioEventEmitter buttonOnClickEmitter;
    
    LobbyManager lobbyManager;
    
    private void OnEnable()
    {
        lobbyManager = LobbyManager.instance;
        
        toggleGameModeInputAction.action.performed += OnToggleGameModeSelection;
        toggleMatchSettingsInputAction.action.performed += OnToggleMatchSettingsSelection;
        goToMainMenuInputAction.action.performed += OnGoToMainMenu;
        startGameInputAction.action.started += OnStartGamePressed;
        startGameInputAction.action.canceled += OnStartGameReleased;

        toggleGameModeInputAction.action.Enable();
        toggleMatchSettingsInputAction.action.Enable();
        goToMainMenuInputAction.action.Enable();
        startGameInputAction.action.Enable();

        if (radialFillImage != null)
            radialFillImage.fillAmount = 0f;

        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
    }

    private void OnDisable()
    {
        toggleGameModeInputAction.action.performed -= OnToggleGameModeSelection;
        toggleMatchSettingsInputAction.action.performed -= OnToggleMatchSettingsSelection;
        goToMainMenuInputAction.action.performed -= OnGoToMainMenu;
        startGameInputAction.action.started -= OnStartGamePressed;
        startGameInputAction.action.canceled -= OnStartGameReleased;

        toggleGameModeInputAction.action.Disable();
        toggleMatchSettingsInputAction.action.Disable();
        goToMainMenuInputAction.action.Disable();
        startGameInputAction.action.Disable();

        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
    }

    private void Update()
    {
        if (!isPressingStartGame || gameStarting) return;

        float heldTime = Time.time - startGamePressTime;
        float progress = Mathf.Clamp01(heldTime / startGameHoldDuration);

        if (progress > 0.1f && !emitting)
        {
            emitting = true;
            progressEmitter.Play();
        }

        if (radialFillImage != null)
            radialFillImage.fillAmount = progress;

        if (heldTime >= startGameHoldDuration)
        {
            gameStarting = true;
            StartCoroutine(lobbyManager.LoadGameScene());
        }
    }

    private async void OnApplicationQuit()
    {
        await CleanupLobby();
    }
    
    private void OnToggleGameModeSelection(InputAction.CallbackContext context)
    {
        if (IsHost())
            ToggleGameMode();
    }

    private void OnToggleMatchSettingsSelection(InputAction.CallbackContext context)
    {
        if (IsHost())
            ToggleMatchSettings();
    }

    private void OnGoToMainMenu(InputAction.CallbackContext context)
    {
        GoToMainMenu();
    }

    private void OnStartGamePressed(InputAction.CallbackContext context)
    {
        StartGame();
    }

    private void OnStartGameReleased(InputAction.CallbackContext context)
    {
        StopStartGame();
    }

    // ---------------------------------------------------------
    // Public UI Button Hooks
    // ---------------------------------------------------------

    public void OnToggleGameModeButton() => ToggleGameMode();
    public void OnToggleMatchSettingsButton() => ToggleMatchSettings();
    public void OnGoToMainMenuButton() => GoToMainMenu();
    public void OnStartGameButton() => StartGame();
    public void OnStopStartGameButton() => StopStartGame();

    // ---------------------------------------------------------
    // Core Logic
    // ---------------------------------------------------------

    private void OnClientDisconnect(ulong clientId)
    {
        if (clientId != 1) return;

        Debug.Log("Host disconnected — returning to main menu...");
        LeaveToMainMenu();
    }

    private void LeaveToMainMenu()
    {
        if (lobbyManager.GameModeSelection.activeSelf || lobbyManager.MatchSettingsSelection.activeSelf)
            return;
        
        buttonOnClickEmitter.Play();
            
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            NetworkManager.Singleton.Shutdown();

        Destroy(NetworkManager.Singleton.gameObject);

        GlobalLobby.CurrentLobby = null;
        SceneManager.LoadScene("UI_MainMenu");
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

    private void StartGame()
    {
        if (TransportSwitcher.Instance.isUsingRelay && !NetworkManager.Singleton.IsServer)
            return;

        if (!LobbyManager.instance.allPlayersReady || LobbyManager.instance.players.Count <= 0)
            return;

        isPressingStartGame = true;
        startGamePressTime = Time.time;
        gameStarting = false;

        if (radialFillImage != null)
            radialFillImage.fillAmount = 0f;
    }

    private void StopStartGame()
    {
        isPressingStartGame = false;
        startGamePressTime = 0f;

        if (!gameStarting && radialFillImage != null)
            radialFillImage.fillAmount = 0f;

        emitting = false;
        progressEmitter.Stop();
    }

    private bool IsHost() => NetworkManager.Singleton.IsHost;

    public void ToggleGameMode()
    {
        if (TransportSwitcher.Instance && TransportSwitcher.Instance.isUsingRelay &&
            !NetworkManager.Singleton.IsServer) return;

        lobbyManager.GameModeSelection.SetActive(!lobbyManager.GameModeSelection.activeSelf);
        if (lobbyManager.GameModeSelection.activeSelf)
            lobbyManager.MatchSettingsSelection.SetActive(false);
        
        buttonOnClickEmitter.Play();
    }

    public void ToggleMatchSettings()
    {
        if (TransportSwitcher.Instance && TransportSwitcher.Instance.isUsingRelay &&
            !NetworkManager.Singleton.IsServer) return;

        lobbyManager.MatchSettingsSelection.SetActive(!lobbyManager.MatchSettingsSelection.activeSelf);
        if (lobbyManager.MatchSettingsSelection.activeSelf)
            lobbyManager.GameModeSelection.SetActive(false);
        
        buttonOnClickEmitter.Play();
    }
}