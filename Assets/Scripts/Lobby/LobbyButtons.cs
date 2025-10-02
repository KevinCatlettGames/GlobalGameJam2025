using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.Services.Lobbies;       
using Unity.Services.Authentication; 
using System.Threading.Tasks;

/// <summary>
/// Handles all lobby button interactions and input actions, including toggling game mode,
/// match settings, starting the game, and returning to the main menu. Supports hold-to-start logic.
/// </summary>
public class LobbyButtons : MonoBehaviour
{
    /// <summary>
    /// Input action for toggling the game mode panel.
    /// </summary>
    public InputActionProperty toggleGameModeInputAction;

    /// <summary>
    /// Input action for toggling the match settings panel.
    /// </summary>
    public InputActionProperty toggleMatchSettingsInputAction;

    /// <summary>
    /// Input action for going back to the main menu.
    /// </summary>
    public InputActionProperty goToMainMenuInputAction;

    /// <summary>
    /// Input action for starting the game.
    /// </summary>
    public InputActionProperty startGameInputAction;

    /// <summary>
    /// UI panel for game mode selection.
    /// </summary>
    public GameObject gameModePanel;

    /// <summary>
    /// UI panel for match settings.
    /// </summary>
    public GameObject matchSettingsPanel;

    /// <summary>
    /// Radial image used to show hold-to-start progress.
    /// </summary>
    public Image radialFillImage;

    /// <summary>
    /// Timestamp when the start game input was pressed.
    /// </summary>
    private float startGamePressTime;

    /// <summary>
    /// Tracks whether the start game button is currently being pressed.
    /// </summary>
    private bool isPressingStartGame;

    /// <summary>
    /// Tracks whether the game is in the process of starting.
    /// </summary>
    private bool gameStarting;

    /// <summary>
    /// Duration in seconds required to hold the start button to confirm starting the game.
    /// </summary>
    private const float startGameHoldDuration = 1f;

    /// <summary>
    /// Name of the level to load when starting the game.
    /// </summary>
    public string levelToLoad = "Lvl_MainScene";

    /// <summary>
    /// Unity OnEnable method. Sets up input actions and subscribes to disconnect callbacks.
    /// </summary>
    private void OnEnable()
    {
        toggleGameModeInputAction.action.performed += OnToggleGameModeSelection;
        toggleGameModeInputAction.action.Enable();

        toggleMatchSettingsInputAction.action.performed += OnToggleMatchSettingsSelection;
        toggleMatchSettingsInputAction.action.Enable();

        goToMainMenuInputAction.action.performed += OnGoToMainMenu;
        goToMainMenuInputAction.action.Enable();

        startGameInputAction.action.started += OnStartGamePressed;
        startGameInputAction.action.canceled += OnStartGameReleased;
        startGameInputAction.action.Enable();

        if (radialFillImage != null)
            radialFillImage.fillAmount = 0f;

        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
    }

    /// <summary>
    /// Unity OnDisable method. Cleans up input actions and unsubscribes from callbacks.
    /// </summary>
    private void OnDisable()
    {
        toggleGameModeInputAction.action.performed -= OnToggleGameModeSelection;
        toggleGameModeInputAction.action.Disable();

        toggleMatchSettingsInputAction.action.performed -= OnToggleMatchSettingsSelection;
        toggleMatchSettingsInputAction.action.Disable();

        goToMainMenuInputAction.action.performed -= OnGoToMainMenu;
        goToMainMenuInputAction.action.Disable();

        startGameInputAction.action.started -= OnStartGamePressed;
        startGameInputAction.action.canceled -= OnStartGameReleased;
        startGameInputAction.action.Disable();

        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
    }

    /// <summary>
    /// Callback invoked when a client disconnects. Handles returning to the main menu if host disconnects.
    /// </summary>
    /// <param name="clientId">The client ID that disconnected.</param>
    private void OnClientDisconnect(ulong clientId)
    {
        if (clientId == 1)
        {
            Debug.Log("Host disconnected — returning to main menu...");
            LeaveToMainMenu();
        }
    }

    /// <summary>
    /// Cleans up the network and lobby and returns to the main menu scene.
    /// </summary>
    private void LeaveToMainMenu()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            NetworkManager.Singleton.Shutdown();

        Destroy(NetworkManager.Singleton.gameObject);
        GlobalLobby.CurrentLobby = null;
        SceneManager.LoadScene("MainMenu");
    }

    /// <summary>
    /// Unity Update method. Handles the hold-to-start-game logic.
    /// </summary>
    private void Update()
    {
        if (isPressingStartGame && !gameStarting)
        {
            float heldTime = Time.time - startGamePressTime;
            float progress = Mathf.Clamp01(heldTime / startGameHoldDuration);

            if (radialFillImage != null)
                radialFillImage.fillAmount = progress;

            if (heldTime >= startGameHoldDuration)
            {
                gameStarting = true;
                NetworkManager.Singleton.SceneManager.LoadScene(levelToLoad, LoadSceneMode.Single);
            }
        }
    }

    /// <summary>
    /// Handles lobby cleanup when the application quits.
    /// </summary>
    private async void OnApplicationQuit()
    {
        try
        {
            if (GameLobby.instance != null && GlobalLobby.CurrentLobby != null)
            {
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
                    await LobbyService.Instance.RemovePlayerAsync(lobbyId, playerId);

                GlobalLobby.CurrentLobby = null;
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to clean up lobby: {e}");
        }
    }

    /// <summary>
    /// Cleans up the lobby and returns to main menu.
    /// </summary>
    private async void GoToMainMenu()
    {
        try
        {
            if (GameLobby.instance != null && GlobalLobby.CurrentLobby != null)
            {
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
                    await LobbyService.Instance.RemovePlayerAsync(lobbyId, playerId);

                GlobalLobby.CurrentLobby = null;
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to clean up lobby: {e}");
        }

        LeaveToMainMenu();
    }

    /// <summary>
    /// Begins the hold-to-start-game process if conditions are met.
    /// </summary>
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

    /// <summary>
    /// Stops the hold-to-start-game process and resets progress visuals.
    /// </summary>
    private void StopStartGame()
    {
        isPressingStartGame = false;
        startGamePressTime = 0f;

        if (!gameStarting && radialFillImage != null)
            radialFillImage.fillAmount = 0f;
    }

    /// <summary>
    /// Returns true if the local client is the host.
    /// </summary>
    private bool IsHost()
    {
        return NetworkManager.Singleton.IsHost;
    }

    // --- Input System Callbacks ---
    private void OnToggleGameModeSelection(InputAction.CallbackContext context)
    {
        if (!IsHost()) return;
        ToggleGameMode();
    }

    private void OnToggleMatchSettingsSelection(InputAction.CallbackContext context)
    {
        if (!IsHost()) return;
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

    // --- UI Button Methods ---
    public void OnToggleGameModeButton() => ToggleGameMode();
    public void OnToggleMatchSettingsButton() => ToggleMatchSettings();
    public void OnGoToMainMenuButton() => GoToMainMenu();
    public void OnStartGameButton() => StartGame();
    public void OnStopStartGameButton() => StopStartGame();

    // --- UI Panel Toggles ---
    private void ToggleGameMode()
    {
        if (TransportSwitcher.Instance && TransportSwitcher.Instance.isUsingRelay &&
            !NetworkManager.Singleton.IsServer) return;
        
        gameModePanel.SetActive(!gameModePanel.activeSelf);
        if (gameModePanel.activeSelf) matchSettingsPanel.SetActive(false);
    }

    private void ToggleMatchSettings()
    {
        if (TransportSwitcher.Instance && TransportSwitcher.Instance.isUsingRelay &&
            !NetworkManager.Singleton.IsServer) return;
        
        matchSettingsPanel.SetActive(!matchSettingsPanel.activeSelf);
        if (matchSettingsPanel.activeSelf) gameModePanel.SetActive(false);
    }
}
