using System;
using UnityEngine;
using UnityEngine.UI;
using FMODUnity;
using Unity.Netcode;
using UnityEngine.InputSystem;
using UnityEngine.Localization.Components;

public class GameModeSelection : NetworkBehaviour
{
    [Header("UI")]
    [SerializeField] Image gameModeTypeImage;
    [SerializeField] StudioEventEmitter buttonOnClickEmitter;
    public LocalizeStringEvent matchSettingsGameModeNameStringEvent;
    public InputActionProperty exitGameModeSelectionInputAction;
    public LocalizeStringEvent localizeStringEvent;

    [Header("Lobby Connection")]
    [SerializeField] LobbyButtons lobbyButtons;
    LobbyManager lobbyManager;

    [SerializeField] private GameObject[] teamSelections;

    private void OnEnable()
    {
        lobbyManager = LobbyManager.instance;

        exitGameModeSelectionInputAction.action.performed += ExitGameModeSelectionPerformed;
        exitGameModeSelectionInputAction.action.Enable();
        UpdateGameModeSelectionUI();
    }

    private void OnDisable()
    {
        exitGameModeSelectionInputAction.action.performed -= ExitGameModeSelectionPerformed;
        exitGameModeSelectionInputAction.action.Disable();
    }

    public void UpdateSelectedGameManagerType(bool increment)
    {
        int currentIndex = (int)lobbyManager.SelectedGameMode;
        int enumLength = Enum.GetValues(typeof(GameManager.GameModeType)).Length;

        if (increment)
            currentIndex = (currentIndex + 1) % enumLength;
        else
            currentIndex = (currentIndex - 1 + enumLength) % enumLength;

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

        gameModeTypeImage.sprite = gameModeSoToUse.GameModeTypeImage;
        matchSettingsGameModeNameStringEvent.StringReference = gameModeSoToUse.GameModeLocalizationProperty.LocalizedString;
        localizeStringEvent.StringReference = gameModeSoToUse.GameModeLocalizationProperty.LocalizedString;

        foreach (GameObject teamSelection in teamSelections)
            teamSelection.SetActive(gameModeSoToUse.GameModeType == GameManager.GameModeType.Team);
    }

    private void IncrementActionPerformed(InputAction.CallbackContext obj)
    {
        UpdateSelectedGameManagerType(true);
    }

    private void DecrementActionPerformed(InputAction.CallbackContext obj)
    {
        UpdateSelectedGameManagerType(false);
    }

    private void ExitGameModeSelectionPerformed(InputAction.CallbackContext obj)
    {
        buttonOnClickEmitter.Play();
    }
}