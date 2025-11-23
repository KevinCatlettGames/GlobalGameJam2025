using System;
using UnityEngine;
using System.Collections;
using UnityEngine.Localization.Settings;
using TMPro;

public class LocaleSelector : MonoBehaviour
{
    #region Serialized Fields

    // Dropdown UI element for selecting language
    public TMP_Dropdown languageDropdown;

    #endregion

    #region Private Variables

    // Singleton instance of LocaleSelector
    public static LocaleSelector Instance;

    // Flag to prevent multiple locale changes in quick succession
    private bool active = false;

    #endregion

    #region Unity Methods

    // Called when the script is initialized
    private void Awake()
    {
        // Ensure the singleton instance is set up
        if (Instance == null)
            Instance = this;
    }

    #endregion

    #region Locale Management

    // Coroutine to set the selected locale
    private IEnumerator SetLocale(int localeID)
    {
        active = true;

        // Wait until localization system is ready
        yield return LocalizationSettings.InitializationOperation;

        // ✅ Use the provided localeID, not the dropdown's value
        if (localeID >= 0 && localeID < LocalizationSettings.AvailableLocales.Locales.Count)
        {
            LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[localeID];
            //Debug.Log("Locale set to: " + LocalizationSettings.SelectedLocale.Identifier.Code);
        }
        else
        {
            //Debug.LogWarning("Invalid locale ID: " + localeID);
        }

        active = false;
    }

    // Method to handle locale change triggered by the dropdown
    public void ChangeLocale(int localeID)
    {
        //Debug.Log("Locale changed");
        // Return early if a locale change is already in progress
        if (active == true) return;

        // Start the coroutine to change the locale
        StartCoroutine(SetLocale(localeID));
    }

    #endregion
}