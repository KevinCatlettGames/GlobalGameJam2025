using System;
using UnityEngine;
using EditorAttributes;

#if (UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_EDITOR) && !UNITY_SWITCH
using Steamworks;
using Steamworks.Data;
#endif

public class SteamIntegration : MonoBehaviour
{
    public static SteamIntegration instance;

    // Event fired when Steam stats are successfully fetched
    public static event Action OnSteamStatsReady;

    [Header("Steam initialization")]
    [SerializeField] private bool isFullVersion;
    public bool IsFullVersion => isFullVersion;

    public bool statsLoaded = false;

    private void Awake()
    {
        transform.parent = null;

        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

#if (UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_EDITOR) && !UNITY_SWITCH
    private void Start()
    {
        try
        {
            InitializeSteam();
        }
        catch (Exception e)
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
                    OnSteamStatsLoaded();
                }
                return;
            }

            uint appId = isFullVersion ? 3670670u : 3769210u;
            SteamClient.Init(appId);

            // Listen to Facepunch's native callback when UserStats arrive
            SteamUserStats.OnAchievementProgress += OnAchievementProgressCallback;

            bool success = SteamUserStats.RequestCurrentStats();
            if (success)
            {
                OnSteamStatsLoaded();
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Steam Client Init failed: {e.Message}");
        }
    }

    private void OnSteamStatsLoaded()
    {
        statsLoaded = true;
        SetLocaleBasedOnSteamLanguage();

        // Notify any listeners (like AchievementSaveSystem)
        Debug.Log("Steam stats loaded successfully - Invoking OnSteamStatsReady");
        OnSteamStatsReady?.Invoke();
    }

    private void OnAchievementProgressCallback(Achievement ach, int progress, int max)
    {
        // Optional callback for stat progress triggers
    }
#endif

    #region Localization
    private void SetLocaleBasedOnSteamLanguage()
    {
#if (UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_EDITOR) && !UNITY_SWITCH
        try
        {
            if (LocaleSelector.Instance)
            {
                switch (SteamApps.GameLanguage)
                {
                    case "english": LocaleSelector.Instance.ChangeLocale(0); break;
                    case "french": LocaleSelector.Instance.ChangeLocale(1); break;
                    case "german": LocaleSelector.Instance.ChangeLocale(2); break;
                    case "italian": LocaleSelector.Instance.ChangeLocale(3); break;
                    case "polish": LocaleSelector.Instance.ChangeLocale(4); break;
                    case "brazilian": LocaleSelector.Instance.ChangeLocale(5); break;
                    case "spanish": LocaleSelector.Instance.ChangeLocale(6); break;
                    case "turkish": LocaleSelector.Instance.ChangeLocale(7); break;
                    default: LocaleSelector.Instance.ChangeLocale(0); break;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Failed setting locale from Steam: {e.Message}");
        }
#endif
    }
    #endregion

    #region Achievements
    public bool IsThisAchievementUnlocked(string achievementID)
    {
#if (UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_EDITOR) && !UNITY_SWITCH
        try
        {
            if (!SteamClient.IsValid) return false;

            var ach = new Steamworks.Data.Achievement(achievementID);
            return ach.State;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error checking achievement {achievementID}: {e.Message}");
            return false;
        }
#else
        return false;
#endif
    }

    public void UnlockAchievement(int achievementIndex)
    {
#if (UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_EDITOR) && !UNITY_SWITCH
        if (!SteamClient.IsValid) return;

        var list = AchievementSaveSystem.instance.AchievementList;
        if (achievementIndex < 0 || achievementIndex >= list.Count) return;

        string id = list[achievementIndex].AchievementName;
        var ach = new Steamworks.Data.Achievement(id);
        ach.Trigger();
        Debug.Log($"Steam Achievement Unlocked: {id}");
#endif
    }

    public void ClearAchievement(int achievementIndex)
    {
#if (UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_EDITOR) && !UNITY_SWITCH
        if (!SteamClient.IsValid) return;

        try
        {
            var list = AchievementSaveSystem.instance.AchievementList;
            if (achievementIndex < 0 || achievementIndex >= list.Count) return;

            string id = list[achievementIndex].AchievementName;
            var ach = new Steamworks.Data.Achievement(id);
            ach.Clear();
            Debug.Log($"Steam Achievement Cleared: {id}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed clearing achievement: {e.Message}");
        }
#endif
    }

    public void ResetAllStats()
    {
#if (UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_EDITOR) && !UNITY_SWITCH
        try
        {
            if (!statsLoaded) return;

            foreach (SO_Achievement achievementSO in AchievementSaveSystem.instance.AchievementList)
            {
                SteamUserStats.SetStat(achievementSO.StatName, 0);
            }

            SteamUserStats.StoreStats();
            Debug.Log("All Steam stats reset");
        }
        catch (Exception e)
        {
            Debug.LogError($"ResetAllStats failed: {e.Message}");
        }
#endif
    }

    public void IncrementIntSteamStat(int achievementIndex, int incrementAmount)
    {
#if (UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_EDITOR) && !UNITY_SWITCH
        if (!isFullVersion || !SteamClient.IsValid || !statsLoaded) return;

        try
        {
            SO_Achievement ach = AchievementSaveSystem.instance.AchievementList[achievementIndex];

            int currentValue = SteamUserStats.GetStatInt(ach.StatName);
            int newValue = currentValue + incrementAmount;

            if (newValue >= ach.StatThreshold)
                newValue = ach.StatThreshold;

            SteamUserStats.SetStat(ach.StatName, newValue);

            if (newValue >= ach.StatThreshold)
                UnlockAchievement(achievementIndex);

            SteamUserStats.StoreStats();
        }
        catch (Exception e)
        {
            Debug.LogError($"IncrementIntSteamStat failed: {e.Message}");
        }
#endif
    }

    public int GetSteamStatInt(string statName)
    {
#if (UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_EDITOR) && !UNITY_SWITCH
        if (!SteamClient.IsValid || !statsLoaded) return 0;
        try
        {
            return SteamUserStats.GetStatInt(statName);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed reading stat {statName}: {e.Message}");
            return 0;
        }
#else
    return 0;
#endif
    }

    #endregion
}