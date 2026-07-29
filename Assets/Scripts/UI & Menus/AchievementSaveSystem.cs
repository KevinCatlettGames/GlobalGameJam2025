using EditorAttributes;
using System.Collections.Generic;
using UnityEngine;

public class AchievementSaveSystem : MonoBehaviour
{
    public static AchievementSaveSystem instance;

    [SerializeField] private List<SO_Achievement> achievementList;
    public List<SO_Achievement> AchievementList => achievementList;

    private const string ACHIEV_SAVE_PREFIX = "Ach_";
    private const string STAT_SAVE_PREFIX = "AchStat_";

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

    private void OnEnable()
    {
#if (UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_EDITOR) && !UNITY_SWITCH
        SteamIntegration.OnSteamStatsReady += HandleSteamStatsReady;

        if (SteamIntegration.instance != null && SteamIntegration.instance.statsLoaded)
        {
            HandleSteamStatsReady();
        }
#endif
    }

    private void OnDisable()
    {
#if (UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_EDITOR) && !UNITY_SWITCH
        SteamIntegration.OnSteamStatsReady -= HandleSteamStatsReady;
#endif
    }

    private void HandleSteamStatsReady()
    {
        //Debug.Log("Syncing local stats and achievements with Steam...");
        SyncAchievementsFromPlatform();
    }

    public void SyncAchievementsFromPlatform()
    {
#if (UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_EDITOR) && !UNITY_SWITCH
        if (SteamIntegration.instance == null || !SteamIntegration.instance.statsLoaded) return;

        bool localUpdated = false;
        bool steamUpdated = false;

        for (int i = 0; i < achievementList.Count; i++)
        {
            SO_Achievement ach = achievementList[i];

            bool isUnlockedInSteam = SteamIntegration.instance.IsThisAchievementUnlocked(ach.AchievementName);
            bool isUnlockedLocally = IsAchievementUnlocked(i);

            if (isUnlockedInSteam && !isUnlockedLocally)
            {
                SetLocalAchievementState(ach.AchievementName, true);
                localUpdated = true;
            }
            else if (isUnlockedLocally && !isUnlockedInSteam)
            {
                SteamIntegration.instance.UnlockAchievement(i);
                steamUpdated = true;
            }

            if (!string.IsNullOrEmpty(ach.StatName))
            {
                int steamStatVal = SteamIntegration.instance.GetSteamStatInt(ach.StatName);
                int localStatVal = GetStatInt(ach.StatName);

                if (steamStatVal > localStatVal)
                {
                    SetStatInt(ach.StatName, steamStatVal);
                    localUpdated = true;
                }
                else if (localStatVal > steamStatVal)
                {
                    SteamIntegration.instance.SetSteamStatInt(ach.StatName, localStatVal);
                    steamUpdated = true;
                }
            }
        }

        if (localUpdated)
        {
            PlayerPrefs.Save();
        }

        if (steamUpdated)
        {
            Steamworks.SteamUserStats.StoreStats();
            //Debug.Log("Offline progress successfully pushed to Steam!");
        }
#endif
    }

    #region Achievement Unlocks
    [Button]
    public void UnlockAchievement(int index)
    {
        if (index < 0 || index >= achievementList.Count) return;

        SO_Achievement ach = achievementList[index];

        SetLocalAchievementState(ach.AchievementName, true);
        PlayerPrefs.Save();

#if (UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_EDITOR) && !UNITY_SWITCH
        if (SteamIntegration.instance != null)
        {
            SteamIntegration.instance.UnlockAchievement(index);
        }
#endif

        //Debug.Log($"Achievement Unlocked: {ach.AchievementName}");
    }

    [Button]
    public bool IsAchievementUnlocked(int index)
    {
        if (index < 0 || index >= achievementList.Count) return false;

        string key = ACHIEV_SAVE_PREFIX + achievementList[index].AchievementName;
        //Debug.Log("Achievement is: " + PlayerPrefs.GetInt(key));
        return PlayerPrefs.GetInt(key, 0) == 1;
    }

    private void SetLocalAchievementState(string achievementID, bool unlocked)
    {
        string key = ACHIEV_SAVE_PREFIX + achievementID;
        PlayerPrefs.SetInt(key, unlocked ? 1 : 0);
    }

    [Button]
    public void ClearAchievement(int index)
    {
        if (index < 0 || index >= achievementList.Count) return;

        SO_Achievement ach = achievementList[index];

        SetLocalAchievementState(ach.AchievementName, false);
        PlayerPrefs.Save();

#if (UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_EDITOR) && !UNITY_SWITCH
        if (SteamIntegration.instance != null)
        {
            SteamIntegration.instance.ClearAchievement(index);
        }
#endif

        //Debug.Log($"Achievement Cleared: {ach.AchievementName}");
    }

    #endregion

    #region Stat Tracking (Incremental Achievements)

    [Button]
    public void IncrementStat(int achievementIndex, int amount = 1)
    {
        if (achievementIndex < 0 || achievementIndex >= achievementList.Count) return;

        SO_Achievement ach = achievementList[achievementIndex];
        if (string.IsNullOrEmpty(ach.StatName)) return;

        int currentVal = GetStatInt(ach.StatName);
        int newVal = Mathf.Min(currentVal + amount, ach.StatThreshold);
        SetStatInt(ach.StatName, newVal);
        PlayerPrefs.Save();

#if (UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_EDITOR) && !UNITY_SWITCH
        if (SteamIntegration.instance != null)
        {
            SteamIntegration.instance.SetSteamStatIntAndIndicate(ach.StatName, ach.AchievementName, newVal, ach.StatThreshold);
        }
#endif

        if (newVal >= ach.StatThreshold && !IsAchievementUnlocked(achievementIndex))
        {
            UnlockAchievement(achievementIndex);
        }
    }

    public int GetStatInt(string statName)
    {
        string key = STAT_SAVE_PREFIX + statName;
        return PlayerPrefs.GetInt(key, 0);
    }

    private void SetStatInt(string statName, int value)
    {
        string key = STAT_SAVE_PREFIX + statName;
        PlayerPrefs.SetInt(key, value);
    }

    #endregion

    #region Editor Debug Buttons

    [Button]
    public void UnlockAllAchievements()
    {
        for (int i = 0; i < achievementList.Count; i++)
        {
            UnlockAchievement(i);
        }
    }

    [Button]
    public void ClearAllAchievements()
    {
        for (int i = 0; i < achievementList.Count; i++)
        {
            SO_Achievement ach = achievementList[i];
            SetLocalAchievementState(ach.AchievementName, false);

            if (!string.IsNullOrEmpty(ach.StatName))
            {
                SetStatInt(ach.StatName, 0);
            }

#if (UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_EDITOR) && !UNITY_SWITCH
            if (SteamIntegration.instance != null)
            {
                SteamIntegration.instance.ClearAchievement(i);
            }
#endif
        }
        PlayerPrefs.Save();
        //Debug.Log("All achievements and stats cleared.");
    }
    #endregion
}