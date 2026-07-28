using UnityEngine;
using System.Collections.Generic;
using EditorAttributes;

public class AchievementSaveSystem : MonoBehaviour
{
    public static AchievementSaveSystem instance;

    [SerializeField] private List<SO_Achievement> achievementList;
    public List<SO_Achievement> AchievementList => achievementList;

    private const string ACHIEV_SAVE_PREFIX = "Ach_";
    private const string STAT_SAVE_PREFIX = "AchStat_";

    private void Awake()
    {
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
        Debug.Log("Syncing local stats and achievements with Steam...");
        SyncAchievementsFromPlatform();
    }

    /// <summary>
    /// Syncs Steam stats & unlocks down into local PlayerPrefs.
    /// </summary>
    public void SyncAchievementsFromPlatform()
    {
#if (UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_EDITOR) && !UNITY_SWITCH
        if (SteamIntegration.instance == null || !SteamIntegration.instance.statsLoaded) return;

        bool updatedAny = false;

        for (int i = 0; i < achievementList.Count; i++)
        {
            SO_Achievement ach = achievementList[i];

            // 1. Sync Achievement Unlock State
            bool isUnlockedInSteam = SteamIntegration.instance.IsThisAchievementUnlocked(ach.AchievementName);
            if (isUnlockedInSteam)
            {
                SetLocalAchievementState(ach.AchievementName, true);
                updatedAny = true;
            }

            // 2. Sync Progress Stat Value (if achievement uses stats)
            if (!string.IsNullOrEmpty(ach.StatName))
            {
                int steamStatVal = SteamIntegration.instance.GetSteamStatInt(ach.StatName);
                int localStatVal = GetStatInt(ach.StatName);

                // Steam is authority; take the higher value
                if (steamStatVal > localStatVal)
                {
                    SetStatInt(ach.StatName, steamStatVal);
                    updatedAny = true;
                }
            }
        }

        if (updatedAny)
        {
            PlayerPrefs.Save();
        }
#endif
    }

    #region Achievement Unlocks

    /// <summary>
    /// Unlocks an achievement both locally (Switch/PC) and on Steam.
    /// </summary>
    [Button]
    public void UnlockAchievement(int index)
    {
        if (index < 0 || index >= achievementList.Count) return;

        SO_Achievement ach = achievementList[index];

        // 1. Local Unlock (Switch & PC)
        SetLocalAchievementState(ach.AchievementName, true);
        PlayerPrefs.Save();

        // 2. Steam Unlock
#if (UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_EDITOR) && !UNITY_SWITCH
        if (SteamIntegration.instance != null)
        {
            SteamIntegration.instance.UnlockAchievement(index);
        }
#endif

        Debug.Log($"Achievement Unlocked: {ach.AchievementName}");
    }

    [Button]
    public bool IsAchievementUnlocked(int index)
    {
        if (index < 0 || index >= achievementList.Count) return false;

        string key = ACHIEV_SAVE_PREFIX + achievementList[index].AchievementName;
        Debug.Log("Achievement is: " + PlayerPrefs.GetInt(key));
        return PlayerPrefs.GetInt(key, 0) == 1;
    }

    private void SetLocalAchievementState(string achievementID, bool unlocked)
    {
        string key = ACHIEV_SAVE_PREFIX + achievementID;
        PlayerPrefs.SetInt(key, unlocked ? 1 : 0);
    }

    #endregion

    #region Stat Tracking (Incremental Achievements)

    /// <summary>
    /// Call this from gameplay to add progress toward an achievement stat.
    /// Works seamlessly on Nintendo Switch, PC offline, and Steam.
    /// </summary>
    [Button]
    public void IncrementStat(int achievementIndex, int amount = 1)
    {
        if (achievementIndex < 0 || achievementIndex >= achievementList.Count) return;

        SO_Achievement ach = achievementList[achievementIndex];
        if (string.IsNullOrEmpty(ach.StatName)) return;

        // 1. Update Local PlayerPrefs Stat
        int currentVal = GetStatInt(ach.StatName);
        int newVal = Mathf.Min(currentVal + amount, ach.StatThreshold);
        SetStatInt(ach.StatName, newVal);
        PlayerPrefs.Save();

        // 2. Update Steam Stat
#if (UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_EDITOR) && !UNITY_SWITCH
        if (SteamIntegration.instance != null)
        {
            SteamIntegration.instance.IncrementIntSteamStat(achievementIndex, amount);
        }
#endif

        // 3. Local Unlock Check (Triggers unlock for Nintendo Switch / Offline play)
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
        Debug.Log("All achievements and stats cleared.");
    }

    #endregion
}