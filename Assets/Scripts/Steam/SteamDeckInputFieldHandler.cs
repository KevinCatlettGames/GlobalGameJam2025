using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.Localization.SmartFormat.Core.Parsing;
#if !UNITY_SWITCH
using Steamworks;
#endif
[RequireComponent(typeof(TMP_InputField))]
public class SteamDeckInputFieldHandler : MonoBehaviour
{
    #if !UNITY_SWITCH
    private TMP_InputField inputField;
    public void OpenSteamDeckKeyboard()
    {
        GamepadTextInputMode inputMode = GamepadTextInputMode.Normal;
        GamepadTextInputLineMode lineMode = GamepadTextInputLineMode.SingleLine;
        string description = "Enter text:";
        int maxChar = inputField.characterLimit > 0 ? inputField.characterLimit : 32;
        string existingText = inputField.text;

        SteamUtils.ShowGamepadTextInput(inputMode, lineMode, description, maxChar, existingText);
    }
#endif
}