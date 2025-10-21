using UnityEngine;
using FMODUnity;
using Unity.Netcode;
using UnityEngine.InputSystem; 

public class MatchSettingsSelection : NetworkBehaviour
{
    [Header("UI")]
    [SerializeField] StudioEventEmitter buttonOnClickEmitter;
    
    [Header("Input")]
    public InputActionProperty exitMatchSettingsInputAction;
    
    [Header("Lobby Connection")]
    [SerializeField] LobbyButtons lobbyButtons;
    
    private void OnEnable()
    {
        exitMatchSettingsInputAction.action.performed += ExitMatchSettingsSelectionPerformed;
        exitMatchSettingsInputAction.action.Enable();
    }

    private void OnDisable()
    {
        exitMatchSettingsInputAction.action.performed -= ExitMatchSettingsSelectionPerformed;
        exitMatchSettingsInputAction.action.Disable();
    }
    
    private void ExitMatchSettingsSelectionPerformed(InputAction.CallbackContext obj)
    {
        lobbyButtons.ToggleMatchSettings();
    }
}