using FMODUnity;
using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

public class GameModeSelection : NetworkBehaviour
{
    [Header("UI")]
    [SerializeField] StudioEventEmitter buttonOnClickEmitter;
    public LocalizeStringEvent matchSettingsGameModeNameStringEvent;
    public InputActionProperty gameModeSwitchInputAction;
    public LocalizeStringEvent localizeStringEvent;
    public Button gameModeButton;
    public Image[] gameModeBubbleImages;

    [Header("Lobby Connection")]
    [SerializeField] LobbyButtons lobbyButtons;
    LobbyManager lobbyManager;

    [SerializeField] private GameObject[] teamSelections;
    [SerializeField] private float stickThreshold = 0.5f;
    [SerializeField] private float pageInitialDelay = 0.35f;
    [SerializeField] private float pageRepeatRate = 0.12f;
    public bool stickInUse;
    private float pageHoldTimer = 0f;
    [SerializeField] private Sprite activePageDotSprite;
    [SerializeField] private Color activePageDotColor;
    [SerializeField] private Sprite inactivePageDotSprite;
    [SerializeField] private Color inactivePageDotColor;

    private void OnEnable()
    {
        lobbyManager = LobbyManager.instance;

        gameModeSwitchInputAction.action.Enable();
        UpdateGameModeSelectionUI();
    }

    private void OnDisable()
    {
        gameModeSwitchInputAction.action.Disable();
    }

    private void Update()
    {
        Vector2 stick = gameModeSwitchInputAction.action.ReadValue<Vector2>();
        int direction = 0;

        if (stick.x < -stickThreshold) direction = -1;
        else if (stick.x > stickThreshold) direction = 1;

        if (direction != 0)
        {
            if (!stickInUse)
            {
                if (EventSystem.current.currentSelectedGameObject != gameModeButton.gameObject) return;
                pageHoldTimer = 0f;
                UpdateGameMode(true);
                stickInUse = true;
            }
            else
            {
                if (EventSystem.current.currentSelectedGameObject != gameModeButton.gameObject) return;
                pageHoldTimer += Time.deltaTime;
                if (pageHoldTimer >= pageInitialDelay)
                {
                    UpdateGameMode(false);
                    pageHoldTimer = pageInitialDelay - pageRepeatRate;
                }
            }
        }
        else
        {
            stickInUse = false;
            pageHoldTimer = 0f;
        }
    }

    public void UpdateGameMode(bool increment)
    {
        int currentIndex = (int)lobbyManager.SelectedGameMode;
        int enumLength = Enum.GetValues(typeof(GameManager.GameModeType)).Length;

        if (increment)
            currentIndex = (currentIndex + 1) % enumLength;
        else
            currentIndex = (currentIndex - 1 + enumLength) % enumLength;

        for (int i = 0; i < gameModeBubbleImages.Length; i++) 
        {
            if (currentIndex == i)
            {
                gameModeBubbleImages[i].sprite = activePageDotSprite;
                gameModeBubbleImages[i].color = activePageDotColor;
            }
            else
            {
                gameModeBubbleImages[i].sprite = inactivePageDotSprite;
                gameModeBubbleImages[i].color = inactivePageDotColor;
            }
        }

        lobbyManager.SelectedGameMode = (GameManager.GameModeType)currentIndex;

        UpdateGameModeSelectionUI();
        buttonOnClickEmitter.Play();
    }

    void UpdateGameModeSelectionUI()
    {
        GameModeSO gameModeSoToUse = lobbyManager.GameModes[0];
        foreach (GameModeSO gameModeSo in lobbyManager.GameModes)
        {
            if (lobbyManager.SelectedGameMode == gameModeSo.GameModeType)
            {
                gameModeSoToUse = gameModeSo;
                break;
            }
        }

        matchSettingsGameModeNameStringEvent.StringReference = gameModeSoToUse.GameModeLocalizationProperty.LocalizedString;
        localizeStringEvent.StringReference = gameModeSoToUse.GameModeLocalizationProperty.LocalizedString;

        foreach (GameObject teamSelection in teamSelections)
            teamSelection.SetActive(gameModeSoToUse.GameModeType == GameManager.GameModeType.Team);

        foreach(GameObject skinChange in LobbyManager.instance.playerContainers)
        {
            skinChange.GetComponent<PlayerContainerSkinChange>().UpdateBlur();
        }    
    }
}