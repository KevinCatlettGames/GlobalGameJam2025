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

    /// <summary>
    /// Call this method right after the local player hosts or joins a multiplayer lobby.
    /// </summary>
    /// <param name="lobbyId">The Steam ID of your lobby/room</param>
    public void SetPlayerReadyToBeJoined(string lobbyId)
    {
        Debug.Log("In method");
        if (!SteamIntegration.instance.SteamInitialized) return;

        // 1. Tell Steam how a friend should connect to this player
        // The key MUST be "connect" for Steam's overlay to recognize it
        SteamFriends.SetRichPresence("connect", lobbyId);

        // 2. (Optional but recommended) Group players together in the UI
        // This activates the "Invite to Game" button in the overlay
        SteamFriends.SetRichPresence("status", "Playing with friends");
    }

    /// <summary>
    /// Call this when the player leaves the lobby to disable joining.
    /// </summary>
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
        {
            StartCoroutine(Join());
        }
    }

    private IEnumerator Join()
    {
        if (MenuSelection.Instance.startScreen.activeSelf)
        {
            MenuSelection.Instance.startScreen.GetComponent<CallUnityEventOnInputAction>().OnInputActionPerformed.Invoke();
            MenuSelection.Instance.MakeCamPriority(3);
        }
        MenuSelection.Instance.startScreen.SetActive(false);
        MenuSelection.Instance.mainMenu.SetActive(false);
        MenuSelection.Instance.localOnline.SetActive(false);
        MenuSelection.Instance.onlineMatchmaking.SetActive(true);
        yield return new WaitForSeconds(.5f);
        GameLobby.instance.JoinWithCode(currentConnectString);
    }
}