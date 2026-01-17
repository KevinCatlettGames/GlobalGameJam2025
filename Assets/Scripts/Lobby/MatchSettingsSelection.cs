using UnityEngine;
using FMODUnity;
using TMPro;
using Unity.Netcode;
using UnityEngine.InputSystem; 
using UnityEngine.Localization.Components;
using UnityEngine.Localization.PropertyVariants.TrackedProperties;

public class MatchSettingsSelection : NetworkBehaviour
{
    [Header("UI")]
    [SerializeField] StudioEventEmitter buttonOnClickEmitter;
    
    [Header("Input")]
    public InputActionProperty exitMatchSettingsInputAction;
    
    [Header("Lobby Connection")] 
    [SerializeField] LobbyButtons lobbyButtons;

    [SerializeField] private TextMeshProUGUI backButtonText;
    [SerializeField] LocalizedStringProperty normalBackButtonStringProperty;
    [SerializeField] LocalizedStringProperty activeBackButtonStringProperty;
    
    private void OnEnable()
    {
        exitMatchSettingsInputAction.action.performed += ExitMatchSettingsSelectionPerformed;
        exitMatchSettingsInputAction.action.Enable();
        backButtonText.GetComponent<LocalizeStringEvent>().StringReference = activeBackButtonStringProperty.LocalizedString; 

    }

    private void OnDisable()
    {
        exitMatchSettingsInputAction.action.performed -= ExitMatchSettingsSelectionPerformed;
        exitMatchSettingsInputAction.action.Disable();
        backButtonText.GetComponent<LocalizeStringEvent>().StringReference = normalBackButtonStringProperty.LocalizedString; 

    }
    
    private void ExitMatchSettingsSelectionPerformed(InputAction.CallbackContext obj)
    {
        lobbyButtons.ToggleMatchSettings();
    }
}