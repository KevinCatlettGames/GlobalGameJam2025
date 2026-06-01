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
    [SerializeField] private StudioEventEmitter buttonOnClickEmitter;

    public LocalizeStringEvent matchSettingsGameModeNameStringEvent;
    public LocalizeStringEvent localizeStringEvent;

    [SerializeField] private InputActionProperty gameModeSwitchInputAction;
    [SerializeField] private Button gameModeButton;
    [SerializeField] private Button loadoutButton;

    [Header("Lobby")]
    private LobbyManager lobbyManager;

    [SerializeField] private GameObject[] teamSelections;

    [Header("Stick Input")]
    [SerializeField] private float stickThreshold = 0.5f;
    [SerializeField] private float pageInitialDelay = 0.35f;
    [SerializeField] private float pageRepeatRate = 0.12f;

    private bool stickInUse;
    private float pageHoldTimer;

    [Header("UI")]
    [SerializeField] private Image[] gameModeBubbleImages;

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

    //private void Update()
    //{
    //    Vector2 stick =
    //        gameModeSwitchInputAction.action.ReadValue<Vector2>();

    //    int direction = 0;

    //    if (stick.x < -stickThreshold)
    //        direction = -1;
    //    else if (stick.x > stickThreshold)
    //        direction = 1;

    //    if (direction != 0)
    //    {
    //        if (EventSystem.current.currentSelectedGameObject != gameModeButton.gameObject)
    //            return;

    //        if (!stickInUse)
    //        {
    //            pageHoldTimer = 0f;
    //            UpdateGameMode(direction > 0);
    //            stickInUse = true;
    //        }
    //        else
    //        {
    //            pageHoldTimer += Time.deltaTime;

    //            if (pageHoldTimer >= pageInitialDelay)
    //            {
    //                UpdateGameMode(direction > 0);
    //                pageHoldTimer = pageInitialDelay - pageRepeatRate;
    //            }
    //        }
    //    }
    //    else
    //    {
    //        stickInUse = false;
    //        pageHoldTimer = 0f;
    //    }
    //}

    public void OnGameModeButtonClick()
    {
        UpdateGameMode(true, true);
    }

    public void UpdateGameMode(bool increment, bool allowPositiveLoop = false)
    {
        int currentIndex = (int)lobbyManager.SelectedGameMode;

        int enumLength =
            Enum.GetValues(typeof(GameManager.GameModeType)).Length;

        if (increment)
        {
            if (allowPositiveLoop)
            {
                currentIndex = (currentIndex + 1) % enumLength;
            }
            else
            {
                currentIndex = Mathf.Min(currentIndex + 1, enumLength - 1);
            }
        }
        else
        {
            currentIndex = (currentIndex - 1 + enumLength) % enumLength;
        }

        lobbyManager.SelectedGameMode =
            (GameManager.GameModeType)currentIndex;

        UpdateBubbles(currentIndex);

        UpdateGameModeSelectionUI();

        RefreshNavigation(currentIndex, enumLength);

        buttonOnClickEmitter.Play();
    }

    private void RefreshNavigation(int currentIndex, int maxIndex)
    {
        Navigation nav = gameModeButton.navigation;
        nav.mode = Navigation.Mode.Explicit;

        nav.selectOnRight = loadoutButton;

        gameModeButton.navigation = nav;
    }

    private void UpdateBubbles(int currentIndex)
    {
        for (int i = 0; i < gameModeBubbleImages.Length; i++)
        {
            bool isActive = i == currentIndex;

            gameModeBubbleImages[i].sprite =
                isActive ? activePageDotSprite : inactivePageDotSprite;

            gameModeBubbleImages[i].color =
                isActive ? activePageDotColor : inactivePageDotColor;
        }
    }

    private void UpdateGameModeSelectionUI()
    {
        GameModeSO selected = lobbyManager.GameModes[0];

        foreach (GameModeSO mode in lobbyManager.GameModes)
        {
            if (mode.GameModeType == lobbyManager.SelectedGameMode)
            {
                selected = mode;
                break;
            }
        }

        matchSettingsGameModeNameStringEvent.StringReference =
            selected.GameModeLocalizationProperty.LocalizedString;

        localizeStringEvent.StringReference =
            selected.GameModeLocalizationProperty.LocalizedString;

        foreach (GameObject teamSelection in teamSelections)
        {
            teamSelection.SetActive(
                selected.GameModeType == GameManager.GameModeType.Team
            );
        }

        foreach (GameObject skin in LobbyManager.instance.playerContainers)
        {
            skin.GetComponent<PlayerContainerSkinChange>().UpdateBlur();
        }
    }
}