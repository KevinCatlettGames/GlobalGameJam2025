using FMODUnity;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public class WinScreenButtons : MonoBehaviour
{
    public Image restartRadialFillImage;
    public Image backRadialFillImage; 

    [SerializeField] private float restartHoldDuration = 1f;
    [SerializeField] private float backHoldDuration = 1f;
    [SerializeField] private PauseManager pauseManager;

    private float startGamePressTime;
    private float backPressTime;

    private HashSet<int> playersHoldingStart = new();
    private HashSet<int> playersHoldingBack = new();

    private bool gameRestarting;
    private bool isLeaving;

    private bool startEmitting;
    private bool backEmitting;

    [Tooltip("Audio emitter for start game hold progress")]
    public StudioEventEmitter startProgressEmitter;

    [Tooltip("Audio emitter for button click feedback")]
    public StudioEventEmitter buttonOnClickEmitter;

    private EventSystem eventSystem;
    private InputSystemUIInputModule inputModuleUI;
    private InputAction startAction;
    private InputAction backAction;

    private void Start()
    {
        eventSystem = EventSystem.current;
        if (eventSystem != null)
        {
            inputModuleUI = eventSystem.gameObject.GetComponent<InputSystemUIInputModule>();
            if (inputModuleUI != null && inputModuleUI.actionsAsset != null)
            {
                startAction = inputModuleUI.actionsAsset.FindAction("UI/Submit");
                backAction = inputModuleUI.actionsAsset.FindAction("UI/Back");

                if (startAction != null)
                {
                    startAction.started += OnRestartButtonDown;
                    startAction.canceled += OnRestartButtonUp;
                    startAction.Enable();
                }
                if (backAction != null)
                {
                    backAction.started += OnBackToMenuButtonDown;
                    backAction.canceled += OnBackToMenuButtonUp;
                    backAction.Enable();
                }
            }
        }
    }
    private void OnEnable()
    {
        pauseManager.PausingEnabled = false;
    }

    private void OnDisable()
    {
        pauseManager.PausingEnabled = true;
    }

    private void OnDestroy()
    {
        if (startAction != null)
        {
            startAction.started -= OnRestartButtonDown;
            startAction.canceled -= OnRestartButtonUp;
        }
        if (backAction != null)
        {
            backAction.started -= OnBackToMenuButtonDown;
            backAction.canceled -= OnBackToMenuButtonUp;
        }
    }

    private void Update()
    {
        HandleRestartGameHold();
        HandleBackHold();
    }

    private void HandleRestartGameHold()
    {
        if (playersHoldingStart.Count == 0 || gameRestarting)
            return;

        float heldTime = Time.time - startGamePressTime;
        float progress = Mathf.Clamp01(heldTime / restartHoldDuration);

        if (progress > 0.1f && !startEmitting)
        {
            startEmitting = true;
            startProgressEmitter.Play();
        }

        if (restartRadialFillImage != null)
            restartRadialFillImage.fillAmount = progress;

        if (heldTime >= restartHoldDuration)
        {
            gameRestarting = true;
            ResetRestartRadial();
            pauseManager.RestartGame();
        }
    }

    private void HandleBackHold()
    {
        if (playersHoldingBack.Count == 0 || gameRestarting || isLeaving)
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
            isLeaving = true;
            ResetBackRadial();

            buttonOnClickEmitter?.Play();
            pauseManager.ReturnToMainMenu();
        }
    }

    private void ResetRestartRadial()
    {
        if (restartRadialFillImage != null)
            restartRadialFillImage.fillAmount = 0f;

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

    public void OnRestartButtonDown(InputAction.CallbackContext context) => RestartGameHold(0);
    public void OnRestartButtonUp(InputAction.CallbackContext context) => StopRestartHold(0);

    public void RestartGameHold(int playerIndex)
    {
        if (!LobbyManager.instance.allPlayersReady || LobbyManager.instance.players.Count <= 0)
            return;
        if (gameRestarting)
            return;

        if (playersHoldingStart.Contains(playerIndex))
            return;

        playersHoldingStart.Add(playerIndex);

        if (playersHoldingStart.Count == 1)
        {
            startGamePressTime = Time.time;
            ResetRestartRadial();
        }
    }

    public void StopRestartHold(int playerIndex)
    {
        playersHoldingStart.Remove(playerIndex);

        if (playersHoldingStart.Count <= 0)
        {
            startGamePressTime = 0f;

            if (!gameRestarting)
                ResetRestartRadial();
        }
    }

    public void OnBackToMenuButtonDown(InputAction.CallbackContext context) => StartBackHold(0);
    public void OnBackToMenuButtonUp(InputAction.CallbackContext context) => StopBackHold(0);

    public void StartBackHold(int playerIndex)
    {
        if (isLeaving)
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

    public void StopBackHold(int playerIndex)
    {
        playersHoldingBack.Remove(playerIndex);

        if (playersHoldingBack.Count <= 0)
        {
            backPressTime = 0f;

            if (!isLeaving)
                ResetBackRadial();
        }
    }
}
