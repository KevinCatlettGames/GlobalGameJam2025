using UnityEngine;
using UnityEngine.UI; 

public class SwitchLobbyStarter : MonoBehaviour
{
    public SwitchControllerSupport switchControllerSupport; 

#if UNITY_SWITCH
    Button button;
    public GameObject localOnlineMenu;
    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(StartSwitchLobby);
    }

    void StartSwitchLobby()
    {
        localOnlineMenu.SetActive(false);
        switchControllerSupport.ToggleCanShowApplet();
    }
#endif 
}