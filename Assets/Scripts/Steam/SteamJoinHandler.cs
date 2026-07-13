using Steamworks;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SteamJoinHandler : MonoBehaviour
{
    public static SteamJoinHandler instance;
    string currentConnectString; 
    bool cameFromLevel = false; 

    private void Awake()
    {
        if (instance == null)
            instance = this; 

        ClearRichPresence();
    }

    void OnEnable()
    {
        SteamFriends.OnGameRichPresenceJoinRequested += OnJoinRequested;
        SceneManager.sceneLoaded += OnSceneLoadedCallback; 
    }


    void OnDisable()
    {
        SteamFriends.OnGameRichPresenceJoinRequested -= OnJoinRequested;
        SceneManager.sceneLoaded -= OnSceneLoadedCallback; 
    }

    void OnSceneLoadedCallback(Scene scene, LoadSceneMode loadSceneMode)
    {
        if (cameFromLevel)
            StartCoroutine(Join());
    }

    public void SetPlayerReadyToBeJoined(string lobbyId)
    {
        if (!SteamIntegration.instance.SteamInitialized) return;
        SteamFriends.SetRichPresence("connect", lobbyId);
        SteamFriends.SetRichPresence("status", "Playing with friends");
    }

    public void ClearRichPresence()
    {
        if (!SteamIntegration.instance.SteamInitialized) return;

        SteamFriends.ClearRichPresence();
    }

    private void OnJoinRequested(Friend friend, string connectString)
    {
        currentConnectString = connectString;
        PrepareForJoining();
    }

    private void PrepareForJoining()
    {
        if (SceneManager.GetActiveScene().name != "UI_MainMenu")
        {
            if (PauseManager.Instance)
            {
                cameFromLevel = true;
                PauseManager.Instance.ReturnToMainMenu();
            }     
        }
        else if(!LobbyManager.instance)
            StartCoroutine(Join());
    }

    private IEnumerator Join()
    {
        MenuSelection.Instance.startScreen.GetComponent<CallUnityEventOnInputAction>().OnInputActionPerformed.Invoke();
        MenuSelection.Instance.MakeCamPriority(3);
        MenuSelection.Instance.startScreen.SetActive(false);
        MenuSelection.Instance.mainMenu.SetActive(false);
        MenuSelection.Instance.localOnline.SetActive(false);
        MenuSelection.Instance.onlineMatchmaking.SetActive(true);
        yield return new WaitForSeconds(.5f);
        GameLobby.instance.JoinWithCode(currentConnectString);
    }
}