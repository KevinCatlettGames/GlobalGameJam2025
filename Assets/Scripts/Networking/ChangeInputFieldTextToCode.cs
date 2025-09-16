using UnityEngine;
using TMPro; 

public class ChangeInputFieldTextToCode : MonoBehaviour
{
    public TMP_InputField inputField;
    
    private void Start()
    {
        if(inputField == null)
            inputField = GetComponent<TMP_InputField>();
            
        inputField.text = GlobalLobby.CurrentLobby.LobbyCode; 
    }
}
