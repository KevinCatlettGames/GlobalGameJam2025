using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro; 
using FMODUnity;
using Unity.Netcode;
using UnityEngine.InputSystem;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.PropertyVariants.TrackedProperties;

public class GameModeSelection : NetworkBehaviour
{
    [Header("UI")]
    [SerializeField] Image gameModeTypeImage;
    [SerializeField] StudioEventEmitter buttonOnClickEmitter;
    
    [Header("Input")]
    public InputActionProperty incrementInputAction;
    public InputActionProperty decrementInputAction;
    public InputActionProperty exitGameModeSelectionInputAction;
    
    [Header("Lobby Connection")]
    [SerializeField] LobbyButtons lobbyButtons;
    LobbyManager lobbyManager;

    [SerializeField] private TextMeshProUGUI backButtonText;
    [SerializeField] LocalizedStringProperty normalBackButtonStringProperty;
    [SerializeField] LocalizedStringProperty activeBackButtonStringProperty;
    
    private void OnEnable()
    {
        lobbyManager = LobbyManager.instance;
        
        incrementInputAction.action.performed += IncrementActionPerformed;
        decrementInputAction.action.performed += DecrementActionPerformed;
        exitGameModeSelectionInputAction.action.performed += ExitGameModeSelectionPerformed;
        exitGameModeSelectionInputAction.action.Enable();
        backButtonText.GetComponent<LocalizeStringEvent>().StringReference = activeBackButtonStringProperty.LocalizedString; 
        UpdateGameModeSelectionUI();
    }

    private void OnDisable()
    {
        incrementInputAction.action.performed -= IncrementActionPerformed;
        decrementInputAction.action.performed -= DecrementActionPerformed;
        exitGameModeSelectionInputAction.action.performed -= ExitGameModeSelectionPerformed;
        exitGameModeSelectionInputAction.action.Disable();
        backButtonText.GetComponent<LocalizeStringEvent>().StringReference = normalBackButtonStringProperty.LocalizedString; 
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
        
        int indexOfUsedGameMode = 0;
        for (int i = 0; i < LobbyManager.instance.GameModes.Length; i++)
        {
            if (gameModeSoToUse == LobbyManager.instance.GameModes[i])
            {
                indexOfUsedGameMode = i;
                break;
            }
        }
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
        lobbyButtons.ToggleGameMode();
    }
}