using UnityEngine;
using System.Collections;
using UnityEngine.Localization.Settings;
using TMPro;

public class LocaleSelector : MonoBehaviour
{
    public TMP_Dropdown languageDropdown;
    public static LocaleSelector Instance;
    private bool active = false;
    
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }
    
    private IEnumerator SetLocale(int localeID)
    {
        active = true;
        
        yield return LocalizationSettings.InitializationOperation;
        
        if (localeID >= 0 && localeID < LocalizationSettings.AvailableLocales.Locales.Count)
            LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[localeID];
        
        active = false;
    }

    public void ChangeLocale(int localeID)
    {
        if (active == true) return;
        StartCoroutine(SetLocale(localeID));
    }
}