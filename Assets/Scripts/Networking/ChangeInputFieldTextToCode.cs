using UnityEngine;
using TMPro; 

public class ChangeInputFieldTextToCode : MonoBehaviour
{
    public TMP_InputField inputField;
    
    private void Start()
    {
#if (UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_EDITOR) && !UNITY_SWITCH

        if (inputField == null)
            inputField = GetComponent<TMP_InputField>();
            
        inputField.text = GlobalLobby.CurrentLobby.LobbyCode; 
#endif
    }
}
