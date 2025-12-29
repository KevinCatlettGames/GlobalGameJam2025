using System.Globalization;
using UnityEngine;
using FMODUnity;
using TMPro;
using Unity.Netcode;
using UnityEngine.InputSystem; 
using UnityEngine.UI; 

public class MatchSettingsSelection : NetworkBehaviour
{
    [Header("UI")]
    [SerializeField] StudioEventEmitter buttonOnClickEmitter;
    
    [Header("Input")]
    public InputActionProperty exitMatchSettingsInputAction;
    // public InputActionProperty mapEventsToggle;

    [SerializeField] private Toggle mapEventToggle;
    
    [Header("Lobby Connection")] 
    [SerializeField] LobbyButtons lobbyButtons;

    [SerializeField] private TextMeshProUGUI pointsForGameEndValueText;
    [SerializeField] private TextMeshProUGUI matchTimeValueText;

    private void OnEnable()
    {
        // mapEventsToggle.action.performed += ToggleMapEvents;
        // mapEventsToggle.action.Enable();
        exitMatchSettingsInputAction.action.performed += ExitMatchSettingsSelectionPerformed;
        exitMatchSettingsInputAction.action.Enable();
    }

    private void OnDisable()
    {
        // mapEventsToggle.action.performed -= ToggleMapEvents;
        // mapEventsToggle.action.Disable();
        exitMatchSettingsInputAction.action.performed -= ExitMatchSettingsSelectionPerformed;
        exitMatchSettingsInputAction.action.Disable();
    }

    void ToggleMapEvents(InputAction.CallbackContext context)
    {
        mapEventToggle.isOn = !mapEventToggle.isOn;
    }
    
    private void ExitMatchSettingsSelectionPerformed(InputAction.CallbackContext obj)
    {
        lobbyButtons.ToggleMatchSettings();
    }

    public void ChangePointsForGameEnd(float newValue)
    {
        int value = (int)newValue;
        LobbyManager.instance.pointsForGameEnd = value;
        pointsForGameEndValueText.text = value.ToString();
    }

    public void ChangeMatchTime(float newValue)
    {
        int value = (int)newValue;
        LobbyManager.instance.matchTime = value;
        matchTimeValueText.text = value.ToString() + "min.";
    }
}