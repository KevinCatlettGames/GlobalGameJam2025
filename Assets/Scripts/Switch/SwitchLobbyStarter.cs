using Cinemachine;
using UnityEngine;
using UnityEngine.UI; 

public class SwitchLobbyStarter : MonoBehaviour
{
#if UNITY_SWITCH
    Button button;
    public GameObject localOnlineMenu;
    public MainMenuLobbyCreator mainMenuLobbyCreator;
    public CinemachineVirtualCamera virtualCamera;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(StartSwitchLobby);
    }

    void StartSwitchLobby()
    {
        localOnlineMenu.SetActive(false);
        mainMenuLobbyCreator.StartSceneLocal("UI_Lobby");
        virtualCamera.Priority = 1;
    }
#endif 
}