using System;
using UnityEngine;

#if UNITY_SWITCH && !UNITY_EDITOR
using nn.oe;
#endif 

public class SwitchLocalization : MonoBehaviour
{
#if UNITY_SWITCH && !UNITY_EDITOR
    void Start()
    {
        string desiredLanguage = Language.GetDesired();

        switch (desiredLanguage)
        {
            case "de":
                LocaleSelector.Instance.ChangeLocale(2);
                break;
            case "es":
                LocaleSelector.Instance.ChangeLocale(6);
                break;
            case "fr":
                LocaleSelector.Instance.ChangeLocale(1);
                break;
            case "it":
                LocaleSelector.Instance.ChangeLocale(3);
                break;
            case "pt":
                LocaleSelector.Instance.ChangeLocale(5);
                break;
            default:
                LocaleSelector.Instance.ChangeLocale(0);
                break;
        }
    }
#endif
}