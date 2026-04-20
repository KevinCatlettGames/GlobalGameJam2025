using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro; 

public class SetDynamicSelectedUIElement : MonoBehaviour
{
    #region Serialized Fields

    // The EventSystem used for UI interactions
    public EventSystem eventSystem;

    // Flag to determine if the button should be clicked automatically on selection
    public bool initializeButtonOnSelection = false;

    public Button buttonToUse;

    #endregion

    #region Unity Methods

    // Called when the script is enabled
    private void OnEnable()
    {
        // Change the selected UI element when the script is enabled
        Change();
    }

    #endregion

    #region UI Selection Logic

    // Changes the currently selected UI button based on the device and selection conditions
    public void Change()
    {
        // If eventSystem is null or the active game device is Keyboard/Mouse, do nothing
        if (eventSystem == null) return;


        // If no button was found, return
        if (buttonToUse == null) return;

        // Set the selected GameObject in the EventSystem
        eventSystem.SetSelectedGameObject(buttonToUse.gameObject);

        // If the flag is true, invoke the button's click event
        if (initializeButtonOnSelection)
            buttonToUse.onClick.Invoke();
    }

    #endregion
}