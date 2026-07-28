using System;
using UnityEngine;
using EditorAttributes;
using Steamworks;
using Steamworks.Data;
using System.Collections.Generic;

public class SteamIntegration : MonoBehaviour
{
    public static SteamIntegration instance;

    [Header("Steam initialization")]
    [SerializeField] bool isFullVersion; 
    public bool IsFullVersion => isFullVersion;

    private bool statsLoaded = false;

    [SerializeField] private List<SO_Achievement> achievementSOs;

    private void Awake()
    {
        gameObject.transform.parent = null;

        if (instance == null)
            instance = this;
        else Destroy(gameObject);
    }

#if (UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_EDITOR) && !UNITY_SWITCH
    private void Start()
    {
        try
        {
            InitializeSteam();
            SetLocaleBasedOnSteamLanguage();
            DontDestroyOnLoad(this);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error initializing Steam: {e.Message}");
        }
    }

    private void Update()
    {
        if (SteamClient.IsValid)
            SteamClient.RunCallbacks();
    }

    private void OnApplicationQuit()
    {
        if (SteamClient.IsValid)
            SteamClient.Shutdown();
    }
    
    private void InitializeSteam()
    {
        try
        {
            if (SteamClient.IsValid)
            {
                bool loaded = SteamUserStats.RequestCurrentStats();
                
                if (loaded)
                {
                    statsLoaded = true;
                    SetLocaleBasedOnSteamLanguage();
                }
                return;
            }

            if(isFullVersion) 
                Steamworks.SteamClient.Init(3670670);
            else
                Steamworks.SteamClient.Init(3769210);
            
            bool success = SteamUserStats.RequestCurrentStats();
            if (success)
                statsLoaded = true;           
        }
        catch (Exception e)
        {
        }
    }
#endif

    #region Localization
    private void SetLocaleBasedOnSteamLanguage()
    {
#if (UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_EDITOR) && !UNITY_SWITCH

        try
        {
            Debug.Log("Steam Language: " + Steamworks.SteamApps.GameLanguage);
            
            if (LocaleSelector.Instance)
            {
                switch (SteamApps.GameLanguage)
                {
                    case "english":
                        LocaleSelector.Instance.ChangeLocale(0);
                        Debug.Log("Locale set to english");
                        break;
                    case "french":
                        LocaleSelector.Instance.ChangeLocale(1);
                        Debug.Log("Locale set to french");
                        break;
                    case "german":
                        LocaleSelector.Instance.ChangeLocale(2);
                        Debug.Log("Locale set to german");
                        break;
                    case "italian":
                        LocaleSelector.Instance.ChangeLocale(3);
                        Debug.Log("Locale set to italian");
                        break;
                    case "polish":
                        LocaleSelector.Instance.ChangeLocale(4);
                        Debug.Log("Locale set to polish");
                        break;
                    case "brazilian":
                        LocaleSelector.Instance.ChangeLocale(5);
                        Debug.Log("Locale set to portuguese (brazil)");
                        break;
                    case "spanish":
                        LocaleSelector.Instance.ChangeLocale(6);
                        Debug.Log("Locale set to spanish");
                        break;
                    case "turkish":
                        LocaleSelector.Instance.ChangeLocale(7);
                        Debug.Log("Locale set to turkish");
                        break;
                    default:
                        LocaleSelector.Instance.ChangeLocale(0);
                        Debug.Log("Locale set to english");
                        break;
                }
            }
        }
        catch
        {
            //Debug.LogError("No steam locale settable");
        }
#endif
    }
#endregion
    
    #region Achievements
    [Button]
    public void UnlockAllAchievements()
    {
#if (UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_EDITOR) && !UNITY_SWITCH

        try
        {
            foreach (SO_Achievement achievementSO in achievementSOs)
            {
                Steamworks.Data.Achievement ach = new Steamworks.Data.Achievement(achievementSO.AchievementName);
                ach.Trigger();
            }
            Debug.Log("All achievements unlocked");
        }
        catch (System.Exception e)
        {
            Debug.Log(e);
        }
#endif
    }

#if (UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_EDITOR) && !UNITY_SWITCH

    [Button]
    public void ClearAllAchievements()
    {
        try
        {
            foreach (SO_Achievement achievementSO in achievementSOs)
            {
                Steamworks.Data.Achievement ach = new Steamworks.Data.Achievement(achievementSO.AchievementName);
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

            foreach (SO_Achievement achievementSO in achievementSOs)
                SteamUserStats.SetStat(achievementSO.StatName, 0);
    
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
#endif
    [Button]
    public void UnlockAchievement(int achievementIndex)
    {
#if (UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_EDITOR) && !UNITY_SWITCH

        if (!SteamClient.IsValid) return;

        Achievement ach = new Steamworks.Data.Achievement(achievementSOs[achievementIndex].AchievementName);
        Debug.Log("Achievement Unlocked");
        ach.Trigger();
#endif
    }

#if (UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_EDITOR) && !UNITY_SWITCH

    [Button]
    public void ClearAchievement(int achievementIndex)
    {
        try
        {
            Steamworks.Data.Achievement ach = new Steamworks.Data.Achievement(achievementSOs[achievementIndex].AchievementName);
            ach.Clear();
            Debug.Log($"Achievement {achievementSOs[achievementIndex].AchievementName} cleared");
        }
        catch (System.Exception e)
        {
            Debug.Log(e);
        }
    }
#endif

    [Button]
    public void IncrementIntSteamStat(int achievementIndex, int incrementAmount)
    {
#if (UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_EDITOR) && !UNITY_SWITCH

        if (!isFullVersion) return;  
        
        try
        {
            if (!statsLoaded)
            {
                Debug.LogWarning("Steam stats not loaded yet.");
                return;
            }
    
            int currentValue = SteamUserStats.GetStatInt(achievementSOs[achievementIndex].StatName);
            Debug.Log("Stat current value: " + currentValue);
    
            int newValue = currentValue + incrementAmount;
            Debug.Log("Stat new value: " + newValue);
    
            if (newValue >= achievementSOs[achievementIndex].StatThreshold)
            {
                Debug.Log("Value was higher then threshold, reduced it to threshold " + achievementSOs[achievementIndex].StatThreshold);
                newValue = achievementSOs[achievementIndex].StatThreshold;
            }
    
            SteamUserStats.SetStat(achievementSOs[achievementIndex].StatName, newValue);
    
            if (newValue >= achievementSOs[achievementIndex].StatThreshold)
                UnlockAchievement(achievementIndex);
    
            SteamUserStats.StoreStats();
        }
        catch (System.Exception e)
        {
            Debug.Log(e);
        }
#endif
    }
#endregion Achievements
    }