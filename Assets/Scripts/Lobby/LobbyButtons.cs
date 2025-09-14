using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyButtons : MonoBehaviour
{
    public InputActionProperty toggleGameModeInputAction;
    public InputActionProperty toggleMatchSettingsInputAction;
    public InputActionProperty goToMainMenuInputAction;
    public InputActionProperty startGameInputAction;

    public GameObject gameModePanel;
    public GameObject matchSettingsPanel;
    public Image radialFillImage;

    private float startGamePressTime;
    private bool isPressingStartGame;
    private bool gameStarting;

    private const float startGameHoldDuration = 1f;

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
    }

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
    }

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
                //Debug.Log("Start game auto-triggered after holding for " + heldTime + " seconds");
                NetworkManager.Singleton.SceneManager.LoadScene("Lvl_MainScene", LoadSceneMode.Single);
            }
        }
    }

    // Shared logic for toggling game mode
    private void ToggleGameMode()
    {
        if (gameModePanel.activeSelf)
        {
            gameModePanel.SetActive(false);
            matchSettingsPanel.SetActive(false);
        }
        else
        {
            gameModePanel.SetActive(true);
            matchSettingsPanel.SetActive(false);
        }
    }

    // Shared logic for toggling match settings
    private void ToggleMatchSettings()
    {
        if (matchSettingsPanel.activeSelf)
        {
            matchSettingsPanel.SetActive(false);
            gameModePanel.SetActive(false);
        }
        else
        {
            matchSettingsPanel.SetActive(true);
            gameModePanel.SetActive(false);
        }
    }

    private void GoToMainMenu()
    {
        NetworkManager.Singleton.Shutdown();
        SceneManager.LoadScene("MainMenu");
    }

    private void StartGame()
    {
        if (!IsHost()) return;
        
        if (!LobbyManager.instance.allPlayersReady || LobbyManager.instance.players.Count <= 0) return;
        if (TransportSwitcher.Instance.isUsingRelay && LobbyManager.instance.players.Count <= 1) return; 
        
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
    }

    private bool IsHost()
    {
        return NetworkManager.Singleton.IsHost;
    }

    // ---- Input System Callbacks ----
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

    // ---- UI Button Methods ----
    public void OnToggleGameModeButton() => ToggleGameMode();
    public void OnToggleMatchSettingsButton() => ToggleMatchSettings();
    public void OnGoToMainMenuButton() => GoToMainMenu();
    public void OnStartGameButton() => StartGame();
    public void OnStopStartGameButton() => StopStartGame();
}
