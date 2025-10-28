using System;
using UnityEngine;
using EditorAttributes;
using Steamworks;
using Unity.VisualScripting;

public class SteamIntegration : MonoBehaviour
{
    public static SteamIntegration instance;
    
    [Header("Steam initialization")]
    [SerializeField] bool isFullVersion = true;
    private bool statsLoaded = false;
    private float retryInterval = 5f;
    private float retryTimer = 0f;
    private bool steamInitialized = false; 
    
    [Header("Achievements")]
    [SerializeField] string[] achievementNames;
    [SerializeField] string[] statNames;
    
    [Header("Matchmaking")]
    public Friend steamFriendToJoin;
    [ReadOnly]
    public string lobbyIDToJoin;
    
    #region Unity Life Cycle
    private void Awake()
    {
        if (instance == null)
            instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        try
        {
            InitializeSteam();
            DontDestroyOnLoad(this);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error initializing Steam: {e.Message}");
        }
    }


    private void Update()
    {
        if (steamInitialized)
            SteamClient.RunCallbacks();
        else
        {
            retryTimer -= Time.deltaTime;
            if (retryTimer <= 0f)
            {
                InitializeSteam();
                retryTimer = retryInterval;
            }
        }
    }

    private void OnApplicationQuit()
    {
        if (SteamClient.IsValid)
            SteamClient.Shutdown();
    }

    private void OnEnable()
    {
        SteamFriends.OnGameRichPresenceJoinRequested += RichPresenceJoinRequested;
    }

    private void OnDisable()
    {
        SteamFriends.OnGameRichPresenceJoinRequested -= RichPresenceJoinRequested;
    }

    #endregion
    
    private void InitializeSteam()
    {
        try
        {
            if (SteamClient.IsValid)
            {
                bool loaded = SteamUserStats.RequestCurrentStats();
                steamInitialized = true;
                
                if (loaded)
                {
                    statsLoaded = true;
                    //Debug.Log("Steam stats loaded.");
                }
                else
                   // Debug.LogWarning("Steam initialized, but failed to load stats.");

                return;
            }

            if(isFullVersion) 
                Steamworks.SteamClient.Init(3670670);
            else
                Steamworks.SteamClient.Init(3769210);
            
            bool success = SteamUserStats.RequestCurrentStats();
            if (success)
            {
                statsLoaded = true;
                //Debug.Log("Steam stats loaded.");
            }
            else
               // Debug.LogWarning("Steam initialized, but failed to load stats.");

            steamInitialized = true;
        }
        catch (Exception e)
        {
            steamInitialized = false;
            //Debug.LogWarning($"Steam initialization failed: {e.Message}");
        }
    }
    
    #region Matchmaking
    private void RichPresenceJoinRequested(Friend steamFriend, string lobbyID)
    {
        //Debug.Log("Trying to join a lobby through steam friend list stuff...");
        steamFriendToJoin = steamFriend;
        lobbyIDToJoin = lobbyID;
        if (MainMenuLobbyCreator.Instance != null)
            MainMenuLobbyCreator.Instance.OpenLobby();
    }
    #endregion 
    
    #region Localization
    // private void SetLocaleBasedOnSteamLanguage()
    // {
    //     try
    //     {
    //         Debug.Log("Steam Language: " + Steamworks.SteamApps.GameLanguage);
    //         
    //         if (LocaleSelector.Instance)
    //         {
    //             switch (SteamApps.GameLanguage)
    //             {
    //                 case "german":
    //                     LocaleSelector.Instance.ChangeLocale(1);
    //                     Debug.Log("Locale set to german");
    //                     break;
    //                 case "chinese":
    //                     LocaleSelector.Instance.ChangeLocale(2);
    //                     Debug.Log("Locale set to chinese");
    //                     break;
    //                 case "japanese":
    //                     LocaleSelector.Instance.ChangeLocale(3);
    //                     Debug.Log("Locale set to japanese");
    //                     break;
    //                 case "portuguese":
    //                     LocaleSelector.Instance.ChangeLocale(4);
    //                     Debug.Log("Locale set to portuguese");
    //                     break;
    //                 case "russian":
    //                     LocaleSelector.Instance.ChangeLocale(5);
    //                     Debug.Log("Locale set to russian");
    //                     break;
    //                 case "spanish":
    //                     LocaleSelector.Instance.ChangeLocale(6);
    //                     Debug.Log("Locale set to spanish");
    //                     break;
    //                 default:
    //                     LocaleSelector.Instance.ChangeLocale(0);
    //                     Debug.Log("Locale set to english");
    //                     break;
    //             }
    //         }
    //     }
    //     catch
    //     {
    //         Debug.LogError("No steam locale settable");
    //     }
    // }
     #endregion
    
    #region Achievements
    [Button]
    public void UnlockAllAchievements()
    {
        try
        {
            foreach (string id in achievementNames)
            {
                Steamworks.Data.Achievement ach = new Steamworks.Data.Achievement(id);
                ach.Trigger();
            }
            Debug.Log("All achievements unlocked");
        }
        catch (System.Exception e)
        {
            Debug.Log(e);
        }
    }
    
    [Button]
    public void ClearAllAchievements()
    {
        try
        {
            foreach (string id in achievementNames)
            {
                Steamworks.Data.Achievement ach = new Steamworks.Data.Achievement(id);
                ach.Clear();
            }
            Debug.Log("All achievements cleared");
        }
        catch (System.Exception e)
        {
            Debug.Log(e);
        }
    }
    
    [Button]
    public void ResetAllStats()
    {
        try
        {
            if (!statsLoaded)
            {
                Debug.LogWarning("Steam stats not loaded yet.");
                return;
            }
    
            foreach (string id in statNames)
                SteamUserStats.SetStat(id, 0);
    
            SteamUserStats.StoreStats();
            Debug.Log("All stats reset");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"ResetAllStats failed: {e}");
        }
    }
    
    
    [Button]
    public void IsThisAchievementUnlocked(int id)
    {
        try
        {
            Steamworks.Data.Achievement ach = new Steamworks.Data.Achievement(id.ToString());
            Debug.Log($"Achievement {id} status: " + ach.State);
        }
        catch (System.Exception e)
        {
            Debug.Log(e);
        }
    }
    
    [Button]
    public void UnlockAchievement(int achievementNameID)
    {
        if (!steamInitialized) return; 
        
        for (int i = 0; i < achievementNames.Length; i++)
        {
            if (i == achievementNameID)
            {
                var ach = new Steamworks.Data.Achievement(achievementNames[i]);
                ach.Trigger();
            }
        }
    }
    
    [Button]
    public void ClearAchievement(int id)
    {
        try
        {
            Steamworks.Data.Achievement ach = new Steamworks.Data.Achievement(id.ToString());
            ach.Clear();
            Debug.Log($"Achievement {id} cleared");
        }
        catch (System.Exception e)
        {
            Debug.Log(e);
        }
    }
    
    [Button]
    public void IncrementIntSteamStat(int statNameID, int incrementAmount, int achievementThreshold, int achievementNameID)
    {
        if (!isFullVersion) return;  
        
        try
        {
            if (!statsLoaded)
            {
                //Debug.LogWarning("Steam stats not loaded yet.");
                return;
            }
    
            int currentValue = SteamUserStats.GetStatInt(statNames[statNameID]);
            //Debug.Log("Stat current value: " + currentValue);
    
            int newValue = currentValue + incrementAmount;
            //Debug.Log("Stat new value: " + newValue);
    
            if (newValue >= achievementThreshold)
            {
                //Debug.Log("Value was higher then threshold, reduced it to threshold " + achievementThreshold);
                newValue = achievementThreshold;
            }
    
            SteamUserStats.SetStat(statNames[statNameID], newValue);
    
            if (newValue >= achievementThreshold)
                UnlockAchievement(achievementNameID);
    
            SteamUserStats.StoreStats();
        }
        catch (System.Exception e)
        {
            Debug.Log(e);
        }
    }
    #endregion Achievements
}