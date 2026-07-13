using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.Localization.SmartFormat.Core.Parsing;
using Steamworks;

[RequireComponent(typeof(TMP_InputField))]
public class SteamDeckInputFieldHandler : MonoBehaviour
{
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
}